using UnityEngine;

/* NOMBRE CLASE: MembershipFunction
 * AUTOR: Lucía García López
 * FECHA: 06/05/2025
 * DESCRIPCIÓN: Define las funciones de membresía para la lógica difusa, incluyendo booleanas, triángulos, trapecios y de grado.
 */

public static class MembershipFunction
{
    //Forma: _|¯
    public static float Boolean(float value, float x0)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 0f;
        else
            membership = 1f;

        return membership;
    }

    //Forma: ¯|_
    public static float InverseBoolean(float value, float x0)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 1f;
        else
            membership = 0f;

        return membership;
    }

    //Forma: ¯\_
    public static float LeftShoulder(float value, float x0, float x1)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 1f;
        else if (value > x0 && value < x1)
            membership = (-value / (x1 - x0)) + (x1 / (x1 - x0));
        else if (value >= x1)
            membership = 0f;

        return membership;
    }

    //Forma: _/¯
    public static float RightShoulder(float value, float x0, float x1)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 0f;
        else if (value > x0 && value < x1)
            membership = (value / (x1 - x0)) - (x0 / (x1 - x0));
        else if (value >= x1)
            membership = 1f;

        return membership;
    }

    //Forma: _/\_
    public static float Triangle(float value, float x0, float x1, float x2)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 0f;
        else if (value > x0 && value < x1)
            membership = (value / (x1 - x0)) - (x0 / (x1 - x0));
        else if (value == x1)
            membership = 1f;
        else if (value > x1 && value < x2)
            membership = (-value / (x2 - x1)) + (x2 / (x2 - x1));
        else if (value >= x2)
            membership = 0f;

        return membership;
    }

    //Forma: _/¯\_
    public static float Trapezoid(float value, float x0, float x1, float x2, float x3)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 0f;
        else if (value > x0 && value < x1)
            membership = (value / (x1 - x0)) - (x0 / (x1 - x0));
        else if (value >= x1 && value <= x2)
            membership = 1f;
        else if (value > x2 && value < x3)
            membership = (-value / (x3 - x2)) + (x3 / (x3 - x2));
        else if (value >= x3)
            membership = 0f;

        return membership;
    }
}
