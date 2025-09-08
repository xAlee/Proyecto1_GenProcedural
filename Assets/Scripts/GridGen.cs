using System.Collections.Generic;
using UnityEngine;

public class GridGen : MonoBehaviour
{
    public GameObject grassPrefab;
    public GameObject dirtPrefab;

    [Header("Edificios / Casas")]
    public GameObject buildingPrefab1;
    public GameObject buildingPrefab2;
    public GameObject cityGroundPrefab; // Prefab del suelo de la ciudad
    public enum ContextType { SoloTierra, ConEdificios }
    public ContextType currentContext = ContextType.SoloTierra;

    [Header("Regenerar Mapa")]
    public bool regenerarMapa = false;

    [SerializeField] public int sizeX = 40;
    [SerializeField] public int sizeZ = 40;
    [SerializeField] public int noiseHeight = 8;
    [SerializeField] private float SeparacionGrid = 1.0f;
    [SerializeField] public int seed = 12345;

    [Header("Perlin Noise")]
    [SerializeField] public float noiseScale = 0.1f;
    [SerializeField] public int octaves = 4;
    [SerializeField] public float persistence = 0.5f;
    [SerializeField] public float lacunarity = 2.0f;

    [Header("Ciudades")]
    [SerializeField] private int alturaMinEdificio = 2;
    [SerializeField, Range(0f, 1f)] private float probabilidadEdificio = 0.2f;

    public int[,] heightMap { get; private set; } // Para que otro script pueda acceder

    [SerializeField] private List<GameObject> spawnedCubes = new List<GameObject>();

    void Start()
    {
        GenerarMapa();
    }

    public event System.Action OnMapGenerated;

    void Update()
    {
        if (regenerarMapa)
        {
            GenerarMapa();
            regenerarMapa = false;
        }
    }

    public void GenerarMapa()
    {
        // Limpia cubos existentes
        foreach (var cube in spawnedCubes)
            if (cube != null) DestroyImmediate(cube);
        spawnedCubes.Clear();

        InitPerm(seed);

        // Ajustamos parámetros de Perlin según contexto
        float contextNoiseHeight = noiseHeight;
        float contextNoiseScale = noiseScale;
        int contextOctaves = octaves;
        float contextPersistence = persistence;
        float contextLacunarity = lacunarity;

        if (currentContext == ContextType.ConEdificios)
        {
            // Suavizamos el terreno para calles y edificios
            contextNoiseHeight = Mathf.Max(1, noiseHeight / 3f); // alturas bajas
            contextNoiseScale = noiseScale * 2f;                // más suave
            contextOctaves = 3;
            contextPersistence = 0.4f;
            contextLacunarity = 2f;
        }

        heightMap = new int[sizeX, sizeZ];

        // Calculamos alturas de columnas
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                float heightValue = FractalPerlin2D(x * contextNoiseScale, z * contextNoiseScale, contextOctaves, contextPersistence, contextLacunarity) * contextNoiseHeight;
                heightMap[x, z] = Mathf.RoundToInt(heightValue);
            }
        }

        // Generamos superficie y edificios
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                int colHeight = heightMap[x, z];
                Vector3 pos = new Vector3(x * SeparacionGrid, colHeight, z * SeparacionGrid);

                // Elegimos el prefab de suelo según contexto
                GameObject sueloPrefab = currentContext == ContextType.ConEdificios
                    ? cityGroundPrefab
                    : grassPrefab;

                GameObject cube = Instantiate(sueloPrefab, pos, Quaternion.identity, this.transform);
                spawnedCubes.Add(cube);

                // Contexto de edificios
                if (currentContext == ContextType.ConEdificios && colHeight >= alturaMinEdificio)
                {
                    if (Random.value < probabilidadEdificio)
                    {
                        GameObject buildingToSpawn = Random.value < 0.5f ? buildingPrefab1 : buildingPrefab2;
                        GameObject building = Instantiate(buildingToSpawn, pos + Vector3.up * 0.5f, Quaternion.identity, this.transform);
                        spawnedCubes.Add(building);
                    }
                }
            }
        }

        OnMapGenerated?.Invoke();
    }


    // Modificamos FractalPerlin2D para recibir parámetros dinámicos
    private float FractalPerlin2D(float x, float y, int oct, float pers, float lac)
    {
        float total = 0f, amplitude = 1f, frequency = 1f, maxValue = 0f;
        for (int i = 0; i < oct; i++)
        {
            total += Perlin2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= pers;
            frequency *= lac;
        }
        return total / maxValue;
    }


    // --- Perlin Noise Functions ---
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
}
