using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

public class CellularAutomata : MonoBehaviour
{
    private BoxelDataList dataList;
    private List<GameObject> currentBoxels;
    [SerializeField] private NoiseGen dataObject;

    // Referencia al instancer (asignar en el inspector)
    [SerializeField] public BoxelInstancer instancer;

    // Nueva: origen Y para la generación / dibujo del autómata
    [SerializeField] public float yOrigin = 0f;

    public int iterations = 1;

    private string path => Application.dataPath + "/boxelgrid.bin";

    // Mantengo la firma para compatibilidad: ahora delega a la versión asíncrona
    public void ExecuteAutomata()
    {
        // Llamada no bloqueante
        ExecuteAutomataAsync();
    }

    // Versión asíncrona: cargas, calculas en background y actualizas en el hilo principal
    public async void ExecuteAutomataAsync()
    {
        // LoadGrid poblara dataList (síncrono, rápido si usas binario)
        LoadGrid();

        if (instancer != null)
        {
            instancer.Clear();
        }

        if (dataList == null || dataList.items == null || dataList.items.Count == 0)
        {
            Debug.LogWarning("No hay datos para ejecutar el autómata.");
            return;
        }

        int width = GetMaxX() + 1;
        int height = GetMaxY() + 1;

        // Construir grid simple para cálculo (operaciones puramente gestionadas)
        byte[,] grid = BuildGridFromDataList(width, height);

        // Ejecutar autómata en un hilo de trabajo
        byte[,] result = await Task.Run(() => RunAutomataOnGrid(grid, width, height, iterations));

        // Reconstruir dataList desde resultado (en hilo principal)
        dataList.items = new List<BoxelData>(width * height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dataList.items.Add(new BoxelData { x = x, y = y, type = result[x, y] });
            }
        }

        // Actualizar visuales en hilo principal.
        // IMPORTANTE: no ejecutar APIs de Unity desde Task.Run. Aquí estamos en main thread.
        if (instancer != null)
        {
            // Aplicar origen Y antes de construir batches
            instancer.yOffset = yOrigin;

            // No usar parallel:true aquí — construir matrices usa Matrix4x4/Vector3 (Unity types)
            instancer.UpdateBatchesFromDataList(dataList.items, width, height, parallel: false);
        }
        else
        {
            RebuildScene(); // fallback que usa Instantiate/Destroy
        }

        // Guardado en disco asíncrono para no bloquear el frame
        await Task.Run(() =>
        {
            try
            {
                var ordered = dataList.items.OrderBy(d => d.x).ThenBy(d => d.y).Select(d => d.type).ToList();
                BoxelBinary.WriteBitPacked(path, width, height, ordered);
                Debug.Log($"Automata guardado: {dataList.items.Count} celdas en {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error guardando BIN (async): {e.Message}");
            }
        });
    }

    private void LoadGrid()
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"No se encontró el archivo binario en: {path}");
            return;
        }

        int width, height;
        List<int> types = BoxelBinary.ReadBitPacked(path, out width, out height);

        dataList = new BoxelDataList();
        dataList.items = new List<BoxelData>(width * height);

        int idx = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = new BoxelData
                {
                    x = x,
                    y = y,
                    type = types[idx++]
                };
                dataList.items.Add(cell);
            }
        }

        Debug.Log($"Cargadas {dataList.items.Count} celdas desde BIN ({width}x{height}).");
    }

    // Construye una matriz de bytes desde dataList (uso en cálculos)
    private byte[,] BuildGridFromDataList(int width, int height)
    {
        byte[,] grid = new byte[width, height];
        foreach (var c in dataList.items)
        {
            if (c.x >= 0 && c.x < width && c.y >= 0 && c.y < height)
                grid[c.x, c.y] = (byte)c.type;
        }
        return grid;
    }

    // Algoritmo del autómata sobre matriz (puede ejecutarse en Task.Run)
    private byte[,] RunAutomataOnGrid(byte[,] grid, int width, int height, int iters)
    {
        byte[,] cur = grid;
        byte[,] next = new byte[width, height];

        for (int iter = 0; iter < iters; iter++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighbors = CountAliveNeighborsFromGrid(cur, x, y, width, height);
                    next[x, y] = (neighbors > 4) ? (byte)1 : (byte)0;
                }
            }
            // swap referencias (evita copiar)
            (next, cur) = (cur, next);
        }

        return cur;
    }

    // Conteo de vecinos puramente sobre la matriz (sin llamadas a Unity APIs)
    private int CountAliveNeighborsFromGrid(byte[,] grid, int x, int y, int width, int height)
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
                    alive++; // fuera de límites = pared
                }
                else
                {
                    if (grid[nx, ny] == 1) alive++;
                }
            }
        }
        return alive;
    }

    private void RebuildScene()
    {
        // Destruir los GameObjects anteriores (si existen) para evitar duplicados
        if (currentBoxels != null)
        {
            foreach (GameObject boxel in currentBoxels)
            {
                if (boxel != null) Destroy(boxel);
            }
            currentBoxels.Clear();
        }
        else
        {
            currentBoxels = new List<GameObject>();
        }

        // Calcular dimensiones actuales
        int width = GetMaxX() + 1;
        int height = GetMaxY() + 1;

        // Si hay un BoxelInstancer asignado, usarlo (no crear GameObjects)
        if (instancer != null)
        {
            // Aplicar origen Y antes de construir batches
            instancer.yOffset = yOrigin;

            // Construir batches en el hilo principal (safe): no use parallel:true aquí si se llama desde el main thread
            // Si quieres paralelizar, prepara posiciones en background y transforma a Matrix4x4 en main thread.
            instancer.UpdateBatchesFromDataList(dataList.items, width, height, parallel: false);
            return;
        }
    }
    private int GetMaxX()
    {
        int max = 0;
        foreach (var cell in dataList.items)
        {
            if (cell.x > max) max = cell.x;
        }
        return max;
    }

    private int GetMaxY()
    {
        int max = 0;
        foreach (var cell in dataList.items)
        {
            if (cell.y > max) max = cell.y;
        }
        return max;
    }

    private void SaveGrid()
    {
        // Convencional síncrono (ya que SaveGrid async está en ExecuteAutomataAsync)
        int width = GetMaxX() + 1;
        int height = GetMaxY() + 1;

        var ordered = dataList.items.OrderBy(d => d.x).ThenBy(d => d.y).Select(d => d.type).ToList();

        try
        {
            BoxelBinary.WriteBitPacked(path, width, height, ordered);
            Debug.Log($"Automata guardado: {dataList.items.Count} celdas en {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error guardando BIN: {e.Message}");
        }
    }
}