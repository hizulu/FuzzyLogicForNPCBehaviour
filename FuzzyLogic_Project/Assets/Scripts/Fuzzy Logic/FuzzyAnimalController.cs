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
    PlayerController playerController;

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
    public Transform bestPrey;

    [Header("Cooldown de Caza")]
    public float huntingCooldownDuration = 30f;
    private float huntingCooldownTimer = 0f;

    [Header("Estabilidad de decisión")]
    public float goalHoldTime = 2.0f;
    public float goalSwitchMargin = 0.08f;

    private enum StableGoal
    {
        None, FollowPlayer, GoFood, Idle
    }

    private StableGoal currentStableGoal = StableGoal.None;
    private float goalLockUntil = 0f;
    private Vector3 lastIssuedDestination;
    private bool hasLastIssuedDestination = false;

    private Vector3 previousPlayerPosition;
    private float playerSpeed;
    private Vector3 playerMovementDir;

    private bool wasIdle = false;
    private float stuckTimer = 0f;
    private bool isApproachingPlayer = false;
    private bool isFleeingPlayer = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        allActiveAnimals.Clear();
    }

    public static List<FuzzyAnimalController> allActiveAnimals = new List<FuzzyAnimalController>();

    void OnEnable()
    {
        if (!allActiveAnimals.Contains(this))
        {
            allActiveAnimals.Add(this);
        }
    }

    void OnDisable()
    {
        if (allActiveAnimals.Contains(this))
        {
            allActiveAnimals.Remove(this);
        }
    }

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
            foodCooldownTimer -= Time.deltaTime;

        if (huntingCooldownTimer > 0f)
            huntingCooldownTimer -= Time.deltaTime;

        UpdateInputVariables();

        Transform nearestPredator = GetNearestActivePredator(predators);

        Transform previousPrey = bestPrey;
        bestPrey = GetIsolatedPrey(prey);

        if (previousPrey != null && bestPrey == null)
        {
            FuzzyAnimalController preyCtrl = previousPrey.GetComponentInParent<FuzzyAnimalController>();
            if (preyCtrl != null && IsPreySafe(preyCtrl))
            {
                huntingCooldownTimer = huntingCooldownDuration;
            }
        }

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
        bool arrived = !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance;

        if (!arrived && !isWaiting)
        {
            if (agent.hasPath && agent.velocity.magnitude < 0.2f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 1.0f)
                {
                    stuckTimer = 0f;
                    FindNewDestination();
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
        //Buscar comida
        Transform closestFood = GetClosestFoodSource();
        if (closestFood != null)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(closestFood.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.isStopped = false;
                agent.SetDestination(closestFood.position);
                agent.speed = walkSpeed;
                isWaiting = false;
                stuckTimer = 0f;
                return;
            }
        }

        //Si no hay comida, buscar un punto aleatorio dentro del área de patrulla
        Vector3 centerOfZone = (homePoint != null) ? actualHomePosition : transform.position;
        Vector3 finalDestination = centerOfZone;
        bool foundValidPoint = false;
        float searchRadius = Mathf.Max(areaRadius - 3f, 5f);

        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint = centerOfZone + Random.insideUnitSphere * searchRadius;
            randomPoint.y = centerOfZone.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 5.0f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) > 2.5f)
                {
                    finalDestination = hit.position;
                    foundValidPoint = true;
                    break;
                }
            }
        }

        if (!foundValidPoint)
        {
            finalDestination = transform.position + transform.forward * 3f;
        }

        stuckTimer = 0f;
        agent.isStopped = false;
        agent.SetDestination(finalDestination);
        agent.speed = walkSpeed;
        isWaiting = false;
    }

    //Establece el destino del NavMeshAgent de manera estable, evitando cambios bruscos de destino si el nuevo destino está cerca del último destino emitido, para que el animal no cambie constantemente de dirección si el jugador o la comida se mueve ligeramente.
    private void SetDestinationStable(Vector3 destination)
    {
        if (hasLastIssuedDestination && Vector3.Distance(lastIssuedDestination, destination) < 1.5f)
            return;

        lastIssuedDestination = destination;
        hasLastIssuedDestination = true;
        agent.SetDestination(destination);
    }
    #endregion

    //Determina la acción de movimiento continua del animal en función de la intención calculada por la lógica difusa, priorizando huir de depredadores, luego huir del jugador, luego acechar presas, luego buscar comida, y finalmente patrullar o quedarse quieto según corresponda.
    void HandleContinuousMovement(float intentionValue, Transform nearestPredator, Transform nearestPrey)
    {
        //PRIORIDAD 1: HUIR DE UN DEPREDADOR
        if (nearestPredator != null)
        {
            isApproachingPlayer = false;
            isFleeingPlayer = false;

            if (wasIdle) { animator.SetInteger("WaitType", -1); wasIdle = false; }
            agent.isStopped = false;
            currentDominantAction = Action.FastFlee;

            Vector3 dirAwayFromPredator = (transform.position - nearestPredator.position).normalized;
            dirAwayFromPredator.y = 0f;

            Vector3 escapeTarget = transform.position + dirAwayFromPredator * 10f; // Intentar alejarse 10 metros
            NavMeshHit hit;
            if (NavMesh.SamplePosition(escapeTarget, out hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                Vector3 groupCenter = GetNearestGroupCenter();
                agent.SetDestination(groupCenter != Vector3.zero ? groupCenter : transform.position);
            }

            agent.speed = Mathf.MoveTowards(agent.speed, animal.maxSpeed, Time.deltaTime * fuzzyAcceleration);
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 2: HUIR DEL JUGADOR
        float fleeThreshold = isFleeingPlayer ? -0.05f : -0.2f;
        bool isAfraidOfPlayer = accumulatedFear > 0.45f || intentionValue < fleeThreshold;

        if (isAfraidOfPlayer)
        {
            isFleeingPlayer = true;
            isApproachingPlayer = false;
            currentDominantAction = Action.FastFlee;
            if (wasIdle) { animator.SetInteger("WaitType", -1); wasIdle = false; }
            agent.isStopped = false;

            Vector3 dirAwayFromPlayer = (transform.position - player.position).normalized;
            dirAwayFromPlayer.y = 0f;
            dirAwayFromPlayer.Normalize();

            float escapeDistance = 20f;
            Vector3 escapeTarget = transform.position + dirAwayFromPlayer * escapeDistance;

            NavMeshHit hit;
            Vector3 finalEscapeDestination = escapeTarget;

            if (NavMesh.SamplePosition(escapeTarget, out hit, 10.0f, NavMesh.AllAreas))
            {
                finalEscapeDestination = hit.position;
            }
            else
            {
                Vector3 groupCenter = GetNearestGroupCenter();
                if (groupCenter != Vector3.zero) finalEscapeDestination = groupCenter;
            }

            SetDestinationStable(finalEscapeDestination);

            float targetSpeed = Mathf.Abs(intentionValue);
            if (playerSpeed > playerMovementThreshold)
            {
                float runIntensity = Mathf.InverseLerp(playerMovementThreshold, 5.0f, playerSpeed);
                targetSpeed = Mathf.Lerp(targetSpeed, animal.maxSpeed, runIntensity);
            }

            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, Time.deltaTime * (fuzzyAcceleration * 2f));
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }
        else
        {
            isFleeingPlayer = false;
        }

        //PRIORIDAD 3: CAZAR
        if (nearestPrey != null)
        {
            currentStableGoal = StableGoal.None;
            isApproachingPlayer = false;
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

        //PRIORIDAD 4: ACERCARSE AL JUGADOR (Curiosidad)
        Transform closestFood = GetClosestFoodSource();
        float approachThreshold = isApproachingPlayer ? (idleThreshold * 0.5f) : (idleThreshold + 0.05f);

        bool wantsPlayer = intentionValue > approachThreshold + goalSwitchMargin;
        bool wantsFood = closestFood != null && intentionValue < approachThreshold - goalSwitchMargin;

        if (Time.time < goalLockUntil)
        {
            if (currentStableGoal == StableGoal.FollowPlayer) wantsPlayer = true;
            if (currentStableGoal == StableGoal.GoFood) wantsFood = true;
        }
        else
        {
            if (wantsPlayer && currentStableGoal != StableGoal.FollowPlayer)
            {
                currentStableGoal = StableGoal.FollowPlayer;
                goalLockUntil = Time.time + goalHoldTime;
            }
            else if (wantsFood && currentStableGoal != StableGoal.GoFood)
            {
                currentStableGoal = StableGoal.GoFood;
                goalLockUntil = Time.time + goalHoldTime;
            }
            else
            {
                currentStableGoal = StableGoal.None;
            }
        }

        if (currentStableGoal == StableGoal.FollowPlayer)
        {
            isApproachingPlayer = true;
            isFleeingPlayer = false;

            if (Mathf.Abs(intentionValue) <= idleThreshold)
            {
                if (!wasIdle)
                {
                    animator.SetInteger("WaitType", Random.Range(0, 2));
                    wasIdle = true;
                }
                agent.isStopped = true;
                agent.speed = Mathf.MoveTowards(agent.speed, 0f, Time.deltaTime * fuzzyAcceleration);
                UpdateAnimations(agent.velocity.magnitude);
                return;
            }

            if (wasIdle)
            {
                animator.SetInteger("WaitType", -1);
                wasIdle = false;
            }

            agent.isStopped = false;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0f;
            dirToPlayer.Normalize();

            if (dirToPlayer == Vector3.zero) dirToPlayer = transform.forward;

            Vector3 dynamicDestination = player.position - dirToPlayer * 2.0f;
            agent.SetDestination(dynamicDestination);

            agent.speed = Mathf.MoveTowards(agent.speed, intentionValue, Time.deltaTime * fuzzyAcceleration);
            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 5: IR A LOS PUNTOS DE INTERÉS (COMIDA)
        else if (currentStableGoal == StableGoal.GoFood && closestFood != null)
        {
            isApproachingPlayer = false;
            if (wasIdle)
            {
                animator.SetInteger("WaitType", -1);
                wasIdle = false;
            }

            float distanceToFood = Vector3.Distance(transform.position, closestFood.position);
            bool arrivedAtFood = (distanceToFood <= foodInteractionRadius) ||
                                 (distanceToFood <= 3.5f && !agent.pathPending && agent.velocity.magnitude < 0.2f);

            agent.isStopped = false;

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
                    currentStableGoal = StableGoal.None;
                }
            }
            else
            {
                SetDestinationStable(closestFood.position);
                agent.speed = Mathf.MoveTowards(agent.speed, walkSpeed, Time.deltaTime * fuzzyAcceleration);
            }

            UpdateAnimations(agent.velocity.magnitude);
            return;
        }

        //PRIORIDAD 6: ESTADO INACTIVO
        if (!wasIdle)
        {
            animator.SetInteger("WaitType", Random.Range(0, 2));
            wasIdle = true;
        }
        agent.isStopped = true;
        agent.speed = Mathf.MoveTowards(agent.speed, 0f, Time.deltaTime * fuzzyAcceleration);
        UpdateAnimations(agent.velocity.magnitude);
    }

    //Busca depredadores activos dentro de un radio de detección y evalúa cuál es el más cercano que está cazando activamente al animal, para que el animal pueda decidir huir específicamente de ese depredador en lugar de simplemente huir del jugador o de cualquier amenaza genérica.
    private Transform GetNearestActivePredator(List<AnimalBase> targetList)
    {
        if (targetList == null || targetList.Count == 0) return null;

        Transform bestTarget = null;
        float closestDistanceSqr = animal.detectionRadius * animal.detectionRadius;

        foreach (FuzzyAnimalController otherAnimal in allActiveAnimals)
        {
            if (otherAnimal == this) continue;

            if (targetList.Contains(otherAnimal.animal))
            {
                if (otherAnimal.bestPrey != this.transform) continue;

                float distSqr = (transform.position - otherAnimal.transform.position).sqrMagnitude;
                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    bestTarget = otherAnimal.transform;
                }
            }
        }
        return bestTarget;
    }

    //Busca presas dentro de un radio de detección y evalúa cuál es la presa más aislada (con menos aliados cerca) y que no esté segura (cerca de su refugio o del jugador), para que el animal pueda decidir perseguir específicamente a esa presa en lugar de simplemente acechar al jugador o a cualquier presa genérica.
    private Transform GetIsolatedPrey(List<AnimalBase> targetList)
    {
        if (targetList == null || targetList.Count == 0) return null;
        if (huntingCooldownTimer > 0f) return null;

        Transform foundPrey = null;
        float bestScore = Mathf.Infinity;
        float detectionSqr = animal.detectionRadius * animal.detectionRadius;

        foreach (FuzzyAnimalController otherAnimal in allActiveAnimals)
        {
            if (otherAnimal == this) continue;

            if (targetList.Contains(otherAnimal.animal))
            {
                float distSqr = (transform.position - otherAnimal.transform.position).sqrMagnitude;

                if (distSqr <= detectionSqr)
                {
                    if (IsPreySafe(otherAnimal)) continue;

                    int alliesNearby = CountAlliesNear(otherAnimal, 7f);
                    float distanceToPrey = Mathf.Sqrt(distSqr);
                    float score = distanceToPrey + (alliesNearby * 30f);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        foundPrey = otherAnimal.transform;
                    }
                }
            }
        }
        return foundPrey;
    }

    //Evalúa si una presa está lo suficientemente cerca de su refugio o del jugador como para considerarla segura, lo que haría que el animal deje de perseguirla para evitar riesgos innecesarios.
    private bool IsPreySafe(FuzzyAnimalController prey)
    {
        float safeHomeRadius = 5f;
        float safePlayerRadius = 4f;

        bool nearHome = Vector3.Distance(prey.transform.position, prey.actualHomePosition) <= safeHomeRadius;

        bool nearPlayer = Vector3.Distance(prey.transform.position, player.position) <= safePlayerRadius;

        return nearHome || nearPlayer;
    }

    //Cuenta cuántos animales del mismo tipo hay cerca de un objetivo dado dentro de un radio específico, para que el animal pueda evaluar si una presa está aislada o si un grupo de aliados está cerca para decidir su comportamiento.
    private int CountAlliesNear(FuzzyAnimalController targetAnimal, float radius)
    {
        int count = 0;
        float radiusSqr = radius * radius;

        foreach (FuzzyAnimalController other in allActiveAnimals)
        {
            if (other == targetAnimal) continue;

            if (other.animal == targetAnimal.animal)
            {
                float distSqr = (targetAnimal.transform.position - other.transform.position).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    count++;
                }
            }
        }
        return count;
    }

    //Busca comida dentro de un radio definido alrededor del animal, priorizando las fuentes de comida que estén dentro de su área de patrulla y que no estén en cooldown, para que el animal pueda decidir ir a comer en lugar de interactuar con el jugador o patrullar.
    private Transform GetClosestFoodSource()
    {
        if (foodCooldownTimer > 0f) return null;

        if (homePoint != null)
        {
            float distanceToHome = Vector3.Distance(transform.position, actualHomePosition);
            if (distanceToHome > areaRadius * 2.0f)
            {
                return null;
            }
        }

        PointOfInterest[] allFoods = GameObject.FindObjectsByType<PointOfInterest>(FindObjectsSortMode.None);
        Transform bestTarget = null;
        float closestDistance = foodSearchRadius;

        foreach (PointOfInterest food in allFoods)
        {
            if (!food.CanAnimalEat(animal))
                continue;

            //Si tiene casa, verifica que la comida no esté en una zona alejada
            if (homePoint != null)
            {
                float foodToHomeDist = Vector3.Distance(food.transform.position, actualHomePosition);
                if (foodToHomeDist > areaRadius * 1.3f) continue;
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
        Vector3 center = Vector3.zero;
        int groupCount = 0;
        float searchRadiusSqr = groupSearchRadius * groupSearchRadius;

        foreach (FuzzyAnimalController otherAnimal in allActiveAnimals)
        {
            if (otherAnimal == this) continue;

            if (otherAnimal.animal == this.animal)
            {
                float distSqr = (transform.position - otherAnimal.transform.position).sqrMagnitude;
                if (distSqr <= searchRadiusSqr)
                {
                    center += otherAnimal.transform.position;
                    groupCount++;
                }
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

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (playerController != null)
        {
            playerSpeed = playerController.CurrentSpeed;

            playerMovementDir = (player.position - previousPlayerPosition).normalized;
            previousPlayerPosition = player.position;
        }
        else
        {
            playerSpeed = (player.position - previousPlayerPosition).magnitude / Time.deltaTime;
            playerMovementDir = (player.position - previousPlayerPosition).normalized;
            previousPlayerPosition = player.position;
        }

        float pWalkSpeed = playerController != null ? playerController.WalkSpeed : 2.0f;
        float pRunSpeed = playerController != null ? playerController.RunSpeed : 5.0f;

        float fearMultiplier = 1f;
        if (playerSpeed <= pWalkSpeed + 0.1f)
        {
            fearMultiplier = 0.5f;
        }
        else
        {
            float runIntensity = Mathf.InverseLerp(pWalkSpeed, pRunSpeed, playerSpeed);
            fearMultiplier = Mathf.Lerp(1.0f, 3.0f, runIntensity);
        }

        float stressFactor = Mathf.Clamp01(1 - (currentDistance / animal.detectionRadius));

        if (currentDistance <= animal.detectionRadius)
        {
            if (playerSpeed > 0.1f)
            {
                accumulatedFear += stressFactor * animal.fearSensitivity * fearMultiplier * Time.deltaTime;
            }
            else
            {
                accumulatedFear -= (animal.fearDecay * 0.5f) * Time.deltaTime;
            }
        }
        else
        {
            accumulatedFear -= animal.fearDecay * Time.deltaTime;
        }

        accumulatedFear = Mathf.Clamp01(accumulatedFear);

        if (accumulatedFear > 0.45f)
        {
            currentCuriosity -= accumulatedFear * 2f * Time.deltaTime;
        }
        else if (accumulatedFear < 0.45f && currentDistance < animal.detectionRadius * 1.5f)
        {
            float curiousImpulse = (1 - (currentDistance / (animal.detectionRadius * 1.5f))) * 0.5f;
            currentCuriosity += curiousImpulse * Time.deltaTime;
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