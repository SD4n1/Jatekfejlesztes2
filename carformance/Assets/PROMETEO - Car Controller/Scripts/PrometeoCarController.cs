using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AudioController;

public class PrometeoCarController : MonoBehaviour
{
    // --- CAR SETUP ---
    [Space(20)]
    [Range(100, 380)] public int maxSpeed = 320;
    [Range(10, 120)] public int maxReverseSpeed = 45;
    [Range(4, 16)] public int accelerationMultiplier = 4;
    [Space(10)]
    [Range(10, 50)] public int maxSteeringAngle = 27;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.5f;
    [Space(10)]
    [Range(100, 1000)] public int brakeForce = 350;

    // EZT KIVETTEM/ÁTÍRTAM: A Deceleration most már finomabb
    // [Range(1, 10)] public int decelerationMultiplier = 2; 
    [Tooltip("Mennyire lassuljon gázelvételnél (Kisebb szám = hosszabb kigurulás)")]
    [Range(0.01f, 1f)] public float coastingDrag = 0.05f; // Nagyon kicsi érték a hosszú guruláshoz

    [Range(1, 10)] public int handbrakeDriftMultiplier = 5;
    [Space(10)]
    public Vector3 bodyMassCenter;

    // --- TAPADÁS ÉS FELÜLET (ÚJ RÉSZ) ---
    [Header("SURFACE & TRACTION")]
    [Tooltip("Alap tapadás aszfalton (1.0 = normál, 4.0 = nagyon tapad/nem driftel)")]
    [Range(1f, 10f)] public float asphaltGrip = 10.0f;

    [Tooltip("Tapadás fűvön (0.5 = csúszik)")]
    [Range(0.1f, 1f)] public float grassGrip = 0.6f;

    [Tooltip("A tag neve, amit a fű objektumokra raksz")]
    public string slipperySurfaceTag = "Grass";

    [Tooltip("Hányszorosára növeljük a coastingDrag-ot fűvön")]
    [Range(1f, 5f)] public float grassCoastingMultiplier = 1.0f;

    [Tooltip("Hány százalékát használjuk a maxSpeed-nek fűvön (0.0-1.0)")]
    [Range(0.1f, 1f)] public float grassMaxSpeedMultiplier = 0.5f;

    [Tooltip("A tag neve, amit a kavicsos objektumokra raksz")]
    public string gravelSurfaceTag = "Gravel";

    [Tooltip("Hányszorosára növeljük a coastingDrag-ot kavicson")]
    [Range(1f, 5f)] public float gravelCoastingMultiplier = 4f;

    [Tooltip("Hány százalékát használjuk a maxSpeed-nek kavicson (0.0-1.0)")]
    [Range(0.1f, 1f)] public float gravelMaxSpeedMultiplier = 0.1f;

    // --- WHEELS ---
    public GameObject frontLeftMesh; public WheelCollider frontLeftCollider;
    public GameObject frontRightMesh; public WheelCollider frontRightCollider;
    public GameObject rearLeftMesh; public WheelCollider rearLeftCollider;
    public GameObject rearRightMesh; public WheelCollider rearRightCollider;

    // --- EFFECTS ---
    public bool useEffects = false;
    public ParticleSystem RLWParticleSystem; public ParticleSystem RRWParticleSystem;
    public TrailRenderer RLWTireSkid; public TrailRenderer RRWTireSkid;

    // --- UI ---
    public bool useUI = false;
    public Text carSpeedText;
    // NEW: gear UI text (similar to carSpeedText)
    [Tooltip("Optional UI Text to display current gear (1..N).")]
    public Text gearText;

    // --- AUDIO (VÁLTOZATLAN PRO VERZIÓ) ---
    [Space(20)]
    [Header("PRO AUDIO SYSTEM")]
    public bool useSounds = false;
    public AudioClip[] engineClips;
    public AudioClip tireScreechClip;
    [Tooltip("Collision sound that plays when car touches an object tagged 'Border'.")]
    public AudioClip collisionClip;
    [Range(0f, 1f)] public float collisionVolume = 1f;

    [Range(4, 8)] public int numberOfGears = 8;
    [Range(1f, 20f)] public float revUpSpeed = 5f;
    [Range(0.5f, 10f)] public float revDownSpeed = 2.5f;

