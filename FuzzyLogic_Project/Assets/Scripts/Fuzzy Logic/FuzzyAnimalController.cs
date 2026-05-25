using UnityEngine;
using UnityEngine.AI;

/* NOMBRE CLASE: FuzzyAnimalController
 * AUTOR: Lucía García López
 * FECHA: 06/05/2025
 * DESCRIPCIÓN: Controlador de animales basado en lógica difusa que determina el comportamiento de un animal en función de su miedo, curiosidad y distancia al jugador. 
 *              El animal puede decidir entre huir, acercarse o quedarse quieto.
 */

public class FuzzyAnimalController : MonoBehaviour
{
    public AnimalBase animal;
    public Transform player;

    private NavMeshAgent agent;
    private Animator animator;

    private float currentDistance;
    private float accumulatedFear;
    private float currentCuriosity;
    private float intention;

    [Header("Configuración de Suavizado")]
    public float fuzzyAcceleration = 2.5f;
    public float idleThreshold = 0.15f;

    [Header("Detección del Jugador")]
    public float playerMovementThreshold = 1.5f;
    public float fearMultiplier = 3.0f;

    [Header("Comportamiento de Manada")]
    public float groupSearchRadius = 20f;

    [Header("Indicadores Visuales (Iconos)")]
    public GameObject fearIcon;
    public GameObject curiosityIcon;
    private Action currentDominantAction = Action.Idle;
    private bool fearIconDisplayed = false;
    private bool curiosityIconDisplayed = false;
    private Coroutine fearCoroutineRef;
    private Coroutine curiosityCoroutineRef;

    private Vector3 previousPlayerPosition;
    private float playerSpeed;

    private bool wasIdle = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentCuriosity = 0.5f;

        if (player != null) previousPlayerPosition = player.position;

        if (fearIcon != null) fearIcon.SetActive(false);
        if (curiosityIcon != null) curiosityIcon.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        UpdateInputVariables();
        intention = ProcessFuzzyLogic();
        HandleContinuousMovement(intention);

