using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float health;

    public float explosionForce = 20f;
    public float explosionUpwardForce = 5f;

    public float restartDelay = 3f;

    public MonoBehaviour[] scriptsToDisable;

    public Image damageFlash;
    public float flashAlpha = 0.6f;
    public float flashDuration = 0.15f;

    private Rigidbody rb;
    private bool dead;
    private Coroutine flashCoroutine;

    void Start()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody>();

        if (damageFlash != null)
        {
            Color color = damageFlash.color;
            color.a = 0f;
            damageFlash.color = color;
        }
    }

    public void TakeDamage(float damage)
    {
        if (dead)
            return;

        health -= damage;

        FlashRed();

        if (health <= 0f)
        {
            health = 0f;
            Die(Vector3.zero, false);
        }
    }

    public void InstantKill(Vector3 explosionPosition)
    {
        if (dead)
            return;

        health = 0f;

        FlashRed();

        Die(explosionPosition, true);
    }

    void FlashRed()
    {
        if (damageFlash == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlash());
    }

    IEnumerator DamageFlash()
    {
        Color color = damageFlash.color;
        color.a = flashAlpha;
        damageFlash.color = color;

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(
                flashAlpha,
                0f,
                timer / flashDuration
            );

            damageFlash.color = color;

            yield return null;
        }

        color.a = 0f;
        damageFlash.color = color;
    }

    void Die(Vector3 explosionPosition, bool hitByExplosion)
    {
        if (dead)
            return;

        dead = true;

        for (int i = 0; i < scriptsToDisable.Length; i++)
        {
            if (scriptsToDisable[i] != null)
                scriptsToDisable[i].enabled = false;
        }

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (hitByExplosion)
            {
                Vector3 direction =
                    (transform.position - explosionPosition).normalized;

                direction += Vector3.up * 0.4f;

                rb.AddForce(
                    direction.normalized * explosionForce +
                    Vector3.up * explosionUpwardForce,
                    ForceMode.Impulse
                );

                rb.AddTorque(
                    Random.insideUnitSphere * explosionForce,
                    ForceMode.Impulse
                );
            }
        }

        StartCoroutine(RestartLevel());
    }

    IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(restartDelay);

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}