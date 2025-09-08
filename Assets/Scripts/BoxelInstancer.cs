using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class BoxelInstancer : MonoBehaviour
{
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int batchSize = 1023; // límite de Unity por llamada
    public float yOffset = 0f;

    //Habilita extrusión vertical por celda"
    public bool extrude = true;

    //Altura máxima (en unidades) a la que puede extruirse una celda
    public int maxExtrudeHeight = 6;

    //Habilita reflejar la geometría hacia arriba (crear 'techo' invertido)
    public bool mirrorCeiling = true;

    //Distancia vertical entre la base (yOffset) y la base del techo
    public float caveHeight = 12f;

    //Número de capas sólidas bajo la base (0 = desactivar)
    public int baseThickness = 2;
    //Número de capas sólidas encima del techo (0 = desactivar)
    public int ceilingThickness = 2;

    //Número de capas exteriores (anillo) a eliminar antes de procesar (0 = desactivar)
    public int outerShell = 1;

    // Batches preparados para dibujar cada frame
    private List<Matrix4x4[]> batches = new List<Matrix4x4[]>();

    void Awake()
    {
        if (instanceMaterial != null && !instanceMaterial.enableInstancing)
        {
            instanceMaterial.enableInstancing = true;
        }
    }

    // Llamar desde tu código (p.e. CellularAutomata) después de calcular dataList
    public void UpdateBatchesFromDataList(List<BoxelData> items, int width, int height, bool parallel = false)
    {
        if (items == null)
        {
            batches.Clear();
            return;
        }

        // Build a fast grid lookup (type as byte) to compute neighbor counts for extrusion
        byte[,] grid = new byte[width, height];
        foreach (var d in items)
        {
            if (d.x >= 0 && d.x < width && d.y >= 0 && d.y < height)
                grid[d.x, d.y] = (byte)(d.type != 0 ? 1 : 0);
        }

        // --- Recortar capa(s) exterior en la rejilla antes de cualquier procesamiento ---
        if (outerShell > 0)
        {
            int trim = Math.Max(0, outerShell);
            for (int t = 0; t < trim; t++)
            {
                // t-th layer: remove cells with index == t or == width-1-t / height-1-t
                int minX = t;
                int maxX = width - 1 - t;
                int minY = t;
                int maxY = height - 1 - t;

                // top / bottom rows
                for (int x = minX; x <= maxX; x++)
                {
                    if (minY <= maxY) grid[x, minY] = 0;
                    if (minY != maxY) grid[x, maxY] = 0;
                }
                // left / right columns (exclude corners already set)
                for (int y = minY + 1; y <= maxY - 1; y++)
                {
                    if (minX <= maxX) grid[minX, y] = 0;
                    if (minX != maxX) grid[maxX, y] = 0;
                }
            }
        }

        // Extraer sólo celdas visibles (type != 0) pero usando la rejilla ya recortada
        var visibles = items.Where(d => d.x >= 0 && d.x < width && d.y >= 0 && d.y < height && grid[d.x, d.y] == 1).ToArray();
        int visibleCount = visibles.Length;

        // Si no hay nada visible y no hay capas base/ceiling, limpia y retorna
        if (visibleCount == 0 && baseThickness == 0 && ceilingThickness == 0)
        {
            batches.Clear();
            return;
        }

        // Usamos una lista dinámica para acumular todas las matrices (suelo, techo espejo, base y techo superior)
        var mats = new List<Matrix4x4>(visibleCount * (mirrorCeiling ? 2 : 1) + width * height * (baseThickness + ceilingThickness));

        // Construir floor + mirrored ceiling (por cada celda visible)
        Action<int> buildAt = i =>
        {
            var d = visibles[i];
            float heightScale = 1f;
            float yPosFloor = yOffset + 0.5f; // centro por defecto

            if (extrude)
            {
                int neighbors = CountAliveNeighborsGrid(grid, d.x, d.y, width, height);
                // Mapear neighbors (0..8) a altura (1..maxExtrudeHeight)
                int h = 1 + Mathf.RoundToInt((neighbors / 8f) * (Mathf.Max(1, maxExtrudeHeight) - 1));
                heightScale = h;
                yPosFloor = yOffset + (h / 2f);
            }

            // Matriz de la instancia del suelo
            lock (mats) { mats.Add(Matrix4x4.TRS(new Vector3(d.x, yPosFloor, d.y), Quaternion.identity, new Vector3(1f, heightScale, 1f))); }

            if (mirrorCeiling)
            {
                // Posición y de la base del techo
                float ceilingBaseY = yOffset + caveHeight;
                // Matriz de la instancia espejo (colgada desde ceilingBaseY)
                float yPosCeil = ceilingBaseY - (heightScale / 2f);
                // Rotación para "darla vuelta" (útil si la malla tiene orientación)
                Quaternion rot = Quaternion.Euler(180f, 0f, 0f);
                lock (mats) { mats.Add(Matrix4x4.TRS(new Vector3(d.x, yPosCeil, d.y), rot, new Vector3(1f, heightScale, 1f))); }
            }
        };

        if (!parallel)
        {
            for (int i = 0; i < visibleCount; i++) buildAt(i);
        }
        else
        {
            Parallel.For(0, visibleCount, i => buildAt(i));
        }

        // Ahora añadir capa base sólida (si baseThickness>0) - cubre toda la rejilla
        if (baseThickness > 0)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    for (int layer = 0; layer < baseThickness; layer++)
                    {
                        // capa 0 queda inmediatamente bajo yOffset: y = yOffset - 0.5
                        float wy = yOffset - (layer + 0.5f);
                        mats.Add(Matrix4x4.TRS(new Vector3(x, wy, z), Quaternion.identity, Vector3.one));
                    }
                }
            }
        }

        // Añadir capa techo sólida por encima de caveHeight (si ceilingThickness>0)
        if (ceilingThickness > 0)
        {
            float ceilingBaseY = yOffset + caveHeight;
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    for (int layer = 0; layer < ceilingThickness; layer++)
                    {
                        // primera capa encima del techo: ceilingBaseY + 0.5
                        float wy = ceilingBaseY + (layer + 0.5f);
                        mats.Add(Matrix4x4.TRS(new Vector3(x, wy, z), Quaternion.identity, Vector3.one));
                    }
                }
            }
        }

        // Convertir lista a array y dividir en batches
        int totalInstances = mats.Count;
        if (totalInstances == 0)
        {
            batches.Clear();
            return;
        }

        Matrix4x4[] matrices = mats.ToArray();

        // Dividir en batches de <= batchSize
        batches.Clear();
        int offset = 0;
        while (offset < totalInstances)
        {
            int take = Math.Min(batchSize, totalInstances - offset);
            Matrix4x4[] batch = new Matrix4x4[take];
            Array.Copy(matrices, offset, batch, 0, take);
            batches.Add(batch);
            offset += take;
        }
    }

    // Conteo simple 8-vecinos en la cuadrícula
    private int CountAliveNeighborsGrid(byte[,] grid, int x, int y, int width, int height)
    {
        int alive = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    alive++; // fuera = pared (puede aumentar extrusión en bordes)
                }
                else
                {
                    if (grid[nx, ny] == 1) alive++;
                }
            }
        }
        return alive;
    }

    // Cada frame dibuja las batches. Si la escena es estática puedes llamar Draw una sola vez por frame.
    void Update()
    {
        if (instanceMesh == null || instanceMaterial == null || batches.Count == 0) return;

        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(instanceMesh, 0, instanceMaterial, batch);
        }
    }

    // Si necesitas limpiar visuales
    public void Clear()
    {
        batches.Clear();
    }
}