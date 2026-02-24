using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
        StartCoroutine(DelayedLoadResultScene());
    }

    private IEnumerator DelayedLoadResultScene()
    {
        Debug.Log("[ResultBroadcaster] Waiting 4 seconds before loading ResultScene...");

        yield return new WaitForSeconds(4f);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(resultSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[ResultBroadcaster] NetworkManager/SceneManager missing.");
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
        Debug.Log($"[ResultBroadcaster] SceneLoaded: {scene.name}, HasPayload: {HasPayload}");

        // ResultScene 아니면 아무것도 안 함
        if (!scene.name.Equals(resultSceneName))
            return;

        // 서버에서 플레이어 V자 배치
        if (IsServer)
        {
            ArrangePlayersInVShape();
        }

        // payload 없으면 UI 안 띄움
        if (!HasPayload)
        {
            Debug.LogWarning("[ResultBroadcaster] HasPayload is false.");
            return;
        }

        // 결과 UI 표시
        var ui = FindFirstObjectByType<ResultScreenUI>(FindObjectsInactive.Include);

        if (ui != null)
            ui.Open(LastPayload);
        else
            Debug.LogError("[ResultBroadcaster] ResultScreenUI not found in ResultScene.");
    }

    private void ArrangePlayersInVShape()
    {
        PlayerView[] playerViews = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);

        if (playerViews.Length == 0)
            return;

        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("[ResultBroadcaster] Main Camera not found.");
            return;
        }

        Vector3 center = cam.transform.position;
        center.z = 0f;

        float xSpacing = 2.5f;
        float ySpacing = 1.2f;

        for (int i = 0; i < playerViews.Length; i++)
        {
            Vector3 pos;

            if (i == 0)
            {
                pos = center;
            }
            else
            {
                int pairIndex = (i - 1) / 2;
                bool isLeft = (i - 1) % 2 == 0;

                float xOffset = (pairIndex + 1) * xSpacing * (isLeft ? -1 : 1);
                float yOffset = (pairIndex + 1) * ySpacing;

                pos = center + new Vector3(xOffset, yOffset, 0f);
            }

            playerViews[i].transform.position = pos;
        }

        Debug.Log("[ResultBroadcaster] Players arranged in V shape.");
    }
}
