using System.Collections.Generic;
using UnityEngine;

public class FillDirt : MonoBehaviour
{
    public GridGen gridGen;
    public GameObject dirtPrefab;
    public int fillDepth = 5;
    public float SeparacionGrid = 1.0f;

    private List<GameObject> spawnedDirt = new List<GameObject>();

    void OnEnable()
    {
        if (gridGen != null)
            gridGen.OnMapGenerated += RellenarTierra;
    }

    void OnDisable()
    {
        if (gridGen != null)
            gridGen.OnMapGenerated -= RellenarTierra;
    }

    public void RellenarTierra()
    {
        if (gridGen.heightMap == null)
        {
            Debug.LogError("HeightMap no generado todavía!");
            return;
        }

        // Limpiar tierra existente
        foreach (var dirt in spawnedDirt)
            if (dirt != null) DestroyImmediate(dirt);
        spawnedDirt.Clear();

        int[,] heightMap = gridGen.heightMap;
        int sizeX = heightMap.GetLength(0);
        int sizeZ = heightMap.GetLength(1);

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                int surfaceY = heightMap[x, z];
                int startY = surfaceY - 1;
                int endY = Mathf.Max(surfaceY - fillDepth, 0);

                for (int y = startY; y >= endY; y--)
                {
                    // Si el bloque está totalmente rodeado, no lo instanciamos
                    if (EstaRodeado(heightMap, x, y, z, sizeX, sizeZ))
                        continue;

                    Vector3 pos = new Vector3(x * SeparacionGrid, y, z * SeparacionGrid);
                    var dirt = Instantiate(dirtPrefab, pos, Quaternion.identity, this.transform);
                    spawnedDirt.Add(dirt);
                }
            }
        }
    }

    private bool EstaRodeado(int[,] heightMap, int x, int y, int z, int sizeX, int sizeZ)
    {
        // Bloques arriba (superficie)
        if (y == heightMap[x, z]) return false;

        // Revisar 6 direcciones (como un cubo en Minecraft)
        Vector3Int[] dirs = {
            new Vector3Int(1,0,0),
            new Vector3Int(-1,0,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,-1,0),
            new Vector3Int(0,0,1),
            new Vector3Int(0,0,-1)
        };

        foreach (var d in dirs)
        {
            int nx = x + d.x;
            int ny = y + d.y;
            int nz = z + d.z;

            if (nx < 0 || nx >= sizeX || nz < 0 || nz >= sizeZ)
                return false; // borde -> visible

            if (ny > heightMap[nx, nz])
                return false; // aire -> visible
        }

        return true; // rodeado por tierra
    }
}