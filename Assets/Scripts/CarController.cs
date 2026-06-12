using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Car Settings")]
    public float motorForce = 1800f;
    public float brakeForce = 3500f;
    public float maxSteerAngle = 28f;
    public float maxSpeed = 180f;

    [Header("Driving Feel")]
    public float accelerationSmoothness = 3f;
    public float brakeSmoothness = 5f;
    public float steeringSmoothness = 5f;

    [Header("Grip System")]
    public float tireTemperature = 20f;
    public float optimalTemperature = 90f;
    public float currentSurfaceGrip = 1f;
    public float finalGrip = 1f;

    public float tireHeatingSpeed = 8f;
    public float tireCoolingSpeed = 3f;

    [Header("Aerodynamics")]
    public float downforce = 50f;

    [Header("Anti Roll Bars")]
    public float antiRollForceFront = 6000f;
    public float antiRollForceRear = 5000f;

    private Rigidbody rb;

    private float verticalInput;
    private float horizontalInput;

    private float currentMotorForce;
    private float currentBrakeForce;
    private float currentSteerAngle;

    private float speed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 1200f;
        rb.drag = 0.05f;
        rb.angularDrag = 2f;

        // Centro de masa bajo para que no se sienta como caja
        rb.centerOfMass = new Vector3(0f, -0.9f, 0.2f);
    }

    void Update()
    {
        GetInput();
    }

    void FixedUpdate()
    {
        CalculateSpeed();

        UpdateGripSystem();

        HandleMotor();
        HandleSteering();

        ApplyDownforce();

        ApplyAntiRollBar(frontLeftWheel, frontRightWheel, antiRollForceFront);
        ApplyAntiRollBar(rearLeftWheel, rearRightWheel, antiRollForceRear);
    }

    void GetInput()
    {
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
    }

    void CalculateSpeed()
    {
        speed = rb.velocity.magnitude * 3.6f;
    }

    void HandleMotor()
    {
        bool isAccelerating = verticalInput > 0.1f;
        bool isBraking = verticalInput < -0.1f;

        if (isAccelerating && speed < maxSpeed)
        {
            currentMotorForce = Mathf.Lerp(
                currentMotorForce,
                verticalInput * motorForce,
                accelerationSmoothness * Time.fixedDeltaTime
            );

            currentBrakeForce = 0f;
        }
        else
        {
            currentMotorForce = Mathf.Lerp(
                currentMotorForce,
                0f,
                accelerationSmoothness * Time.fixedDeltaTime
            );
        }

        if (isBraking)
        {
            currentBrakeForce = Mathf.Lerp(
                currentBrakeForce,
                brakeForce,
                brakeSmoothness * Time.fixedDeltaTime
            );

            currentMotorForce = 0f;
        }
        else
        {
            currentBrakeForce = Mathf.Lerp(
                currentBrakeForce,
                0f,
                brakeSmoothness * Time.fixedDeltaTime
            );
        }

        // Tracción trasera
        rearLeftWheel.motorTorque = currentMotorForce;
        rearRightWheel.motorTorque = currentMotorForce;

        // Freno en las 4 ruedas
        frontLeftWheel.brakeTorque = currentBrakeForce;
        frontRightWheel.brakeTorque = currentBrakeForce;
        rearLeftWheel.brakeTorque = currentBrakeForce;
        rearRightWheel.brakeTorque = currentBrakeForce;
    }

    void HandleSteering()
    {
        // A más velocidad, menos ángulo de giro
        float speedFactor = Mathf.Clamp01(speed / 120f);

        float steerLimit = Mathf.Lerp(maxSteerAngle, 8f, speedFactor);

        // Si hay poco agarre, también gira menos
        float gripSteerFactor = Mathf.Clamp(finalGrip, 0.25f, 1.2f);

        float targetSteerAngle = horizontalInput * steerLimit * gripSteerFactor;

        // Suaviza la dirección para que no gire de golpe
        currentSteerAngle = Mathf.Lerp(
            currentSteerAngle,
            targetSteerAngle,
            steeringSmoothness * Time.fixedDeltaTime
        );

        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;
    }

    void UpdateGripSystem()
    {
        UpdateTireTemperature();
        DetectSurface();
        ApplyGripToWheels();
    }

    void UpdateTireTemperature()
    {
        bool isMovingFast = speed > 20f;
        bool isTurning = Mathf.Abs(horizontalInput) > 0.2f;
        bool isAcceleratingOrBraking = Mathf.Abs(verticalInput) > 0.2f;

        if (isMovingFast && (isTurning || isAcceleratingOrBraking))
        {
            tireTemperature += tireHeatingSpeed * Time.fixedDeltaTime;
        }
        else
        {
            tireTemperature -= tireCoolingSpeed * Time.fixedDeltaTime;
        }

        tireTemperature = Mathf.Clamp(tireTemperature, 20f, 150f);
    }

    void DetectSurface()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, -transform.up, out hit, 3f))
        {
            if (hit.collider.CompareTag("AsfaltoSuave"))
            {
                currentSurfaceGrip = 1.20f;
            }
            else if (hit.collider.CompareTag("AsfaltoDuro"))
            {
                currentSurfaceGrip = 1.00f;
            }
            else if (hit.collider.CompareTag("Tierra"))
            {
                currentSurfaceGrip = 0.65f;
            }
            else if (hit.collider.CompareTag("Pasto"))
            {
                currentSurfaceGrip = 0.55f;
            }
            else if (hit.collider.CompareTag("Nieve"))
            {
                currentSurfaceGrip = 0.35f;
            }
            else if (hit.collider.CompareTag("Hielo"))
            {
                currentSurfaceGrip = 0.12f;
            }
            else
            {
                currentSurfaceGrip = 1.00f;
            }
        }
        else
        {
            currentSurfaceGrip = 1.00f;
        }
    }

    float GetTemperatureGrip()
    {
        float difference = Mathf.Abs(tireTemperature - optimalTemperature);

        if (difference < 10f)
        {
            return 1.10f;
        }
        else if (difference < 30f)
        {
            return 1.00f;
        }
        else if (difference < 50f)
        {
            return 0.75f;
        }
        else
        {
            return 0.55f;
        }
    }

    void ApplyGripToWheels()
    {
        float temperatureGrip = GetTemperatureGrip();

        finalGrip = currentSurfaceGrip * temperatureGrip;

        ApplyGripToWheel(frontLeftWheel, finalGrip);
        ApplyGripToWheel(frontRightWheel, finalGrip);
        ApplyGripToWheel(rearLeftWheel, finalGrip);
        ApplyGripToWheel(rearRightWheel, finalGrip);
    }

    void ApplyGripToWheel(WheelCollider wheel, float grip)
    {
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;

        sidewaysFriction.extremumSlip = 0.25f;
        sidewaysFriction.extremumValue = 1.0f;
        sidewaysFriction.asymptoteSlip = 0.6f;
        sidewaysFriction.asymptoteValue = 0.75f;
        sidewaysFriction.stiffness = grip;

        wheel.sidewaysFriction = sidewaysFriction;

        WheelFrictionCurve forwardFriction = wheel.forwardFriction;

        forwardFriction.extremumSlip = 0.35f;
        forwardFriction.extremumValue = 1.0f;
        forwardFriction.asymptoteSlip = 0.8f;
        forwardFriction.asymptoteValue = 0.7f;
        forwardFriction.stiffness = grip;

        wheel.forwardFriction = forwardFriction;
    }

    void ApplyDownforce()
    {
        rb.AddForce(-transform.up * downforce * rb.velocity.magnitude);
    }

    void ApplyAntiRollBar(WheelCollider leftWheel, WheelCollider rightWheel, float antiRollForce)
    {
        WheelHit hit;

        float travelLeft = 1.0f;
        float travelRight = 1.0f;

        bool groundedLeft = leftWheel.GetGroundHit(out hit);

        if (groundedLeft)
        {
            travelLeft = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;
        }

        bool groundedRight = rightWheel.GetGroundHit(out hit);

        if (groundedRight)
        {
            travelRight = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;
        }

        float antiRoll = (travelLeft - travelRight) * antiRollForce;

        if (groundedLeft)
        {
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRoll, leftWheel.transform.position);
        }

        if (groundedRight)
        {
            rb.AddForceAtPosition(rightWheel.transform.up * antiRoll, rightWheel.transform.position);
        }
    }
}