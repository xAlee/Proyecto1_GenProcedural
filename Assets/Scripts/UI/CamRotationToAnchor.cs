using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRotationToAnchor : MonoBehaviour
{
    [SerializeField] private GameObject anchor;

    [Header("Rotaci�n")]
    [SerializeField, Tooltip("Velocidad de rotaci�n en grados por segundo")] private float rotationSpeed = 30f;
    [SerializeField, Tooltip("Eje alrededor del cual orbitar (por ejemplo Vector3.up)")] private Vector3 rotationAxis = default;
    [SerializeField, Tooltip("Mantener la c�mara mirando al anchor mientras orbita")] private bool lookAtAnchor = true;

    [Header("Tama�o del �rea (X = width, Z = height)")]
    [SerializeField, Tooltip("Ancho en X")] public float width = 10f;
    [SerializeField, Tooltip("Alto en Z")] public float height = 10f;
    [SerializeField, Tooltip("Desplazamiento vertical del anchor (Y)")] private float anchorYOffset = 0f;

    [Header("Distancia autom�tica")]
    [SerializeField, Tooltip("Factor que escala la distancia calculada a partir de la diagonal")] private float distanceFactor = 1f;
    [SerializeField, Tooltip("Distancia m�nima desde la c�mara al anchor")] private float minDistance = 5f;
    [SerializeField, Tooltip("Distancia m�xima desde la c�mara al anchor")] private float maxDistance = 100f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (anchor == null)
        {
            Debug.LogWarning($"{nameof(CamRotationToAnchor)}: 'anchor' no est� asignado.");
        }

        if (rotationAxis == default)
        {
            rotationAxis = Vector3.up;
        }
    }

    // Usar __LateUpdate__ para c�maras suele dar mejores resultados visuales
    void LateUpdate()
    {
        if (anchor == null) return;

        // 1) Mover el anchor al centro seg�n width (X) y height (Z)
        Vector3 center = new Vector3(width * 0.5f, anchorYOffset, height * 0.5f);
        anchor.transform.position = center;

        // 3) Rotar alrededor del punto 'anchor' a velocidad constante (grados por segundo)
        float angle = rotationSpeed * Time.deltaTime;
        transform.RotateAround(anchor.transform.position, rotationAxis.normalized, angle);

        // 4) Forzar la distancia deseada (mantiene el �ngulo de rotaci�n)
        Vector3 dir = transform.position - anchor.transform.position;

        if (lookAtAnchor)
        {
            // Mantener la c�mara mirando al anchor
            transform.LookAt(anchor.transform);
        }
    }
}