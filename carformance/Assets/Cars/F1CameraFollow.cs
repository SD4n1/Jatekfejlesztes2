using UnityEngine;

public class F1CameraFollow : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform target; // Húzd ide az autót
    public PrometeoCarController carController; // Húzd ide az autót (hogy olvassuk a sebességet)

    [Header("Position & Rotation Settings")]
    public Vector3 offset = new Vector3(0f, 2.5f, -6f); // Kamera pozíciója az autóhoz képest
    public float followSpeed = 10f; // Milyen gyorsan kövesse a pozíciót
    public float lookSpeed = 10f;   // Milyen gyorsan forduljon az autó felé
    public float rotationTiltAmount = 2.0f; // Mennyire dõljön be kanyarban

    [Header("Dynamic FOV (Sense of Speed)")]
    public bool useDynamicFOV = true;
    public float minFOV = 60f; // Álló helyzetben
    public float maxFOV = 85f; // Végsebességnél (320 km/h)

    [Header("High Speed Shake")]
    public bool useShake = true;
    public float shakeStartSpeed = 200f; // Ennél a sebességnél kezd remegni
    public float shakeAmount = 0.05f; // A remegés erõssége

    // Privát változók
    private Vector3 currentVelocity;
    private Camera cam;
    private float defaultShakeAmount;

    private void Start()
    {
        cam = GetComponent<Camera>();
        
        // Ha elfelejtetted behúzni a scriptet, megpróbáljuk megkeresni
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
        // Kiszámoljuk, hol kéne lennie a kamerának (az autó mögött + offset)
        // TransformPoint: A helyi (local) koordinátát világ (world) koordinátává alakítja
        Vector3 targetPos = target.TransformPoint(offset);

        // Simán interpolálunk a jelenlegi és a cél pozíció között
        // Lerp helyett SmoothDamp-et használunk, mert az kevésbé "rugós", inkább "filmes"
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 1f / followSpeed);
    }

    void LookAtTarget()
    {
        // Megnézzük, merre néz az autó
        Quaternion targetRotation = Quaternion.LookRotation(target.forward, Vector3.up);

        // Kanyar effekt: Ha kanyarodunk, kicsit dõljön a kamera a Z tengelyen
        if (carController != null)
        {
            // A localVelocityX mutatja, mennyire csúszunk/kanyarodunk oldalra
            // Ezt használjuk a dõléshez (Z tengely)
            float tilt = -Input.GetAxis("Horizontal") * rotationTiltAmount; 
            
            // Hozzáadjuk a dõlést a cél rotációhoz
            Vector3 euler = targetRotation.eulerAngles;
            euler.z += tilt;
            targetRotation = Quaternion.Euler(euler);
        }

        // Finoman ráfordítjuk a kamerát
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
    }

    void UpdateFOV()
    {
        if (carController == null || cam == null) return;

        float speed = Mathf.Abs(carController.carSpeed);
        float maxCarSpeed = (float)carController.maxSpeed;

        // Kiszámoljuk az arányt (0.0 és 1.0 között)
        float speedPercent = Mathf.Clamp01(speed / maxCarSpeed);

        // Beállítjuk a FOV-ot a sebesség alapján
        // Mathf.Lerp: lineáris átmenet a min és max között
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speedPercent);

        // Finoman változtatjuk, nem azonnal
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }

    void HandleCameraShake()
    {
        if (carController == null) return;

        float speed = Mathf.Abs(carController.carSpeed);

        // Csak akkor remeg, ha gyorsan megyünk
        if (speed > shakeStartSpeed)
        {
            // Mennyire remegjen? (Minél gyorsabb, annál jobban)
            float shakeFactor = (speed - shakeStartSpeed) / (carController.maxSpeed - shakeStartSpeed);
            float currentShake = shakeAmount * shakeFactor;

            // Véletlenszerû elmozdulás
            Vector3 shakePos = Random.insideUnitSphere * currentShake;
            
            // Csak a pozícióhoz adjuk hozzá, nem mentjük el (hogy ne másszon el a kamera)
            transform.position += shakePos;
        }
    }
}