using UnityEngine;
using System.Collections.Generic;

public class TreeGen : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private int iterations = 3;

    [SerializeField] private float angleMin = 25f;
    [SerializeField] private float angleMax = 45f;

    [SerializeField] private float length = 1f;
    private int roundingDecimals = 3; // controla la precisión para detectar posiciones iguales

    private string axiom = "F";
    private string currentString;

    private Dictionary<char, string> rules = new Dictionary<char, string>();
    private HashSet<string> occupiedPositions = new HashSet<string>();

    void Start()
    {
        rules.Add('F', "F[+F]F[-F]F[\\F][&F][^F]");

        currentString = axiom;
        for (int i = 0; i < iterations; i++)
            currentString = ApplyRules(currentString);
        GenerateTree(currentString);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            Transform previousTree = transform.Find("LSystemTree");
            if (previousTree != null)
                Destroy(previousTree.gameObject);

            GenerateRandomRule(); // <-- Aleatoriza reglas cada vez
            currentString = axiom;
            for (int i = 0; i < iterations; i++)
                currentString = ApplyRules(currentString);
            GenerateTree(currentString);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            rules.Clear();
            Transform previousTree = transform.Find("LSystemTree");
            if (previousTree != null)
                Destroy(previousTree.gameObject);

            rules.Add('F', "F[+F]F[-F]F[\\F][&F][^F]");

            currentString = axiom;
            for (int i = 0; i < iterations; i++)
                currentString = ApplyRules(currentString);

            GenerateTree(currentString);
        }
    }

    private void GenerateRandomRule()
    {
        string[] directions = { "+F", "-F", "/F", "\\F", "&F", "^F" };
        int numBranches = Random.Range(2, 7); // Número de ramas aleatorio
        List<string> branches = new List<string>();

        for (int i = 0; i < numBranches; i++)
        {
            string dir = directions[Random.Range(0, directions.Length)];
            // 50% de probabilidad de poner la rama entre corchetes (ramificación)
            if (Random.value > 0.5f)
                branches.Add("[" + dir + "]");
            else
                branches.Add(dir);
        }

        // Siempre puedes agregar un 'F' central si quieres
        string rule = "F" + string.Join("", branches);
        rules['F'] = rule;
    }

    string ApplyRules(string input)
    {
        string result = "";
        foreach (char c in input)
        {
            if (rules.ContainsKey(c))
                result += rules[c];
            else
                result += c.ToString();
        }
        return result;
    }

    void GenerateTree(string instructions)
    {
        occupiedPositions.Clear();
        Stack<TransformInfo> transformStack = new Stack<TransformInfo>();
        Vector3 position = Vector3.zero;             // punto "base" desde donde crece la siguiente sección
        Quaternion rotation = Quaternion.identity;   // orientación "tortuga"

        // Padre para mantener jerarquía ordenada
        Transform parent = new GameObject("LSystemTree").transform;
        parent.SetParent(this.transform, false);

        foreach (char c in instructions)
        {
            switch (c)
            {
                case 'F':
                    // center = posición del centro del cubo (si asumimos pivot en el centro)
                    Vector3 center = position + rotation * Vector3.up * (length * 0.5f);

                    // clave redondeada para comparar posiciones (evita problemas por floats)
                    string key = PosKey(center);

                    // Si ya hay un cubo en esa posición (aprox), no lo instanciamos de nuevo
                    if (!occupiedPositions.Contains(key))
                    {
                        GameObject cube = Instantiate(cubePrefab, center, rotation, parent);
                        cube.transform.localScale = new Vector3(0.2f, length, 0.2f);
                        occupiedPositions.Add(key);
                    }

                    // avanzamos la "tortuga" la longitud completa (independiente de si instanciamos)
                    position += rotation * Vector3.up * length;
                    break;
                case '+': 
                    rotation *= Quaternion.Euler(Random.Range(angleMin, angleMax), 
                        0, Random.Range(angleMin, angleMax));
                    break;
                case '-': 
                    rotation *= Quaternion.Euler(-Random.Range(angleMin, angleMax), 
                        0, -Random.Range(angleMin, angleMax));
                    break;
                case '&': 
                    rotation *= Quaternion.Euler(Random.Range(angleMin, angleMax), 
                        Random.Range(angleMin, angleMax), 0);
                    break;
                case '^': 
                    rotation *= Quaternion.Euler(-Random.Range(angleMin, angleMax), 
                        -Random.Range(angleMin, angleMax), 0);
                    break;
                case '/': 
                    rotation *= Quaternion.Euler(0, Random.Range(angleMin, angleMax), 0);
                    break;
                case '\\': 
                    rotation *= Quaternion.Euler(0, -Random.Range(angleMin, angleMax), 0);
                    break;
                case '[': 
                    transformStack.Push(new TransformInfo { position = position, rotation = rotation });
                    break;
                case ']':
                    TransformInfo ti = transformStack.Pop();
                    position = ti.position;
                    rotation = ti.rotation;
                    break;
                default:
                    Debug.LogWarning($"Carácter no reconocido en instrucciones: {c}");
                    continue;
            }

        }
    }

    string PosKey(Vector3 v)
    {
        // Redondeamos a N decimales para manejar pequeñas diferencias de float
        return $"{v.x.ToString($"F{roundingDecimals}")}|{v.y.ToString($"F{roundingDecimals}")}|{v.z.ToString($"F{roundingDecimals}")}";
    }

    struct TransformInfo
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}
