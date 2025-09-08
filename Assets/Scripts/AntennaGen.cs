using UnityEngine;
using System.Collections.Generic;

public class AntennaGen : MonoBehaviour
{
    [Header("Prefabs (usa primitives si quieres)")]
    [SerializeField] private GameObject mastPrefab;   // cilindro para el mástil
    [SerializeField] private GameObject rodPrefab;    // cilindro delgado para brazos/boom/perchas
    [SerializeField] private GameObject panelPrefab;  // quad/cubo delgado para “rejilla/panel”
    [SerializeField] private GameObject dishPrefab;   // esfera/semiesfera opcional (puedes dejar null)

    [Header("Mástil")]
    [SerializeField] private float mastHeight = 4f;
    [SerializeField] private float mastRadius = 0.05f;
    [SerializeField] private int mastSegments = 2;           // segmentos apilados
    [SerializeField] private float primitiveBaseHeight = 2f; // 2 para Cylinder, 1 para Cube

    [Header("Elementos (rangos)")]
    [SerializeField] private Vector2Int crossbarCount = new Vector2Int(2, 5);   // brazos sueltos
    [SerializeField] private Vector2 crossbarLen = new Vector2(0.6f, 1.4f);
    [SerializeField] private Vector2 crossbarRadius = new Vector2(0.015f, 0.03f);

    [SerializeField] private Vector2Int yagiCount = new Vector2Int(1, 3);       // antenas tipo Yagi
    [SerializeField] private Vector2 yagiBoomLen = new Vector2(1.2f, 2.2f);
    [SerializeField] private Vector2Int yagiElements = new Vector2Int(4, 8);
    [SerializeField] private Vector2 yagiElementSpacing = new Vector2(0.12f, 0.22f);
    [SerializeField] private Vector2 yagiElementRadius = new Vector2(0.01f, 0.02f);
    [SerializeField] private Vector2 yagiElementLen = new Vector2(0.25f, 0.5f);

    [SerializeField] private Vector2Int panelCount = new Vector2Int(0, 2);       // paneles/“rejillas”
    [SerializeField] private Vector2 panelSize = new Vector2(0.6f, 1.2f);        // ancho/alto (se elige aleatorio dentro)

    [Header("Extras")]
    [SerializeField, Range(0f, 1f)] private float dishChance = 0.15f; // prob de plato pequeño
    [SerializeField] private Vector2 dishRadius = new Vector2(0.15f, 0.28f);

    [Header("Aleatoriedad")]
    public bool useSeed = true;
    public int seed = 12345;

