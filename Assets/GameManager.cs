using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject endImage;
    public string nextSceneName;
    public float nextSceneDelay = 2f;

    private bool levelComplete;
    private bool loadingNextScene;

    void Start()
    {
        if (endImage != null)
        {
            endImage.SetActive(false);
        }
    }

    void Update()
    {
        if (!levelComplete)
        {
            enemySpawner[] spawners =
                FindObjectsByType<enemySpawner>(
                    FindObjectsSortMode.None
                );

            SimpleEnemy[] enemies =
                FindObjectsByType<SimpleEnemy>(
                    FindObjectsSortMode.None
                );

            if (
                spawners.Length == 0 &&
                enemies.Length == 0
            )
            {
                CompleteLevel();
            }
        }
        else if (!loadingNextScene)
        {
            if (
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter)
            )
            {
                StartCoroutine(
                    LoadNextScene()
                );
            }
        }
    }

    void CompleteLevel()
    {
        levelComplete = true;

        if (endImage != null)
        {
            endImage.SetActive(true);
        }

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
    }

    IEnumerator LoadNextScene()
    {
        loadingNextScene = true;

        yield return new WaitForSeconds(
            nextSceneDelay
        );

        SceneManager.LoadScene(
            nextSceneName
        );
    }
}
