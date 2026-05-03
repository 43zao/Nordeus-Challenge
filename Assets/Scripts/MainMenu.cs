using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        StartCoroutine(StartGameFlow());
    }

    private IEnumerator StartGameFlow()
    {
        yield return ApiClient.Instance.GetConfig(OnConfigReceived);
    }

    private void OnConfigReceived(string json)
    {
        Debug.Log("CONFIG RECEIVED:");
        Debug.Log(json);

        GameState.Instance.SetConfig(json);

        SceneManager.LoadScene("LevelSelect");
    }

    public void ExitGame()
    {
        Debug.Log("Exit pressed");
        Application.Quit();
    }
}