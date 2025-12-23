using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AudioController;

public class RallyCarController : MonoBehaviour
{
    // --- CAR SETUP ---
    [Space(20)]
    [Header("SPEED SETTINGS")]
    [Range(100, 500)] public int maxSpeed = 200;
    [Range(10, 120)] public int maxReverseSpeed = 40;
    [Range(1, 30)] public int accelerationMultiplier = 8;

    [Space(10)]
    [Header("STEERING")]
    [Range(10, 50)] public int maxSteeringAngle = 30;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.6f;

    [Space(10)]
    [Header("BRAKES")]
    [Range(100, 1000)] public int brakeForce = 400;

    [Space(10)]
    [Header("RALLY SETTINGS")]
    [Tooltip("AWD nyomaték elosztás (0 = csak hátsó, 1 = csak elsõ, 0.5 = egyenlõ)")]
    [Range(0f, 1f)] public float awd_FrontBias = 0.4f;

    [Tooltip("Normál tapadás (aszfalt)")]
    [Range(1f, 5f)] public float asphaltGrip = 2.5f;

    [Tooltip("Dirt/földút tapadás")]
    [Range(0.3f, 2f)] public float dirtGrip = 0.8f;

    [Tooltip("Kavics tapadás")]
    [Range(0.2f, 1.5f)] public float gravelGrip = 0.5f;

    [Tooltip("Hó/jég tapadás")]
    [Range(0.1f, 1f)] public float snowGrip = 0.3f;

    [Tooltip("Kigurulási lassulás")]
    [Range(0.01f, 0.2f)] public float coastingDrag = 0.04f;

    [Space(10)]
    public Vector3 bodyMassCenter = new Vector3(0, -0.3f, 0.2f);

    // --- SURFACE TAGS ---
    [Header("SURFACE TAGS")]
    public string dirtTag = "Dirt";
    public string gravelTag = "Gravel";
    public string snowTag = "Snow";
    public string asphaltTag = "Asphalt";

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

    // --- DIRT/GROUND EFFECTS ---
    [Header("DIRT PARTICLE EFFECTS")]
    [Tooltip("Engedélyezi a föld particle effecteket")]
    public bool useDirtEffects = true;

    [Tooltip("Bal elsõ kerék föld effect")]
    public ParticleSystem FLWDirtParticle;
    [Tooltip("Jobb elsõ kerék föld effect")]
    public ParticleSystem FRWDirtParticle;
    [Tooltip("Bal hátsó kerék föld effect")]
    public ParticleSystem RLWDirtParticle;
    [Tooltip("Jobb hátsó kerék föld effect")]
    public ParticleSystem RRWDirtParticle;

    [Tooltip("Minimum sebesség a dirt effecthez")]
    [Range(5f, 30f)] public float dirtEffectMinSpeed = 10f;

    [Tooltip("Minimum emission rate")]
    [Range(10f, 100f)] public float dirtMinEmission = 15f;
    [Tooltip("Maximum emission rate")]
    [Range(50f, 300f)] public float dirtMaxEmission = 100f;

    [Tooltip("Minimum particle méret")]
    [Range(0.1f, 1f)] public float dirtMinSize = 0.3f;
    [Tooltip("Maximum particle méret")]
    [Range(0.5f, 3f)] public float dirtMaxSize = 1.5f;

    // --- TIRE MARKS ---
    [Header("TIRE EFFECTS")]
    public bool useEffects = false;
    public TrailRenderer FLWTireSkid;
    public TrailRenderer FRWTireSkid;
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    // --- SMOKE EFFECTS ---
    [Header("SMOKE EFFECTS (Asphalt)")]
    public ParticleSystem RLWSmokeParticle;
    public ParticleSystem RRWSmokeParticle;
    [Range(5f, 30f)] public float smokeStartAngle = 15f;

    // --- UI ---
    [Header("UI")]
    public bool useUI = false;
    public Text carSpeedText;
    public Text gearText;

    // --- AUDIO ---
    [Space(20)]
    [Header("AUDIO")]
    public bool useSounds = false;
    public AudioClip[] engineClips;
    public AudioClip tireScreechClip;
    public AudioClip dirtDrivingClip;
    public AudioClip collisionClip;
    [Range(0f, 1f)] public float collisionVolume = 1f;

    [Header("ENGINE AUDIO")]
    [Range(4, 8)] public int numberOfGears = 6;
    [Range(1f, 20f)] public float revUpSpeed = 10f;
    [Range(0.5f, 10f)] public float revDownSpeed = 4f;