    private AudioSource[] engineSources;
    private AudioSource tireSource;
    private float engineRPM = 0.1f;
    private float targetRPM = 0f;
    private int currentGear = 1;
    private float[] gearSpeeds;
    private float engineLoad = 0f;
    private const float MIN_IDLE_RPM = 0.20f;

    // --- CONTROLS ---
    public bool useTouchControls = false;
    public GameObject throttleButton; PrometeoTouchInput throttlePTI;
    public GameObject reverseButton; PrometeoTouchInput reversePTI;
    public GameObject turnRightButton; PrometeoTouchInput turnRightPTI;
    public GameObject turnLeftButton; PrometeoTouchInput turnLeftPTI;
    public GameObject handbrakeButton; PrometeoTouchInput handbrakePTI;

    // --- DATA ---
    [HideInInspector] public float carSpeed;
    [HideInInspector] public bool isDrifting;
    [HideInInspector] public bool isTractionLocked;

    Rigidbody carRigidbody;
    float steeringAxis; float throttleAxis; float driftingAxis;
    float localVelocityZ; float localVelocityX;
    bool deceleratingCar; bool touchControlsSetup = false;

    // Súrlódási görbék tárolása
    WheelFrictionCurve FL_Sideways, FR_Sideways, RL_Sideways, RR_Sideways;
    float defaultSlip; // Az eredeti csúszási érték (referencia)
    // Eredeti beállítások visszaállításához
    float defaultCoastingDrag;
    int defaultMaxSpeed;

    void Start()
    {
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        // NE használj linearDamping-et, az zavarja a sebességet!
        carRigidbody.linearDamping = 0f;
        carRigidbody.angularDamping = 0.1f;

        // Elmentjük az eredeti beállításokat
        SaveDefaultFriction();
        CalculateGearRatios();

        // Mentjük a coastingDrag és maxSpeed alapértékeit
        defaultCoastingDrag = coastingDrag;
        defaultMaxSpeed = maxSpeed;

        if (useSounds && AudioManager.Instance != null)
        {
            if (engineClips != null && engineClips.Length > 0)
            {
                engineSources = new AudioSource[3];
                for (int i = 0; i < 3; i++)
                {
                    engineSources[i] = AudioManager.Instance.CreateLoopingSource(this.transform, engineClips[0], 0f, true);
                    SetupSource(engineSources[i]);
                }
            }
            if (tireScreechClip != null) tireSource = AudioManager.Instance.CreateLoopingSource(this.transform, tireScreechClip, 0f, true);
        }
        else if (useSounds) { Debug.LogError("Nincs AudioManager!"); useSounds = false; }

        if (useUI) InvokeRepeating("CarSpeedUI", 0f, 0.1f);
        if (!useEffects) StopEffects();
        SetupTouchControls();

        // Forward Friction növelése a jobb fékezéshez
        WheelFrictionCurve forwardFriction;

        forwardFriction = frontLeftCollider.forwardFriction;
        forwardFriction.stiffness = 2f;
        frontLeftCollider.forwardFriction = forwardFriction;

        forwardFriction = frontRightCollider.forwardFriction;
        forwardFriction.stiffness = 2f;
        frontRightCollider.forwardFriction = forwardFriction;

        forwardFriction = rearLeftCollider.forwardFriction;
        forwardFriction.stiffness = 2f;
        rearLeftCollider.forwardFriction = forwardFriction;

        forwardFriction = rearRightCollider.forwardFriction;
        forwardFriction.stiffness = 2f;
        rearRightCollider.forwardFriction = forwardFriction;
    }

    void SetupSource(AudioSource s)
    {
        s.spatialBlend = 1.0f; s.dopplerLevel = 0.5f; s.minDistance = 5f; s.maxDistance = 800f; s.playOnAwake = false;
    }

    void Update()
    {
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        HandleInput();
        AnimateWheelMeshes();

        // ÚJ: Felület ellenőrzése minden frame-ben
        CheckSurfaceAndApplyGrip();

        if (useSounds) UpdateEngineAudio();
    }

