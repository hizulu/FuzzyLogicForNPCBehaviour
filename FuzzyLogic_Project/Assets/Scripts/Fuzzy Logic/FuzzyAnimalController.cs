using UnityEngine;
using UnityEngine.AI;

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

    private Vector3 previousPlayerPosition;
    private float playerSpeed;

    private bool wasIdle = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentCuriosity = 0.5f;

        if (player != null) previousPlayerPosition = player.position;
    }

    void Update()
    {
        if (player == null) return;

        UpdateInputVariables();
        intention = ProcessFuzzyLogic();
        HandleContinuousMovement(intention);

        Debug.Log($"Miedo: {accumulatedFear:F2} | Curiosidad: {currentCuriosity:F2} | Intención: {intention:F2}");
    }

    void HandleContinuousMovement(float intentionValue)
    {
        if (Mathf.Abs(intentionValue) <= idleThreshold)
        {
            if (!wasIdle)
            {
                animator.SetInteger("WaitType", Random.Range(0, 2));
                wasIdle = true;
            }

            agent.isStopped = true;
            agent.speed = Mathf.MoveTowards(agent.speed, 0f, Time.deltaTime * fuzzyAcceleration);
            UpdateAnimations(agent.speed);
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
                //APROXIMACIÓN: El destino es la posición del jugador MENOS 2 metro de distancia
                dynamicDestination = player.position - dirToPlayer * 2.0f;
            }
            else
            {
                //HUIDA: El destino es la posición actual del animal MÁS 2 metros alejándose del jugador
                dynamicDestination = transform.position - dirToPlayer * 2f;
            }

            agent.SetDestination(dynamicDestination);

            float targetSpeed = Mathf.Abs(intentionValue);

            if (intentionValue < 0 && playerSpeed > playerMovementThreshold)
            {
                float runIntensity = Mathf.InverseLerp(playerMovementThreshold, 5.0f, playerSpeed);
                targetSpeed = Mathf.Lerp(targetSpeed, animal.maxSpeed, runIntensity);
            }

            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, Time.deltaTime * fuzzyAcceleration);
            UpdateAnimations(agent.speed);
        }
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

        return Defuzzification.Defuzzify(outputWeights, outputValues);
    }

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