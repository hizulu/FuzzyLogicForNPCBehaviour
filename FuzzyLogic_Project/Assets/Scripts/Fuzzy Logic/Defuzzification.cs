using UnityEngine;

public class Defuzzification : MonoBehaviour
{
    public static float Defuzzify(float[] membershipGrades, float[] targetValues)
    {
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

        // Prevención de división por cero si ninguna regla se activó
        if (sumDenominator == 0) return 0f;

        return sumNumerator / sumDenominator;
    }
}
