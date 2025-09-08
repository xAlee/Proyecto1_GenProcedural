using System.Collections.Generic;
using UnityEngine;

public class TreeOnGrassSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GridGen grid;              // arrástralo en el Inspector
    [SerializeField] private GameObject treePrefab;     // prefab con TreeGen

    [Header("Spawn")]
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.30f;
    [SerializeField] private int maxTrees = 10;
    [SerializeField] private float minSpacing = 2.0f;
    [SerializeField] private float extraYOffset = 0.0f; // ajuste fino sobre la tapa del bloque
    [SerializeField] private bool randomYaw = true;

    [Header("Clearance (opcional)")]
    [SerializeField] private bool useClearance = false;
    [SerializeField] private Vector3 clearanceHalfExtents = new Vector3(0.75f, 2f, 0.75f);
    [SerializeField] private LayerMask obstructionMask;

    [Header("Semilla global (para spawn determinista)")]
    [SerializeField] private bool useSeed = true;
    [SerializeField] private int masterSeed = 123456789;

    // (opcional) regla override para futuras instancias; NO se usa para respawn automático
    [Header("Override de regla (solo para aplicar por UI o futuras instancias)")]
    [SerializeField] private bool useCustomRuleOverride = false;
    [SerializeField, TextArea(1, 4)] private string customRuleF = "F[+F]F[-F]F";

    private readonly List<Vector3> plantedBases = new List<Vector3>();
    private readonly List<GameObject> spawnedTrees = new List<GameObject>();

    void OnEnable()
    {
        if (!grid) grid = FindObjectOfType<GridGen>();
        if (grid) grid.OnMapGenerated += HandleMapReady;
    }

    void OnDisable()
    {
        if (grid) grid.OnMapGenerated -= HandleMapReady;
    }

    void Start()
    {
        // por si el mapa ya estaba listo
        if (grid && grid.transform.childCount > 0)
        {
            HandleMapReady();
        }
    }

    private async void HandleMapReady()
    {
        DoSpawn();
    }

    // ========= API pública UI =========

    // Aplica la regla escrita SOLO a los árboles ya instanciados (sin respawn)
    public void ApplyCustomRuleToForest(string fRule)
    {
        if (string.IsNullOrWhiteSpace(fRule))
        {
            Debug.LogWarning("[TreeOnGrassSpawner] Regla vacía. No se aplicó.");
            return;
        }

        useCustomRuleOverride = true;
        customRuleF = fRule.Trim();

        foreach (var go in spawnedTrees)
        {
            if (!go) continue;
            var tg = go.GetComponent<TreeGen>();
            if (tg) tg.SetCustomRuleAndRegenerate(customRuleF);
        }
    }

    // (Opcional) si quieres regenerar SOLO semillas sin tocar reglas:
    public void RandomizeSeedAndRespawn()
    {
        masterSeed = UnityEngine.Random.Range(int.MinValue + 1, int.MaxValue);
        DoSpawn();
    }

    public void RespawnWithSeed(int newSeed)
    {
        masterSeed = newSeed;
        DoSpawn();
    }

    public void Respawn() => DoSpawn();

    // ========= Núcleo del spawn determinista =========
    private void DoSpawn()
    {
        // limpiar árboles previos
        foreach (var go in spawnedTrees)
        {
            if (go) Destroy(go);
        }
        spawnedTrees.Clear();
        plantedBases.Clear();

        if (!grid || !treePrefab) return;

        // 1) recolectar todos los bloques de pasto (hijos de GridGen)
        var grasses = new List<Transform>(grid.transform.childCount);
        foreach (Transform t in grid.transform) grasses.Add(t);

        // 2) crear RNG determinista
        System.Random rng = useSeed ? new System.Random(masterSeed) : new System.Random();

        // 3) barajar deterministamente
        for (int i = grasses.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (grasses[i], grasses[j]) = (grasses[j], grasses[i]);
        }

        int planted = 0;
        foreach (var g in grasses)
        {
            if (planted >= maxTrees) break;

            // tirar dado de spawn con la misma RNG
            if (NextFloat01(rng) > spawnChance) continue;

            // base para espaciado (centro del bloque en mundo)
            Vector3 baseWorld = g.position;
            if (!HasSpacing(baseWorld)) continue;

            // punto superior del bloque (máxima altura)
            Vector3 topWorld = GetTopPoint(g) + Vector3.up * extraYOffset;

            if (useClearance)
            {
                Vector3 checkCenter = topWorld + Vector3.up * (clearanceHalfExtents.y + 0.05f);
                if (Physics.CheckBox(checkCenter, clearanceHalfExtents, Quaternion.identity, obstructionMask))
                    continue;
            }

            Quaternion rot = Quaternion.identity;
            if (randomYaw)
            {
                float yaw = Lerp(rng, 0f, 360f);
                rot = Quaternion.Euler(0f, yaw, 0f);
            }

            // instanciar en la TAPA del bloque y dejarlo como HIJO
            GameObject tree = Instantiate(treePrefab, topWorld, rot, g);

            var tg = tree.GetComponent<TreeGen>();
            if (tg != null)
            {
                tg.useSeed = true;

                // semilla única por árbol, derivada de la RNG del spawner
                int s1 = rng.Next(int.MaxValue);
                int sign = rng.Next(0, 2) == 0 ? 1 : -1;
                tg.seed = s1 * sign;

                // Si hay override activo, aplica esa regla al crear;
                // si no, respeta la regla interna del TreeGen.
                if (useCustomRuleOverride)
                    tg.SetCustomRuleAndRegenerate(customRuleF);
                else
                    tg.RegenerateKeepRules();
            }

            plantedBases.Add(baseWorld);
            spawnedTrees.Add(tree);
            planted++;
        }

        Debug.Log($"[TreeOnGrassSpawner] Árboles plantados: {planted} (seed {masterSeed})");
    }

    // ====== helpers ======
    private static float NextFloat01(System.Random rng) => (float)rng.NextDouble();
    private static float Lerp(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    // calcula la distancia mínima en planta (XZ) usando la base del bloque
    private bool HasSpacing(Vector3 p)
    {
        float minSqr = minSpacing * minSpacing;
        foreach (var q in plantedBases)
        {
            Vector2 a = new Vector2(p.x, p.z);
            Vector2 b = new Vector2(q.x, q.z);
            if ((a - b).sqrMagnitude < minSqr) return false;
        }
        return true;
    }

    // detectar altura máxima del objeto
    private Vector3 GetTopPoint(Transform t)
    {
        // 1) collider si existe
        var col = t.GetComponent<Collider>();
        if (col)
        {
            Bounds b = col.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        // 2) renderer si no hay collider
        var rend = t.GetComponent<Renderer>();
        if (rend)
        {
            Bounds b = rend.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        // 3) fallback: asumir pivot al centro y usar media altura por escala
        return t.position + Vector3.up * (t.lossyScale.y * 0.5f);
    }
}
