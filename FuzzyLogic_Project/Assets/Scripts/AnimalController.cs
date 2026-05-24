using UnityEngine;
using UnityEngine.AI;
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]

/* NOMBRE CLASE: AnimalController
 * AUTOR: Lucía García López
 * FECHA: 22/04/2025
 * DESCRIPCIÓN: Controlador de animales que se mueve aleatoriamente dentro de un área definida. 
 *              El animal alterna entre caminar, correr y esperar, con animaciones correspondientes.
 */

public class AnimalController : MonoBehaviour
{
    [Header("Ajustes de IA")]
    public float areaRadius = 10f;
    public float minWaitingTime = 3f;
    public float maxWaitingTime = 7f;

    [Header("Velocidades")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    private NavMeshAgent agent;
    private Animator animator;
    private float waitingTimer;
    private bool isWaiting = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        StartWait();
    }

    void Update()
    {
        //Comprobar si ha llegado al destino
        //pathPending es por si el NavMesh aún está calculando el camino
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting) StartWait();
        }

        if (isWaiting)
        {
            UpdateWaiting();
        }

        UpdateAnimations();
    }

    void FindNewDestination()
    {
        //Se busca un punto aleatorio dentro de un area
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * areaRadius;
        NavMeshHit hit;

        //Se comprueba que el punto aleatorio es válido en el NavMesh
        if (NavMesh.SamplePosition(randomPoint, out hit, areaRadius, 1))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);

            bool isGoingToRun = Random.value > 0.8f;
            agent.speed = isGoingToRun ? runSpeed : walkSpeed;

            isWaiting = false;
            animator.SetBool("IsMoving", true);
        }
    }

    void StartWait()
    {
        isWaiting = true; 
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        animator.SetBool("IsMoving", false);
        waitingTimer = Random.Range(minWaitingTime, maxWaitingTime);

        //Elige aleatoriamente entre Idle (0) o Comer (1) mientras espera
        animator.SetInteger("WaitType", Random.Range(0, 2));
    }
    
    void UpdateWaiting()
    {
        waitingTimer -= Time.deltaTime;

        if (waitingTimer <= 0)
        {
            FindNewDestination();
        }
    }

    void UpdateAnimations()
    {
        float realSpeed = agent.velocity.magnitude;

        //Si la velocidad es muy baja cambia el isMoving a negativo
        if (realSpeed > 0.1f)
        {
            animator.SetBool("IsMoving", true);

            float targetMultiplier = (realSpeed <= walkSpeed) ? 1.0f : realSpeed / walkSpeed;

            float smoothedMultiplier = Mathf.Lerp(animator.GetFloat("MovementSpeed"), targetMultiplier, Time.deltaTime * 8f);
            animator.SetFloat("MovementSpeed", smoothedMultiplier);
        }
        else
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MovementSpeed", 1.0f);
        }
    }
}