using System.Collections.Generic;
using UnityEngine;

public class GridGen : MonoBehaviour
{
    public GameObject grassPrefab;
    public GameObject dirtPrefab;

    [SerializeField] private int sizeX = 40;
    [SerializeField] private int sizeZ = 40;
    [SerializeField] private int noiseHeight = 8;
    [SerializeField] private float SeparacionGrid = 1.0f;
    [SerializeField] private int seed = 12345;

    [Header("Perlin Noise")]
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2.0f;

    public int[,] heightMap { get; private set; } // Para que otro script pueda acceder

    private List<GameObject> spawnedCubes = new List<GameObject>();

    void Start()
    {
        GenerarMapa();
    }

    public event System.Action OnMapGenerated;

     public void GenerarMapa()
    {
        // Limpia cubos existentes
        foreach (var cube in spawnedCubes)
            if (cube != null) DestroyImmediate(cube);
        spawnedCubes.Clear();
        InitPerm(seed);

        heightMap = new int[sizeX, sizeZ];

        // Calculamos alturas de columnas
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                float heightValue = FractalPerlin2D(x * noiseScale, z * noiseScale) * noiseHeight;
                heightMap[x, z] = Mathf.RoundToInt(heightValue);
            }
        }

        // Generamos solo la superficie
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                int colHeight = heightMap[x, z];
                Vector3 pos = new Vector3(x * SeparacionGrid, colHeight, z * SeparacionGrid);
                GameObject cube = Instantiate(grassPrefab, pos, Quaternion.identity, this.transform);
                spawnedCubes.Add(cube);
            }
        }

        // Disparamos el evento
        OnMapGenerated?.Invoke();
    }
    
    // --- Perlin Noise Functions (igual que antes) ---
    private static int[] p;
    public static void InitPerm(int seed)
    {
        System.Random rand = new System.Random(seed);
        int[] perm = new int[256];
        for (int i = 0; i < 256; i++) perm[i] = i;
        for (int i = 255; i > 0; i--)
        {
            int swapIndex = rand.Next(i + 1);
            int temp = perm[i];
            perm[i] = perm[swapIndex];
            perm[swapIndex] = temp;
        }
        p = new int[512];
        for (int i = 0; i < 512; i++) p[i] = perm[i % 256];
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 7;
        float u = h < 4 ? x : y;
        float v = h < 4 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    public static float Perlin2D(float x, float y)
    {
        int X = Mathf.FloorToInt(x) & 255;
        int Y = Mathf.FloorToInt(y) & 255;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        float u = Fade(x);
        float v = Fade(y);
        int aa = p[p[X] + Y];
        int ab = p[p[X] + Y + 1];
        int ba = p[p[X + 1] + Y];
        int bb = p[p[X + 1] + Y + 1];
        float res = Lerp(
            Lerp(Grad(aa, x, y), Grad(ba, x - 1, y), u),
            Lerp(Grad(ab, x, y - 1), Grad(bb, x - 1, y - 1), u),
            v
        );
        return (res + 1f) / 2f;
    }
    private float FractalPerlin2D(float x, float y)
    {
        float total = 0f, amplitude = 1f, frequency = 1f, maxValue = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += Perlin2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return total / maxValue;
    }
}
