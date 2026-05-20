using UnityEngine;

public enum DistanceTag { VeryClose = 0, Close = 1, Medium = 2, Far = 3, VeryFar = 4 }
public enum FearTag { Relaxed = 0, Cautious = 1, Alert = 2, Scared = 3, Panic = 4 }
public enum CuriosityTag { None = 0, Low = 1, Medium = 2, High = 3, Extreme = 4 }
public enum Action { FastFlee = 0, SlowRetreat = 1, Idle = 2, SlowApproach = 3, FastApproach = 4 }

public static class RuleTables
{
    public static int GetAction(int curiosity, int distance, int fear)
    {
        //1. PRIORIDAD ABSOLUTA: Miedo extremo (Supervivencia)
        if (fear == (int)FearTag.Panic) return (int)Action.FastFlee;
        if (fear == (int)FearTag.Scared)
        {
            return (distance <= (int)DistanceTag.Medium) ? (int)Action.FastFlee : (int)Action.SlowRetreat;
        }

        //2. Proximidad Crítica (Muy cerca)
        if (distance == (int)DistanceTag.VeryClose)
        {
            if (curiosity >= (int)CuriosityTag.Medium && fear <= (int)FearTag.Cautious)
                return (int)Action.Idle;

            return (int)Action.FastFlee;
        }

        //3. High Curiosidad vs Cautela
        if (curiosity == (int)CuriosityTag.Extreme) return (int)Action.FastApproach;
        if (curiosity >= (int)CuriosityTag.High && fear <= (int)FearTag.Cautious)
        {
            return (distance >= (int)DistanceTag.Far) ? (int)Action.FastApproach : (int)Action.SlowApproach;
        }

        //4. Miedo moderado en distancias normales
        if (fear >= (int)FearTag.Alert) return (int)Action.SlowRetreat;

        //5. Comportamiento por defecto (Low curiosity o equilibrio)
        if (curiosity <= (int)CuriosityTag.Low && distance <= (int)DistanceTag.Medium)
        {
            return (int)Action.SlowRetreat;
        }

        return (int)Action.Idle;
    }
}