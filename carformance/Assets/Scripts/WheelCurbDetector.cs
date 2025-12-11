using UnityEngine;

/// <summary>
/// Ez a script a kerék mesh-ekre kerül és figyeli a kerékvetõs ütközést.
/// Automatikusan hozzáadódik a PrometeoCarController által.
/// </summary>
public class WheelCurbDetector : MonoBehaviour
{
    [HideInInspector] public PrometeoCarController carController;

    private bool isOnCurb = false;
    private float lastTriggerTime = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (carController == null) return;
        if (!other.CompareTag(carController.curbTag)) return;

        if (!isOnCurb)
        {
            isOnCurb = true;
            carController.WheelEnteredCurb();
        }

        carController.OnWheelHitCurb(transform.position);
    }

    void OnTriggerStay(Collider other)
    {
        if (carController == null) return;
        if (!other.CompareTag(carController.curbTag)) return;

        // Folyamatos hang amíg a kerék a kerékvetõn van
        if (Time.time - lastTriggerTime >= carController.curbSoundCooldown)
        {
            lastTriggerTime = Time.time;
            carController.OnWheelHitCurb(transform.position);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (carController == null) return;
        if (!other.CompareTag(carController.curbTag)) return;

        if (isOnCurb)
        {
            isOnCurb = false;
            carController.WheelExitedCurb();
        }
    }
}