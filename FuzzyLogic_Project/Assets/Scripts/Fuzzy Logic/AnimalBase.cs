using UnityEngine;

/* NOMBRE CLASE: AnimalBase
 * AUTOR: Lucía García López
 * FECHA: 24/03/2025
 * DESCRIPCIÓN: Define los atributos base de un animal.
 */

[CreateAssetMenu(fileName = "AnimalBase", menuName = "Scriptable Objects/New Animal")]
public class AnimalBase : ScriptableObject
{
    [Header("Distance")]
    public float detectionRadius;

    [Header("Fear")]
    public float fearSensitivity;
    public float fearDecay;

    [Header("Curiosity")]
    public float curiosityDisinterest;
    public float curiosityDecay;

    [Header("Speed")]
    public float baseSpeed;
    public float fleeSpeed;
    public float maxSpeed;

    [Header("Outputs")]
    public float fastFlee;
    public float slowRetreat;
    public float idle;
    public float slowApproach;
    public float fastApproach;
}
