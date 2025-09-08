using UnityEngine;

public class FillDirt : MonoBehaviour
{
    public GridGen gridGen; // referencia a GridGen
    public GameObject dirtPrefab;
    public int fillDepth = 5;
    public float SeparacionGrid = 1.0f;

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

        int[,] heightMap = gridGen.heightMap;
        int sizeX = heightMap.GetLength(0);
        int sizeZ = heightMap.GetLength(1);

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                int surfaceY = heightMap[x, z];

                // Aseguramos que Y no sea menor a 0
                int startY = surfaceY - 1;
                int endY = Mathf.Max(surfaceY - fillDepth, 0);

                for (int y = startY; y >= endY; y--)
                {
                    Vector3 pos = new Vector3(x * SeparacionGrid, y, z * SeparacionGrid);
                    Instantiate(dirtPrefab, pos, Quaternion.identity, this.transform);
                }
            }
        }
    }
}
