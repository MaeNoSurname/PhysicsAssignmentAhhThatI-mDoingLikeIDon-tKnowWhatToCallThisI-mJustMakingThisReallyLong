using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPointHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 200f;
    public float health = 200f;

    [Header("Damage Limit")]
    public float maximumDamagePerSecond = 75f;

    [Header("Impact Damage")]
    public float minimumImpactSpeed = 2f;
    public float impactDamageMultiplier = 4f;
    public float maximumImpactDamage = 100f;
    public float impactDamageCooldown = 0.15f;

    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.12f;

    [Header("Constant Shake")]
    public float maximumShakeAmount = 0.25f;
    public float shakeSpeed = 35f;
    public bool shakeRotation = true;
    public float maximumRotationShake = 3f;

    [Header("Death")]
    public float deathExplosionForce = 15f;
    public float deathExplosionRadius = 5f;
    public float deathUpwardForce = 2f;
    public float bodyPartLifetime = 10f;

    private bool dead;

    private float lastImpactTime;

    private float damageTakenThisSecond;
    private float damageWindowStartTime;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private readonly Dictionary<Renderer, Color> originalColors = new();
    private readonly Dictionary<Renderer, Coroutine> flashCoroutines = new();


    void Awake()
    {
        health = maxHealth;

        damageWindowStartTime = Time.time;

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        SetupChildColliders();
    }


    void Update()
    {
        if (dead)
            return;

        UpdateShake();
    }


    void SetupChildColliders()
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            ColliderRelay relay;

            if (!col.TryGetComponent(out relay))
            {
                relay =
                    col.gameObject.AddComponent<ColliderRelay>();
            }

            relay.SetOwner(
                this,
                col
            );
        }
    }


    public void HandleCollision(
        Collision collision,
        Collider hitCollider
    )
    {
        if (dead)
            return;

        if (collision == null)
            return;

        if (
            Time.time - lastImpactTime <
            impactDamageCooldown
        )
            return;


        ThrowableObject throwable =
            collision.gameObject
                .GetComponentInParent<ThrowableObject>();

        if (throwable == null)
            return;


        float impactSpeed =
            collision.relativeVelocity.magnitude;


        if (impactSpeed < minimumImpactSpeed)
            return;


        float damage =
            (
                impactSpeed -
                minimumImpactSpeed
            ) *
            impactDamageMultiplier;


        damage =
            Mathf.Clamp(
                damage,
                0f,
                maximumImpactDamage
            );


        lastImpactTime =
            Time.time;


        TakeDamage(
            damage
        );


        FlashHitPart(
            hitCollider
        );
    }


    public void TakeDamage(float amount)
    {
        if (dead)
            return;

        if (amount <= 0f)
            return;


        if (
            Time.time -
            damageWindowStartTime >=
            1f
        )
        {
            damageWindowStartTime =
                Time.time;

            damageTakenThisSecond =
                0f;
        }


        float remainingDamage =
            maximumDamagePerSecond -
            damageTakenThisSecond;


        if (remainingDamage <= 0f)
            return;


        float actualDamage =
            Mathf.Min(
                amount,
                remainingDamage
            );


        damageTakenThisSecond +=
            actualDamage;


        health -=
            actualDamage;


        health =
            Mathf.Max(
                health,
                0f
            );


        Debug.Log(
            $"{name} took {actualDamage:F1} damage. " +
            $"Health: {health:F1}/{maxHealth:F1}"
        );


        if (health <= 0f)
        {
            DieFromExplosion(
                transform.position
            );
        }
    }


    void UpdateShake()
    {
        if (maxHealth <= 0f)
            return;


        float missingHealthPercent =
            1f -
            Mathf.Clamp01(
                health /
                maxHealth
            );


        if (missingHealthPercent <= 0f)
        {
            ResetTransform();
            return;
        }


        float shakeAmount =
            maximumShakeAmount *
            missingHealthPercent;


        float rotationAmount =
            maximumRotationShake *
            missingHealthPercent;


        float time =
            Time.time *
            shakeSpeed;


        Vector3 noise =
            new Vector3(
                Mathf.PerlinNoise(
                    time,
                    0f
                ) - 0.5f,

                Mathf.PerlinNoise(
                    0f,
                    time + 100f
                ) - 0.5f,

                Mathf.PerlinNoise(
                    time + 200f,
                    time
                ) - 0.5f
            ) * 2f;


        transform.localPosition =
            originalLocalPosition +
            noise *
            shakeAmount;


        if (shakeRotation)
        {
            Vector3 rotationOffset =
                new Vector3(
                    noise.z,
                    noise.x,
                    noise.y
                ) *
                rotationAmount;


            transform.localRotation =
                originalLocalRotation *
                Quaternion.Euler(
                    rotationOffset
                );
        }
        else
        {
            transform.localRotation =
                originalLocalRotation;
        }
    }


    void ResetTransform()
    {
        transform.SetLocalPositionAndRotation(
            originalLocalPosition,
            originalLocalRotation
        );
    }


    void FlashHitPart(
        Collider hitCollider
    )
    {
        if (hitCollider == null)
            return;


        Renderer hitRenderer;

        if (!hitCollider.TryGetComponent(out hitRenderer))
        {
            hitRenderer =
                hitCollider.GetComponentInChildren<Renderer>();
        }


        if (hitRenderer == null)
            return;


        Material material =
            hitRenderer.material;


        if (!originalColors.ContainsKey(hitRenderer))
        {
            originalColors.Add(
                hitRenderer,
                material.color
            );
        }


        if (
            flashCoroutines.TryGetValue(
                hitRenderer,
                out Coroutine existingCoroutine
            ) &&
            existingCoroutine != null
        )
        {
            StopCoroutine(
                existingCoroutine
            );
        }


        flashCoroutines[hitRenderer] =
            StartCoroutine(
                FlashRenderer(
                    hitRenderer
                )
            );
    }


    IEnumerator FlashRenderer(
        Renderer targetRenderer
    )
    {
        if (targetRenderer == null)
            yield break;


        Material material =
            targetRenderer.material;


        Color originalColor =
            originalColors[targetRenderer];


        material.color =
            hitFlashColor;


        yield return new WaitForSeconds(
            hitFlashDuration
        );


        if (targetRenderer != null)
        {
            material.color =
                originalColor;

            flashCoroutines.Remove(
                targetRenderer
            );
        }
    }


    public void DieFromExplosion(
        Vector3 explosionPosition
    )
    {
        if (dead)
            return;


        dead = true;

        ResetTransform();

        StopAllCoroutines();


        Transform[] children =
            GetComponentsInChildren<Transform>(
                true
            );


        for (
            int i = children.Length - 1;
            i >= 0;
            i--
        )
        {
            Transform part =
                children[i];


            if (
                part == null ||
                part == transform
            )
                continue;


            if (
                !part.TryGetComponent(
                    out Renderer _
                ) &&
                !part.TryGetComponent(
                    out Collider _
                )
            )
                continue;


            part.SetParent(
                null,
                true
            );


            Rigidbody partRb;


            if (!part.TryGetComponent(out partRb))
            {
                partRb =
                    part.gameObject.AddComponent<Rigidbody>();
            }


            partRb.isKinematic =
                false;

            partRb.useGravity =
                true;

            partRb.constraints =
                RigidbodyConstraints.None;


            partRb.AddExplosionForce(
                deathExplosionForce,
                explosionPosition,
                deathExplosionRadius,
                deathUpwardForce,
                ForceMode.Impulse
            );


            Destroy(
                part.gameObject,
                bodyPartLifetime
            );
        }


        Destroy(
            gameObject
        );
    }


    public bool IsDead()
    {
        return dead;
    }


    void OnDisable()
    {
        if (!dead)
        {
            ResetTransform();
        }
    }


    private class ColliderRelay : MonoBehaviour
    {
        private EnemySpawnPointHealth owner;
        private Collider myCollider;


        public void SetOwner(
            EnemySpawnPointHealth newOwner,
            Collider newCollider
        )
        {
            owner = newOwner;
            myCollider = newCollider;
        }


        void OnCollisionEnter(
            Collision collision
        )
        {
            if (owner == null)
                return;


            owner.HandleCollision(
                collision,
                myCollider
            );
        }
    }
}
