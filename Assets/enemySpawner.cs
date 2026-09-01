using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class enemySpawner : MonoBehaviour
{
    [Header("Enemies")]
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;

    public float spawnDelay = 2f;
    public int maxAliveEnemies = 8;

    [Header("Spawn Point Health")]
    public float spawnPointMaxHealth = 200f;

    [Header("Missile Survival Phase")]
    public AutoTurret[] missileTurrets;
    public float missileSurvivalTime = 5f;

    [Header("UI")]
    public TMP_Text objectiveText;
    public float flashSpeed = 6f;
    public float minimumTextAlpha = 0.2f;

    [Header("Exit Door")]
    public Transform exitDoor;
    public Transform exitDoorEndPoint;
    public float doorSpeed = 2f;

    private Transform player;

    private readonly List<GameObject> spawnedEnemies = new();

    private bool missilePhaseStarted;
    private bool missilePhaseFinished;

    private float missileTimer;

    private bool doorOpening;
    private bool doorFinished;

    private Coroutine spawnRoutine;

    private bool allSpawnPointsDestroyed;
    private int lastAliveBaseCount = -1;

    void Start()
    {
        PlayerMoveTeck playerScript =
            FindAnyObjectByType<PlayerMoveTeck>();

        if (playerScript != null)
        {
            player = playerScript.transform;
        }

        if (missileTurrets != null)
        {
            foreach (AutoTurret turret in missileTurrets)
            {
                if (turret != null)
                {
                    turret.SetFiring(false);
                }
            }
        }

        SetupSpawnPointHealth();

        spawnRoutine =
            StartCoroutine(
                SpawnEnemiesForever()
            );

        UpdateObjectiveUI();
    }

    void Update()
    {
        RemoveDeadEnemies();

        CheckSpawnPoints();

        if (
            allSpawnPointsDestroyed &&
            spawnedEnemies.Count == 0
        )
        {
            Destroy(gameObject);
            return;
        }

        if (missilePhaseStarted)
        {
            missileTimer -= Time.deltaTime;

            UpdateMissileUI();
            FlashObjectiveText();

            if (missileTimer <= 0f)
            {
                FinishMissilePhase();
            }

            return;
        }

        ResetObjectiveTextAlpha();

        if (
            missilePhaseFinished &&
            !doorOpening &&
            !doorFinished
        )
        {
            doorOpening = true;
        }

        if (doorOpening)
        {
            LowerDoor();
        }
    }

    void SetupSpawnPointHealth()
    {
        if (spawnPoints == null)
            return;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            EnemySpawnPointHealth health;

            if (
                !spawnPoint.TryGetComponent(
                    out health
                )
            )
            {
                health =
                    spawnPoint.gameObject
                        .AddComponent<EnemySpawnPointHealth>();
            }

            health.maxHealth =
                spawnPointMaxHealth;

            health.health =
                spawnPointMaxHealth;
        }
    }

    IEnumerator SpawnEnemiesForever()
    {
        while (!allSpawnPointsDestroyed)
        {
            RemoveDeadEnemies();

            if (
                spawnedEnemies.Count <
                maxAliveEnemies
            )
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(
                spawnDelay
            );
        }
    }

    void SpawnEnemy()
    {
        if (
            enemyPrefabs == null ||
            enemyPrefabs.Length == 0
        )
            return;

        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
            return;

        List<Transform> availableSpawnPoints =
            new();

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            EnemySpawnPointHealth health;

            if (
                point.TryGetComponent(
                    out health
                )
            )
            {
                if (health.IsDead())
                    continue;
            }

            availableSpawnPoints.Add(
                point
            );
        }

        if (availableSpawnPoints.Count == 0)
        {
            SetAllSpawnPointsDestroyed();
            return;
        }

        GameObject enemyPrefab =
            enemyPrefabs[
                Random.Range(
                    0,
                    enemyPrefabs.Length
                )
            ];

        Transform spawnPoint =
            availableSpawnPoints[
                Random.Range(
                    0,
                    availableSpawnPoints.Count
                )
            ];

        GameObject enemy =
            Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        SimpleEnemy enemyScript;

        if (
            enemy.TryGetComponent(
                out enemyScript
            )
        )
        {
            enemyScript.SetPlayer(
                player
            );

            enemyScript.enabled =
                true;
        }

        spawnedEnemies.Add(
            enemy
        );

        UpdateObjectiveUI();
    }

    void CheckSpawnPoints()
    {
        if (allSpawnPointsDestroyed)
            return;

        int aliveBases =
            GetAliveBaseCount();

        if (aliveBases != lastAliveBaseCount)
        {
            lastAliveBaseCount =
                aliveBases;

            UpdateObjectiveUI();
        }

        if (aliveBases == 0)
        {
            SetAllSpawnPointsDestroyed();
        }
    }

    int GetAliveBaseCount()
    {
        if (spawnPoints == null)
            return 0;

        int aliveBases = 0;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            EnemySpawnPointHealth health;

            if (
                point.TryGetComponent(
                    out health
                )
            )
            {
                if (!health.IsDead())
                {
                    aliveBases++;
                }
            }
            else
            {
                aliveBases++;
            }
        }

        return aliveBases;
    }

    void SetAllSpawnPointsDestroyed()
    {
        if (allSpawnPointsDestroyed)
            return;

        allSpawnPointsDestroyed =
            true;

        if (spawnRoutine != null)
        {
            StopCoroutine(
                spawnRoutine
            );

            spawnRoutine =
                null;
        }

        UpdateObjectiveUI();
    }

    void RemoveDeadEnemies()
    {
        bool enemyRemoved =
            false;

        for (
            int i = spawnedEnemies.Count - 1;
            i >= 0;
            i--
        )
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);

                enemyRemoved =
                    true;
            }
        }

        if (enemyRemoved)
        {
            UpdateObjectiveUI();
        }
    }

    void UpdateObjectiveUI()
    {
        if (objectiveText == null)
            return;

        if (
            missilePhaseStarted ||
            missilePhaseFinished
        )
            return;

        ResetObjectiveTextAlpha();

        if (!allSpawnPointsDestroyed)
        {
            int basesLeft =
                GetAliveBaseCount();

            objectiveText.text =
                "DESTROY ALL BASES\nBASES LEFT: " +
                basesLeft;

            return;
        }

        objectiveText.text =
            "KILL ALL ENEMIES\nENEMIES LEFT: " +
            spawnedEnemies.Count;
    }

    void StartMissilePhase()
    {
        missilePhaseStarted =
            true;

        missileTimer =
            missileSurvivalTime;

        if (missileTurrets != null)
        {
            foreach (AutoTurret turret in missileTurrets)
            {
                if (turret != null)
                {
                    turret.SetFiring(true);
                }
            }
        }

        UpdateMissileUI();
    }

    void FinishMissilePhase()
    {
        missilePhaseStarted =
            false;

        missilePhaseFinished =
            true;

        if (missileTurrets != null)
        {
            foreach (AutoTurret turret in missileTurrets)
            {
                if (turret != null)
                {
                    turret.SetFiring(false);
                }
            }
        }

        ResetObjectiveTextAlpha();

        if (objectiveText != null)
        {
            objectiveText.text =
                "SURVIVED! EXIT OPENING";
        }
    }

    void UpdateMissileUI()
    {
        if (objectiveText == null)
            return;

        int secondsLeft =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    missileTimer
                )
            );

        objectiveText.text =
            "SURVIVE MISSILES: " +
            secondsLeft;
    }

    void FlashObjectiveText()
    {
        if (objectiveText == null)
            return;

        Color color =
            objectiveText.color;

        float alpha =
            Mathf.Lerp(
                minimumTextAlpha,
                1f,
                Mathf.PingPong(
                    Time.time * flashSpeed,
                    1f
                )
            );

        color.a =
            alpha;

        objectiveText.color =
            color;
    }

    void ResetObjectiveTextAlpha()
    {
        if (objectiveText == null)
            return;

        Color color =
            objectiveText.color;

        color.a =
            1f;

        objectiveText.color =
            color;
    }

    void LowerDoor()
    {
        if (
            exitDoor == null ||
            exitDoorEndPoint == null
        )
            return;

        exitDoor.position =
            Vector3.MoveTowards(
                exitDoor.position,
                exitDoorEndPoint.position,
                doorSpeed *
                Time.deltaTime
            );

        if (
            Vector3.Distance(
                exitDoor.position,
                exitDoorEndPoint.position
            ) <= 0.01f
        )
        {
            exitDoor.position =
                exitDoorEndPoint.position;

            doorOpening =
                false;

            doorFinished =
                true;

            if (objectiveText != null)
            {
                objectiveText.text =
                    "ESCAPE!";
            }
        }
    }
}