using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AudioController;

public class DriftCarController : MonoBehaviour
{
    // --- CAR SETUP ---
    [Space(20)]
    [Header("SPEED SETTINGS")]
    [Range(100, 380)] public int maxSpeed = 280;
    [Range(10, 120)] public int maxReverseSpeed = 45;
    [Range(1, 16)] public int accelerationMultiplier = 6;

    [Space(10)]
    [Header("STEERING")]
    [Range(10, 50)] public int maxSteeringAngle = 35;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.5f;

    [Space(10)]
    [Header("BRAKES")]
    [Range(100, 1000)] public int brakeForce = 350;

    [Space(10)]
    [Header("DRIFT SETTINGS")]
    [Tooltip("Hátsó kerekek tapadása normál állapotban (alacsonyabb = könnyebb drift)")]
    [Range(0.5f, 3f)] public float rearGripNormal = 1.5f;

    [Tooltip("Hátsó kerekek tapadása drift közben")]
    [Range(0.1f, 1f)] public float rearGripDrift = 0.4f;

    [Tooltip("Elsõ kerekek tapadása (magasabb = jobb kontroll)")]
    [Range(1f, 4f)] public float frontGrip = 2.5f;

    [Tooltip("Milyen gyorsan csúszik ki a hátulja gázadáskor")]
    [Range(0.5f, 3f)] public float driftIntensity = 1.5f;

    [Tooltip("Mennyire segít a gáz fenntartani a driftet")]
    [Range(0.1f, 1f)] public float throttleDriftBoost = 0.5f;

    [Tooltip("Drift közben mennyire lassuljon (0 = nem lassul)")]
    [Range(0f, 0.1f)] public float driftDrag = 0.01f;

    [Tooltip("Normál kigurulási lassulás")]
    [Range(0.01f, 0.2f)] public float coastingDrag = 0.03f;

    [Space(10)]
    public Vector3 bodyMassCenter = new Vector3(0, -0.5f, 0.3f);

    // --- SURFACE ---
    [Header("SURFACE & TRACTION")]
    [Tooltip("A tag neve, amit a fû objektumokra raksz")]
    public string slipperySurfaceTag = "Grass";
    [Range(0.1f, 1f)] public float grassGrip = 0.5f;
    [Range(1f, 5f)] public float grassCoastingMultiplier = 1.5f;
    [Range(0.1f, 1f)] public float grassMaxSpeedMultiplier = 0.6f;

    public string gravelSurfaceTag = "Gravel";
    [Range(1f, 5f)] public float gravelCoastingMultiplier = 3f;
    [Range(0.1f, 1f)] public float gravelMaxSpeedMultiplier = 0.3f;

    // --- WHEELS ---
    [Header("WHEELS")]
    public GameObject frontLeftMesh; public WheelCollider frontLeftCollider;
    public GameObject frontRightMesh; public WheelCollider frontRightCollider;
    public GameObject rearLeftMesh; public WheelCollider rearLeftCollider;
    public GameObject rearRightMesh; public WheelCollider rearRightCollider;

    // --- LIGHTS ---
    [Header("LIGHTS")]
    public GameObject rearLeftBrakeLight;
    public GameObject rearRightBrakeLight;

    // --- EFFECTS ---
    [Header("EFFECTS")]
    public bool useEffects = false;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    [Header("SMOKE EFFECTS")]
    [Tooltip("Bal hátsó kerék füst effect")]
    public ParticleSystem RLWSmokeParticle;
    [Tooltip("Jobb hátsó kerék füst effect")]
    public ParticleSystem RRWSmokeParticle;
    [Tooltip("Ennyi foknál kezd füstölni")]
    [Range(5f, 30f)] public float smokeStartAngle = 10f;
    [Tooltip("Ennyi foknál lesz maximális a füst")]
    [Range(15f, 60f)] public float smokeMaxAngle = 35f;
    [Tooltip("Minimum sebesség a füsthöz")]
    [Range(5f, 30f)] public float smokeMinSpeed = 15f;
    [Tooltip("Burnout/elkaparás füst engedélyezése")]
    public bool enableBurnoutSmoke = true;
    [Tooltip("Minimum emission rate")]
    [Range(10f, 100f)] public float smokeMinEmission = 20f;
    [Tooltip("Maximum emission rate")]
    [Range(50f, 300f)] public float smokeMaxEmission = 150f;
    [Tooltip("Minimum füst méret")]
    [Range(0.5f, 3f)] public float smokeMinSize = 1f;
    [Tooltip("Maximum füst méret")]
    [Range(2f, 10f)] public float smokeMaxSize = 5f;

    // --- UI ---
    [Header("UI")]
    public bool useUI = false;
    public Text carSpeedText;
    public Text gearText;
    public Text driftAngleText;

