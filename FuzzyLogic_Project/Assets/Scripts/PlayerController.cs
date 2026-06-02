using UnityEngine;

/* NOMBRE CLASE: PlayerController
 * AUTOR: Lucía García López
 * FECHA: 14/04/2025
 * DESCRIPCIÓN: Controlador del jugador que maneja el movimiento basado en el input del teclado, con una dirección relativa a la cámara.
 */

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float acceleration = 10.0f;

    [Header("Referencia a la Cámara")]
    [SerializeField] private Transform cameraTransform;

    private Animator animator;
    private CharacterController controller;

    private float currentSpeed;
    private float velocityY;
    private Vector3 moveDirection;

    private static readonly int PlayerSpeedParam = Animator.StringToHash("PlayerSpeed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();
        UpdateAnimator();
    }

    private void HandleMovement()
    {
        //Obtener input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 input = new Vector2(horizontal, vertical).normalized;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        //Calcular dirección de movimiento relativa a la cámara
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMove = (forward * input.y + right * input.x);

        if (input.magnitude >= 0.1f)
        {
            //Rotación suave hacia la dirección de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(desiredMove);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            //Velocidad objetivo según si corre o camina
            float targetSpeed = isRunning ? runSpeed : walkSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            moveDirection = desiredMove * currentSpeed;
        }
        else
        {
            //Sin input: desacelerar
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, acceleration * Time.deltaTime);
            moveDirection = Vector3.zero;
        }

        controller.Move(moveDirection * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        float animationSpeed = 0f;
        if (currentSpeed > 0.1f)
        {
            float speedRatio = Mathf.InverseLerp(walkSpeed, runSpeed, currentSpeed);
            animationSpeed = Mathf.Lerp(1f, 2f, speedRatio);
        }

        animator.SetFloat(PlayerSpeedParam, animationSpeed, 0.1f, Time.deltaTime);
    }

    public float CurrentSpeed => currentSpeed;
    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
}