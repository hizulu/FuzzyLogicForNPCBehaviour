using UnityEngine;

/* NOMBRE CLASE: AnimalBase
 * AUTOR: Lucía García López
 * FECHA: 24/03/2025
 * DESCRIPCIÓN: Define los atributos base de un animal.
 * VERSIÓN: 1.0
 */

[CreateAssetMenu(fileName = "AnimalBase", menuName = "Scriptable Objects/Nuevo Perfil Animal")]
public class AnimalBase : ScriptableObject
{
    [Header("Distancia")]
    public float radioDeteccion;

    [Header("Miedo")]
    public float sensibilidadMiedo;
    public float decaimientoMiedo;

    [Header("Curiosidad")]
    public float desinteresCuriosidad;
    public float decaimientoCuriosidad;

    [Header("Velocidad")]
    public float velocidadBase;
    public float velocidadHuida;
}