    // --- AUDIO ---
    [Space(20)]
    [Header("AUDIO")]
    public bool useSounds = false;
    public AudioClip[] engineClips;
    public AudioClip tireScreechClip;
    public AudioClip collisionClip;
    [Range(0f, 1f)] public float collisionVolume = 1f;

    [Header("CURB SOUNDS")]
    public AudioClip curbClip;
    [Range(0f, 1f)] public float curbVolume = 0.8f;
    public string curbTag = "Curb";
    [Range(0.02f, 0.2f)] public float curbSoundCooldown = 0.06f;
    private float lastCurbSoundTime = 0f;
    private int wheelsOnCurb = 0;

    [Header("ENGINE AUDIO")]
    [Range(4, 8)] public int numberOfGears = 6;
    [Range(1f, 20f)] public float revUpSpeed = 8f;
    [Range(0.5f, 10f)] public float revDownSpeed = 3f;

    private AudioSource[] engineSources;
    private AudioSource tireSource;
    private float engineRPM = 0.1f;
    private float targetRPM = 0f;
    private int currentGear = 1;
    private float[] gearSpeeds;
    private float engineLoad = 0f;
    private const float MIN_IDLE_RPM = 0.20f;

    // --- CONTROLS ---
    [Header("TOUCH CONTROLS")]
    public bool useTouchControls = false;
    public GameObject throttleButton; PrometeoTouchInput throttlePTI;
    public GameObject reverseButton; PrometeoTouchInput reversePTI;
    public GameObject turnRightButton; PrometeoTouchInput turnRightPTI;
    public GameObject turnLeftButton; PrometeoTouchInput turnLeftPTI;
    public GameObject handbrakeButton; PrometeoTouchInput handbrakePTI;

    // --- PUBLIC DATA ---
    [HideInInspector] public float carSpeed;
    [HideInInspector] public float driftAngle;
    [HideInInspector] public bool isDrifting;
    [HideInInspector] public bool isTractionLocked;

    // --- PRIVATE ---
    Rigidbody carRigidbody;
    float steeringAxis;
    float throttleAxis;
    float driftingAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;
    bool touchControlsSetup = false;

    // Friction curves
    WheelFrictionCurve FL_Sideways, FR_Sideways, RL_Sideways, RR_Sideways;
    WheelFrictionCurve FL_Forward, FR_Forward, RL_Forward, RR_Forward;

    float defaultCoastingDrag;
    int defaultMaxSpeed;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;
        carRigidbody.linearDamping = 0f;
        carRigidbody.angularDamping = 0.5f;

        SaveDefaultFriction();
        SetupInitialFriction();
        CalculateGearRatios();

        defaultCoastingDrag = coastingDrag;
        defaultMaxSpeed = maxSpeed;