    // --- ÚJ TAPADÁS ÉS FELÜLET LOGIKA ---
    void CheckSurfaceAndApplyGrip()
    {
        // Megnézzük, min áll a bal hátsó kerék (elég egyet vizsgálni általában)
        WheelHit hit;
        float currentGripMultiplier = asphaltGrip; // Alapból aszfalt tapadás (magas)
        bool onGravel = false;
        bool onGrass = false;

        if (rearLeftCollider.GetGroundHit(out hit))
        {
            // Ha a talaj Tag-je "Grass"
            if (hit.collider.CompareTag(slipperySurfaceTag))
            {
                currentGripMultiplier = grassGrip; // Leesik a tapadás
                onGrass = true;
            }

            // Ha a talaj Tag-je "Gravel"
            if (hit.collider.CompareTag(gravelSurfaceTag))
            {
                onGravel = true;
            }
        }

        // Ha behúzzuk a kéziféket, akkor a tapadás drasztikusan csökken
        if (isTractionLocked)
        {
            // Kézifék drift logika (eredeti logika módosítva)
            ApplyFrictionToWheels(grassGrip * 0.5f); // Nagyon csúszik
        }
        else
        {
            // Normál vezetés (Aszfalt, Fű vagy Kavics)
            ApplyFrictionToWheels(currentGripMultiplier);

            // Ha kavicson vagy füvön vagyunk, módosítjuk a coastingDrag-ot és a maxSpeed-et
            if (onGravel)
            {
                coastingDrag = defaultCoastingDrag * gravelCoastingMultiplier;
                maxSpeed = Mathf.RoundToInt(defaultMaxSpeed * gravelMaxSpeedMultiplier);
            }
            else if (onGrass)
            {
                coastingDrag = defaultCoastingDrag * grassCoastingMultiplier;
                maxSpeed = Mathf.RoundToInt(defaultMaxSpeed * grassMaxSpeedMultiplier);
            }
            else
            {
                // Visszaállítjuk az alapértékeket, ha nem kavicson vagy füvön vagyunk
                coastingDrag = defaultCoastingDrag;
                maxSpeed = defaultMaxSpeed;
            }
        }
    }

    void ApplyFrictionToWheels(float stiffnessMultiplier)
    {
        // Mind a 4 kerékre alkalmazzuk a keménységet (Stiffness)
        // A Stiffness növelése =jobb tapadás = kevesebb drift

        ModifyWheelStiffness(frontLeftCollider, ref FL_Sideways, stiffnessMultiplier);
        ModifyWheelStiffness(frontRightCollider, ref FR_Sideways, stiffnessMultiplier);
        ModifyWheelStiffness(rearLeftCollider, ref RL_Sideways, stiffnessMultiplier);
        ModifyWheelStiffness(rearRightCollider, ref RR_Sideways, stiffnessMultiplier);
    }

    void ModifyWheelStiffness(WheelCollider wc, ref WheelFrictionCurve baseCurve, float mult)
    {
        WheelFrictionCurve curve = baseCurve;
        curve.stiffness = baseCurve.stiffness * mult;
        wc.sidewaysFriction = curve;
    }

    void SaveDefaultFriction()
    {
        FL_Sideways = frontLeftCollider.sidewaysFriction;
        FR_Sideways = frontRightCollider.sidewaysFriction;
        RL_Sideways = rearLeftCollider.sidewaysFriction;
        RR_Sideways = rearRightCollider.sidewaysFriction;
        defaultSlip = FL_Sideways.extremumSlip;
    }

