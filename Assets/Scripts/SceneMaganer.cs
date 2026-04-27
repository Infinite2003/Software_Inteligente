using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMaganer : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (sceneName != string.Empty)
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("Scene not selected");
    }

    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("No more scenes to load.");
        }
    }

    public void LoadPreviousScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int previousSceneIndex = currentSceneIndex - 1;
        if (previousSceneIndex >= 0)
        {
            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            Debug.LogWarning("No previous scenes to load.");
        }
    }
}
