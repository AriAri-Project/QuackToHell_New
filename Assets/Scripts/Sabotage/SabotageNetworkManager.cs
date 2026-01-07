using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum SabotageType
{
    LightsOff,     
    ForcedInteract   
}

public class SabotageNetworkManager : NetworkBehaviour
{
    public static SabotageNetworkManager Instance;

    [Header("시야 연출 컨트롤러 (씬에 1개)")]
    public SabotageVisualController visualController;

    [Header("LightsOff 사보타지 유지 시간(초)")]
    public float sabotageDuration = 8f;

    [Header("ForcedInteract 제한 시간(초)")]
    public float forcedInteractLimit = 20f;

    private Coroutine forcedInteractRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 버튼에서 호출: 사보타지 시작 요청 (타입만 넘김)
    /// 로컬 -> 서버 -> 전체 브로드캐스트
    /// </summary>
    public void TryStartSabotage(SabotageType type)
    {
        if (IsServer)
        {
            StartSabotageServer(type);
        }
        else
        {
            RequestSabotageServerRpc(type);
        }
    }

    /// <summary>
    /// 플레이어 객체 기반으로 요청하고 싶을 때 사용 (소유권 검증 포함)
    /// </summary>
    public void TryStartSabotageFromPlayer(GameObject player, SabotageType type)
    {
        if (player == null) return;

        var no = player.GetComponent<NetworkObject>();
        if (no == null) return;

        if (IsServer)
            StartSabotageServer(type);
        else
            RequestSabotageFromPlayerServerRpc(no.NetworkObjectId, type);
    }

    // ---- Server RPCs ----

    [ServerRpc(RequireOwnership = false)]
    private void RequestSabotageServerRpc(SabotageType type, ServerRpcParams rpcParams = default)
    {
        StartSabotageServer(type);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSabotageFromPlayerServerRpc(ulong playerNetId, SabotageType type, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out var playerObj)) return;
        if (playerObj.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        StartSabotageServer(type);
    }

    // ---- Server logic ----

    private void StartSabotageServer(SabotageType type)
    {
        switch (type)
        {
            case SabotageType.LightsOff:
                TriggerSabotageClientRpc(type, sabotageDuration);
                break;

            case SabotageType.ForcedInteract:
                TriggerSabotageClientRpc(type, 0f);

                // 서버에서 20초 제한 타이머 시작(실패 시 몰살 로직을 여기)
                StartForcedInteractSabotageServer();
                break;
        }
    }

    private void StartForcedInteractSabotageServer()
    {
        if (!IsServer) return;

        if (forcedInteractRoutine != null)
            StopCoroutine(forcedInteractRoutine);

        forcedInteractRoutine = StartCoroutine(ForcedInteractRoutine());
    }

    private IEnumerator ForcedInteractRoutine()
    {
        float t = 0f;

        // TODO: 필요 상호작용 카운트/상태 초기화
        // 예: required = 3; current = 0;

        while (t < forcedInteractLimit)
        {
            // TODO: 아이템 상호작용 성공 조건 체크
            // if (current >= required)
            // {
            //     StopForcedInteractSuccess();
            //     yield break;
            // }

            t += Time.deltaTime;
            yield return null;
        }

        KillAllPlayersServer();

        forcedInteractRoutine = null;
    }

    private void KillAllPlayersServer()
    {
        // TODO: "전원 사망 처리" 함수로 연결
        Debug.Log("[Sabotage] ForcedInteract FAIL -> KILL ALL (TODO 구현 필요)");
    }

    // ---- Client RPC ----

    [ClientRpc]
    private void TriggerSabotageClientRpc(SabotageType type, float duration)
    {
        switch (type)
        {
            case SabotageType.LightsOff:
                if (visualController != null)
                    visualController.PlaySabotageOnce(type, duration);
                break;

            case SabotageType.ForcedInteract:
                break;
        }
    }
}
