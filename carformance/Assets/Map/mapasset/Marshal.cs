using UnityEngine;

public class Marshal : MonoBehaviour
{
    public bool lockYAxis = true;

    void LateUpdate()
    {
        Camera targetCam = FindActiveCamera();

        if (targetCam != null)
        {
            transform.LookAt(transform.position + targetCam.transform.rotation * Vector3.forward,
                             targetCam.transform.rotation * Vector3.up);

            if (lockYAxis)
            {
                Vector3 eulerAngles = transform.eulerAngles;
                eulerAngles.x = 0;
                eulerAngles.z = 0;
                transform.eulerAngles = eulerAngles;
            }
        }
    }

    Camera FindActiveCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            return Camera.main;
        }

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.isActiveAndEnabled && cam.cameraType == CameraType.Game)
            {
                return cam;
            }
        }

        Debug.LogError("HIBA: A Marshal nem talál aktív kamerát ebben a Scene-ben! Ellenõrizd a Kamera Tag-et!");
        return null;
    }
}