        //Debug.Log($"Miedo: {accumulatedFear:F2} | Curiosidad: {currentCuriosity:F2} | Intención: {intention:F2}");
        UpdateVisualIcons();
    }

    //Procesa la intención de movimiento y ajusta la velocidad y destino del NavMeshAgent de forma suave, además de actualizar las animaciones correspondientes.
    void HandleContinuousMovement(float intentionValue)
    {
        if (Mathf.Abs(intentionValue) <= idleThreshold)
        {
            if (!wasIdle)
            {
                //Si el animal acaba de entrar en estado de inactividad, elige aleatoriamente entre animación de Idle o Comer
                animator.SetInteger("WaitType", Random.Range(0, 2));
                wasIdle = true;
            }

            agent.isStopped = true;
            agent.speed = Mathf.MoveTowards(agent.speed, 0f, Time.deltaTime * fuzzyAcceleration);

            UpdateAnimations(agent.velocity.magnitude);
        }
        else
        {
            wasIdle = false;
            agent.isStopped = false;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0f;
            dirToPlayer.Normalize();

            Vector3 dynamicDestination;

            if (intentionValue > 0)
            {
                dynamicDestination = player.position - dirToPlayer * 2.0f;
            }
            else
            {
                Vector3 groupCenter = GetNearestGroupCenter();

                if (groupCenter != Vector3.zero)
                {
                    dynamicDestination = groupCenter;
                }
                else
                {
                    dynamicDestination = transform.position - dirToPlayer * 2f;
                }
            }

            agent.SetDestination(dynamicDestination);

            float targetSpeed = Mathf.Abs(intentionValue);

            if (intentionValue < 0 && playerSpeed > playerMovementThreshold)
            {
                float runIntensity = Mathf.InverseLerp(playerMovementThreshold, 5.0f, playerSpeed);
                targetSpeed = Mathf.Lerp(targetSpeed, animal.maxSpeed, runIntensity);
            }

            if (!agent.pathPending && agent.remainingDistance <= 0.15f)
            {
                targetSpeed = 0f;
            }

            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, Time.deltaTime * fuzzyAcceleration);

            UpdateAnimations(agent.velocity.magnitude);
        }
    }

    //Busca otros animales del mismo tipo en un radio definido y calcula el centro de masa de ese grupo para que el animal pueda decidir acercarse a la manada en lugar de al jugador.
    private Vector3 GetNearestGroupCenter()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, groupSearchRadius);
        Vector3 center = Vector3.zero;
        int groupCount = 0;

        foreach (Collider col in nearbyColliders)
        {
            if (col.gameObject == this.gameObject) continue;

            FuzzyAnimalController otherAnimal = col.GetComponent<FuzzyAnimalController>();

            if (otherAnimal != null && otherAnimal.animal == this.animal)
            {
                center += otherAnimal.transform.position;
                groupCount++;
            }
        }

        if (groupCount > 0)
        {
            return center / groupCount;
        }

        return Vector3.zero;
    }

    void UpdateInputVariables()
    {
        currentDistance = Vector3.Distance(transform.position, player.position);

        playerSpeed = (player.position - previousPlayerPosition).magnitude / Time.deltaTime;
        previousPlayerPosition = player.position;

        float stressFactor = Mathf.Clamp01(1 - (currentDistance / animal.detectionRadius));

        if (currentDistance <= animal.detectionRadius)
        {
            if (playerSpeed > playerMovementThreshold)
            {
                accumulatedFear += stressFactor * animal.fearSensitivity * fearMultiplier * Time.deltaTime;
            }
            else if (playerSpeed < 0.1f)
            {
                accumulatedFear -= (animal.fearDecay * 0.5f) * Time.deltaTime;
            }
            else
            {
                accumulatedFear += stressFactor * animal.fearSensitivity * Time.deltaTime;
            }
        }
        else
        {
            accumulatedFear -= animal.fearDecay * Time.deltaTime;
        }

        accumulatedFear = Mathf.Clamp01(accumulatedFear);

        if (accumulatedFear < 0.7f && currentDistance < animal.detectionRadius * 1.5f)
        {
            float impulsoCurioso = (1 - (currentDistance / (animal.detectionRadius * 1.5f))) * 0.5f;
            currentCuriosity += impulsoCurioso * Time.deltaTime;
        }
        else
        {
            currentCuriosity -= animal.curiosityDecay * Time.deltaTime;
        }

        currentCuriosity = Mathf.Clamp01(currentCuriosity);
    }

    //Actualiza los iconos visuales de miedo y curiosidad en función de la acción dominante actual del animal, mostrando el icono correspondiente durante un tiempo limitado cuando el animal está huyendo o acercándose.
    void UpdateVisualIcons()
    {
        bool isFleeingOrRetreating = currentDominantAction == Action.FastFlee || currentDominantAction == Action.SlowRetreat;
        bool isApproaching = currentDominantAction == Action.FastApproach || currentDominantAction == Action.SlowApproach;

        if (isFleeingOrRetreating)
        {
            if (curiosityCoroutineRef != null)
            {
                StopCoroutine(curiosityCoroutineRef);
                curiosityCoroutineRef = null;
            }
            if (curiosityIcon != null) curiosityIcon.SetActive(false);
            curiosityIconDisplayed = false;

            if (!fearIconDisplayed && fearIcon != null)
            {
                fearIconDisplayed = true;
                fearCoroutineRef = StartCoroutine(fearIconTemporarilyDisplayed());
            }
        }
        else
        {
            fearIconDisplayed = false;
        }

        if (isApproaching)
        {
            if (fearCoroutineRef != null)
            {
                StopCoroutine(fearCoroutineRef);
                fearCoroutineRef = null;
            }
            if (fearIcon != null) fearIcon.SetActive(false);
            fearIconDisplayed = false;

            if (!curiosityIconDisplayed && curiosityIcon != null)
            {
                curiosityIconDisplayed = true;
                curiosityCoroutineRef = StartCoroutine(curiosityIconTemporarilyDisplayed());
            }
        }
        else
        {
            curiosityIconDisplayed = false;
        }
    }

    private System.Collections.IEnumerator fearIconTemporarilyDisplayed()
    {
        fearIcon.SetActive(true);
        yield return new WaitForSeconds(3f);
        fearIcon.SetActive(false);
        fearCoroutineRef = null;
    }

    private System.Collections.IEnumerator curiosityIconTemporarilyDisplayed()
    {
        curiosityIcon.SetActive(true);
        yield return new WaitForSeconds(3f);
        curiosityIcon.SetActive(false);
        curiosityCoroutineRef = null;
    }

    //Procesa la lógica difusa combinando las variables de entrada (miedo, curiosidad y distancia) con las reglas definidas en la tabla de reglas para determinar la intención de movimiento del animal.
    float ProcessFuzzyLogic()
    {
        float[] fCuriosity = FuzzifyCuriosity(currentCuriosity);
        float[] fDistance = FuzzifyDistance(currentDistance);
        float[] fFear = FuzzifyFear(accumulatedFear);

        float[] outputWeights = new float[5];
        float[] outputValues = {
            animal.fastFlee,
            animal.slowRetreat,
            animal.idle,
            animal.slowApproach,
            animal.fastApproach
        };

        for (int c = 0; c < 5; c++)
        {
            for (int d = 0; d < 5; d++)
            {
                for (int m = 0; m < 5; m++)
                {
                    float ruleStrength = Mathf.Min(fCuriosity[c], Mathf.Min(fDistance[d], fFear[m]));

                    if (ruleStrength > 0)
                    {
                        int actionIndex = RuleTables.GetAction(c, d, m);
                        outputWeights[actionIndex] = Mathf.Max(outputWeights[actionIndex], ruleStrength);
                    }
                }
            }
        }
        int winningIndex = 2;
        float maxWeight = -1f;

        for (int i = 0; i < 5; i++)
        {
            if (outputWeights[i] > maxWeight)
            {
                maxWeight = outputWeights[i];
                winningIndex = i;
            }
        }
        currentDominantAction = (Action)winningIndex;

        return Defuzzification.Defuzzify(outputWeights, outputValues);
    }

    //Funciones de fuzzificación para cada variable de entrada, utilizando funciones de membresía adecuadas para representar los diferentes niveles de miedo, distancia y curiosidad.
    private float[] FuzzifyFear(float valor)
    {
        float[] degrees = new float[5];
        degrees[(int)FearTag.Relaxed] = MembershipFunction.LeftShoulder(valor, 0.1f, 0.3f);
        degrees[(int)FearTag.Cautious] = MembershipFunction.Triangle(valor, 0.2f, 0.4f, 0.6f);
        degrees[(int)FearTag.Alert] = MembershipFunction.Trapezoid(valor, 0.3f, 0.45f, 0.7f, 0.9f);
        degrees[(int)FearTag.Scared] = MembershipFunction.Triangle(valor, 0.6f, 0.8f, 0.9f);
        degrees[(int)FearTag.Panic] = MembershipFunction.RightShoulder(valor, 0.8f, 1.0f);
        return degrees;
    }

    private float[] FuzzifyDistance(float valor)
    {
        float[] degrees = new float[5];
        degrees[(int)DistanceTag.VeryClose] = MembershipFunction.LeftShoulder(valor, 2f, 5f);
        degrees[(int)DistanceTag.Close] = MembershipFunction.Triangle(valor, 3f, 8f, 12f);
        degrees[(int)DistanceTag.Medium] = MembershipFunction.Trapezoid(valor, 10f, 15f, 20f, 25f);
        degrees[(int)DistanceTag.Far] = MembershipFunction.Triangle(valor, 18f, 25f, 30f);
        degrees[(int)DistanceTag.VeryFar] = MembershipFunction.RightShoulder(valor, 28f, 35f);
        return degrees;
    }

    private float[] FuzzifyCuriosity(float valor)
    {
        float[] degrees = new float[5];
        degrees[(int)CuriosityTag.None] = MembershipFunction.LeftShoulder(valor, 0.1f, 0.3f);
        degrees[(int)CuriosityTag.Low] = MembershipFunction.Triangle(valor, 0.2f, 0.4f, 0.6f);
        degrees[(int)CuriosityTag.Medium] = MembershipFunction.Trapezoid(valor, 0.3f, 0.5f, 0.7f, 0.9f);
        degrees[(int)CuriosityTag.High] = MembershipFunction.Triangle(valor, 0.6f, 0.8f, 0.9f);
        degrees[(int)CuriosityTag.Extreme] = MembershipFunction.RightShoulder(valor, 0.8f, 1.0f);
        return degrees;
    }

    void UpdateAnimations(float speed)
    {
        animator.SetBool("IsMoving", speed > 0.1f);
        animator.SetFloat("MovementSpeed", speed / 2f);
    }
}