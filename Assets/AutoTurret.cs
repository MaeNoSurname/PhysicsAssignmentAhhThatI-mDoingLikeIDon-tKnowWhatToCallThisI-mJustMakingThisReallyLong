using UnityEngine;

public class AutoTurret : MonoBehaviour
{
    [Header("Missile")]
    public GameObject missilePrefab;
    public Transform shootPoint;

    [Header("Turret Settings")]
    public float range = 50f;

    public float startingFireRate = 2f;
    public float fireRateReductionPerScore = 0.1f;
    public float minimumFireRate = 0.15f;

    [Header("State")]
    public bool canFire = false;

    private PlayerMoveTeck target;
    private ScoreManager scoreManager;

    private float fireTimer;

    void Start()
    {
        target =
            FindAnyObjectByType<PlayerMoveTeck>();

        scoreManager =
            FindAnyObjectByType<ScoreManager>();
    }

    void Update()
    {

        if (!canFire)
            return;

        if (target == null)
        {
            target =
                FindAnyObjectByType<PlayerMoveTeck>();

            return;
        }

        if (scoreManager == null)
        {
            scoreManager =
                FindAnyObjectByType<ScoreManager>();
        }

        float distance =
            Vector3.Distance(
                transform.position,
                target.transform.position
            );

        if (distance > range)
            return;

        fireTimer += Time.deltaTime;

        int score = 0;

        if (scoreManager != null)
        {
            score =
                scoreManager.score;
        }

        float currentFireRate =
            Mathf.Max(
                minimumFireRate,
                startingFireRate -
                score *
                fireRateReductionPerScore
            );

        if (fireTimer >= currentFireRate)
        {
            Shoot();

            fireTimer = 0f;
        }
    }

    public void SetFiring(bool firing)
    {
        canFire = firing;


        fireTimer = 0f;
    }

    void Shoot()
    {
        if (missilePrefab == null)
            return;

        if (shootPoint == null)
            return;

        Instantiate( 

            missilePrefab,
            shootPoint.position,
            shootPoint.rotation
        );
    }
}
