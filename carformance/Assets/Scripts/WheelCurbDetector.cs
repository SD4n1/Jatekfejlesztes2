using UnityEngine;

/// <summary>
/// Kerék curb detector - támogatja mind a PrometeoCarController-t, mind a DriftCarController-t
/// </summary>
public class WheelCurbDetector : MonoBehaviour
{
    [HideInInspector] public PrometeoCarController carController;
    [HideInInspector] public DriftCarController driftCarController;

    private bool isOnCurb = false;
    private float lastTriggerTime = 0f;

    private string CurbTag
    {
        get
        {
            if (carController != null) return carController.curbTag;
            if (driftCarController != null) return driftCarController.curbTag;
            return "Curb";
        }
    }

    private float CurbSoundCooldown
    {
        get
        {
            if (carController != null) return carController.curbSoundCooldown;
            if (driftCarController != null) return driftCarController.curbSoundCooldown;
            return 0.06f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (carController == null && driftCarController == null) return;
        if (!other.CompareTag(CurbTag)) return;

        if (!isOnCurb)
        {
            isOnCurb = true;
            if (carController != null) carController.WheelEnteredCurb();
            if (driftCarController != null) driftCarController.WheelEnteredCurb();
        }

        if (carController != null) carController.OnWheelHitCurb(transform.position);
        if (driftCarController != null) driftCarController.OnWheelHitCurb(transform.position);
    }

    void OnTriggerStay(Collider other)
    {
        if (carController == null && driftCarController == null) return;
        if (!other.CompareTag(CurbTag)) return;

        if (Time.time - lastTriggerTime >= CurbSoundCooldown)
        {
            lastTriggerTime = Time.time;
            if (carController != null) carController.OnWheelHitCurb(transform.position);
            if (driftCarController != null) driftCarController.OnWheelHitCurb(transform.position);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (carController == null && driftCarController == null) return;
        if (!other.CompareTag(CurbTag)) return;

        if (isOnCurb)
        {
            isOnCurb = false;
            if (carController != null) carController.WheelExitedCurb();
            if (driftCarController != null) driftCarController.WheelExitedCurb();
        }
    }
}