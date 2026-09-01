using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    [Header("Thrown State")]
    public float thrownDamageTime = 3f;
    public float minimumThrownSpeed = 3f;

    [Header("Impact Damage")]
    public float minimumDamageSpeed = 2f;
    public float damageMultiplier = 4f;
    public float maximumDamage = 100f;
    public float hitCooldown = 0.15f;

    public bool thrown;

    private Rigidbody rb;
    private float thrownTimer;
    private float lastHitTime;
    private bool activated;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = GetComponentInChildren<Rigidbody>();
    }

    void Start()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    void FixedUpdate()
    {
        if (!activated && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    void Update()
    {
        if (!thrown)
            return;

        thrownTimer -= Time.deltaTime;

        if (thrownTimer <= 0f)
        {
            thrown = false;
            return;
        }

        if (
            rb != null &&
            rb.linearVelocity.magnitude < minimumThrownSpeed
        )
        {
            thrown = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!activated)
            return;

        if (Time.time - lastHitTime < hitCooldown)
            return;

        if (collision.contactCount == 0)
            return;

        SimpleEnemy enemy =
            collision.collider.GetComponentInParent<SimpleEnemy>();

        if (enemy == null)
            return;

        float impactSpeed =
            collision.relativeVelocity.magnitude;

        if (impactSpeed < minimumDamageSpeed)
            return;

        float damage =
            (impactSpeed - minimumDamageSpeed) *
            damageMultiplier;

        damage =
            Mathf.Clamp(
                damage,
                0f,
                maximumDamage
            );

        lastHitTime = Time.time;

        enemy.TakeDamage(damage);

        enemy.FlashHitPart(
            collision.collider
        );

        enemy.KnockDown();
    }

    public void Activate()
    {
        activated = true;

        if (rb != null)
            rb.WakeUp();
    }

    public void MarkGrabbed()
    {
        Activate();

        thrown = false;
        thrownTimer = 0f;
    }

    public void MarkDropped()
    {
        Activate();

        thrown = false;
        thrownTimer = 0f;
    }

    public void MarkThrown()
    {
        Activate();

        thrown = true;
        thrownTimer = thrownDamageTime;
    }

    public void MarkForcePushed()
    {
        Activate();

        thrown = true;
        thrownTimer = thrownDamageTime;
    }

    public bool IsThrown()
    {
        return thrown;
    }
}