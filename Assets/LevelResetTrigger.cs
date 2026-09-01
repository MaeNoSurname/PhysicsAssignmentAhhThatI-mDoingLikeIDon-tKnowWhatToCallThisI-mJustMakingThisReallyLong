using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelResetTrigger : MonoBehaviour
{
    private bool resetting;

    void OnTriggerEnter(Collider other)
    {
        if (resetting)
            return;

        if (other.isTrigger)
            return;

        PlayerMoveTeck player =
            other.GetComponentInParent<PlayerMoveTeck>();

        if (player == null)
            return;

        resetting = true;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}