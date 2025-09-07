using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class CellularAutomata : MonoBehaviour
{
    public GameObject boxelPrefab;
    public Material whiteCol;
    public Material blackCol;

    private BoxelDataList dataList;
    private List<GameObject> currentBoxels;
    [SerializeField] private NoiseGen dataObject;

    public int iterations = 1;

    private string path => Application.dataPath + "/boxelgrid.bin";

    public void ExecuteAutomata()
    {
        currentBoxels = dataObject.currentBoxels;

        LoadGrid();
        RunAutomata();
        RebuildScene();
        SaveGrid();
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

    private void RunAutomata()
    {
        int width = GetMaxX() + 1;
        int height = GetMaxY() + 1;

        for (int iter = 0; iter < iterations; iter++)
        {
            List<BoxelData> newData = new List<BoxelData>();

            foreach (BoxelData cell in dataList.items)
            {
                int neighbors = CountAliveNeighbors(cell.x, cell.y, width, height);

                BoxelData newCell = new BoxelData
                {
                    x = cell.x,
                    y = cell.y,
                    type = (neighbors > 4) ? 1 : 0
                };

                newData.Add(newCell);
            }

            dataList.items = newData;
        }
    }

    private void RebuildScene()
    {
        // Limpiar los boxels anteriores
        foreach (GameObject boxel in currentBoxels)
        {
            Destroy(boxel);
        }
        currentBoxels.Clear();

        // Construir de nuevo con los nuevos valores
        foreach (BoxelData data in dataList.items)
        {
            if (data.type != 0)
            {
                GameObject go = Instantiate(boxelPrefab, new Vector3(data.x, 0, data.y), Quaternion.identity);

                currentBoxels.Add(go);
            }
        }
    }

    private int CountAliveNeighbors(int x, int y, int width, int height)
    {
        int alive = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // no contar la celda misma

                int nx = x + dx;
                int ny = y + dy;

                // si se sale de los límites, contar como "pared" (type=1)
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    alive++;
                }
                else
                {
                    BoxelData neighbor = dataList.items.Find(c => c.x == nx && c.y == ny);
                    if (neighbor != null && neighbor.type == 1)
                    {
                        alive++;
                    }
                }
            }
        }

        return alive;
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
        // Convertir dataList de nuevo a lista de tipos en el mismo orden usado para escribir
        int width = GetMaxX() + 1;
        int height = GetMaxY() + 1;

        // Asegurar orden consistente: x primero, luego y (coincide con NoiseGen)
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