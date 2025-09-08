using UnityEngine;
using System.Collections.Generic;

public class TreeGen : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject cubePrefab;   // Rama/tronco (Cylinder recomendado)
    [SerializeField] private GameObject leafPrefab;   // Hoja (esfera/quad)

    [Header("L-System")]
    [SerializeField] private int iterations = 3;
    [SerializeField] private float angleMin = 20f;
    [SerializeField] private float angleMax = 35f;
    [SerializeField] private float length = 1f;

    [Header("Aspecto ramas")]
    [SerializeField] private Vector2 branchRadius = new Vector2(0.12f, 0.2f);
    [SerializeField] private float leafSize = 0.35f;

    [Header("Taper (afinado por profundidad)")]
    [SerializeField, Range(0.7f, 1f)] private float radiusDecay = 0.9f;
    [SerializeField, Range(0.8f, 1f)] private float lengthDecay = 0.95f;

    [Header("Aleatoriedad")]
    [SerializeField] public bool useSeed = true;
    [SerializeField] public int seed = 12345;

    [SerializeField] private float primitiveBaseHeight = 2f;

    private int roundingDecimals = 3;
    private string axiom = "F";
    private string currentString;

    private Dictionary<char, string> rules = new Dictionary<char, string>();
    private HashSet<string> occupiedPositions = new HashSet<string>();

    [Header("Regla inicial (puedes editarla en Inspector)")]
    [SerializeField, TextArea(1, 3)] private string initialRule = "F[+F]F[-F]F";

    void Start()
    {
        if (useSeed) Random.InitState(seed);

        // Aplica la regla inicial al comenzar
        rules.Clear();
        rules.Add('F', initialRule.Trim());

        currentString = axiom;
        for (int i = 0; i < iterations; i++)
            currentString = ApplyRules(currentString);

        GenerateTree(currentString);
    }

    // ---- API pública: cambiar solo la semilla (manteniendo reglas) ----
    public void RegenerateKeepRules()
    {
        if (useSeed) Random.InitState(seed);
        RegenerateWithCurrentRules();
    }

    public void SetSeedAndRegenerate(int newSeed)
    {
        seed = newSeed;
        RegenerateKeepRules();
    }

    public void SetSeedOnly(int newSeed)
    {
        seed = newSeed;
        RegenerateKeepRules();
    }

    // ---- API pública: aplicar regla escrita desde UI ----
    public void SetCustomRuleAndRegenerate(string fRule)
    {
        if (string.IsNullOrWhiteSpace(fRule))
        {
            Debug.LogWarning("[TreeGen] Regla vacía. No se aplicó.");
            return;
        }

        if (useSeed) Random.InitState(seed);

        rules.Clear();
        rules.Add('F', fRule.Trim());

        currentString = axiom;
        for (int i = 0; i < iterations; i++)
            currentString = ApplyRules(currentString);

        var previousTree = transform.Find("LSystemTree");
        if (previousTree != null) Destroy(previousTree.gameObject);

        GenerateTree(currentString);
    }

    // ================= Internals =================

    private void RegenerateWithCurrentRules()
    {
        var previousTree = transform.Find("LSystemTree");
        if (previousTree != null) Destroy(previousTree.gameObject);

        currentString = axiom;
        for (int i = 0; i < iterations; i++)
            currentString = ApplyRules(currentString);

        GenerateTree(currentString);
    }

    private string ApplyRules(string input)
    {
        string result = "";
        foreach (char c in input)
        {
            if (rules.ContainsKey(c)) result += rules[c];
            else result += c.ToString();
        }
        return result;
    }

    private void GenerateTree(string instructions)
    {
        occupiedPositions.Clear();
        Stack<TransformInfo> transformStack = new Stack<TransformInfo>();

        Vector3 posLocal = Vector3.zero;
        Quaternion rotLocal = Quaternion.identity;

        Transform parent = new GameObject("LSystemTree").transform;
        parent.SetParent(this.transform, false);
        Transform branchesParent = new GameObject("Branches").transform;
        branchesParent.SetParent(parent, false);
        Transform leavesParent = new GameObject("Leaves").transform;
        leavesParent.SetParent(parent, false);

        foreach (char c in instructions)
        {
            switch (c)
            {
                case 'F':
                    {
                        int depth = transformStack.Count;

                        float segLen = length * Mathf.Pow(lengthDecay, depth);
                        float rBase = Random.Range(branchRadius.x, branchRadius.y);
                        float r = rBase * Mathf.Pow(radiusDecay, depth);

                        Vector3 centerLocal = posLocal + rotLocal * Vector3.up * (segLen * 0.5f);

                        Vector3 centerWorld = transform.TransformPoint(centerLocal);
                        Quaternion rotWorld = transform.rotation * rotLocal;

                        string key = PosKey(centerWorld);
                        if (!occupiedPositions.Contains(key))
                        {
                            GameObject branch = Instantiate(cubePrefab, centerWorld, rotWorld, branchesParent);
                            branch.transform.localScale = new Vector3(r, segLen / primitiveBaseHeight, r);
                            occupiedPositions.Add(key);
                        }

                        posLocal += rotLocal * Vector3.up * segLen;
                        break;
                    }

                case '+':
                    rotLocal *= Quaternion.Euler(Random.Range(angleMin, angleMax), 0, Random.Range(angleMin, angleMax) * 0.5f);
                    break;
                case '-':
                    rotLocal *= Quaternion.Euler(-Random.Range(angleMin, angleMax), 0, -Random.Range(angleMin, angleMax) * 0.5f);
                    break;
                case '&':
                    rotLocal *= Quaternion.Euler(Random.Range(angleMin, angleMax), Random.Range(5f, angleMax * 0.6f), 0);
                    break;
                case '^':
                    rotLocal *= Quaternion.Euler(-Random.Range(angleMin, angleMax), -Random.Range(5f, angleMax * 0.6f), 0);
                    break;
                case '/':
                    rotLocal *= Quaternion.Euler(0, Random.Range(angleMin * 0.7f, angleMax), 0);
                    break;
                case '\\':
                    rotLocal *= Quaternion.Euler(0, -Random.Range(angleMin * 0.7f, angleMax), 0);
                    break;

                case '[':
                    transformStack.Push(new TransformInfo { position = posLocal, rotation = rotLocal });
                    rotLocal *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    break;

                case ']':
                    {
                        if (leafPrefab != null)
                        {
                            Vector3 leafWorld = transform.TransformPoint(posLocal);
                            Quaternion leafRotWorld = transform.rotation * rotLocal;

                            string leafKey = PosKey(leafWorld);
                            if (!occupiedPositions.Contains(leafKey))
                            {
                                GameObject leaf = Instantiate(leafPrefab, leafWorld, leafRotWorld, leavesParent);
                                int depthHere = Mathf.Max(0, transformStack.Count - 1);
                                float scale = leafSize * Mathf.Pow(radiusDecay, depthHere + 1);
                                leaf.transform.localScale = Vector3.one * scale;
                                occupiedPositions.Add(leafKey);
                            }
                        }

                        TransformInfo ti = transformStack.Pop();
                        posLocal = ti.position;
                        rotLocal = ti.rotation;
                        break;
                    }

                default:
                    Debug.LogWarning($"[TreeGen] Carácter no reconocido en instrucciones: {c}");
                    break;
            }
        }

        if (leafPrefab != null)
        {
            Vector3 finalLeafWorld = transform.TransformPoint(posLocal);
            Quaternion finalRotWorld = transform.rotation * rotLocal;

            string finalLeafKey = PosKey(finalLeafWorld);
            if (!occupiedPositions.Contains(finalLeafKey))
            {
                GameObject leaf = Instantiate(leafPrefab, finalLeafWorld, finalRotWorld, leavesParent);
                leaf.transform.localScale = Vector3.one * leafSize;
                occupiedPositions.Add(finalLeafKey);
            }
        }
    }

    private string PosKey(Vector3 v)
    {
        return $"{v.x.ToString($"F{roundingDecimals}")}|{v.y.ToString($"F{roundingDecimals}")}|{v.z.ToString($"F{roundingDecimals}")}";
    }

    private struct TransformInfo
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}
