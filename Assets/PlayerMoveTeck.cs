using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveTeck : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;

    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 22f;
    public float deceleration = 10f;
    public float inputSmooth = 8f;

    [Header("Air Movement")]
    [Range(0f, 1f)]
    public float airControl = 0.3f;
    public float airDeceleration = 0.5f;

    [Header("Falling")]
    public float fallAcceleration = 25f;
    [Range(0f, 1f)]
    public float risingGravityMultiplier = 0.25f;

    [Header("Jump")]
    public float JumpForce = 8f;
    public bool JumpDet = false;

    [Header("Ground Detection")]
    [Range(0f, 1f)]
    public float groundNormalThreshold = 0.55f;
    public float groundMemory = 0.1f;

    public float CurrentAccel;

    private float lastGroundedTime;

    private Vector2 rawInput;
    private Vector2 smoothInput;
    private Vector2 inputVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        rawInput = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );

        smoothInput = Vector2.SmoothDamp(
            smoothInput,
            rawInput,
            ref inputVelocity,
            1f / inputSmooth
        );

        if (Input.GetKeyDown(KeyCode.Space) && JumpDet)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        UpdateGroundedState();

        Vector3 input = new Vector3(
            smoothInput.x,
            0f,
            smoothInput.y
        );

        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 moveDirection =
            transform.TransformDirection(input);

        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.001f)
            moveDirection.Normalize();

        Vector3 horizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        if (input.sqrMagnitude > 0.001f)
        {
            Accelerate(
                moveDirection,
                horizontalVelocity,
                input.magnitude
            );
        }
        else
        {
            ApplyDeceleration(horizontalVelocity);
        }

        ApplyFallAcceleration();
    }

    void Accelerate(
        Vector3 moveDirection,
        Vector3 horizontalVelocity,
        float inputAmount
    )
    {
        float control = JumpDet ? 1f : airControl;

        Vector3 targetVelocity =
            moveDirection * maxSpeed * inputAmount;

        Vector3 velocityDifference =
            targetVelocity - horizontalVelocity;

        Vector3 force =
            velocityDifference * acceleration * control;

        CurrentAccel = force.magnitude;

        rb.AddForce(
            force,
            ForceMode.Acceleration
        );
    }

    void ApplyDeceleration(Vector3 horizontalVelocity)
    {
        if (horizontalVelocity.sqrMagnitude < 0.001f)
            return;

        float braking =
            JumpDet
                ? deceleration
                : airDeceleration;

        rb.AddForce(
            -horizontalVelocity * braking,
            ForceMode.Acceleration
        );
    }

    void ApplyFallAcceleration()
    {
        if (JumpDet)
            return;

        float gravityStrength;

        if (rb.linearVelocity.y > 0f)
        {
            gravityStrength =
                fallAcceleration * risingGravityMultiplier;
        }
        else
        {
            gravityStrength = fallAcceleration;
        }

        rb.AddForce(
            Vector3.down * gravityStrength,
            ForceMode.Acceleration
        );
    }

    void Jump()
    {
        if (rb.linearVelocity.y < 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        rb.AddForce(
            Vector3.up * JumpForce,
            ForceMode.VelocityChange
        );

        JumpDet = false;
        lastGroundedTime = -999f;
    }

    void UpdateGroundedState()
    {
        JumpDet =
            Time.time - lastGroundedTime
            <= groundMemory;
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y >= groundNormalThreshold)
            {
                lastGroundedTime = Time.time;
                JumpDet = true;
                return;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y >= groundNormalThreshold)
            {
                lastGroundedTime = Time.time;
                JumpDet = true;
                return;
            }
        }
    }
}