    // --- HANG LOGIKA (VÁLTOZATLAN) ---
    void UpdateEngineAudio()
    {
        if (engineSources == null || engineClips.Length == 0) return;
        float absSpeed = Mathf.Abs(carSpeed);
        bool isGasPedalPressed = Mathf.Abs(throttleAxis) > 0.1f;

        for (int i = 0; i < gearSpeeds.Length; i++) { if (absSpeed < gearSpeeds[i]) { currentGear = i + 1; break; } }
        float minGearSpeed = (currentGear == 1) ? 0 : gearSpeeds[currentGear - 2];
        float maxGearSpeed = gearSpeeds[currentGear - 1];
        float gearPercent = Mathf.InverseLerp(minGearSpeed, maxGearSpeed, absSpeed);
        float speedBasedRPM = Mathf.Lerp(MIN_IDLE_RPM, 1.0f, gearPercent);

        if (isGasPedalPressed)
        {
            targetRPM = speedBasedRPM;
            engineLoad = Mathf.Lerp(engineLoad, 1.0f, Time.deltaTime * 5f);
        }
        else
        {
            targetRPM = Mathf.Max(speedBasedRPM * 0.6f, MIN_IDLE_RPM);
            engineLoad = Mathf.Lerp(engineLoad, 0.0f, Time.deltaTime * 3f);
        }
        if (absSpeed < 2f) targetRPM = MIN_IDLE_RPM;

        float inertia = isGasPedalPressed ? revUpSpeed : revDownSpeed;
        engineRPM = Mathf.Lerp(engineRPM, targetRPM, Time.deltaTime * inertia);
        engineRPM = Mathf.Max(engineRPM, MIN_IDLE_RPM);

        float adjustedRPM = (engineRPM - MIN_IDLE_RPM) / (1.0f - MIN_IDLE_RPM);
        if (adjustedRPM < 0) adjustedRPM = 0;
        float exactIndex = adjustedRPM * (engineClips.Length - 1);
        int indexA = Mathf.Clamp(Mathf.FloorToInt(exactIndex), 0, engineClips.Length - 1);
        int indexB = Mathf.Clamp(Mathf.CeilToInt(exactIndex), 0, engineClips.Length - 1);

        AudioSource sourceA = GetSourceForClip(indexA);
        AudioSource sourceB = GetSourceForClip(indexB);

        float blend = exactIndex - indexA;
        float loadPitchFactor = Mathf.Lerp(0.85f, 1.0f, engineLoad);
        float loadVolFactor = Mathf.Lerp(0.5f, 1.0f, engineLoad);
        float rpmVolCurve = Mathf.Lerp(0.4f, 1.0f, adjustedRPM);
        float masterVol = rpmVolCurve * loadVolFactor;

        if (sourceA != null)
        {
            sourceA.volume = (1.0f - blend) * masterVol;
            sourceA.pitch = (1.0f + (blend * 0.1f)) * loadPitchFactor;
        }
        if (sourceB != null)
        {
            sourceB.volume = blend * masterVol;
            sourceB.pitch = (0.9f + (blend * 0.1f)) * loadPitchFactor;
        }
        MuteUnusedSources(sourceA, sourceB);
        if (tireSource != null)
        {
            bool screeching = (isDrifting || (isTractionLocked && absSpeed > 12f));
            tireSource.volume = Mathf.Lerp(tireSource.volume, screeching ? 1.0f : 0.0f, Time.deltaTime * 10f);
        }
    }

    AudioSource GetSourceForClip(int clipIndex)
    {
        AudioClip targetClip = engineClips[clipIndex];
        foreach (var s in engineSources) if (s.clip == targetClip && s.isPlaying) return s;
        AudioSource bestCandidate = engineSources[0]; float lowestVol = 100f;
        foreach (var s in engineSources) { if (!s.isPlaying) { bestCandidate = s; break; } if (s.volume < lowestVol) { lowestVol = s.volume; bestCandidate = s; } }
        bestCandidate.clip = targetClip; bestCandidate.volume = 0f; bestCandidate.time = 0f; bestCandidate.Play(); return bestCandidate;
    }
    void MuteUnusedSources(AudioSource activeA, AudioSource activeB)
    {
        foreach (var s in engineSources) { if (s != activeA && s != activeB) { s.volume = Mathf.Lerp(s.volume, 0f, Time.deltaTime * 10f); if (s.volume < 0.01f && s.isPlaying) s.Stop(); } }
    }
    void CalculateGearRatios()
    {
        gearSpeeds = new float[numberOfGears]; for (int i = 0; i < numberOfGears; i++) { float t = (float)(i + 1) / numberOfGears; gearSpeeds[i] = Mathf.Lerp(0, maxSpeed, Mathf.Pow(t, 0.7f)); }
    }

