using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -6f);
    [SerializeField] private float height = 2f;
    [SerializeField] private float distance = 6f;
    
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("G-Force Simulation")]
    [SerializeField] private float lateralShiftAmount = 2f;
    [SerializeField] private float lateralShiftSpeed = 3f;
    [SerializeField] private float maxLateralOffset = 3f;
    
    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.2f;
    
    private Vector3 currentVelocity;
    private float currentLateralOffset;
    private float lateralVelocity;
    private Rigidbody targetRigidbody;
    private Vector3 previousVelocity;

    private void Start()
    {
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody>();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // G-erő számítás
        float lateralGForce = CalculateLateralGForce();
        
        // Kamera pozíció számítás G-erővel
        Vector3 targetPosition = CalculateTargetPosition(lateralGForce);
        
        // Simított kamera mozgás
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);
        
        // Kamera forgás az autó felé
        Vector3 lookDirection = target.position - transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private float CalculateLateralGForce()
    {
        if (targetRigidbody == null)
            return 0f;

        // Sebesség változás számítás (gyorsulás)
        Vector3 acceleration = (targetRigidbody.linearVelocity - previousVelocity) / Time.deltaTime;
        previousVelocity = targetRigidbody.linearVelocity;

        // Oldalirányú gyorsulás az autó helyi koordinátarendszerében
        Vector3 localAcceleration = target.InverseTransformDirection(acceleration);
        float lateralAcceleration = localAcceleration.x;

        // Angular velocity alapú kanyarodás detektálás (pontosabb)
        float angularVelocityY = targetRigidbody.angularVelocity.y;
        float speed = targetRigidbody.linearVelocity.magnitude;
        float centrifugalForce = angularVelocityY * speed * lateralShiftAmount;

        // Kombinált G-erő
        float totalLateralForce = lateralAcceleration + centrifugalForce;

        // Simított oldalirányú eltolás
        currentLateralOffset = Mathf.SmoothDamp(
            currentLateralOffset, 
            totalLateralForce, 
            ref lateralVelocity, 
            1f / lateralShiftSpeed
        );

        // Limit alkalmazása
        currentLateralOffset = Mathf.Clamp(currentLateralOffset, -maxLateralOffset, maxLateralOffset);

        return currentLateralOffset;
    }

    private Vector3 CalculateTargetPosition(float lateralOffset)
    {
        // Alap pozíció az autó mögött
        Vector3 desiredPosition = target.position;
        desiredPosition -= target.forward * distance;
        desiredPosition += target.up * height;
        
        // G-erő miatti oldalirányú eltolás (ellentétes irányban)
        desiredPosition -= target.right * lateralOffset;

        return desiredPosition;
    }

    // Editor helper
    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Gizmos.color = Color.yellow;
        Vector3 targetPos = CalculateTargetPosition(currentLateralOffset);
        Gizmos.DrawWireSphere(targetPos, 0.5f);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, target.position);
    }
}

