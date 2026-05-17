using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearSceneMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}
