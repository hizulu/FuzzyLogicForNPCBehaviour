using UnityEngine;

/* NOMBRE CLASE: Defuzzification
 * AUTOR: Lucía García López
 * FECHA: 06/05/2025
 * DESCRIPCIÓN: Implementa el proceso de defuzzificación utilizando el método singleton para convertir los grados de pertenencia difusos en un valor concreto que determine la acción del animal.
 */

public static class Defuzzification
{
    public static float Defuzzify(float[] membershipGrades, float[] targetValues)
    {
        //Si hay 3 grados de pertenencia pero solo 2 valores objetivo hay un error
        if (membershipGrades.Length != targetValues.Length || membershipGrades.Length == 0)
        {
            Debug.LogError("Las listas de membershipGrades y targetValues deben tener el mismo tamaño.");
            return 0f;
        }

        float sumNumerator = 0f;
        float sumDenominator = 0f;

        //Sumatorio
        for (int i = 0; i < membershipGrades.Length; i++)
        {
            sumNumerator += membershipGrades[i] * targetValues[i];
            sumDenominator += membershipGrades[i];
        }

        //Prevención de división por cero si ninguna regla se activó
        if (sumDenominator == 0) return 0f;

        return sumNumerator / sumDenominator;
    }
}
