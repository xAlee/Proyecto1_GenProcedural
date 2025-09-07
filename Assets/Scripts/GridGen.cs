using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGen : MonoBehaviour
{
    public GameObject boxelPrefab;

    [SerializeField] private int sizeX = 20;//tamaño del grid
    [SerializeField] private int sizeZ = 20;

    [SerializeField] private int noiseHeight = 3;//multiplicador de altura, a mas grande montañas mal altas, a mas bajo, mas plano
    [SerializeField] private float SeparacionGrid = 1.1f;//separacion entre los cubos

    [SerializeField] private int seed = 12345; 
    [SerializeField] private bool reiterar = false;

    [Header("Perlin Noise")]
    [SerializeField] private float noiseScale = 0.1f;//Valor para la escala de ruido de perlin Noise
                                                     //Valores pequeños permiten crear montañas, valores altos hace que el terreno se mas rugoso
    [SerializeField] private int octaves = 4;//Las octavas son capas de ruido, a mas capas mas "montañas en las montañas", menos capas
                                             //hace que las montantañas sean mas planas, sin detalles
    [SerializeField] private float persistence = 0.5f;//Persistence mide que tan pesada es la octava, y que tanto influye, a mas valor mas detalles
    [SerializeField] private float lacunarity = 2.0f;//Lacunarity es una medida para espacios vacios, a mas se aplican mas octavas por ende
                                                     //hay mas detalle, y a menos valor es mas plano

    private List<GameObject> spawnedCubes = new List<GameObject>();

    void Start()
    {
        GenerarMapa();
    }

    void Update()
    {
        if (reiterar)
        {
            reiterar = false;
            GenerarMapa();
        }
    }

    //Estos son preconfiguraciones que fui probando, se pueden aplicar desde el menú contextual del script:)
    [ContextMenu("Montañas suaves")]
    public void MontanasSuaves()
    {
        noiseScale = 0.1f;
        octaves = 4;
        persistence = 0.5f;
        lacunarity = 2.0f;
        GenerarMapa();
    }

    [ContextMenu("Colinas suaves")]
    public void ColinasSuaves()
    {
        noiseScale = 0.05f;
        octaves = 2;
        persistence = 0.3f;
        lacunarity = 2.0f;
        GenerarMapa();
    }

    [ContextMenu("Terreno caótico")]
    public void TerrenoCaotico()
    {
        noiseScale = 0.2f;
        octaves = 6;
        persistence = 0.8f;
        lacunarity = 3.0f;
        GenerarMapa();
    }

    private void GenerarMapa()
    {
        foreach (var cube in spawnedCubes)
            if (cube != null) DestroyImmediate(cube);

        spawnedCubes.Clear();
        InitPerm(seed);

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                float heightValue = FractalPerlin2D(x * noiseScale, z * noiseScale) * noiseHeight;
                Vector3 pos = new Vector3(x * SeparacionGrid, heightValue, z * SeparacionGrid);

                GameObject Cube = Instantiate(boxelPrefab, pos, Quaternion.identity, this.transform);
                spawnedCubes.Add(Cube);
            }
        }
    }

    //Esta es una tabla de permutacion pseudo aleatoria, la usamos mas adelante para calcular las gradientes de los cubos
    private static int[] p;

    public static void InitPerm(int seed)
    {
        System.Random rand = new System.Random(seed);
        int[] perm = new int[256];
        for (int i = 0; i < 256; i++) perm[i] = i;

        //esto es Fisher–Yates shuffle, es un algoritmo para mezclar los indices, los valores finales depende de la seed
        //este algoritmo se usa para hacer permutaciones c:
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

    //Fade, Lerp y Grad son funciones auxiliares propias de perlin noise, son formulas matematicas, en el caso de fade
    //hace que no hayan por ejemplo cambios muy bruscos, si no que siempre hayan interpolaciones suaves
    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    //Lerp calcula una interpolacion lineal, si t es 0 => a, si t es 1 => b, y si t es 0.5 => punto medio
    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    //grad calcula y fuerza a tener gradiantes entre 0 y 7, basicamente 8, para 8 direcciones
    //luego calcula el producto punto entre la gradiente que se calculo y el vector de cada esquina
    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 7;
        float u = h < 4 ? x : y;
        float v = h < 4 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    
    //
    public static float Perlin2D(float x, float y)
    {
        int X = Mathf.FloorToInt(x) & 255;
        int Y = Mathf.FloorToInt(y) & 255;

        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
 
        //aqui se aplica la interpolacion punto por punto
        float u = Fade(x);
        float v = Fade(y);

        //Aqui se calculan las interpolaciones para cada esquina
        int aa = p[p[X] + Y];
        int ab = p[p[X] + Y + 1];
        int ba = p[p[X + 1] + Y];
        int bb = p[p[X + 1] + Y + 1];
        
        //y finalmente se toma un valor entre -1 y 1
        float res = Lerp(
            Lerp(Grad(aa, x, y), Grad(ba, x - 1, y), u),
            Lerp(Grad(ab, x, y - 1), Grad(bb, x - 1, y - 1), u),
            v
        );

        return (res + 1f) / 2f;
    }

    //Aqui se aplican las iteraciones con las octavas, y se ajustan
    private float FractalPerlin2D(float x, float y)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

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
