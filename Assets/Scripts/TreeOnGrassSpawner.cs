using System.Collections.Generic;
using UnityEngine;

public class TreeOnGrassSpawner : MonoBehaviour
{
    public enum SpawnContext { Trees = 1, Antennas = 2 }

    [Header("Contexto")]
    [SerializeField] private SpawnContext context = SpawnContext.Trees;

    [Header("Referencias")]
    [SerializeField] private GridGen grid;              // Para Trees (puede quedar null si solo usas Houses)
    [SerializeField] private GameObject treePrefab;     // Prefab con TreeGen
    [SerializeField] private GameObject antennaPrefab;  // Prefab con AntennaGen

    [Header("Spawn")]
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.30f;
    [SerializeField] private int maxObjects = 10;
    [SerializeField] private float minSpacing = 2.0f;
    [SerializeField] private float extraYOffset = 0.0f;
    [SerializeField] private bool randomYaw = true;

    [Header("Clearance (opcional)")]
    [SerializeField] private bool useClearance = false;
    [SerializeField] private Vector3 clearanceHalfExtents = new Vector3(0.75f, 2f, 0.75f);
    [SerializeField] private LayerMask obstructionMask;

    [Header("Semilla global")]
    [SerializeField] private bool useSeed = true;
    [SerializeField] private int masterSeed = 123456789;

    [Header("�NCLAS PARA ANTENAS")]
    [Tooltip("Tag que identifica las casas donde se montar�n las antenas")]
    [SerializeField] private string houseTag = "House";

    [Header("Regla TreeGen (opcional, sin respawn)")]
    [SerializeField] private bool useCustomRuleOverride = false;
    [SerializeField, TextArea(1, 4)] private string customRuleF = "F[+F]F[-F]F";

    private readonly List<Vector3> plantedBases = new List<Vector3>();
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

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
        // Si el grid ya gener�, dispara; si no, igual podr�s usar Houses en contexto Antennas
        if (grid && grid.transform.childCount > 0) HandleMapReady();
        else if (context == SpawnContext.Antennas) DoSpawn(); // soporta escena sin grid
    }

    private void HandleMapReady() => DoSpawn();

    // ========= API p�blica =========
    public void SetContext(SpawnContext newContext)
    {
        context = newContext;
        DoSpawn();
    }

    public void ApplyCustomRuleToForest(string fRule)
    {
        if (string.IsNullOrWhiteSpace(fRule))
        {
            Debug.LogWarning("[Spawner] Regla vac�a. No se aplic�.");
            return;
        }
        useCustomRuleOverride = true;
        customRuleF = fRule.Trim();

        foreach (var go in spawnedObjects)
        {
            if (!go) continue;
            var tg = go.GetComponent<TreeGen>();
            if (tg) tg.SetCustomRuleAndRegenerate(customRuleF);
        }
    }

    public void RandomizeSeedAndRespawn()
    {
        masterSeed = UnityEngine.Random.Range(int.MinValue + 1, int.MaxValue);
        DoSpawn();
    }
    public void RespawnWithSeed(int newSeed) { masterSeed = newSeed; DoSpawn(); }
    public void Respawn() => DoSpawn();

    // ========= N�cleo =========
    private void DoSpawn()
    {
        // limpiar previos
        foreach (var go in spawnedObjects) if (go) Destroy(go);
        spawnedObjects.Clear();
        plantedBases.Clear();

        // elegir prefab y bases seg�n contexto
        GameObject prefab = context == SpawnContext.Trees ? treePrefab : antennaPrefab;
        if (!prefab)
        {
            Debug.LogWarning($"[Spawner] Prefab no asignado para contexto {context}");
            return;
        }

        List<Transform> bases = CollectBasesForContext(context);
        if (bases.Count == 0)
        {
            Debug.LogWarning($"[Spawner] No se encontraron bases para {context}. " +
                             (context == SpawnContext.Antennas ? $"�Tienen tag '{houseTag}'?" : "�Grid gener� bloques?"));
            return;
        }

        // RNG determinista
        System.Random rng = useSeed ? new System.Random(masterSeed) : new System.Random();

        // barajar bases
        for (int i = bases.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (bases[i], bases[j]) = (bases[j], bases[i]);
        }

        int spawned = 0;
        foreach (var b in bases)
        {
            if (spawned >= maxObjects) break;
            if (rng.NextDouble() > spawnChance) continue;

            Vector3 baseWorld = b.position;
            if (!HasSpacing(baseWorld)) continue;

            Vector3 topWorld = GetTopPointAny(b) + Vector3.up * extraYOffset;

            if (useClearance)
            {
                Vector3 checkCenter = topWorld + Vector3.up * (clearanceHalfExtents.y + 0.05f);
                if (Physics.CheckBox(checkCenter, clearanceHalfExtents, Quaternion.identity, obstructionMask))
                    continue;
            }

            Quaternion rot = Quaternion.identity;
            if (randomYaw)
            {
                float yaw = (float)(rng.NextDouble() * 360.0);
                rot = Quaternion.Euler(0f, yaw, 0f);
            }

            // parent: para �rboles, al bloque de grid; para antenas, a la casa
            GameObject obj = Instantiate(prefab, topWorld, rot, b);

            // semilla por instancia
            int s1 = rng.Next(int.MaxValue);
            int sign = rng.Next(0, 2) == 0 ? 1 : -1;
            int instSeed = s1 * sign;

            if (context == SpawnContext.Trees)
            {
                var tg = obj.GetComponent<TreeGen>();
                if (tg != null)
                {
                    tg.useSeed = true;
                    tg.seed = instSeed;
                    if (useCustomRuleOverride) tg.SetCustomRuleAndRegenerate(customRuleF);
                    else tg.RegenerateKeepRules();
                }
            }
            else // Antennas
            {
                var ag = obj.GetComponent<AntennaGen>();
                if (ag != null)
                {
                    ag.useSeed = true;
                    ag.seed = instSeed;
                    ag.Generate();
                }
            }

            plantedBases.Add(baseWorld);
            spawnedObjects.Add(obj);
            spawned++;
        }

        Debug.Log($"[Spawner] Generados: {spawned} | Contexto: {context} | Seed: {masterSeed}");
    }

    // === Bases por contexto ===
    private List<Transform> CollectBasesForContext(SpawnContext ctx)
    {
        var list = new List<Transform>();

        if (ctx == SpawnContext.Trees)
        {
            if (!grid)
            {
                Debug.LogWarning("[Spawner] Grid no asignado para Trees.");
                return list;
            }
            foreach (Transform t in grid.transform) list.Add(t);
        }
        else // Antennas: tomar todos los objetos con tag House
        {
            var houses = GameObject.FindGameObjectsWithTag(houseTag);
            foreach (var go in houses) if (go) list.Add(go.transform);
        }
        return list;
    }

    // === helpers ===
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

    // Soporta objetos complejos (usa bounds combinados en hijos)
    private Vector3 GetTopPointAny(Transform t)
    {
        // 1) Colliders en hijos
        var cols = t.GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        // 2) Renderers en hijos
        var rends = t.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        // 3) Fallback al propio transform
        return t.position + Vector3.up * (t.lossyScale.y * 0.5f);
    }
}
