using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class NoiseGen : MonoBehaviour
{
    [SerializeField] private int noise_width = 10;
    [SerializeField] private int noise_height = 10;
    [SerializeField] private GameObject boxel;
    [SerializeField] private Material whiteCol; 
    [SerializeField] private Material blackCol;
    [SerializeField] private float chanceRange = 0.5f;
    [SerializeField] private int iterations = 5;
    [SerializeField] private bool reset = false;

    private List<GameObject> currentBoxels = new List<GameObject>();

    private string JsonPath => Application.dataPath + "/boxelgrid.json";

    void Start()
    {
        DeleteJsonFile();
        Noise_Start();          
        int[,] grid = LoadGridFromJson(); 
        CellularAutomaton(ref grid, iterations); 
        BuildBoxelsFromGrid(grid);       
    }

    private void Noise_Start()
    {
        BoxelDataCollection dataCollection = new BoxelDataCollection();

        for (int i = 0; i < noise_width; i++)
        {
            for (int j = 0; j < noise_height; j++)
            {
                float chance = Random.Range(0.0f, 1.0f);
                int type = (chance > chanceRange) ? 0 : 1;

                dataCollection.boxels.Add(new BoxelData
                {
                    x = i,
                    y = j,
                    type = type
                });
            }
        }

        string fullJson = JsonUtility.ToJson(dataCollection, true);
        File.WriteAllText(JsonPath, fullJson);
    }

    private int[,] LoadGridFromJson()
    {
        if (!File.Exists(JsonPath)) return null;

        string json = File.ReadAllText(JsonPath);
        BoxelDataCollection dataCollection = JsonUtility.FromJson<BoxelDataCollection>(json);

        int[,] grid = new int[noise_width, noise_height];
        foreach (var boxel in dataCollection.boxels)
        {
            grid[boxel.x, boxel.y] = boxel.type;
        }
        return grid;
    }

    private void CellularAutomaton(ref int[,] grid, int count)
    {
        for (int c = 0; c < count; c++)
        {
            int[,] tempGrid = (int[,])grid.Clone();

            for (int y = 0; y < noise_height; y++)
            {
                for (int x = 0; x < noise_width; x++)
                {
                    int neighborWallCount = 0;

                    for (int ny = y - 1; ny <= y + 1; ny++)
                    {
                        for (int nx = x - 1; nx <= x + 1; nx++)
                        {
                            if (nx >= 0 && nx < noise_width && ny >= 0 && ny < noise_height)
                            {
                                if (nx != x || ny != y)
                                {
                                    if (tempGrid[nx, ny] == 1) neighborWallCount++;
                                }
                            }
                            else
                            {
                                neighborWallCount++;
                            }
                        }
                    }

                    if (neighborWallCount > 4)
                        grid[x, y] = 1;
                    else
                        grid[x, y] = 0;
                }
            }
        }
    }

    private void BuildBoxelsFromGrid(int[,] grid)
    {
        for (int i = 0; i < noise_width; i++)
        {
            for (int j = 0; j < noise_height; j++)
            {
                GameObject boxelinstance = Instantiate(boxel, new Vector3(i, 0, j), Quaternion.identity);

                if (grid[i, j] == 0)
                    boxelinstance.GetComponent<Renderer>().material = whiteCol;
                else
                    boxelinstance.GetComponent<Renderer>().material = blackCol;

                currentBoxels.Add(boxelinstance);
            }
        }
    }

    private void DeleteJsonFile()
    {
        if (File.Exists(JsonPath))
        {
            File.Delete(JsonPath);
        }
    }

    private void DeleteBoxels()
    {
        foreach (GameObject boxel in currentBoxels)
        {
            Destroy(boxel);
        }
        currentBoxels.Clear();
    }

    void Update()
    {
        if (reset)
        {
            reset = false;
            DeleteJsonFile();
            DeleteBoxels();

            Noise_Start();
            int[,] grid = LoadGridFromJson();
            CellularAutomaton(ref grid, iterations);
            BuildBoxelsFromGrid(grid);
        }
    }
}