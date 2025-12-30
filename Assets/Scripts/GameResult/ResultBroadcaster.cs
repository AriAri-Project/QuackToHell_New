using Unity.Netcode;
using UnityEngine;

public class ResultBroadcaster : NetworkBehaviour
{
    [SerializeField] private ResultScreenUI resultScreenUI;

    private void Awake()
    {
        // UI는 씬에 존재한다고 가정 (ResultPanel 같은 것)
        if (resultScreenUI == null)
            resultScreenUI = FindFirstObjectByType<ResultScreenUI>(FindObjectsInactive.Include);
    }

    public override void OnNetworkSpawn()
    {
        // 클라에서만 UI를 띄우면 되니까, 여기서는 특별히 할 거 없음.
        // (필요하면 여기서 다시 UI 찾기)
        if (IsClient && resultScreenUI == null)
            resultScreenUI = FindFirstObjectByType<ResultScreenUI>(FindObjectsInactive.Include);
    }

    /// <summary>
    /// 서버에서 게임 종료 확정 시 호출
    /// </summary>
    public void EndGameAndShowResult(GameResultPayload payload)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ResultBroadcaster] EndGameAndShowResult called on client!");
            return;
        }

        ShowResultClientRpc(payload);
    }

    [ClientRpc]
    private void ShowResultClientRpc(GameResultPayload payload)
    {
        if (resultScreenUI == null)
            resultScreenUI = FindFirstObjectByType<ResultScreenUI>(FindObjectsInactive.Include);

        if (resultScreenUI != null)
            resultScreenUI.Open(payload);
        else
            Debug.LogError("[ResultBroadcaster] ResultScreenUI not found in scene!");
    }
}
