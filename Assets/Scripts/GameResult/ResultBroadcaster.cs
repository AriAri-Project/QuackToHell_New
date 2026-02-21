using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResultBroadcaster : NetworkBehaviour
{
    public static ResultBroadcaster Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private string resultSceneName = "ResultScene";

    public bool HasPayload { get; private set; }
    public GameResultPayload LastPayload { get; private set; }

    private void Awake()
    {
        // Singleton + Persist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 서버에서만 호출: payload 전파 + ResultScene 이동
    /// </summary>
    public void EndGameAndShowResult(GameResultPayload payload)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ResultBroadcaster] EndGameAndShowResult called on non-server.");
            return;
        }

        // 서버도 동일하게 캐시
        CachePayload(payload);

        // 모든 클라에 payload 캐시 시킴
        CachePayloadClientRpc(payload);

        // 네트워크 씬 로드 (모든 클라 동기화)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(resultSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[ResultBroadcaster] NetworkManager/SceneManager missing. Cannot load ResultScene.");
        }
    }

    [ClientRpc]
    private void CachePayloadClientRpc(GameResultPayload payload)
    {
        CachePayload(payload);
    }

    private void CachePayload(GameResultPayload payload)
    {
        LastPayload = payload;
        HasPayload = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!HasPayload) return;
        if (!scene.name.Equals(resultSceneName)) return;

        PlayerView[] players = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.SetPlayerVisibility(false);
        }

        // ResultScene 로드가 끝났으면 UI 찾아서 렌더
        var ui = FindFirstObjectByType<ResultScreenUI>(FindObjectsInactive.Include);
        if (ui != null)
            ui.Open(LastPayload);
        else
            Debug.LogError("[ResultBroadcaster] ResultScreenUI not found in ResultScene.");
    }
}
