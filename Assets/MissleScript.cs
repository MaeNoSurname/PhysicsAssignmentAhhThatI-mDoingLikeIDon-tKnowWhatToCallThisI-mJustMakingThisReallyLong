using UnityEngine;

public class MissleScript : MonoBehaviour
{
    public float launchSpeed = 20f;
    public float launchTime = 1.5f;

    public float acceleration = 80f;
    public float maxSpeed = 120f;
    public float turnSpeed = 250f;

    public bool spinOut;
    public float spinForce = 20f;
    public float spinAcceleration = 15f;
    public float spinOutExplosionDelay = 2.5f;

    public float flingSpeed = 60f;
    public float flingAcceleration = 30f;

    public GameObject explosionEffect;
    public float explosionLifetime = 3f;

    public float explosionRadius = 6f;
    public float armDelay = 0.5f;
    public int playerDamage = 30;

    private Rigidbody rb;
    private PlayerMoveTeck player;

    private float timer;
    private float armTimer;
    private float spinOutTimer;

    private bool lockedOn;
    private bool exploded;
    private bool grabbed;
    private bool flung;
    private bool wasSpinningOut;

    private Vector3 flingDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        player =
            FindAnyObjectByType<PlayerMoveTeck>();

        rb.useGravity = false;

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        for (
            int i = 0;
            i < colliders.Length;
            i++
        )
        {
            for (
                int j = i + 1;
                j < colliders.Length;
                j++
            )
            {
                Physics.IgnoreCollision(
                    colliders[i],
                    colliders[j],
                    true
                );
            }
        }

        rb.linearVelocity =
            Vector3.up *
            launchSpeed;
    }

    void FixedUpdate()
    {
        if (exploded)
            return;

        if (grabbed)
        {
            rb.WakeUp();
            return;
        }

        armTimer +=
            Time.fixedDeltaTime;

        if (spinOut)
        {
            if (!wasSpinningOut)
            {
                spinOutTimer = 0f;
                wasSpinningOut = true;
            }

            spinOutTimer +=
                Time.fixedDeltaTime;

            Vector3 randomTorque =
                Random.onUnitSphere;

            rb.AddTorque(
                randomTorque *
                spinForce,
                ForceMode.Acceleration
            );

            rb.AddForce(
                transform.forward *
                spinAcceleration,
                ForceMode.Acceleration
            );

            LimitSpeed();

            if (
                spinOutTimer >=
                spinOutExplosionDelay
            )
            {
                Explode();
            }

            return;
        }

        wasSpinningOut = false;
        spinOutTimer = 0f;

        if (flung)
        {
            if (
                flingDirection !=
                Vector3.zero
            )
            {
                rb.MoveRotation(
                    Quaternion.LookRotation(
                        flingDirection
                    )
                );

                rb.AddForce(
                    flingDirection *
                    flingAcceleration,
                    ForceMode.Acceleration
                );
            }

            LimitSpeed();

            return;
        }

        if (player == null)
            return;

        timer +=
            Time.fixedDeltaTime;

        if (!lockedOn)
        {
            rb.linearVelocity =
                Vector3.up *
                launchSpeed;

            if (timer >= launchTime)
                lockedOn = true;

            return;
        }

        Vector3 direction =
            (
                player.transform.position -
                transform.position
            ).normalized;

        if (
            direction !=
            Vector3.zero
        )
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction
                );

            rb.MoveRotation(
                Quaternion.RotateTowards(
                    rb.rotation,
                    targetRotation,
                    turnSpeed *
                    Time.fixedDeltaTime
                )
            );
        }

        rb.AddForce(
            transform.forward *
            acceleration,
            ForceMode.Acceleration
        );

        LimitSpeed();
    }

    public void SetGrabbed(bool value)
    {
        grabbed = value;

        if (grabbed)
        {
            spinOut = false;
            flung = false;

            spinOutTimer = 0f;
            wasSpinningOut = false;

            rb.isKinematic = false;
            rb.useGravity = false;

            rb.WakeUp();
        }
    }

    public void ReleaseFromGrab(
        Vector3 direction
    )
    {
        grabbed = false;
        flung = true;
        spinOut = false;

        spinOutTimer = 0f;
        wasSpinningOut = false;
        armTimer = 0f;

        rb.isKinematic = false;
        rb.useGravity = false;

        rb.WakeUp();

        flingDirection =
            direction.normalized;

        rb.linearVelocity =
            flingDirection *
            flingSpeed;

        rb.angularVelocity =
            Vector3.zero;

        if (
            flingDirection !=
            Vector3.zero
        )
        {
            rb.rotation =
                Quaternion.LookRotation(
                    flingDirection
                );
        }
    }

    void LimitSpeed()
    {
        if (
            rb.linearVelocity.magnitude >
            maxSpeed
        )
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized *
                maxSpeed;
        }
    }

    void OnCollisionEnter(
        Collision collision
    )
    {
        if (
            exploded ||
            grabbed
        )
        {
            return;
        }

        if (armTimer < armDelay)
            return;

        if (
            collision.transform.root ==
            transform.root
        )
        {
            return;
        }

        ForceGun forceGun =
            collision.collider
                .GetComponentInParent<ForceGun>();

        if (forceGun != null)
            return;

        PlayerHealth hitPlayer =
            collision.collider
                .GetComponentInParent<PlayerHealth>();

        if (hitPlayer != null)
        {
            hitPlayer.TakeDamage(
                playerDamage
            );
        }

        Explode();
    }

    void OnTriggerEnter(
        Collider other
    )
    {
        if (
            exploded ||
            grabbed
        )
        {
            return;
        }

        if (armTimer < armDelay)
            return;

        if (
            other.transform.root ==
            transform.root
        )
        {
            return;
        }

        ForceGun forceGun =
            other.GetComponentInParent<ForceGun>();

        if (forceGun != null)
            return;

        PlayerHealth hitPlayer =
            other.GetComponentInParent<PlayerHealth>();

        if (hitPlayer != null)
        {
            hitPlayer.TakeDamage(
                playerDamage
            );
        }

        Explode();
    }

    void Explode()
    {
        if (
            exploded ||
            grabbed
        )
        {
            return;
        }

        exploded = true;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                explosionRadius
            );

        for (
            int i = 0;
            i < hits.Length;
            i++
        )
        {
            SimpleEnemy enemy =
                hits[i]
                    .GetComponentInParent<SimpleEnemy>();

            if (enemy != null)
            {
                enemy.DieFromExplosion(
                    transform.position
                );
            }
        }

        if (
            explosionEffect !=
            null
        )
        {
            GameObject effect =
                Instantiate(
                    explosionEffect,
                    transform.position,
                    Quaternion.identity
                );

            Destroy(
                effect,
                explosionLifetime
            );
        }

        Destroy(gameObject);
    }
}