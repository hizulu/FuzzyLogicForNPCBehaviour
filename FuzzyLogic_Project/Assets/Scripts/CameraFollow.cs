using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform player;
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);

    [Header("Configuración de Cámara")]
    public float mouseSensitivity = 3.0f;
    public float distanceFromPlayer = 4.0f;
    public Vector2 pitchMinMax = new Vector2(-15f, 60f);

    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (player == null) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

        Vector3 targetPosition = player.position + offset - transform.forward * distanceFromPlayer;
        transform.position = targetPosition;
    }
}