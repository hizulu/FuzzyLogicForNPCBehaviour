using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSpeed = 10.0f;

    private Animator animator;
    private CharacterController controller;
    private float currentVelocity;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        if (direction.magnitude >= 0.1f)
        {
            //Si está corriendo el Blend Tree pone la animación de correr, si no la de caminar
            float speed = isRunning ? 2.0f : 1.0f;

            //Lerp para suavizar la transición entre velocidades
            currentVelocity = Mathf.Lerp(currentVelocity, speed, Time.deltaTime * 10f);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            controller.Move(direction * targetSpeed * Time.deltaTime);
        }
        else
        {
            //Si no hay input, el personaje se detiene suavemente y se pone la animación de idle
            currentVelocity = Mathf.Lerp(currentVelocity, 0f, Time.deltaTime * 10f);
        }

        //Actualiza el parámetro speed del Blend Tree para cambiar entre las animaciones de idle, caminar y correr
        animator.SetFloat("PlayerSpeed", currentVelocity);
    }
}