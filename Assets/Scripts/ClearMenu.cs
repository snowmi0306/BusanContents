using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearSceneMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";

    public void GoToTitle()
    {
        LoadScene(titleSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Title scene name is empty.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
