using UnityEngine;

public class AntennaGen : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject mastPrefab;   // cilindro / tubo
    [SerializeField] private GameObject rodPrefab;    // cilindro delgado
    [SerializeField] private GameObject panelPrefab;  // quad o cube delgado
    [SerializeField] private GameObject dishPrefab;   // esfera/semiesfera (opcional)

    [Header("Mástil")]
    [SerializeField] private float mastHeight = 4f;
    [SerializeField] private float mastRadius = 0.05f;
    [SerializeField] private int mastSegments = 2;            // apilado
    [SerializeField] private float primitiveBaseHeight = 2f;  // cylinder=2, cube=1

    public enum AntennaStyle { YagiArray, Panel, Mixed, Random }
    public enum Complexity { Minimal, Standard, Dense }

    [Header("Preset")]
    [SerializeField] private AntennaStyle style = AntennaStyle.Mixed;
    [SerializeField] private Complexity complexity = Complexity.Standard;

    [Header("Extras")]
    [SerializeField, Range(0f, 1f)] private float dishChance = 0.15f;

    [Header("Aleatoriedad")]
    public bool useSeed = true;
    public int seed = 12345;

    // ---- internos ----
    private System.Random rng;
    private Transform root;

    void Start()
    {
        Generate();
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        // RNG local (no contamina UnityEngine.Random)
        rng = useSeed ? new System.Random(seed) : new System.Random();

        DestroyCurrentRoot();
        root = new GameObject("AntennaRoot").transform;
        root.SetParent(transform, false);

        // ===== Mástil =====
        Transform mastParent = new GameObject("Mast").transform;
        mastParent.SetParent(root, false);

        float segH = mastHeight / Mathf.Max(1, mastSegments);
        float currentY = segH * 0.5f;
        for (int i = 0; i < mastSegments; i++)
        {
            var seg = Instantiate(mastPrefab, mastParent);
            seg.transform.localPosition = new Vector3(0f, currentY, 0f);
            seg.transform.localRotation = Quaternion.identity;
            seg.transform.localScale = new Vector3(mastRadius, segH / primitiveBaseHeight, mastRadius);
            currentY += segH;
        }

        // Helper: punto a cierta altura
        System.Func<float, Vector3> PointOnMast = (h) => new Vector3(0f, Mathf.Clamp(h, 0f, mastHeight), 0f);

        // Parámetros según complejidad (rango base compacto)
        int crossMin, crossMax, yagiMin, yagiMax, panelMin, panelMax, yagiElemMin, yagiElemMax;
        float crossLenMin, crossLenMax, crossRadMin, crossRadMax, boomLenMin, boomLenMax, elemSpaceMin, elemSpaceMax, elemRadMin, elemRadMax, elemLenMin, elemLenMax;

        switch (complexity)
        {
            case Complexity.Minimal:
                crossMin = 0; crossMax = 2; yagiMin = 0; yagiMax = 1; panelMin = 0; panelMax = 1;
                yagiElemMin = 3; yagiElemMax = 5;
                crossLenMin = 0.5f; crossLenMax = 1.0f; crossRadMin = 0.012f; crossRadMax = 0.02f;
                boomLenMin = 1.0f; boomLenMax = 1.6f;
                elemSpaceMin = 0.10f; elemSpaceMax = 0.18f; elemRadMin = 0.009f; elemRadMax = 0.015f; elemLenMin = 0.22f; elemLenMax = 0.38f;
                break;

            case Complexity.Dense:
                crossMin = 3; crossMax = 6; yagiMin = 2; yagiMax = 3; panelMin = 1; panelMax = 3;
                yagiElemMin = 6; yagiElemMax = 10;
                crossLenMin = 0.7f; crossLenMax = 1.6f; crossRadMin = 0.015f; crossRadMax = 0.03f;
                boomLenMin = 1.4f; boomLenMax = 2.4f;
                elemSpaceMin = 0.12f; elemSpaceMax = 0.24f; elemRadMin = 0.01f; elemRadMax = 0.02f; elemLenMin = 0.28f; elemLenMax = 0.55f;
                break;

            default: // Standard
                crossMin = 2; crossMax = 4; yagiMin = 1; yagiMax = 2; panelMin = 0; panelMax = 2;
                yagiElemMin = 4; yagiElemMax = 8;
                crossLenMin = 0.6f; crossLenMax = 1.4f; crossRadMin = 0.012f; crossRadMax = 0.025f;
                boomLenMin = 1.2f; boomLenMax = 2.0f;
                elemSpaceMin = 0.12f; elemSpaceMax = 0.22f; elemRadMin = 0.01f; elemRadMax = 0.02f; elemLenMin = 0.25f; elemLenMax = 0.5f;
                break;
        }

        // Conteos por estilo
        int crossCount = RangeInt(crossMin, crossMax + 1);
        int yagiCount = 0;
        int panelCount = 0;

        AntennaStyle resolvedStyle = style == AntennaStyle.Random ? (AntennaStyle)RangeInt(0, 3) : style;

        switch (resolvedStyle)
        {
            case AntennaStyle.YagiArray:
                yagiCount = RangeInt(yagiMin, yagiMax + 1);
                panelCount = RangeInt(panelMin, panelMin + 1);
                break;
            case AntennaStyle.Panel:
                panelCount = RangeInt(panelMin, panelMax + 1);
                yagiCount = RangeInt(0, 1 + 1); // 0-1
                break;
            case AntennaStyle.Mixed:
            default:
                yagiCount = RangeInt(yagiMin, yagiMax + 1);
                panelCount = RangeInt(panelMin, panelMax + 1);
                break;
        }

        // ===== Brazos sueltos =====
        for (int i = 0; i < crossCount; i++)
        {
            float h = Range(0.3f, mastHeight * 0.95f);
            float len = Range(crossLenMin, crossLenMax);
            float r = Range(crossRadMin, crossRadMax);
            float yaw = Range(0f, 360f);

            Vector3 pos = PointOnMast(h);
            Quaternion rot = Quaternion.Euler(0f, yaw, 90f);

            var arm = Instantiate(rodPrefab, root);
            arm.transform.localPosition = pos;
            arm.transform.localRotation = rot;
            arm.transform.localScale = new Vector3(r, len / primitiveBaseHeight, r);
        }

        // ===== Yagi (boom + elementos perpendiculares) =====
        for (int i = 0; i < yagiCount; i++)
        {
            float h = Range(0.35f, mastHeight * 0.9f);
            float boomLen = Range(boomLenMin, boomLenMax);
            float yaw = Range(0f, 360f);

            var boom = Instantiate(rodPrefab, root);
            boom.transform.localPosition = PointOnMast(h);
            boom.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
            boom.transform.localScale = new Vector3(0.02f, boomLen / primitiveBaseHeight, 0.02f);

            int eCount = RangeInt(yagiElemMin, yagiElemMax + 1);
            float spacing = Range(elemSpaceMin, elemSpaceMax);
            float start = -boomLen * 0.5f + spacing;

            for (int e = 0; e < eCount; e++)
            {
                float along = Mathf.Clamp(start + e * spacing, -boomLen * 0.45f, boomLen * 0.45f);
                Vector3 local = boom.transform.localRotation * (Vector3.right * along);
                Vector3 worldPos = boom.transform.position + local;

                float elLen = Range(elemLenMin, elemLenMax);
                float elRad = Range(elemRadMin, elemRadMax);
                Quaternion elRot = boom.transform.rotation * Quaternion.Euler(0f, 0f, 90f);

                var elem = Instantiate(rodPrefab, root);
                elem.transform.position = worldPos;
                elem.transform.rotation = elRot;
                elem.transform.localScale = new Vector3(elRad, elLen / primitiveBaseHeight, elRad);
            }
        }

        // ===== Paneles =====
        for (int i = 0; i < panelCount; i++)
        {
            float h = Range(0.4f, mastHeight * 0.95f);
            float yaw = Range(0f, 360f);
            float w = Range(0.6f, 1.2f);
            float hgt = Range(0.6f, 1.2f);
            float armLen = Mathf.Min(0.25f, w * 0.25f);

            var arm = Instantiate(rodPrefab, root);
            arm.transform.localPosition = PointOnMast(h);
            arm.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
            arm.transform.localScale = new Vector3(0.015f, armLen / primitiveBaseHeight, 0.015f);

            Vector3 panelPos = arm.transform.position + (arm.transform.right * armLen);
            var panel = Instantiate(panelPrefab, root);
            panel.transform.position = panelPos;
            panel.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var mf = panel.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh && mf.sharedMesh.name.ToLower().Contains("quad"))
                panel.transform.localScale = new Vector3(w, hgt, 1f);
            else
                panel.transform.localScale = new Vector3(w, hgt, 0.02f);
        }

        // ===== Plato opcional =====
        if (dishPrefab != null && Chance(dishChance))
        {
            float h = Range(0.5f, mastHeight * 0.8f);
            float yaw = Range(0f, 360f);
            float r = Range(0.15f, 0.28f);

            var arm = Instantiate(rodPrefab, root);
            arm.transform.localPosition = PointOnMast(h);
            arm.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
            arm.transform.localScale = new Vector3(0.015f, 0.18f / primitiveBaseHeight, 0.015f);

            Vector3 dishPos = arm.transform.position + (arm.transform.right * 0.18f);
            var dish = Instantiate(dishPrefab, root);
            dish.transform.position = dishPos;
            dish.transform.rotation = Quaternion.Euler(0f, yaw + 10f, 0f);
            dish.transform.localScale = Vector3.one * r;
        }
    }

    public void SetSeedAndRegenerate(int newSeed)
    {
        seed = newSeed;
        Generate();
    }

    // ---- helpers ----
    private void DestroyCurrentRoot()
    {
        if (root != null) { DestroyImmediate(root.gameObject); root = null; }
        else
        {
            var leftover = transform.Find("AntennaRoot");
            if (leftover) DestroyImmediate(leftover.gameObject);
        }
    }

    private int RangeInt(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);
    private float Range(float min, float max) => (float)(rng.NextDouble() * (max - min) + min);
    private bool Chance(float p) => rng.NextDouble() < Mathf.Clamp01(p);
}