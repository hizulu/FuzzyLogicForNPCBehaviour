using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    [Header("Configuración del área donde hay Comida")]
    public float radius = 5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
