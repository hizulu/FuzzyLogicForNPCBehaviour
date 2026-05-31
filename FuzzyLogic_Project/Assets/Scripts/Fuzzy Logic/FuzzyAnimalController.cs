using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("Ajustes de Patrulla Autónoma")]
    public Transform homePoint;
    private Vector3 actualHomePosition;
    public float areaRadius = 10f;
    public float minWaitingTime = 3f;
    public float maxWaitingTime = 7f;
    public float walkSpeed = 2f;

    private float waitingTimer;
    private bool isWaiting = true;
    private bool isPatrolling = false;

    [Header("Puntos de Interés (Comida)")]
    public float foodSearchRadius = 15f;
    public float foodInteractionRadius = 2.0f;
    public float foodCooldownDuration = 20f;
    private float foodCooldownTimer = 0f;
    private float eatingTimer;

    [Header("Interacciones entre animales")]
    public List<AnimalBase> predators;
    public List<AnimalBase> prey;
    Transform bestPrey;

    private Vector3 previousPlayerPosition;
    private float playerSpeed;
    private Vector3 playerMovementDir;

    private bool wasIdle = false;
    private float stuckTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentCuriosity = 0.5f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        if (homePoint != null)
            actualHomePosition = homePoint.position;
        else
            actualHomePosition = transform.position;

        if (player != null) previousPlayerPosition = player.position;

        if (fearIcon != null) fearIcon.SetActive(false);
        if (curiosityIcon != null) curiosityIcon.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        if (foodCooldownTimer > 0f)
        {
            foodCooldownTimer -= Time.deltaTime;
        }

        UpdateInputVariables();

        Transform nearestPredator = GetNearestAnimal(predators);
        bestPrey = GetIsolatedPrey(prey);

        if (currentDistance <= animal.detectionRadius || nearestPredator != null || bestPrey != null)
        {
            if (isPatrolling)
            {
                isPatrolling = false;
                isWaiting = false;
                animator.SetInteger("WaitType", -1);
            }
            intention = ProcessFuzzyLogic();
            HandleContinuousMovement(intention, nearestPredator, bestPrey);
        }
        else
        {
            currentDominantAction = Action.Idle;
            ExecutePatrolBehavior();
        }

        UpdateVisualIcons();
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        Vector3 targetRotationForward = Camera.main.transform.rotation * Vector3.forward;
        Vector3 targetRotationUp = Camera.main.transform.rotation * Vector3.up;

        if (fearIcon != null && fearIcon.activeSelf)
        {
            fearIcon.transform.LookAt(fearIcon.transform.position + targetRotationForward, targetRotationUp);
        }

        if (curiosityIcon != null && curiosityIcon.activeSelf)
        {
            curiosityIcon.transform.LookAt(curiosityIcon.transform.position + targetRotationForward, targetRotationUp);
        }
    }

    #region Patrulla
    //Ejecuta el comportamiento de patrulla autónoma cuando el jugador está fuera del rango de detección, haciendo que el animal se mueva aleatoriamente dentro de un área definida, alternando entre caminar y esperar con animaciones correspondientes.
    void ExecutePatrolBehavior()
    {
        if (!isPatrolling)
        {
            isPatrolling = true;
            StartWait();
        }

        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;

        if (!arrived && !isWaiting)
        {
            if (agent.velocity.magnitude < 0.1f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 1.0f)
                {
                    stuckTimer = 0f;
                    FindNewDestination();
                    return;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            Transform closestFood = GetClosestFoodSource();
            if (closestFood != null)
            {
                float distanceToFood = Vector3.Distance(transform.position, closestFood.position);
                if (distanceToFood <= 3.5f && agent.velocity.magnitude < 0.2f)
                {
                    arrived = true;
                }
            }
        }

        if (arrived)
        {
            if (!isWaiting) StartWait();
        }

        if (isWaiting)
        {
            UpdateWaiting();
        }

        UpdateAnimations(agent.velocity.magnitude);
    }

    void StartWait()
    {
        isWaiting = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        waitingTimer = Random.Range(minWaitingTime, maxWaitingTime);
        Transform closestFood = GetClosestFoodSource();
        if (closestFood != null && Vector3.Distance(transform.position, closestFood.position) <= 3.5f)
        {
            animator.SetInteger("WaitType", 1);
        }
        else
        {
            animator.SetInteger("WaitType", Random.Range(0, 2));
        }
    }

    void UpdateWaiting()
    {
        waitingTimer -= Time.deltaTime;

        if (waitingTimer <= 0)
        {
            if (animator.GetInteger("WaitType") == 1)
            {
                foodCooldownTimer = foodCooldownDuration;
            }
            FindNewDestination();
        }
    }

    void FindNewDestination()
    {
        // En la patrulla, la prioridad es buscar comida
        Transform closestFood = GetClosestFoodSource();
        if (closestFood != null)
        {
            agent.isStopped = false;
            agent.SetDestination(closestFood.position);
            agent.speed = walkSpeed;
            isWaiting = false;
            return;
        }

        Vector3 finalDestination = actualHomePosition;
        bool foundValidPoint = false;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = actualHomePosition + Random.insideUnitSphere * (areaRadius - 3f);
            randomPoint.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 3.0f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) > 2f)
                {
                    finalDestination = hit.position;
                    foundValidPoint = true;
                    break;
                }
            }
        }

        if (!foundValidPoint)
        {
            finalDestination = actualHomePosition;
        }

        stuckTimer = 0f;
        agent.isStopped = false;
        agent.SetDestination(finalDestination);
        agent.speed = walkSpeed;

        isWaiting = false;
    }
    #endregion

    //Determina la acción de movimiento continua del animal en función de la intención calculada por la lógica difusa, priorizando huir de depredadores, luego huir del jugador, luego acechar presas, luego buscar comida, y finalmente patrullar o quedarse quieto según corresponda.
    void HandleContinuousMovement(float intentionValue, Transform nearestPredator, Transform nearestPrey)
    {
        //PRIORIDAD 1: HUIR DE UN DEPREDADOR
        if (nearestPredator != null)
        {
            if (wasIdle)
            {
                animator.SetInteger("WaitType", -1);
                wasIdle = false;
                agent.speed = animal.maxSpeed;
            }
            agent.isStopped = false;
            currentDominantAction = Action.FastFlee;

            float distToHome = Vector3.Distance(transform.position, actualHomePosition);
            float distToPlayer = Vector3.Distance(transform.position, player.position);

            Vector3 refuge = (distToHome < distToPlayer) ? actualHomePosition : player.position;

            Vector3 dirToRefuge = (refuge - transform.position).normalized;
            Vector3 dirToPredator = (nearestPredator.position - transform.position).normalized;

            Vector3 escapeDestination;

            if (Vector3.Dot(dirToRefuge, dirToPredator) > 0.3f)
            {
                escapeDestination = transform.position - dirToPredator * 10f;
            }
            else
            {
                escapeDestination = refuge;
            }

            agent.SetDestination(escapeDestination);
            agent.speed = Mathf.MoveTowards(agent.speed, animal.maxSpeed, Time.deltaTime * fuzzyAcceleration);
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 2: HUIR DEL JUGADOR
        bool isAfraidOfPlayer = false;

        if (nearestPrey != null)
        {
            isAfraidOfPlayer = false;
        }
        else
        {
            isAfraidOfPlayer = accumulatedFear > 0.45f || (intentionValue < 0 && nearestPrey == null);
        }

        if (isAfraidOfPlayer)
        {
            if (wasIdle) { animator.SetInteger("WaitType", -1); wasIdle = false; }
            agent.isStopped = false;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0f;
            dirToPlayer.Normalize();

            Vector3 playerToHome = actualHomePosition - player.position;
            playerToHome.y = 0f;

            bool playerHeadingToHome = false;

            if (playerSpeed > 0.5f && playerToHome.sqrMagnitude > 1.0f)
            {
                float dotProduct = Vector3.Dot(playerMovementDir, playerToHome.normalized);
                if (dotProduct > 0.5f) playerHeadingToHome = true;
            }

            Vector3 dynamicDestination;

            if (!playerHeadingToHome)
            {
                dynamicDestination = actualHomePosition;
            }
            else
            {
                Vector3 groupCenter = GetNearestGroupCenter();
                dynamicDestination = (groupCenter != Vector3.zero) ? groupCenter : (transform.position - dirToPlayer * 4f);
            }

            agent.SetDestination(dynamicDestination);

            float targetSpeed = Mathf.Abs(intentionValue);
            if (playerSpeed > playerMovementThreshold)
            {
                float runIntensity = Mathf.InverseLerp(playerMovementThreshold, 5.0f, playerSpeed);
                targetSpeed = Mathf.Lerp(targetSpeed, animal.maxSpeed, runIntensity);
            }

            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, Time.deltaTime * fuzzyAcceleration);
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 3: CAZAR (Zorro persigue Gallinas)
        if (nearestPrey != null)
        {
            if (wasIdle)
            {
                animator.SetInteger("WaitType", -1);
                wasIdle = false;
                agent.speed = animal.baseSpeed;
            }
            agent.isStopped = false;
            currentDominantAction = Action.FastApproach;

            agent.SetDestination(nearestPrey.position);

            agent.speed = Mathf.Lerp(agent.speed, animal.maxSpeed * 0.5f, Time.deltaTime * fuzzyAcceleration * 2f);
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 4: COMER EN LOS PUNTOS DE INTERÉS
        Transform closestFood = GetClosestFoodSource();
        if (closestFood != null)
        {
            float distanceToFood = Vector3.Distance(transform.position, closestFood.position);

            bool arrivedAtFood = (distanceToFood <= foodInteractionRadius) ||
                                 (distanceToFood <= 3.5f && !agent.pathPending && agent.velocity.magnitude < 0.2f);

            if (arrivedAtFood)
            {
                if (!wasIdle)
                {
                    animator.SetInteger("WaitType", 1);
                    wasIdle = true;
                    eatingTimer = Random.Range(minWaitingTime, maxWaitingTime);
                }
                agent.isStopped = true;
                agent.speed = Mathf.MoveTowards(agent.speed, 0f, Time.deltaTime * fuzzyAcceleration);

                eatingTimer -= Time.deltaTime;
                if (eatingTimer <= 0f)
                {
                    foodCooldownTimer = foodCooldownDuration;
                    wasIdle = false;
                }
            }
            else
            {
                if (wasIdle) { animator.SetInteger("WaitType", -1); wasIdle = false; }
                agent.isStopped = false;
                agent.SetDestination(closestFood.position);
                agent.speed = Mathf.MoveTowards(agent.speed, walkSpeed, Time.deltaTime * fuzzyAcceleration);
            }
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 5: COMPORTAMIENTO NORMAL
        if (Mathf.Abs(intentionValue) <= idleThreshold)
        {
            if (!wasIdle)
            {
                animator.SetInteger("WaitType", Random.Range(0, 2));
                wasIdle = true;
            }
            agent.isStopped = true;
            agent.speed = Mathf.MoveTowards(agent.speed, 0f, Time.deltaTime * fuzzyAcceleration);
        }
        else
        {
            if (wasIdle) { animator.SetInteger("WaitType", -1); wasIdle = false; }
            agent.isStopped = false;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0f;
            dirToPlayer.Normalize();

            Vector3 dynamicDestination = player.position - dirToPlayer * 2.0f;
            agent.SetDestination(dynamicDestination);

            agent.speed = Mathf.MoveTowards(agent.speed, intentionValue, Time.deltaTime * fuzzyAcceleration);
        }
        UpdateAnimations(agent.velocity.magnitude);
    }

    //Busca el animal más cercano de una lista dada (depredadores o presas) dentro de un radio de detección, para que el animal pueda decidir huir de depredadores o acechar presas según corresponda.
    private Transform GetNearestAnimal(List<AnimalBase> targetList)
    {
        if (targetList == null || targetList.Count == 0) return null;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, animal.detectionRadius);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in nearbyColliders)
        {
            if (col.gameObject == this.gameObject) continue;

            FuzzyAnimalController otherAnimal = col.GetComponentInParent<FuzzyAnimalController>();

            if (otherAnimal != null && targetList.Contains(otherAnimal.animal))
            {
                float distance = Vector3.Distance(transform.position, otherAnimal.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = otherAnimal.transform;
                }
            }
        }
        return bestTarget;
    }

    //Busca presas dentro de un radio de detección y evalúa cuántos aliados del mismo tipo hay cerca de cada presa para priorizar atacar a las presas más aisladas, evitando enfrentarse a grupos grandes de presas que podrían representar un riesgo o ser menos rentables.
    private Transform GetIsolatedPrey(List<AnimalBase> targetList)
    {
        if (targetList == null || targetList.Count == 0) return null;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, animal.detectionRadius);
        Transform bestPrey = null;
        float bestScore = Mathf.Infinity;

        foreach (Collider col in nearbyColliders)
        {
            if (col.gameObject == this.gameObject) continue;

            FuzzyAnimalController otherAnimal = col.GetComponentInParent<FuzzyAnimalController>();

            if (otherAnimal != null && targetList.Contains(otherAnimal.animal))
            {
                int alliesNearby = CountAlliesNear(otherAnimal, 7f);
                float distanceToPrey = Vector3.Distance(transform.position, otherAnimal.transform.position);
                float score = distanceToPrey + (alliesNearby * 30f);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPrey = otherAnimal.transform;
                }
            }
        }
        return bestPrey;
    }

    //Cuenta cuántos animales del mismo tipo hay cerca de un objetivo dado dentro de un radio específico, para que el animal pueda evaluar si una presa está aislada o si un grupo de aliados está cerca para decidir su comportamiento.
    private int CountAlliesNear(FuzzyAnimalController targetAnimal, float radius)
    {
        Collider[] nearby = Physics.OverlapSphere(targetAnimal.transform.position, radius);
        int count = 0;
        foreach (Collider col in nearby)
        {
            if (col.gameObject == targetAnimal.gameObject) continue;

            FuzzyAnimalController other = col.GetComponentInParent<FuzzyAnimalController>();

            if (other != null && other.animal == targetAnimal.animal)
            {
                count++;
            }
        }
        return count;
    }

    //Busca comida dentro de un radio definido alrededor del animal, priorizando las fuentes de comida que estén dentro de su área de patrulla y que no estén en cooldown, para que el animal pueda decidir ir a comer en lugar de interactuar con el jugador o patrullar.
    private Transform GetClosestFoodSource()
    {
        if (foodCooldownTimer > 0f) return null;

        //Si está muy lejos de casa, prioriza volver
        float distanceToHome = Vector3.Distance(transform.position, actualHomePosition);
        if (distanceToHome > areaRadius * 2.0f)
        {
            return null;
        }

        PointOfInterest[] allFoods = GameObject.FindObjectsByType<PointOfInterest>(FindObjectsSortMode.None);
        Transform bestTarget = null;
        float closestDistance = foodSearchRadius;

        foreach (PointOfInterest food in allFoods)
        {
            if (!food.CanAnimalEat(animal))
                continue;

            float foodToHomeDist = Vector3.Distance(food.transform.position, actualHomePosition);
            if (foodToHomeDist > areaRadius * 1.3f)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, food.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = food.transform;
            }
        }
        return bestTarget;
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