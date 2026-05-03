using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator GetConfig(Action<string> onSuccess)
    {
        string url = "http://127.0.0.1:5000/config";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                yield break;
            }

            onSuccess?.Invoke(request.downloadHandler.text);
        }
    }

    public IEnumerator SendTurn(
    int playerMoveIndex,
    System.Action<TurnResponse> onSuccess)
    {
        string url = "http://127.0.0.1:5000/turn";

        TurnRequest requestData = new TurnRequest
        {
            run_id = GameState.Instance.currentRun.run_id,
            monster_index = GameState.Instance.selectedMonsterIndex,
            player_move_index = playerMoveIndex,
            known_moves = GameState.Instance.knownMoves.ConvertAll(m => m.name)
        };

        string json = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request =
            new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw =
                System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(bodyRaw);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                yield break;
            }

            TurnResponse response =
                JsonConvert.DeserializeObject<TurnResponse>(
                    request.downloadHandler.text
                );

            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator SendLevelUp(string stat, System.Action<StatsData> onSuccess)
    {
        string url = "http://127.0.0.1:5000/levelup";

        var data = new
        {
            run_id = GameState.Instance.currentRun.run_id,
            stat = stat
        };

        string json = JsonConvert.SerializeObject(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                yield break;
            }

            var stats = JsonConvert.DeserializeObject<StatsData>(
                request.downloadHandler.text
            );

            onSuccess?.Invoke(stats);
        }
    }
}