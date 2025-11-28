using UnityEngine;

public class F1CameraFollow : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform target;
    public PrometeoCarController carController;

    [Header("Position & Rotation Settings")]
    public Vector3 offset = new Vector3(0f, 2.5f, -6f);
    public float followSpeed = 10f;
    public float lookSpeed = 10f;
    public float rotationTiltAmount = 2.0f;

    [Header("Dynamic FOV (Sense of Speed)")]
    public bool useDynamicFOV = true;
    public float minFOV = 60f;
    public float maxFOV = 85f;

    [Header("High Speed Shake")]
    public bool useShake = true;
    public float shakeStartSpeed = 200f;
    public float shakeAmount = 0.05f;

    private Vector3 currentVelocity;
    private Camera cam;
    private float defaultShakeAmount;

    private void Start()
    {
        cam = GetComponent<Camera>();
        
        if (carController == null && target != null)
        {
            carController = target.GetComponent<PrometeoCarController>();
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        MoveToTarget();
        LookAtTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (useDynamicFOV) UpdateFOV();
        if (useShake) HandleCameraShake();
    }

    void MoveToTarget()
    {
        Vector3 targetPos = target.TransformPoint(offset);

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 1f / followSpeed);
    }

    void LookAtTarget()
    {
        Quaternion targetRotation = Quaternion.LookRotation(target.forward, Vector3.up);

        if (carController != null)
        {
            float tilt = -Input.GetAxis("Horizontal") * rotationTiltAmount; 
            
            Vector3 euler = targetRotation.eulerAngles;
            euler.z += tilt;
            targetRotation = Quaternion.Euler(euler);
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
    }

    void UpdateFOV()
    {
        if (carController == null || cam == null) return;

        float speed = Mathf.Abs(carController.carSpeed);
        float maxCarSpeed = (float)carController.maxSpeed;

        float speedPercent = Mathf.Clamp01(speed / maxCarSpeed);

        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speedPercent);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }

    void HandleCameraShake()
    {
        if (carController == null) return;

        float speed = Mathf.Abs(carController.carSpeed);

        if (speed > shakeStartSpeed)
        {
            float shakeFactor = (speed - shakeStartSpeed) / (carController.maxSpeed - shakeStartSpeed);
            float currentShake = shakeAmount * shakeFactor;

            Vector3 shakePos = Random.insideUnitSphere * currentShake;
            
            transform.position += shakePos;
        }
    }
}