    // --- INPUT KEZELÉS ---
    // --- INPUT KEZELÉS: Billentyűzet VS Kontroller szétválasztva ---
    void HandleInput()
    {
        // 1. Megnézzük, hogy a játékos a billentyűzetet használja-e kormányzásra
        bool isKeyboardSteering = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        float rawSteeringInput = 0f;

        if (isKeyboardSteering)
        {
            // Billentyűzetnél a "Raw" értéket kérjük el (azonnal 1 vagy -1), nincs Unity-s simítás
            rawSteeringInput = Input.GetAxisRaw("Horizontal");
        }
        else
        {
            // Kontrollernél a sima GetAxis-t használjuk (ez a joystick analóg értéke)
            rawSteeringInput = Input.GetAxis("Horizontal");
        }

        // Átadjuk az adatot a kormányzásnak + azt is, hogy kontroller-e (ha NEM billentyűzet)
        ApplySteering(rawSteeringInput, !isKeyboardSteering);


        // --- INNEN A GÁZ/FÉK RÉSZ (Ez maradt a régi, RT/LT-vel) ---
        float gasTrigger = Input.GetAxis("RT_Gas");
        float brakeTrigger = Input.GetAxis("LT_Brake");
        float keyboardThrottle = Input.GetAxis("Vertical");
        float combinedControllerThrottle = gasTrigger - brakeTrigger;
        float finalThrottleInput = 0f;

        if (Mathf.Abs(combinedControllerThrottle) > 0.05f) finalThrottleInput = combinedControllerThrottle;
        else finalThrottleInput = keyboardThrottle;

        if (useTouchControls && touchControlsSetup) // Touch logika...
        {
            if (throttlePTI.buttonPressed) finalThrottleInput = 1f;
            else if (reversePTI.buttonPressed) finalThrottleInput = -1f;
            if (turnRightPTI.buttonPressed) { rawSteeringInput = 1f; ApplySteering(1f, false); } // Touch mint billentyűzet
            else if (turnLeftPTI.buttonPressed) { rawSteeringInput = -1f; ApplySteering(-1f, false); }
        }

        // Mozgás logika
        if (finalThrottleInput > 0.1f)
        {
            CancelInvoke("DecelerateCar"); deceleratingCar = false; GoForward();
            throttleAxis = finalThrottleInput;
        }
        else if (finalThrottleInput < -0.1f)
        {
            CancelInvoke("DecelerateCar"); deceleratingCar = false; GoReverse();
            throttleAxis = finalThrottleInput;
        }
        else if (!Input.GetButton("Jump") && !deceleratingCar)
        {
            InvokeRepeating("DecelerateCar", 0f, 0.1f); deceleratingCar = true;
        }
        else if (Mathf.Abs(finalThrottleInput) <= 0.1f) ThrottleOff();

        if (Input.GetButton("Jump")) { CancelInvoke("DecelerateCar"); deceleratingCar = false; Handbrake(); }
        else if (Input.GetButtonUp("Jump")) RecoverTraction();
    }