    private AudioSource[] engineSources;
    private AudioSource tireSource;
    private AudioSource dirtSource;
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
    [HideInInspector] public string currentSurface = "Asphalt";

    // --- PRIVATE ---
    Rigidbody carRigidbody;
    float steeringAxis;
    float throttleAxis;
    float driftingAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;
    bool touchControlsSetup = false;

    // Surface detection per wheel
    bool FL_onDirt, FR_onDirt, RL_onDirt, RR_onDirt;
    bool FL_onGravel, FR_onGravel, RL_onGravel, RR_onGravel;
    bool FL_onSnow, FR_onSnow, RL_onSnow, RR_onSnow;

    float defaultCoastingDrag;
    int defaultMaxSpeed;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;
        carRigidbody.linearDamping = 0f;
        carRigidbody.angularDamping = 0.5f;

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
            if (dirtDrivingClip != null)
                dirtSource = AudioManager.Instance.CreateLoopingSource(this.transform, dirtDrivingClip, 0f, true);
        }
        else if (useSounds)
        {
            Debug.LogError("Nincs AudioManager!");
            useSounds = false;
        }

        if (!useEffects) StopEffects();
        SetupTouchControls();
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
        SetWheelFriction(frontLeftCollider, 2f, asphaltGrip);
        SetWheelFriction(frontRightCollider, 2f, asphaltGrip);
        SetWheelFriction(rearLeftCollider, 2f, asphaltGrip);
        SetWheelFriction(rearRightCollider, 2f, asphaltGrip);
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
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        CalculateDriftAngle();
        CheckSurfaceAllWheels();

        HandleInput();
        UpdateSurfaceGrip();
        UpdateDirtEffects();
        UpdateSmokeEffects();
        AnimateWheelMeshes();
        UpdateUI();

        if (useSounds) UpdateEngineAudio();
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
        isDrifting = Mathf.Abs(driftAngle) > 12f && Mathf.Abs(carSpeed) > 10f;
    }

    void CheckSurfaceAllWheels()
    {
        FL_onDirt = CheckWheelSurface(frontLeftCollider, dirtTag);
        FR_onDirt = CheckWheelSurface(frontRightCollider, dirtTag);
        RL_onDirt = CheckWheelSurface(rearLeftCollider, dirtTag);
        RR_onDirt = CheckWheelSurface(rearRightCollider, dirtTag);

        FL_onGravel = CheckWheelSurface(frontLeftCollider, gravelTag);
        FR_onGravel = CheckWheelSurface(frontRightCollider, gravelTag);
        RL_onGravel = CheckWheelSurface(rearLeftCollider, gravelTag);
        RR_onGravel = CheckWheelSurface(rearRightCollider, gravelTag);

        FL_onSnow = CheckWheelSurface(frontLeftCollider, snowTag);
        FR_onSnow = CheckWheelSurface(frontRightCollider, snowTag);
        RL_onSnow = CheckWheelSurface(rearLeftCollider, snowTag);
        RR_onSnow = CheckWheelSurface(rearRightCollider, snowTag);

        // Fõ felület meghatározása (többségi szavazás)
        int dirtCount = (FL_onDirt ? 1 : 0) + (FR_onDirt ? 1 : 0) + (RL_onDirt ? 1 : 0) + (RR_onDirt ? 1 : 0);
        int gravelCount = (FL_onGravel ? 1 : 0) + (FR_onGravel ? 1 : 0) + (RL_onGravel ? 1 : 0) + (RR_onGravel ? 1 : 0);
        int snowCount = (FL_onSnow ? 1 : 0) + (FR_onSnow ? 1 : 0) + (RL_onSnow ? 1 : 0) + (RR_onSnow ? 1 : 0);

        if (snowCount >= 2) currentSurface = "Snow";
        else if (gravelCount >= 2) currentSurface = "Gravel";
        else if (dirtCount >= 2) currentSurface = "Dirt";
        else currentSurface = "Asphalt";
    }

    bool CheckWheelSurface(WheelCollider wc, string tag)
    {
        WheelHit hit;
        if (wc.GetGroundHit(out hit))
        {
            if (hit.collider != null && hit.collider.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }

    void UpdateSurfaceGrip()
    {
        float targetGrip = asphaltGrip;

        switch (currentSurface)
        {
            case "Dirt":
                targetGrip = dirtGrip;
                break;
            case "Gravel":
                targetGrip = gravelGrip;
                break;
            case "Snow":
                targetGrip = snowGrip;
                break;
            default:
                targetGrip = asphaltGrip;
                break;
        }

        if (isTractionLocked)
        {
            targetGrip *= 0.4f;
        }

        // Simán állítjuk a tapadást
        SetWheelGripSmooth(frontLeftCollider, targetGrip);
        SetWheelGripSmooth(frontRightCollider, targetGrip);
        SetWheelGripSmooth(rearLeftCollider, targetGrip);
        SetWheelGripSmooth(rearRightCollider, targetGrip);
    }

    void SetWheelGripSmooth(WheelCollider wc, float targetGrip)
    {
        WheelFrictionCurve sideways = wc.sidewaysFriction;
        sideways.stiffness = Mathf.Lerp(sideways.stiffness, targetGrip, Time.deltaTime * 5f);
        wc.sidewaysFriction = sideways;
    }

    // --- DIRT PARTICLE EFFECTS ---
    void UpdateDirtEffects()
    {
        if (!useDirtEffects) return;

        float absSpeed = Mathf.Abs(carSpeed);
        bool isMovingEnough = absSpeed >= dirtEffectMinSpeed;

        // Intenzitás számítás sebesség alapján
        float speedIntensity = Mathf.InverseLerp(dirtEffectMinSpeed, 100f, absSpeed);

        // Drift/slip hozzáadja az intenzitást
        float slipIntensity = 0f;
        if (isDrifting || isTractionLocked)
        {
            slipIntensity = Mathf.Clamp01(Mathf.Abs(localVelocityX) / 10f);
        }

        float totalIntensity = Mathf.Clamp01(speedIntensity + slipIntensity * 0.5f);

        // Bal elsõ kerék
        UpdateWheelDirtEffect(FLWDirtParticle, FL_onDirt || FL_onGravel || FL_onSnow, isMovingEnough, totalIntensity);

        // Jobb elsõ kerék
        UpdateWheelDirtEffect(FRWDirtParticle, FR_onDirt || FR_onGravel || FR_onSnow, isMovingEnough, totalIntensity);

        // Bal hátsó kerék
        UpdateWheelDirtEffect(RLWDirtParticle, RL_onDirt || RL_onGravel || RL_onSnow, isMovingEnough, totalIntensity);

        // Jobb hátsó kerék
        UpdateWheelDirtEffect(RRWDirtParticle, RR_onDirt || RR_onGravel || RR_onSnow, isMovingEnough, totalIntensity);

        // Dirt hang
        if (useSounds && dirtSource != null)
        {
            bool anyWheelOnDirt = FL_onDirt || FR_onDirt || RL_onDirt || RR_onDirt ||
                                   FL_onGravel || FR_onGravel || RL_onGravel || RR_onGravel;
            float targetVolume = (anyWheelOnDirt && isMovingEnough) ? totalIntensity * 0.7f : 0f;
            dirtSource.volume = Mathf.Lerp(dirtSource.volume, targetVolume, Time.deltaTime * 5f);
        }
    }

    void UpdateWheelDirtEffect(ParticleSystem dirtPS, bool onDirtSurface, bool isMoving, float intensity)
    {
        if (dirtPS == null) return;

        bool shouldEmit = onDirtSurface && isMoving && intensity > 0.1f;

        if (shouldEmit)
        {
            if (!dirtPS.isPlaying) dirtPS.Play();

            var emission = dirtPS.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(dirtMinEmission, dirtMaxEmission, intensity);

            var main = dirtPS.main;
            main.startSizeMultiplier = Mathf.Lerp(dirtMinSize, dirtMaxSize, intensity);

            // Sebesség alapján a particle kilövési sebessége
            main.startSpeedMultiplier = Mathf.Lerp(1f, 5f, intensity);
        }
        else
        {
            if (dirtPS.isPlaying) dirtPS.Stop();
        }
    }

    // --- SMOKE EFFECTS (csak aszfalton) ---
    void UpdateSmokeEffects()
    {
        if (RLWSmokeParticle == null && RRWSmokeParticle == null) return;

        // Smoke csak aszfalton
        bool onAsphalt = currentSurface == "Asphalt";
        bool shouldSmoke = onAsphalt && (isDrifting || isTractionLocked) && Mathf.Abs(carSpeed) > 15f;

        float smokeIntensity = 0f;
        if (shouldSmoke)
        {
            smokeIntensity = Mathf.InverseLerp(smokeStartAngle, 45f, Mathf.Abs(driftAngle));
            smokeIntensity = Mathf.Clamp01(smokeIntensity);
        }

        if (shouldSmoke && smokeIntensity > 0.1f)
        {
            ApplySmokeSettings(RLWSmokeParticle, smokeIntensity);
            ApplySmokeSettings(RRWSmokeParticle, smokeIntensity);
        }
        else
        {
            if (RLWSmokeParticle != null && RLWSmokeParticle.isPlaying) RLWSmokeParticle.Stop();
            if (RRWSmokeParticle != null && RRWSmokeParticle.isPlaying) RRWSmokeParticle.Stop();
        }
    }

    void ApplySmokeSettings(ParticleSystem smoke, float intensity)
    {
        if (smoke == null) return;
        if (!smoke.isPlaying) smoke.Play();

        var emission = smoke.emission;
        emission.rateOverTimeMultiplier = Mathf.Lerp(20f, 100f, intensity);

        var main = smoke.main;
        main.startSizeMultiplier = Mathf.Lerp(1f, 3f, intensity);
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

        // Rally autóknál gyorsabb kormányzás
        if (isDrifting)
        {
            currentSpeed *= 1.3f;
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
            ApplyAWDDrive(1f);
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
            ApplyAWDDrive(-1f);
        }
    }

    void ApplyAWDDrive(float direction)
    {
        float currentMax = direction > 0 ? maxSpeed : maxReverseSpeed;

        if (Mathf.Abs(carSpeed) < currentMax)
        {
            float totalTorque = (accelerationMultiplier * 50f) * throttleAxis;

            // AWD elosztás
            float frontTorque = totalTorque * awd_FrontBias;
            float rearTorque = totalTorque * (1f - awd_FrontBias);

            frontLeftCollider.motorTorque = frontTorque;
            frontRightCollider.motorTorque = frontTorque;
            rearLeftCollider.motorTorque = rearTorque;
            rearRightCollider.motorTorque = rearTorque;
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
            carRigidbody.linearVelocity *= (1f / (1f + coastingDrag * 0.3f));
        }
        else
        {
            carRigidbody.linearVelocity *= (1f / (1f + coastingDrag));
        }

        ApplyTorque(0);

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

        if (currentSpeed > 100f) dynamicBrakeForce = brakeForce * 0.8f;
        else if (currentSpeed > 60f) dynamicBrakeForce = brakeForce * 1.0f;
        else if (currentSpeed > 30f) dynamicBrakeForce = brakeForce * 1.3f;
        else dynamicBrakeForce = brakeForce * 1.8f;

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
        float frontTorque = torque * awd_FrontBias;
        float rearTorque = torque * (1f - awd_FrontBias);

        frontLeftCollider.motorTorque = frontTorque;
        frontRightCollider.motorTorque = frontTorque;
        rearLeftCollider.motorTorque = rearTorque;
        rearRightCollider.motorTorque = rearTorque;
    }

    public void Handbrake()
    {
        ToggleBrakeLights(true);
        CancelInvoke("RecoverTraction");

        driftingAxis = Mathf.MoveTowards(driftingAxis, 1f, Time.deltaTime * 3f);
        isTractionLocked = true;

        rearLeftCollider.brakeTorque = brakeForce * 0.8f;
        rearRightCollider.brakeTorque = brakeForce * 0.8f;
    }

    public void RecoverTraction()
    {
        ToggleBrakeLights(false);
        isTractionLocked = false;

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
            bool screeching = (isDrifting || isTractionLocked) && currentSurface == "Asphalt" && absSpeed > 12f;
            tireSource.volume = Mathf.Lerp(tireSource.volume, screeching ? 0.8f : 0.0f, Time.deltaTime * 10f);
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
        if (FLWDirtParticle != null) FLWDirtParticle.Stop();
        if (FRWDirtParticle != null) FRWDirtParticle.Stop();
        if (RLWDirtParticle != null) RLWDirtParticle.Stop();
        if (RRWDirtParticle != null) RRWDirtParticle.Stop();
        if (RLWSmokeParticle != null) RLWSmokeParticle.Stop();
        if (RRWSmokeParticle != null) RRWSmokeParticle.Stop();
        if (FLWTireSkid != null) FLWTireSkid.emitting = false;
        if (FRWTireSkid != null) FRWTireSkid.emitting = false;
        if (RLWTireSkid != null) RLWTireSkid.emitting = false;
        if (RRWTireSkid != null) RRWTireSkid.emitting = false;
    }

    // --- UI ---
    void UpdateUI()
    {
        if (carSpeedText != null)
            carSpeedText.text = Mathf.RoundToInt(Mathf.Abs(carSpeed)).ToString();

        if (gearText != null)
            gearText.text = currentGear.ToString();

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