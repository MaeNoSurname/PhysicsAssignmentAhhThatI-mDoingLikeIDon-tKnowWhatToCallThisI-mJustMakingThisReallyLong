using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float damage = 10f;
    public float turnSpeed = 8f;

    [Header("Edge Detection")]
    public LayerMask groundLayers = ~0;
    public float edgeCheckForwardDistance = 0.8f;
    public float edgeCheckHeight = 0.5f;
    public float edgeCheckDownDistance = 1.5f;

    [Header("Stun")]
    public float stunTime = 1f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("Impact Damage")]
    public float minimumImpactSpeed = 2f;
    public float impactDamageMultiplier = 4f;
    public float maximumImpactDamage = 100f;
    public float impactDamageCooldown = 0.15f;

    [Header("Ragdoll Collision Damage")]
    public float minimumRagdollImpactSpeed = 5f;
    public float ragdollImpactDamageMultiplier = 6f;
    public float maximumRagdollImpactDamage = 100f;
    public LayerMask ragdollImpactLayers = ~0;

    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.12f;

    [Header("Death")]
    public float deathExplosionForce = 15f;
    public float deathExplosionRadius = 5f;
    public float deathUpwardForce = 2f;
    public float bodyPartLifetime = 10f;

    private Transform player;
    private Rigidbody rb;

    private float attackTimer;
    private float lastImpactTime;

    private bool stunned;
    private bool dead;
    private bool ragdolled;

    private RigidbodyConstraints normalConstraints;
    private Coroutine knockDownCoroutine;

    private Dictionary<Renderer, Color> originalColors =
        new Dictionary<Renderer, Color>();

    private Dictionary<Renderer, Coroutine> flashCoroutines =
        new Dictionary<Renderer, Coroutine>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        normalConstraints = rb.constraints;

        health = maxHealth;
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    void Update()
    {
        if (
            player == null ||
            stunned ||
            dead ||
            ragdolled
        )
            return;

        attackTimer += Time.deltaTime;

        float distance = Vector3.Distance(
            rb.position,
            player.position
        );

        if (distance <= attackRange)
        {
            Attack();
        }
    }

    void FixedUpdate()
    {
        if (
            player == null ||
            stunned ||
            dead ||
            ragdolled
        )
            return;

        Vector3 direction =
            player.position -
            rb.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;

        if (distance <= attackRange)
        {
            StopHorizontalMovement();
            return;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            StopHorizontalMovement();
            return;
        }

        direction.Normalize();

        if (!HasGroundAhead(direction))
        {
            StopHorizontalMovement();
            return;
        }

        rb.linearVelocity =
            new Vector3(
                direction.x * moveSpeed,
                rb.linearVelocity.y,
                direction.z * moveSpeed
            );

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                turnSpeed *
                Time.fixedDeltaTime
            )
        );
    }

    bool HasGroundAhead(Vector3 direction)
    {
        Vector3 rayOrigin =
            rb.position +
            Vector3.up *
            edgeCheckHeight +
            direction *
            edgeCheckForwardDistance;

        bool hitGround =
            Physics.Raycast(
                rayOrigin,
                Vector3.down,
                edgeCheckDownDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );

        return hitGround;
    }

    void StopHorizontalMovement()
    {
        rb.linearVelocity =
            new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );
    }

    void Attack()
    {
        if (attackTimer < attackCooldown)
            return;

        attackTimer = 0f;

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                damage
            );
        }
    }

    public void KnockDown()
    {
        if (dead)
            return;

        if (knockDownCoroutine != null)
        {
            StopCoroutine(
                knockDownCoroutine
            );
        }

        knockDownCoroutine =
            StartCoroutine(
                KnockDownRoutine()
            );
    }

    IEnumerator KnockDownRoutine()
    {
        stunned = true;
        ragdolled = true;

        rb.constraints =
            RigidbodyConstraints.None;

        yield return new WaitForSeconds(
            stunTime
        );

        while (
            rb.linearVelocity.magnitude > 1.5f ||
            rb.angularVelocity.magnitude > 2f
        )
        {
            yield return new WaitForSeconds(
                0.1f
            );
        }

        if (dead)
            yield break;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        Vector3 rotation =
            rb.rotation.eulerAngles;

        rb.rotation =
            Quaternion.Euler(
                0f,
                rotation.y,
                0f
            );

        rb.constraints =
            normalConstraints;

        ragdolled = false;
        stunned = false;
        knockDownCoroutine = null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (dead)
            return;

        if (
            Time.time -
            lastImpactTime <
            impactDamageCooldown
        )
            return;

        if (collision.contactCount == 0)
            return;

        ContactPoint contact =
            collision.GetContact(0);

        Collider otherCollider =
            contact.otherCollider;

        Collider hitBodyPart =
            contact.thisCollider;

        ThrowableObject throwable =
            otherCollider
                .GetComponentInParent<ThrowableObject>();

        if (throwable != null)
        {
            float impactSpeed =
                collision.relativeVelocity.magnitude;

            if (impactSpeed < minimumImpactSpeed)
                return;

            float impactDamage =
                (
                    impactSpeed -
                    minimumImpactSpeed
                ) *
                impactDamageMultiplier;

            impactDamage =
                Mathf.Clamp(
                    impactDamage,
                    0f,
                    maximumImpactDamage
                );

            lastImpactTime =
                Time.time;

            TakeDamage(
                impactDamage
            );

            FlashHitPart(
                hitBodyPart
            );

            if (!dead && !ragdolled)
            {
                KnockDown();
            }

            return;
        }

        if (!ragdolled)
            return;

        int otherLayer =
            collision.gameObject.layer;

        if (
            (ragdollImpactLayers.value &
            (1 << otherLayer)) == 0
        )
            return;

        float impactSpeedIntoSurface =
            Mathf.Abs(
                Vector3.Dot(
                    collision.relativeVelocity,
                    contact.normal
                )
            );

        if (
            impactSpeedIntoSurface <
            minimumRagdollImpactSpeed
        )
            return;

        float ragdollDamage =
            (
                impactSpeedIntoSurface -
                minimumRagdollImpactSpeed
            ) *
            ragdollImpactDamageMultiplier;

        ragdollDamage =
            Mathf.Clamp(
                ragdollDamage,
                0f,
                maximumRagdollImpactDamage
            );

        lastImpactTime =
            Time.time;

        TakeDamage(
            ragdollDamage
        );

        FlashHitPart(
            hitBodyPart
        );

        Debug.Log(
            name +
            " slammed into " +
            collision.gameObject.name +
            " at " +
            impactSpeedIntoSurface.ToString("F1") +
            " m/s and took " +
            ragdollDamage.ToString("F1") +
            " damage."
        );
    }

    public void FlashHitPart(Collider hitCollider)
    {
        if (hitCollider == null)
            return;

        Renderer hitRenderer =
            hitCollider.GetComponent<Renderer>();

        if (hitRenderer == null)
        {
            hitRenderer =
                hitCollider.GetComponentInChildren<Renderer>();
        }

        if (hitRenderer == null)
        {
            hitRenderer =
                hitCollider.GetComponentInParent<Renderer>();
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

        Coroutine newCoroutine =
            StartCoroutine(
                FlashRenderer(
                    hitRenderer
                )
            );

        flashCoroutines[hitRenderer] =
            newCoroutine;
    }

    IEnumerator FlashRenderer(Renderer targetRenderer)
    {
        if (targetRenderer == null)
            yield break;

        Material material =
            targetRenderer.material;

        Color originalColor =
            originalColors[targetRenderer];

        material.color =
            hitFlashColor;

        float elapsed = 0f;

        while (elapsed < hitFlashDuration)
        {
            if (targetRenderer == null)
                yield break;

            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    hitFlashDuration
                );

            material.color =
                Color.Lerp(
                    hitFlashColor,
                    originalColor,
                    t
                );

            yield return null;
        }

        if (targetRenderer != null)
        {
            material.color =
                originalColor;

            flashCoroutines.Remove(
                targetRenderer
            );
        }
    }

    public void TakeDamage(float amount)
    {
        if (dead)
            return;

        health -= amount;

        health =
            Mathf.Max(
                health,
                0f
            );

        Debug.Log(
            name +
            " took " +
            amount.ToString("F1") +
            " damage. Health: " +
            health.ToString("F1")
        );

        if (health <= 0f)
        {
            DieFromExplosion(
                rb.worldCenterOfMass
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

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(
                1
            );
        }

        foreach (
            KeyValuePair<Renderer, Color> pair
            in originalColors
        )
        {
            if (pair.Key != null)
            {
                pair.Key.material.color =
                    pair.Value;
            }
        }

        StopAllCoroutines();

        Transform[] children =
            GetComponentsInChildren<Transform>(
                true
            );

        for (
            int i = 0;
            i < children.Length;
            i++
        )
        {
            Transform part =
                children[i];

            if (part == transform)
                continue;

            if (
                part.parent !=
                transform
            )
                continue;

            part.SetParent(
                null,
                true
            );

            Rigidbody partRb =
                part.GetComponent<Rigidbody>();

            if (partRb == null)
            {
                partRb =
                    part.gameObject
                        .AddComponent<Rigidbody>();
            }

            partRb.isKinematic =
                false;

            partRb.useGravity =
                true;

            partRb.constraints =
                RigidbodyConstraints.None;

            partRb.linearVelocity =
                rb.linearVelocity;

            partRb.angularVelocity =
                rb.angularVelocity;

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

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        Vector3 rayOrigin =
            transform.position +
            Vector3.up *
            edgeCheckHeight +
            direction *
            edgeCheckForwardDistance;

        Gizmos.DrawSphere(
            rayOrigin,
            0.05f
        );

        Gizmos.DrawLine(
            rayOrigin,
            rayOrigin +
            Vector3.down *
            edgeCheckDownDistance
        );
    }
}