    // --- KORMÁNYZÁS: Külön logika a Kontrollernek és a Billentyűzetnek ---
    void ApplySteering(float input, bool isGamepad)
    {
        float targetInput = input;
        float currentSpeed = steeringSpeed; // Ez az alap beállítás az Inspectorból

        if (isGamepad)
        {
            // --- CSAK KONTROLLER LOGIKA ---
            // 1. Görbítjük az inputot (kicsi mozdulat = alig fordul, nagy mozdulat = nagyon fordul)
            targetInput = Mathf.Pow(Mathf.Abs(input), 1.5f) * Mathf.Sign(input);

            // 2. Lassítjuk a kormányzást a precizitásért (pl. felére vesszük az alap sebességet)
            currentSpeed = steeringSpeed * 0.5f;

            // 3. (Opccionális) Sebességfüggő korlátozás csak kontrollerre
            float speedFactor = Mathf.InverseLerp(10f, 150f, Mathf.Abs(carSpeed));
            float maxAngleMultiplier = Mathf.Lerp(1f, 0.5f, speedFactor); // Gyorsan max 50%-ig fordulhat
            targetInput *= maxAngleMultiplier;
        }
        else
        {
            // --- CSAK BILLENTYŰZET LOGIKA ---
            // Billentyűzetnél gyorsabb reakció kell, különben lomha lesz
            currentSpeed = steeringSpeed * 2.0f;
        }

        // Közös végrehajtó rész
        steeringAxis = Mathf.MoveTowards(steeringAxis, targetInput, Time.deltaTime * 10f * currentSpeed);
        steeringAxis = Mathf.Clamp(steeringAxis, -1f, 1f);

        float angle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, angle, currentSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, angle, currentSpeed);
    }

    // --- FIZIKA ÉS MOZGÁS (Javított Deceleration) ---
    public void GoForward() { UpdateDriftState(); 
    // If we're moving backwards, apply brakes first (don't immediately set forward throttle)
    if (localVelocityZ < -1f) 
    { 
        throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 10f); 
        Brakes(); 
    } 
    else 
    { 
        ReleaseBrakes();
        throttleAxis = Mathf.MoveTowards(throttleAxis, 1f, Time.deltaTime * 3f); 
        ApplyDrive(1f); 
    } 
}
    public void GoReverse() { UpdateDriftState(); 
    // If we're moving forward, apply brakes first (don't immediately set reverse throttle)
    if (localVelocityZ > 1f) 
    { 
        throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 10f); 
        Brakes(); 
    } 
    else 
    { 
        ReleaseBrakes();
        throttleAxis = Mathf.MoveTowards(throttleAxis, -1f, Time.deltaTime * 3f); 
        ApplyDrive(-1f); 
    } 
}

    public void DecelerateCar()
    {
        UpdateDriftState();
        throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 10f);

        // EZT AZ EGY SORT VÁLTOZTATTUK MEG DRASZTIKUSAN A KIGURULÁSHOZ:
        // A coastingDrag alapból 0.05f, ami nagyon kicsi ellenállást jelent -> messzire gurul
        carRigidbody.linearVelocity *= (1f / (1f + coastingDrag));

        ApplyTorque(0);
        if (carRigidbody.linearVelocity.magnitude < 0.25f) { carRigidbody.linearVelocity = Vector3.zero; CancelInvoke("DecelerateCar"); }
    }

    public void Brakes()
    {
        // Motor nyomaték nullázása MINDEN kerékre
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;
        rearLeftCollider.motorTorque = 0f;
        rearRightCollider.motorTorque = 0f;

        // Sebességfüggő fékhatás számítása
        float currentSpeed = Mathf.Abs(carSpeed);
        float dynamicBrakeForce;

        if (currentSpeed > 120f)
        {
            // Nagyon nagy sebesség (120+ km/h): Mérsékelt fékhatás
            dynamicBrakeForce = brakeForce * 0.8f;
        }
        else if (currentSpeed > 80f)
        {
            // Nagy sebesség (80-120 km/h): Normál fékhatás
            dynamicBrakeForce = brakeForce * 1.0f;
        }
        else if (currentSpeed > 40f)
        {
            // Közepes sebesség (40-80 km/h): Kicsit erősebb
            dynamicBrakeForce = brakeForce * 1.2f;
        }
        else if (currentSpeed > 10f)
        {
            // Lassú (10-40 km/h): Erősebb fékhatás
            dynamicBrakeForce = brakeForce * 1.5f;
        }
        else if (currentSpeed > 2f)
        {
            // Nagyon lassú (2-10 km/h): Még erősebb
            dynamicBrakeForce = brakeForce * 2f;
        }
        else
        {
            // Majdnem áll (< 2 km/h): Maximum fék
            dynamicBrakeForce = brakeForce * 3f;
        }

        // Fék alkalmazása mind a 4 kerékre
        frontLeftCollider.brakeTorque = dynamicBrakeForce;
        frontRightCollider.brakeTorque = dynamicBrakeForce;
        rearLeftCollider.brakeTorque = dynamicBrakeForce;
        rearRightCollider.brakeTorque = dynamicBrakeForce;

        // Extra: Nagyon enyhe sebesség csökkentés nagy sebességnél
        if (currentSpeed > 50f)
        {
            carRigidbody.linearVelocity *= 0.995f; // Csak 0.5% lassítás
        }
        else if (currentSpeed > 2f)
        {
            carRigidbody.linearVelocity *= 0.99f; // 1% lassítás
        }
        else if (currentSpeed > 0.5f)
        {
            carRigidbody.linearVelocity *= 0.96f; // 4% lassítás lassan
        }
        else
        {
            // Ha majdnem áll, állítsuk meg teljesen
            carRigidbody.linearVelocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;
        }
    }

    void ReleaseBrakes()
    {
        frontLeftCollider.brakeTorque = 0f; frontRightCollider.brakeTorque = 0f; 
        rearLeftCollider.brakeTorque = 0f; rearRightCollider.brakeTorque = 0f;
    }
    public void ThrottleOff() { ApplyTorque(0); }
    void ApplyDrive(float direction) { float currentMax = direction > 0 ? maxSpeed : maxReverseSpeed; if (Mathf.Abs(carSpeed) < currentMax) { ReleaseBrakes(); ApplyTorque((accelerationMultiplier * 50f) * throttleAxis); } else ApplyTorque(0); }
    void ApplyTorque(float torque) { frontLeftCollider.motorTorque = torque; frontRightCollider.motorTorque = torque; rearLeftCollider.motorTorque = torque; rearRightCollider.motorTorque = torque; }
    void UpdateDriftState() { isDrifting = Mathf.Abs(localVelocityX) > 2.5f; DriftCarPS(); }
    public void TurnLeft() { AdjustSteering(-1f); }
    public void TurnRight() { AdjustSteering(1f); }
    public void ResetSteeringAngle() { AdjustSteering(0f); }
    void AdjustSteering(float target)
    {
        if (target == 0) steeringAxis = Mathf.MoveTowards(steeringAxis, 0, Time.deltaTime * 10f * steeringSpeed);
        else steeringAxis = Mathf.Clamp(steeringAxis + (target * Time.deltaTime * 10f * steeringSpeed), -1f, 1f);
        float angle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, angle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, angle, steeringSpeed);
    }

    // collision sound when touching objects tagged "Border"
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null) return;
        if (!collision.collider.CompareTag("Border")) return;
        if (collisionClip == null) return;

        Vector3 hitPoint = collision.contacts != null && collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

        if (useSounds && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound3D(collisionClip, hitPoint, null, collisionVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(collisionClip, hitPoint, collisionVolume);
        }
    }

    // --- KÉZIFÉK LOGIKA (Egyszerűsítve a felületkezeléshez) ---
    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");
        driftingAxis = Mathf.MoveTowards(driftingAxis, 1f, Time.deltaTime);
        isTractionLocked = true;
        UpdateDriftState();
        DriftCarPS();
    }
    public void RecoverTraction()
    {
        isTractionLocked = false;
        driftingAxis = Mathf.MoveTowards(driftingAxis, 0f, Time.deltaTime / 1.5f);
        if (driftingAxis > 0) Invoke("RecoverTraction", Time.deltaTime);
    }

    void SetupTouchControls()
    {
        if (useTouchControls)
        {
            if (throttleButton && reverseButton && turnLeftButton && turnRightButton && handbrakeButton)
            {
                throttlePTI = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePTI = reverseButton.GetComponent<PrometeoTouchInput>();
                turnLeftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();
                turnRightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();
                handbrakePTI = handbrakeButton.GetComponent<PrometeoTouchInput>();
                touchControlsSetup = true;
            }
            else
                Debug.LogWarning("Touch controls not set up!");
        }
    }
    void StopEffects()
    {
        if (RLWParticleSystem)
            RLWParticleSystem.Stop();
        if (RRWParticleSystem)
            RRWParticleSystem.Stop();
        if (RLWTireSkid) RLWTireSkid.emitting = false;
        if (RRWTireSkid) RRWTireSkid.emitting = false;
    }
    public void CarSpeedUI()
    {
        if (useUI && carSpeedText)
            carSpeedText.text = Mathf.RoundToInt(Mathf.Abs(carSpeed)).ToString();

        if (useUI && gearText != null)
        {

            gearText.text = currentGear.ToString();
        }
    }
    public void DriftCarPS()
    {
        if (!useEffects)
            return;
        if (isDrifting)
        {
            RLWParticleSystem.Play();
            RRWParticleSystem.Play();
        }
        else
        {
            RLWParticleSystem.Stop();
            RRWParticleSystem.Stop();
        }
        bool emit = (isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f;
        RLWTireSkid.emitting = emit;
        RRWTireSkid.emitting = emit;
    }
    void AnimateWheelMeshes()
    {
        UpdateWheel(frontLeftCollider, frontLeftMesh);
        UpdateWheel(frontRightCollider, frontRightMesh);
        UpdateWheel(rearLeftCollider, rearLeftMesh);
        UpdateWheel(rearRightCollider, rearRightMesh);
    }
    void UpdateWheel(WheelCollider col, GameObject mesh)
    {
        Vector3 p;
        Quaternion r;
        col.GetWorldPose(out p, out r);
        mesh.transform.position = p;
        mesh.transform.rotation = r;
    }
}