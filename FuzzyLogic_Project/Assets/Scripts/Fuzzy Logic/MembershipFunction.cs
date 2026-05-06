using UnityEngine;

public class MembershipFunction: MonoBehaviour
{
    public static float Boolean(float value, float x0)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 0f;
        else
            membership = 1f;

        return membership;
    }

    public static float InverseBoolean(float value, float x0)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 1f;
        else
            membership = 0f;

        return membership;
    }

    public static float LeftShoulder(float value, float x0, float x1)
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

    public static float RightShoulder(float value, float x0, float x1)
    {
        float membership = 0f;

        if (value <= x0)
            membership = 1f;
        else if (value > x0 && value < x1)
            membership = (-value / (x1 - x0)) + (x0 / (x1 - x0));
        else if (value >= x1)
            membership = 0f;

        return membership;
    }

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