        ToggleBrakeLights(false);

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
            if (tireScreechClip != null)
                tireSource = AudioManager.Instance.CreateLoopingSource(this.transform, tireScreechClip, 0f, true);
        }
        else if (useSounds)
        {
            Debug.LogError("Nincs AudioManager!");
            useSounds = false;
        }

        if (!useEffects) StopEffects();
        SetupTouchControls();
        SetupWheelCurbDetectors();
    }

    void SetupSource(AudioSource s)
    {
        s.spatialBlend = 1.0f;
        s.dopplerLevel = 0.5f;
        s.minDistance = 5f;
        s.maxDistance = 800f;
        s.playOnAwake = false;
    }

    void SetupInitialFriction()
    {
        // Elsõ kerekek - magas tapadás a kontrollhoz
        SetWheelFriction(frontLeftCollider, frontGrip, frontGrip);
        SetWheelFriction(frontRightCollider, frontGrip, frontGrip);

        // Hátsó kerekek - alacsonyabb tapadás a drifthez
        SetWheelFriction(rearLeftCollider, 2f, rearGripNormal);
        SetWheelFriction(rearRightCollider, 2f, rearGripNormal);
    }

    void SetWheelFriction(WheelCollider wc, float forwardStiffness, float sidewaysStiffness)
    {
        WheelFrictionCurve forward = wc.forwardFriction;
        forward.stiffness = forwardStiffness;
        wc.forwardFriction = forward;

        WheelFrictionCurve sideways = wc.sidewaysFriction;
        sideways.stiffness = sidewaysStiffness;
        wc.sidewaysFriction = sideways;
    }

    void Update()
    {
        // Sebesség számítás
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        // Drift szög számítás
        CalculateDriftAngle();

        HandleInput();
        UpdateDriftPhysics();
        AnimateWheelMeshes();
        CheckSurface();

        if (useSounds) UpdateEngineAudio();

        // UI frissítés - akkor is megy ha van Text behúzva
        UpdateUI();
    }

    void CalculateDriftAngle()
    {
        if (carRigidbody.linearVelocity.magnitude > 2f)
        {
            Vector3 velocityDir = carRigidbody.linearVelocity.normalized;
            Vector3 forwardDir = transform.forward;
            driftAngle = Vector3.SignedAngle(velocityDir, forwardDir, Vector3.up);
        }
        else
        {
            driftAngle = 0f;
        }

        // Drift állapot frissítése
        isDrifting = Mathf.Abs(driftAngle) > 15f && Mathf.Abs(carSpeed) > 10f;
    }

    void UpdateDriftPhysics()
    {
        float absSpeed = Mathf.Abs(carSpeed);
        bool isThrottlePressed = throttleAxis > 0.1f;
        float sidewaysSpeed = Mathf.Abs(localVelocityX);

        // Hátsó kerék tapadás dinamikus állítása
        float targetRearGrip;

        if (isTractionLocked)
        {
            // Kézifék - nagyon alacsony tapadás
            targetRearGrip = rearGripDrift * 0.5f;
        }
        else if (isDrifting || sidewaysSpeed > 3f)
        {
            // Drift közben - alacsony tapadás, de gázzal növelhetõ
            float throttleGripBonus = isThrottlePressed ? (throttleAxis * throttleDriftBoost * 0.3f) : 0f;
            targetRearGrip = rearGripDrift + throttleGripBonus;
        }
        else if (isThrottlePressed && absSpeed > 20f)
        {
            // Gázadáskor nagy sebességnél - könnyebb kicsúszás
            float speedFactor = Mathf.InverseLerp(20f, 100f, absSpeed);
            float steerFactor = Mathf.Abs(steeringAxis);
            float driftTendency = speedFactor * steerFactor * driftIntensity * throttleAxis;
            targetRearGrip = Mathf.Lerp(rearGripNormal, rearGripDrift, driftTendency);
        }
        else
        {
            // Normál vezetés
            targetRearGrip = rearGripNormal;
        }

        // Simán állítjuk a tapadást
        WheelFrictionCurve rlSideways = rearLeftCollider.sidewaysFriction;
        WheelFrictionCurve rrSideways = rearRightCollider.sidewaysFriction;

        rlSideways.stiffness = Mathf.Lerp(rlSideways.stiffness, targetRearGrip, Time.deltaTime * 5f);
        rrSideways.stiffness = Mathf.Lerp(rrSideways.stiffness, targetRearGrip, Time.deltaTime * 5f);

        rearLeftCollider.sidewaysFriction = rlSideways;
        rearRightCollider.sidewaysFriction = rrSideways;

        // Drift effektek
        UpdateDriftEffects();
    }

    void UpdateDriftEffects()
    {
        if (!useEffects) return;

        bool showEffects = (isDrifting || isTractionLocked) && Mathf.Abs(carSpeed) > 10f;

        if (showEffects)
        {
            if (RLWParticleSystem != null && !RLWParticleSystem.isPlaying) RLWParticleSystem.Play();
            if (RRWParticleSystem != null && !RRWParticleSystem.isPlaying) RRWParticleSystem.Play();
        }
        else
        {
            if (RLWParticleSystem != null && RLWParticleSystem.isPlaying) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null && RRWParticleSystem.isPlaying) RRWParticleSystem.Stop();
        }

        if (RLWTireSkid != null) RLWTireSkid.emitting = showEffects;
        if (RRWTireSkid != null) RRWTireSkid.emitting = showEffects;

        // Smoke effect
        UpdateSmokeEffects();
    }

    void UpdateSmokeEffects()
    {
        if (RLWSmokeParticle == null && RRWSmokeParticle == null) return;

        float absDriftAngle = Mathf.Abs(driftAngle);
        float absSpeed = Mathf.Abs(carSpeed);
        float absWheelSpin = GetWheelSpinAmount();
        float angularSpeed = Mathf.Abs(carRigidbody.angularVelocity.y); // Forgási sebesség

        // --- KÜLÖNBÖZÕ FÜST TRIGGEREK ---

        // 1. Drift füst (oldalazás)
        bool driftSmoke = absDriftAngle >= smokeStartAngle && absSpeed >= smokeMinSpeed;
        float driftIntensity = 0f;
        if (driftSmoke)
        {
            driftIntensity = Mathf.InverseLerp(smokeStartAngle, smokeMaxAngle, absDriftAngle);
        }

        // 2. Burnout füst (elkaparás - gáz + nem mozog vagy lassan mozog)
        bool burnoutSmoke = false;
        float burnoutIntensity = 0f;
        if (enableBurnoutSmoke)
        {
            bool isThrottleHard = throttleAxis > 0.7f;
            bool isSlowOrStopped = absSpeed < 20f;
            bool wheelsSpinning = absWheelSpin > 50f; // Kerék pörög de autó áll

            burnoutSmoke = isThrottleHard && isSlowOrStopped && wheelsSpinning;
            if (burnoutSmoke)
            {
                burnoutIntensity = Mathf.InverseLerp(50f, 200f, absWheelSpin);
                burnoutIntensity *= throttleAxis; // Gáz mértéke befolyásolja
            }
        }

        // 3. Körözés füst (kézifék + kormány + mozgás)
        bool donutSmoke = false;
        float donutIntensity = 0f;
        if (isTractionLocked && Mathf.Abs(steeringAxis) > 0.5f)
        {
            donutSmoke = true;
            donutIntensity = Mathf.Abs(steeringAxis) * Mathf.Clamp01(absSpeed / 30f);

            // Ha gázt is ad körözés közben, még több füst
            if (throttleAxis > 0.3f)
            {
                donutIntensity *= 1f + throttleAxis;
            }
        }

        // 4. Kézifék csúszás
        bool handbrakeSmoke = isTractionLocked && absSpeed > 10f;
        float handbrakeIntensity = 0f;
        if (handbrakeSmoke)
        {
            handbrakeIntensity = Mathf.InverseLerp(10f, 60f, absSpeed);
        }

        // 5. Kipördülés/spinout füst (nagy forgás + valamilyen sebesség)
        bool spinoutSmoke = false;
        float spinoutIntensity = 0f;
        if (angularSpeed > 1.5f && absSpeed > 5f) // Gyorsan forog és mozog
        {
            spinoutSmoke = true;
            spinoutIntensity = Mathf.InverseLerp(1.5f, 5f, angularSpeed);
            spinoutIntensity *= Mathf.Clamp01(absSpeed / 30f);
        }

        // 6. Nagy drift szög (90°+ - teljesen oldalazik vagy hátrafelé megy)
        bool extremeAngleSmoke = false;
        float extremeAngleIntensity = 0f;
        if (absDriftAngle > 45f && absSpeed > 8f)
        {
            extremeAngleSmoke = true;
            extremeAngleIntensity = Mathf.InverseLerp(45f, 120f, absDriftAngle);
            extremeAngleIntensity *= Mathf.Clamp01(absSpeed / 40f);
        }

        // --- ÖSSZESÍTETT INTENZITÁS ---
        float totalIntensity = Mathf.Max(driftIntensity, burnoutIntensity, donutIntensity, handbrakeIntensity, spinoutIntensity, extremeAngleIntensity);
        bool shouldSmoke = driftSmoke || burnoutSmoke || donutSmoke || handbrakeSmoke || spinoutSmoke || extremeAngleSmoke;

        if (shouldSmoke && totalIntensity > 0.05f)
        {
            totalIntensity = Mathf.Clamp01(totalIntensity);

            // Emission és méret számítás
            float emissionRate = Mathf.Lerp(smokeMinEmission, smokeMaxEmission, totalIntensity);
            float smokeSize = Mathf.Lerp(smokeMinSize, smokeMaxSize, totalIntensity);

            // Bal hátsó kerék füst
            ApplySmokeSettings(RLWSmokeParticle, emissionRate, smokeSize);

            // Jobb hátsó kerék füst
            ApplySmokeSettings(RRWSmokeParticle, emissionRate, smokeSize);
        }
        else
        {
            // Füst kikapcsolása
            if (RLWSmokeParticle != null && RLWSmokeParticle.isPlaying) RLWSmokeParticle.Stop();
            if (RRWSmokeParticle != null && RRWSmokeParticle.isPlaying) RRWSmokeParticle.Stop();
        }
    }

    void ApplySmokeSettings(ParticleSystem smoke, float emissionRate, float size)
    {
        if (smoke == null) return;

        if (!smoke.isPlaying) smoke.Play();

        var emission = smoke.emission;
        emission.rateOverTimeMultiplier = emissionRate;

        var main = smoke.main;
        main.startSizeMultiplier = size;
    }

    float GetWheelSpinAmount()
    {
        // Kerék pörgés számítás - ha a kerék gyorsabban forog mint ahogy az autó halad
        float wheelRPM = Mathf.Abs(rearLeftCollider.rpm) + Mathf.Abs(rearRightCollider.rpm);
        wheelRPM /= 2f; // Átlag

        float expectedRPM = (Mathf.Abs(carSpeed) * 1000f) / (2f * Mathf.PI * rearLeftCollider.radius * 60f);

        // Különbség - ha a kerék gyorsabban pörög mint kellene
        float spinDifference = wheelRPM - expectedRPM;

        return Mathf.Max(0f, spinDifference);
    }

    void CheckSurface()
    {
        WheelHit hit;
        bool onGravel = false;
        bool onGrass = false;

        if (rearLeftCollider.GetGroundHit(out hit))
        {
            if (hit.collider.CompareTag(slipperySurfaceTag)) onGrass = true;
            if (hit.collider.CompareTag(gravelSurfaceTag)) onGravel = true;
        }

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
            coastingDrag = defaultCoastingDrag;
            maxSpeed = defaultMaxSpeed;
        }
    }

    void SaveDefaultFriction()
    {
        FL_Sideways = frontLeftCollider.sidewaysFriction;
        FR_Sideways = frontRightCollider.sidewaysFriction;
        RL_Sideways = rearLeftCollider.sidewaysFriction;
        RR_Sideways = rearRightCollider.sidewaysFriction;

        FL_Forward = frontLeftCollider.forwardFriction;
        FR_Forward = frontRightCollider.forwardFriction;
        RL_Forward = rearLeftCollider.forwardFriction;
        RR_Forward = rearRightCollider.forwardFriction;
    }

    void HandleInput()
    {
        // Kormányzás
        bool isKeyboardSteering = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                                   Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        float rawSteeringInput = isKeyboardSteering ? Input.GetAxisRaw("Horizontal") : Input.GetAxis("Horizontal");
        ApplySteering(rawSteeringInput, !isKeyboardSteering);

        // Gáz/Fék
        float gasTrigger = Input.GetAxis("RT_Gas");
        float brakeTrigger = Input.GetAxis("LT_Brake");
        float keyboardThrottle = Input.GetAxis("Vertical");
        float combinedControllerThrottle = gasTrigger - brakeTrigger;
        float finalThrottleInput = Mathf.Abs(combinedControllerThrottle) > 0.05f ? combinedControllerThrottle : keyboardThrottle;

        // Touch kontroll
        if (useTouchControls && touchControlsSetup)
        {
            if (throttlePTI.buttonPressed) finalThrottleInput = 1f;
            else if (reversePTI.buttonPressed) finalThrottleInput = -1f;
            if (turnRightPTI.buttonPressed) ApplySteering(1f, false);
            else if (turnLeftPTI.buttonPressed) ApplySteering(-1f, false);
        }

        // Gáz/Fék alkalmazása
        if (finalThrottleInput > 0.1f)
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            GoForward();
            throttleAxis = finalThrottleInput;
        }
        else if (finalThrottleInput < -0.1f)
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            GoReverse();
            throttleAxis = finalThrottleInput;
        }
        else
        {
            if (!Input.GetButton("Jump") && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
            ThrottleOff();
        }

        // Kézifék
        if (Input.GetButton("Jump"))
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            Handbrake();
        }
        else if (Input.GetButtonUp("Jump"))
        {
            RecoverTraction();
        }
    }

    void ApplySteering(float input, bool isGamepad)
    {
        float targetInput = input;
        float currentSpeed = steeringSpeed;

        if (isGamepad)
        {
            targetInput = Mathf.Pow(Mathf.Abs(input), 1.5f) * Mathf.Sign(input);
            currentSpeed = steeringSpeed * 0.5f;
        }
        else
        {
            currentSpeed = steeringSpeed * 2.0f;
        }

        // Drift közben gyorsabb kormányzás az ellenkormányhoz
        if (isDrifting)
        {
            currentSpeed *= 1.5f;
        }

        steeringAxis = Mathf.MoveTowards(steeringAxis, targetInput, Time.deltaTime * 10f * currentSpeed);
        steeringAxis = Mathf.Clamp(steeringAxis, -1f, 1f);

        float angle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, angle, currentSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, angle, currentSpeed);
    }

    public void GoForward()
    {
        if (localVelocityZ < -1f)
        {
            Brakes();
        }
        else
        {
            ReleaseBrakes();
            throttleAxis = Mathf.MoveTowards(throttleAxis, 1f, Time.deltaTime * 3f);
            ApplyRWDDrive(1f);
        }
    }

    public void GoReverse()
    {
        if (localVelocityZ > 1f)
        {
            Brakes();
        }
        else
        {
            ReleaseBrakes();
            throttleAxis = Mathf.MoveTowards(throttleAxis, -1f, Time.deltaTime * 3f);
            ApplyRWDDrive(-1f);
        }
    }

    void ApplyRWDDrive(float direction)
    {
        float currentMax = direction > 0 ? maxSpeed : maxReverseSpeed;

        if (Mathf.Abs(carSpeed) < currentMax)
        {
            float torque = (accelerationMultiplier * 50f) * throttleAxis;

            // Drift közben extra nyomaték a hátsó kerekekre
            if (isDrifting && direction > 0)
            {
                torque *= 1.2f;
            }

            // RWD - csak hátsó kerekek hajtanak
            rearLeftCollider.motorTorque = torque;
            rearRightCollider.motorTorque = torque;

            // Elsõ kerekek nem hajtanak
            frontLeftCollider.motorTorque = 0f;
            frontRightCollider.motorTorque = 0f;
        }
        else
        {
            ApplyTorque(0);
        }
    }

    public void DecelerateCar()
    {
        throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 10f);

        float sidewaysAmount = Mathf.Abs(localVelocityX);
        bool isSlidingOrDrifting = sidewaysAmount > 2f || isDrifting || isTractionLocked;

        if (isSlidingOrDrifting)
        {
            // Drift közben minimális lassulás - megtartja a lendületet!
            carRigidbody.linearVelocity *= (1f / (1f + driftDrag));
        }
        else
        {
            // Normál kigurulás
            carRigidbody.linearVelocity *= (1f / (1f + coastingDrag));
        }

        ApplyTorque(0);

        // Csak akkor állítjuk meg, ha NEM csúszik és nagyon lassú
        float totalSpeed = carRigidbody.linearVelocity.magnitude;
        if (totalSpeed < 0.25f && sidewaysAmount < 0.5f && !isSlidingOrDrifting)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
    }

    public void Brakes()
    {
        ToggleBrakeLights(true);
        ApplyTorque(0);

        float currentSpeed = Mathf.Abs(carSpeed);
        float dynamicBrakeForce = brakeForce;

        if (currentSpeed > 120f) dynamicBrakeForce = brakeForce * 0.8f;
        else if (currentSpeed > 80f) dynamicBrakeForce = brakeForce * 1.0f;
        else if (currentSpeed > 40f) dynamicBrakeForce = brakeForce * 1.2f;
        else if (currentSpeed > 10f) dynamicBrakeForce = brakeForce * 1.5f;
        else dynamicBrakeForce = brakeForce * 2f;

        frontLeftCollider.brakeTorque = dynamicBrakeForce;
        frontRightCollider.brakeTorque = dynamicBrakeForce;
        rearLeftCollider.brakeTorque = dynamicBrakeForce;
        rearRightCollider.brakeTorque = dynamicBrakeForce;
    }

    void ReleaseBrakes()
    {
        if (!Input.GetButton("Jump")) ToggleBrakeLights(false);

        frontLeftCollider.brakeTorque = 0f;
        frontRightCollider.brakeTorque = 0f;
        rearLeftCollider.brakeTorque = 0f;
        rearRightCollider.brakeTorque = 0f;
    }

    public void ThrottleOff()
    {
        throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 5f);
        ApplyTorque(0);
    }

    void ApplyTorque(float torque)
    {
        rearLeftCollider.motorTorque = torque;
        rearRightCollider.motorTorque = torque;
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;
    }

    public void Handbrake()
    {
        ToggleBrakeLights(true);
        CancelInvoke("RecoverTraction");

        driftingAxis = Mathf.MoveTowards(driftingAxis, 1f, Time.deltaTime * 3f);
        isTractionLocked = true;

        // Hátsó kerekek blokkolása fékezéssel
        rearLeftCollider.brakeTorque = brakeForce * 0.7f;
        rearRightCollider.brakeTorque = brakeForce * 0.7f;
    }

    public void RecoverTraction()
    {
        ToggleBrakeLights(false);
        isTractionLocked = false;

        // Hátsó fék elengedése
        rearLeftCollider.brakeTorque = 0f;
        rearRightCollider.brakeTorque = 0f;

        driftingAxis = Mathf.MoveTowards(driftingAxis, 0f, Time.deltaTime / 1.5f);
        if (driftingAxis > 0) Invoke("RecoverTraction", Time.deltaTime);
    }

    void ToggleBrakeLights(bool state)
    {
        if (rearLeftBrakeLight != null) rearLeftBrakeLight.SetActive(state);
        if (rearRightBrakeLight != null) rearRightBrakeLight.SetActive(state);
    }

    // --- AUDIO ---
    void UpdateEngineAudio()
    {
        if (engineSources == null || engineClips.Length == 0) return;

        float absSpeed = Mathf.Abs(carSpeed);
        bool isGasPedalPressed = Mathf.Abs(throttleAxis) > 0.1f;

        for (int i = 0; i < gearSpeeds.Length; i++)
        {
            if (absSpeed < gearSpeeds[i]) { currentGear = i + 1; break; }
        }

        float minGearSpeed = (currentGear == 1) ? 0 : gearSpeeds[currentGear - 2];
        float maxGearSpeed = gearSpeeds[currentGear - 1];
        float gearPercent = Mathf.InverseLerp(minGearSpeed, maxGearSpeed, absSpeed);
        float speedRatio = Mathf.Clamp01(absSpeed / maxSpeed);
        float speedBasedMinRPM = Mathf.Lerp(MIN_IDLE_RPM, 0.80f, Mathf.Pow(speedRatio, 0.5f));
        float gearMinRPM = Mathf.Lerp(0.60f, 0.75f, speedRatio);
        float speedBasedRPM = Mathf.Lerp(gearMinRPM, 1.0f, gearPercent);

        if (isGasPedalPressed)
        {
            targetRPM = speedBasedRPM;
            engineLoad = Mathf.Lerp(engineLoad, 1.0f, Time.deltaTime * 5f);
        }
        else
        {
            float coastingRPM = speedBasedRPM * 0.95f;
            targetRPM = Mathf.Max(coastingRPM, speedBasedMinRPM);
            engineLoad = Mathf.Lerp(engineLoad, 0.4f, Time.deltaTime * 1.5f);
        }

        if (absSpeed < 5f) targetRPM = Mathf.Lerp(MIN_IDLE_RPM, speedBasedMinRPM, absSpeed / 5f);

        float actualRevDownSpeed = absSpeed > 10f ? revDownSpeed * 0.2f : revDownSpeed;
        float inertia = isGasPedalPressed ? revUpSpeed : actualRevDownSpeed;
        engineRPM = Mathf.Lerp(engineRPM, targetRPM, Time.deltaTime * inertia);
        engineRPM = Mathf.Max(engineRPM, speedBasedMinRPM);

        float adjustedRPM = Mathf.Max(0, (engineRPM - MIN_IDLE_RPM) / (1.0f - MIN_IDLE_RPM));
        float exactIndex = adjustedRPM * (engineClips.Length - 1);
        int indexA = Mathf.Clamp(Mathf.FloorToInt(exactIndex), 0, engineClips.Length - 1);
        int indexB = Mathf.Clamp(Mathf.CeilToInt(exactIndex), 0, engineClips.Length - 1);

        AudioSource sourceA = GetSourceForClip(indexA);
        AudioSource sourceB = GetSourceForClip(indexB);

        float blend = exactIndex - indexA;
        float loadPitchFactor = Mathf.Lerp(0.96f, 1.0f, engineLoad);
        float loadVolFactor = Mathf.Lerp(0.75f, 1.0f, engineLoad);
        float rpmVolCurve = Mathf.Lerp(0.6f, 1.0f, adjustedRPM);
        float masterVol = rpmVolCurve * loadVolFactor;

        if (sourceA != null) { sourceA.volume = (1.0f - blend) * masterVol; sourceA.pitch = (1.0f + (blend * 0.1f)) * loadPitchFactor; }
        if (sourceB != null) { sourceB.volume = blend * masterVol; sourceB.pitch = (0.9f + (blend * 0.1f)) * loadPitchFactor; }

        MuteUnusedSources(sourceA, sourceB);

        if (tireSource != null)
        {
            bool screeching = isDrifting || (isTractionLocked && absSpeed > 12f);
            tireSource.volume = Mathf.Lerp(tireSource.volume, screeching ? 1.0f : 0.0f, Time.deltaTime * 10f);
        }
    }

    AudioSource GetSourceForClip(int clipIndex)
    {
        AudioClip targetClip = engineClips[clipIndex];
        foreach (var s in engineSources) if (s.clip == targetClip && s.isPlaying) return s;

        AudioSource bestCandidate = engineSources[0];
        float lowestVol = 100f;
        foreach (var s in engineSources)
        {
            if (!s.isPlaying) { bestCandidate = s; break; }
            if (s.volume < lowestVol) { lowestVol = s.volume; bestCandidate = s; }
        }
        bestCandidate.clip = targetClip;
        bestCandidate.volume = 0f;
        bestCandidate.time = 0f;
        bestCandidate.Play();
        return bestCandidate;
    }

    void MuteUnusedSources(AudioSource activeA, AudioSource activeB)
    {
        foreach (var s in engineSources)
        {
            if (s != activeA && s != activeB)
            {
                s.volume = Mathf.Lerp(s.volume, 0f, Time.deltaTime * 10f);
                if (s.volume < 0.01f && s.isPlaying) s.Stop();
            }
        }
    }

    void CalculateGearRatios()
    {
        gearSpeeds = new float[numberOfGears];
        for (int i = 0; i < numberOfGears; i++)
        {
            float t = (float)(i + 1) / numberOfGears;
            gearSpeeds[i] = Mathf.Lerp(0, maxSpeed, Mathf.Pow(t, 0.7f));
        }
    }

    // --- COLLISION ---
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null) return;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

        if (collision.collider.CompareTag("Border") && collisionClip != null)
        {
            if (useSounds && AudioManager.Instance != null)
                AudioManager.Instance.PlaySound3D(collisionClip, hitPoint, null, collisionVolume);
            else
                AudioSource.PlayClipAtPoint(collisionClip, hitPoint, collisionVolume);
        }
    }

    // --- CURB DETECTION ---
    void SetupWheelCurbDetectors()
    {
        if (frontLeftCollider != null) AddCurbDetector(frontLeftCollider.gameObject);
        if (frontRightCollider != null) AddCurbDetector(frontRightCollider.gameObject);
        if (rearLeftCollider != null) AddCurbDetector(rearLeftCollider.gameObject);
        if (rearRightCollider != null) AddCurbDetector(rearRightCollider.gameObject);
    }

    void AddCurbDetector(GameObject wheelObj)
    {
        SphereCollider trigger = wheelObj.AddComponent<SphereCollider>();
        trigger.isTrigger = true;

        WheelCollider wc = wheelObj.GetComponent<WheelCollider>();
        trigger.radius = wc != null ? wc.radius : 0.35f;
        trigger.center = Vector3.zero;

        WheelCurbDetector detector = wheelObj.GetComponent<WheelCurbDetector>();
        if (detector == null) detector = wheelObj.AddComponent<WheelCurbDetector>();
        detector.driftCarController = this;
    }

    public void OnWheelHitCurb(Vector3 position)
    {
        if (curbClip == null) return;

        float absSpeed = Mathf.Abs(carSpeed);
        if (absSpeed < 3f) return;

        float speedFactor = Mathf.InverseLerp(10f, 180f, absSpeed);
        float currentCooldown = Mathf.Lerp(0.08f, 0.03f, speedFactor);

        if (Time.time - lastCurbSoundTime < currentCooldown) return;
        lastCurbSoundTime = Time.time;

        float pitch = Mathf.Lerp(0.6f, 1.6f, speedFactor);
        float dynamicVolume = Mathf.Lerp(0.3f, 1f, speedFactor) * curbVolume;
        float wheelMultiplier = Mathf.Lerp(1f, 1.3f, (wheelsOnCurb - 1) / 3f);
        dynamicVolume *= wheelMultiplier;

        GameObject tempGO = new GameObject("CurbSound");
        tempGO.transform.position = position;
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.clip = curbClip;
        tempSource.volume = dynamicVolume;
        tempSource.pitch = pitch;
        tempSource.spatialBlend = 1f;
        tempSource.minDistance = 2f;
        tempSource.maxDistance = 100f;
        tempSource.Play();
        Destroy(tempGO, curbClip.length / pitch + 0.1f);
    }

    public void WheelEnteredCurb() { wheelsOnCurb++; }
    public void WheelExitedCurb() { wheelsOnCurb = Mathf.Max(0, wheelsOnCurb - 1); }

    // --- TOUCH CONTROLS ---
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

    // --- EFFECTS ---
    void StopEffects()
    {
        if (RLWParticleSystem != null) RLWParticleSystem.Stop();
        if (RRWParticleSystem != null) RRWParticleSystem.Stop();
        if (RLWTireSkid != null) RLWTireSkid.emitting = false;
        if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        if (RLWSmokeParticle != null) RLWSmokeParticle.Stop();
        if (RRWSmokeParticle != null) RRWSmokeParticle.Stop();
    }

    // --- UI ---
    void UpdateUI()
    {
        if (carSpeedText != null)
            carSpeedText.text = Mathf.RoundToInt(Mathf.Abs(carSpeed)).ToString();

        if (gearText != null)
            gearText.text = currentGear.ToString();

        if (driftAngleText != null)
            driftAngleText.text = Mathf.RoundToInt(Mathf.Abs(driftAngle)).ToString() + "°";
    }

    public void CarSpeedUI()
    {
        UpdateUI();
    }

    // --- WHEEL ANIMATION ---
    void AnimateWheelMeshes()
    {
        UpdateWheel(frontLeftCollider, frontLeftMesh);
        UpdateWheel(frontRightCollider, frontRightMesh);
        UpdateWheel(rearLeftCollider, rearLeftMesh);
        UpdateWheel(rearRightCollider, rearRightMesh);
    }

    void UpdateWheel(WheelCollider col, GameObject mesh)
    {
        if (col == null || mesh == null) return;
        Vector3 p;
        Quaternion r;
        col.GetWorldPose(out p, out r);
        mesh.transform.position = p;
        mesh.transform.rotation = r;
    }
}