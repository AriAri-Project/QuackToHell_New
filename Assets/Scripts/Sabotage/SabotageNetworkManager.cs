using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public enum SabotageType
{
    LightsOff,        // 기존: 불 꺼지고 어두워짐
    ForcedInteract    // 신규: 타깃 아이템 상호작용 필요 (불 끄기 연출 X)
}

public class SabotageNetworkManager : NetworkBehaviour
{
    public static SabotageNetworkManager Instance;

    [Header("시야/메시지 연출 컨트롤러 (씬에 1개)")]
    public SabotageVisualController visualController;

    [Header("LightsOff 사보타지 유지 시간(초)")]
    public float sabotageDuration = 8f;

    [Header("ForcedInteract 제한 시간(초)")]
    public float forcedInteractLimit = 10f;

    [Header("ForcedInteract 타깃 아이템 (NetworkObject)")]
    public NetworkObject forcedTargetItem;

    // 서버 상태
    private bool forcedActive = false;
    private ulong forcedTargetItemId = 0;
    private Coroutine forcedRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // =========================
    //  Start Sabotage (Public)
    // =========================

    public void TryStartSabotage(SabotageType type)
    {
        if (IsServer) StartSabotageServer(type);
        else RequestSabotageServerRpc(type);
    }

    public void TryStartSabotageFromPlayer(GameObject player, SabotageType type)
    {
        if (player == null) return;
        var no = player.GetComponent<NetworkObject>();
        if (no == null) return;

        if (IsServer) StartSabotageServer(type);
        else RequestSabotageFromPlayerServerRpc(no.NetworkObjectId, type);
    }

    public bool IsForcedInteractActiveClient()
    {
        return forcedActive;
    }

    // =========================
    //  ForcedInteract Resolve (Public)
    // =========================

    /// <summary>
    /// 타깃 아이템에서 상호작용 시 호출 (로컬->서버)
    /// itemObject는 상호작용한 "그 아이템" GameObject
    /// </summary>
    public void TryResolveForcedInteract(GameObject player, GameObject itemObject)
    {
        if (!forcedActive) return; // (클라에선 로컬 상태라 100% 일치하진 않지만, 불필요 호출 방지)

        if (player == null || itemObject == null) return;

        var playerNO = player.GetComponent<NetworkObject>();
        var itemNO = itemObject.GetComponent<NetworkObject>();
        if (playerNO == null || itemNO == null) return;

        if (IsServer)
        {
            ResolveForcedInteractServer(playerNO.NetworkObjectId, itemNO.NetworkObjectId, default);
        }
        else
        {
            RequestResolveForcedInteractServerRpc(playerNO.NetworkObjectId, itemNO.NetworkObjectId);
        }
    }

    // =========================
    //  Server RPCs
    // =========================

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

    [ServerRpc(RequireOwnership = false)]
    private void RequestResolveForcedInteractServerRpc(
        ulong playerNetId,
        ulong itemNetId,
        ServerRpcParams rpcParams = default)
    {
        ResolveForcedInteractServer(playerNetId, itemNetId, rpcParams);
    }

    // =========================
    //  Server Logic
    // =========================

    private void StartSabotageServer(SabotageType type)
    {
        switch (type)
        {
            case SabotageType.LightsOff:
                TriggerSabotageClientRpc(type, sabotageDuration);
                break;

            case SabotageType.ForcedInteract:
                StartForcedInteractSabotageServer();
                break;
        }
    }

    private void StartForcedInteractSabotageServer()
    {
        if (!IsServer) return;

        if (forcedTargetItem == null)
        {
            Debug.LogWarning("[Sabotage] ForcedInteract target item is null. Assign forcedTargetItem in inspector.");
            return;
        }

        forcedTargetItemId = forcedTargetItem.NetworkObjectId;
        forcedActive = true;

        TriggerForcedInteractStateClientRpc(true);

        if (forcedRoutine != null) StopCoroutine(forcedRoutine);
        forcedRoutine = StartCoroutine(ForcedInteractRoutine());
    }

    private void TryAllKillServer()
    {
        if (!IsServer) return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Sabotage] GameManager.Instance is null. AllKillServer not called.");
            return;
        }

        GameManager.Instance.AllKillServer();
    }

    private IEnumerator ForcedInteractRoutine()
    {
        float t = 0f;

        while (t < forcedInteractLimit)
        {
            if (!forcedActive) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        forcedActive = false;
        TriggerForcedInteractStateClientRpc(false);

        ShowMessageAllClientRpc("실패했습니다");

        TryAllKillServer();

        forcedRoutine = null;
    }

    private void ResolveForcedInteractServer(
        ulong playerNetId,
        ulong itemNetId,
        ServerRpcParams rpcParams)
    {
        if (!IsServer) return;
        if (!forcedActive) return;

        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out var playerObj)) return;
        if (rpcParams.Receive.SenderClientId != 0) // host(0)일 수도 있어서, rpcParams가 default일 경우 대비
        {
            if (rpcParams.Receive.SenderClientId != default && playerObj.OwnerClientId != rpcParams.Receive.SenderClientId)
                return;
        }

        if (itemNetId != forcedTargetItemId) return;

        forcedActive = false;
        TriggerForcedInteractStateClientRpc(false);

        if (forcedRoutine != null)
        {
            StopCoroutine(forcedRoutine);
            forcedRoutine = null;
        }

        // 성공 안내(원하면 다른 문구로)
        ShowMessageAllClientRpc("사보타지 해제!");
    }

    // =========================
    //  Client RPCs
    // =========================

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

    [ClientRpc]
    private void TriggerForcedInteractStateClientRpc(bool active)
    {
        forcedActive = active;

        if (active && visualController != null)
        {
            visualController.ShowCenterMessage
                ($"긴급행동 : {forcedInteractLimit}초 안에 침실에 놓인 꽃병과 상호작용하세요!", 4f);
        }
    }

    [ClientRpc]
    private void ShowMessageAllClientRpc(string msg)
    {
        if (visualController != null)
        {
            visualController.ShowCenterMessage(msg, 2.0f);
        }
        else
        {
            Debug.Log($"[Sabotage Message] {msg}");
        }
    }
    
    //초기화 함수
    public void Initialize()
    {
        forcedActive = false;
        if (forcedRoutine != null)
        {
            StopCoroutine(forcedRoutine);
            forcedRoutine = null;
        }
    }
}
