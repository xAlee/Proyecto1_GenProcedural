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

    private readonly List<Vector3> plantedBases = new List<Vector3>();

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
        if (grid && grid.transform.childCount > 0) HandleMapReady();
    }

    private void HandleMapReady()
    {
        plantedBases.Clear();

        // 1) tomar todos los cubos de pasto (hijos de GridGen)
        var grasses = new List<Transform>(grid.transform.childCount);
        foreach (Transform t in grid.transform) grasses.Add(t);

        // 2) barajar para distribuir mejor
        var rng = new System.Random(987654321);
        for (int i = grasses.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (grasses[i], grasses[j]) = (grasses[j], grasses[i]);
        }

        // 3) plantar
        int planted = 0;
        foreach (var g in grasses)
        {
            if (planted >= maxTrees) break;
            if (Random.value > spawnChance) continue;

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

            Quaternion rot = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                                       : Quaternion.identity;

            // instanciar en la TAPA del bloque y dejarlo como HIJO
            GameObject tree = Instantiate(treePrefab, topWorld, rot, g);

            // semilla única por árbol
            var tg = tree.GetComponent<TreeGen>();
            if (tg != null)
            {
                tg.useSeed = true;
                tg.seed = Random.Range(int.MinValue, int.MaxValue);
            }

            plantedBases.Add(baseWorld);
            planted++;
        }

        Debug.Log($"[TreeOnGrassSpawner] Árboles plantados: {planted}");
    }

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

    // ======= PUNTO CLAVE: detectar altura máxima del objeto =======
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
