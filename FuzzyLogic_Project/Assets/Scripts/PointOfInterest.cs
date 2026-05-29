using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    [Header("Configuración del área donde hay Comida")]
    public float radius = 5f;

    [Header("Filtro de Exclusividad")]

    //Permite que ciertos puntos solo sean interesantes para algunos animales
    public List<AnimalBase> allowedAnimalTypes = new List<AnimalBase>();
    public bool CanAnimalEat(AnimalBase animalType)
    {
        if (allowedAnimalTypes.Count == 0) return true;
        if (animalType == null) return false;
        return allowedAnimalTypes.Contains(animalType);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