    void Start()
    {
        if (useSeed) Random.InitState(seed);
        Generate();
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        // limpia anteriores
        var old = transform.Find("AntennaRoot");
        if (old) DestroyImmediate(old.gameObject);

        Transform root = new GameObject("AntennaRoot").transform;
        root.SetParent(transform, false);

        // ===== Mástil =====
        Transform mastParent = new GameObject("Mast").transform;
        mastParent.SetParent(root, false);

        float segH = mastHeight / Mathf.Max(1, mastSegments);
        float currentY = segH * 0.5f;

        for (int i = 0; i < mastSegments; i++)
        {
            GameObject seg = Instantiate(mastPrefab, mastParent);
            seg.transform.localPosition = new Vector3(0f, currentY, 0f);
            seg.transform.localRotation = Quaternion.identity;
            // Cylinder: altura real = 2 * scale.y ? scale.y = segH/2
            seg.transform.localScale = new Vector3(mastRadius, segH / primitiveBaseHeight, mastRadius);
            currentY += segH;
        }

        // Helper: devuelve un punto a cierta altura del mástil
        System.Func<float, Vector3> PointOnMast = (h) => new Vector3(0f, Mathf.Clamp(h, 0f, mastHeight), 0f);

        // ===== Brazos sueltos =====
        int cCount = Random.Range(crossbarCount.x, crossbarCount.y + 1);
        for (int i = 0; i < cCount; i++)
        {
            float h = Random.Range(0.3f, mastHeight * 0.95f);
            float len = Random.Range(crossbarLen.x, crossbarLen.y);
            float r = Random.Range(crossbarRadius.x, crossbarRadius.y);
            float yaw = Random.Range(0f, 360f);

            Vector3 pos = PointOnMast(h);
            Quaternion rot = Quaternion.Euler(0f, yaw, 90f); // cilindro “acostado”

            GameObject arm = Instantiate(rodPrefab, root);
            arm.transform.localPosition = pos;
            arm.transform.localRotation = rot;
            arm.transform.localScale = new Vector3(r, len / primitiveBaseHeight, r);
        }

        // ===== Yagi (boom + elementos perpendiculares) =====
        int yCount = Random.Range(yagiCount.x, yagiCount.y + 1);
        for (int i = 0; i < yCount; i++)
        {
            float h = Random.Range(0.35f, mastHeight * 0.9f);
            float boomLen = Random.Range(yagiBoomLen.x, yagiBoomLen.y);
            float yaw = Random.Range(0f, 360f);

            // Boom (barra principal)
            GameObject boom = Instantiate(rodPrefab, root);
            boom.transform.localPosition = PointOnMast(h);
            boom.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
            boom.transform.localScale = new Vector3(0.02f, boomLen / primitiveBaseHeight, 0.02f);

            // Elementos
            int eCount = Random.Range(yagiElements.x, yagiElements.y + 1);
            float spacing = Random.Range(yagiElementSpacing.x, yagiElementSpacing.y);
            float start = -boomLen * 0.5f + spacing;

            for (int e = 0; e < eCount; e++)
            {
                float along = Mathf.Clamp(start + e * spacing, -boomLen * 0.45f, boomLen * 0.45f);

                // posición local del elemento a lo largo del boom
                Vector3 local = boom.transform.localRotation * (Vector3.right * along);
                Vector3 worldPos = boom.transform.position + local;

                float elLen = Random.Range(yagiElementLen.x, yagiElementLen.y);
                float elRad = Random.Range(yagiElementRadius.x, yagiElementRadius.y);

                // Rod perpendicular al boom: rotar 90° alrededor del eje longitudinal del boom
                Quaternion elRot = boom.transform.rotation * Quaternion.Euler(0f, 0f, 90f);

                GameObject elem = Instantiate(rodPrefab, root);
                elem.transform.position = worldPos;
                elem.transform.rotation = elRot;
                elem.transform.localScale = new Vector3(elRad, elLen / primitiveBaseHeight, elRad);
            }
        }

        // ===== Paneles (rectángulos con un pequeño brazo de sujeción) =====
        int pCount = Random.Range(panelCount.x, panelCount.y + 1);
        for (int i = 0; i < pCount; i++)
        {
            float h = Random.Range(0.4f, mastHeight * 0.95f);
            float yaw = Random.Range(0f, 360f);
            float w = Random.Range(panelSize.x, panelSize.y);
            float hgt = Random.Range(panelSize.x, panelSize.y);
            float armLen = Mathf.Min(0.25f, w * 0.25f);

            // Brazo del panel
            GameObject arm = Instantiate(rodPrefab, root);
            arm.transform.localPosition = PointOnMast(h);
            arm.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
            arm.transform.localScale = new Vector3(0.015f, armLen / primitiveBaseHeight, 0.015f);

            // Panel al final del brazo
            Vector3 panelPos = arm.transform.position + (arm.transform.right * armLen);
            GameObject panel = Instantiate(panelPrefab, root);
            panel.transform.position = panelPos;
            panel.transform.rotation = Quaternion.Euler(0f, yaw, 0f); // mirar “hacia afuera”

            // Si es Quad: scale.x = ancho, scale.y = alto (usa MeshRenderer.bounds para ajustar si quieres)
            // Si es Cube: hazlo muy delgado en Z
            var mf = panel.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh && mf.sharedMesh.name.ToLower().Contains("quad"))
            {
                panel.transform.localScale = new Vector3(w, hgt, 1f);
            }
            else
            {
                // Cube u otro: thin panel
                panel.transform.localScale = new Vector3(w, hgt, 0.02f);
            }
        }

        // ===== Plato opcional =====
        if (dishPrefab != null && Random.value < dishChance)
        {
            float h = Random.Range(0.5f, mastHeight * 0.8f);
            float yaw = Random.Range(0f, 360f);
            float r = Random.Range(dishRadius.x, dishRadius.y);

            // pequeño brazo
            GameObject arm = Instantiate(rodPrefab, root);
            arm.transform.localPosition = PointOnMast(h);
            arm.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
            arm.transform.localScale = new Vector3(0.015f, 0.18f / primitiveBaseHeight, 0.015f);

            // plato al final
            Vector3 dishPos = arm.transform.position + (arm.transform.right * 0.18f);
            GameObject dish = Instantiate(dishPrefab, root);
            dish.transform.position = dishPos;
            dish.transform.rotation = Quaternion.Euler(0f, yaw + 10f, 0f);
            dish.transform.localScale = Vector3.one * r;
        }
    }
}
