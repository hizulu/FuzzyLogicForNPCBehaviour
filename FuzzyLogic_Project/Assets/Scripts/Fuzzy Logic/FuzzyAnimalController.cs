using UnityEngine;
using UnityEngine.AI;

public class FuzzyAnimalController : MonoBehaviour
{
    public AnimalBase animal;
    public Transform player;

    private NavMeshAgent agent;
    private Animator animator;

    private float distanciaActual;
    private float miedoAcumulado;
    private float curiosidadActual;

    float intencion;

    public float minWaitingTime = 3f;
    public float maxWaitingTime = 7f;
    float waitingTimer;
    bool isWaiting=false;

    // Matriz que almacena la acción resultante para cada combinación [Curiosidad, Distancia, Miedo]
    private int[,,] baseReglas = new int[5, 5, 5];

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        curiosidadActual = 0.5f;
    }

    void Update()
    {
        if (player == null) return;

        ActualizarVariablesEntrada();
        intencion = ProcesarLogicaDifusa();

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

        Debug.Log($"Animal: {animal} | Miedo: {miedoAcumulado} | Curiosidad: {curiosidadActual} | Intencion: {intencion}");
    }

    void ActualizarVariablesEntrada()
    {
        distanciaActual = Vector3.Distance(transform.position, player.position);

        float factorEstres = Mathf.Clamp01(1 - (distanciaActual / animal.radioDeteccion));
        if (distanciaActual <= animal.radioDeteccion)
            miedoAcumulado += factorEstres * animal.sensibilidadMiedo * Time.deltaTime;
        else
            miedoAcumulado -= animal.decaimientoMiedo * Time.deltaTime;

        miedoAcumulado = Mathf.Clamp01(miedoAcumulado);


        if (miedoAcumulado < 0.8f && distanciaActual < animal.radioDeteccion * 1.5f)
        {
            float impulsoCurioso = (1 - (distanciaActual / (animal.radioDeteccion * 1.5f))) * 0.5f;
            curiosidadActual += impulsoCurioso * Time.deltaTime;
        }
        else
        {
            curiosidadActual -= animal.decaimientoCuriosidad * Time.deltaTime;
        }

        curiosidadActual = Mathf.Clamp01(curiosidadActual);
    }

    float ProcesarLogicaDifusa()
    {
        //Fuzzificacion
        float[] fCuriosidad = FuzzificarCuriosidad(curiosidadActual);
        float[] fDistancia = FuzzificarDistancia(distanciaActual);
        float[] fMiedo = FuzzificarMiedo(miedoAcumulado);

        float[] pesosSalida = new float[5];

        float[] valoresSalida = {
            animal.huidaRapida,
            animal.retiradaLenta,
            animal.idle,
            animal.aproxLenta,
            animal.aproxRapida
        };

        //Sistema de inferencia
        for (int c = 0; c < 5; c++)
        {
            for (int d = 0; d < 5; d++)
            {
                for (int m = 0; m < 5; m++)
                {
                    //Operador AND (Mínimo) para ver con que fuerza se activa esta regla
                    float fuerzaRegla = Mathf.Min(fCuriosidad[c], Mathf.Min(fDistancia[d], fMiedo[m]));

                    if (fuerzaRegla > 0)
                    {
                        int indiceAccion = RuleTables.ObtenerAccion(c, d, m);
                        //Operador OR (Máximo) para acumular el peso de la acción resultante
                        pesosSalida[indiceAccion] = Mathf.Max(pesosSalida[indiceAccion], fuerzaRegla);
                    }
                }
            }
        }

        //Defuzzificacion
        return Defuzzification.Defuzzify(pesosSalida, valoresSalida);
    }

    //FUNCIONES DE FUZZIFICACION
    private float[] FuzzificarMiedo(float valor)
    {
        float[] grados = new float[5];
        //TODO: Ajustar segun graficas
        grados[(int)TagMiedo.Relajado] = MembershipFunction.LeftShoulder(valor, 0.1f, 0.3f);
        grados[(int)TagMiedo.Cauto] = MembershipFunction.Triangle(valor, 0.2f, 0.4f, 0.6f);
        grados[(int)TagMiedo.Alerta] = MembershipFunction.Trapezoid(valor, 0.3f, 0.45f, 0.7f, 9f);
        grados[(int)TagMiedo.Asustado] = MembershipFunction.Triangle(valor, 0.6f, 0.8f, 0.9f);
        grados[(int)TagMiedo.Panico] = MembershipFunction.RightShoulder(valor, 0.8f, 1.0f);
        return grados;
    }

    private float[] FuzzificarDistancia(float valor)
    {
        float[] grados = new float[5];
        //TODO: Adaptar al tamaño del juego
        grados[(int)TagDistancia.MuyCerca] = MembershipFunction.LeftShoulder(valor, 2f, 5f);
        grados[(int)TagDistancia.Cerca] = MembershipFunction.Triangle(valor, 3f, 8f, 12f);
        grados[(int)TagDistancia.Media] = MembershipFunction.Trapezoid(valor, 10f, 15f, 20f, 25f);
        grados[(int)TagDistancia.Lejos] = MembershipFunction.Triangle(valor, 18f, 25f, 30f);
        grados[(int)TagDistancia.MuyLejos] = MembershipFunction.RightShoulder(valor, 28f, 35f);
        return grados;
    }

    private float[] FuzzificarCuriosidad(float valor)
    {
        float[] grados = new float[5];
        grados[(int)TagCuriosidad.Nula] = MembershipFunction.LeftShoulder(valor, 0.1f, 0.3f);
        grados[(int)TagCuriosidad.Baja] = MembershipFunction.Triangle(valor, 0.2f, 0.4f, 0.6f);
        grados[(int)TagCuriosidad.Media] = MembershipFunction.Trapezoid(valor, 0.3f, 0.5f, 0.7f, 0.9f);
        grados[(int)TagCuriosidad.Alta] = MembershipFunction.Triangle(valor, 0.6f, 0.8f, 0.9f);
        grados[(int)TagCuriosidad.Extrema] = MembershipFunction.RightShoulder(valor, 0.8f, 1.0f);
        return grados;
    }

    void StartWait()
    {
        isWaiting = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        animator.SetBool("IsMoving", false);
        waitingTimer = Random.Range(minWaitingTime, maxWaitingTime);

        //Elige aleatoriamente entre Idle (0)  Comer (1) mientras espera
        animator.SetInteger("WaitType", Random.Range(0, 2));
    }

    void UpdateWaiting()
    {
        waitingTimer -= Time.deltaTime;

        if (waitingTimer <= 0)
        {
            FindNewDestination(intencion);
        }
    }

    void FindNewDestination(float intencion)
    {
        if(Mathf.Abs(intencion) > 0.1f)
        {
            agent.isStopped = false;
            Vector3 direccion;

            if (intencion > 0)
            {
                direccion = (player.position - transform.position).normalized;
            }
            else
            {
                direccion = (transform.position - player.position).normalized;
            }

            Vector3 destino = transform.position + direccion * 5f;
            agent.SetDestination(destino);

            agent.speed = Mathf.Abs(intencion);

            UpdateAnimations(agent.speed);
        }
    }

    void UpdateAnimations(float speed)
    {
        animator.SetBool("IsMoving", speed > 0.1f);
        animator.SetFloat("MovementSpeed", speed / 2f);
    }
}