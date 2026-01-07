using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VentController : NetworkBehaviour, IInteractable
{
    [Header("Identification")]
    [SerializeField] private string ventId;

    [Header("Vent Animations")]
    [SerializeField] private AnimationClip enterAnimation;
    [SerializeField] private AnimationClip exitAnimation;
    [SerializeField] private Animator animator;

    [Header("Interaction")]
    [Tooltip("권장: false. (Vent 오브젝트가 입력/클릭으로 스스로 토글하지 않게 함)")]
    [SerializeField] private bool enableSelfInput = false;

    [SerializeField] private bool enableSpacebar = true; // legacy (self input 켜면만 사용)
    [SerializeField, Range(0.5f, 5f)] private float interactionRadius = 1f;
    [SerializeField, Range(0f, 2f)] private float cooldownSec = 0.5f;
    [SerializeField] private Vector2 exitOffset = new(0f, 0.5f);

    [Header("Links (Max 4)")]
    [SerializeField] private List<VentController> linkedVents = new();

    [Header("Arrow Placement")]
    [SerializeField, Min(0.1f)] private float arrowDistanceFromSource = 1.1f;
    [SerializeField, Min(0.1f)] private float keepAwayFromTarget = 0.4f;
    [SerializeField, Range(-0.5f, 0.5f)] private float arrowNormalOffset = 0.0f;

    [Header("Player Handling While Inside (Optional Local Only)")]
    [SerializeField] private List<Behaviour> disableWhileInside = new();
    [SerializeField] private bool freezeRigidbody2D = true;

    // 서버 권한 상태
    private readonly NetworkVariable<bool> _occupied =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<ulong> _occupantNetId =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private double _lastExitServerTime;

    private Transform _tr;
    [SerializeField] private GameObject arrowPrefab;
    private readonly List<GameObject> _spawnedArrows = new();

    private static float _localClickSuppressUntil = 0f;

    private VentController _selectedTargetVent;

    void Awake()
    {
        _tr = transform;

        if (string.IsNullOrEmpty(ventId))
            ventId = System.Guid.NewGuid().ToString("N").Substring(0, 6);
        name = $"Vent_{ventId}";
    }

    // =========================
    // IInteractable (Player가 넘겨주는 player를 사용!)
    // =========================
    public bool CanInteract(GameObject player)
    {
        if (player == null) return false;

        if (!_occupied.Value) return true;

        var pNo = player.GetComponent<NetworkObject>();
        if (pNo == null) return false;
        bool iAmOccupant = (_occupantNetId.Value != 0UL) && (_occupantNetId.Value == pNo.NetworkObjectId);
        return iAmOccupant;

        /*
        // 비점유면 누구나 가능, 점유면 "본인 점유자"만 탈출 가능
        float dist = Vector3.Distance(player.transform.position, _tr.position);
        if (!_occupied.Value) return dist <= interactionRadius;

        // 점유 중이면 "내가 점유자"이고, 반경 내(원하면 반경 체크 제거 가능)
        var pNo = player.GetComponent<NetworkObject>();
        if (pNo == null) return false;
        bool iAmOccupant = (_occupantNetId.Value != 0UL) && (_occupantNetId.Value == pNo.NetworkObjectId);
        return iAmOccupant && dist <= interactionRadius;
        */
    }

    public void Interact(GameObject player)
    {
        // 이제 player를 무시하지 않음
        RequestToggleFromPlayer(player);
    }

    // =========================
    // Player가 호출하는 Public API
    // =========================
    public void RequestToggleFromPlayer(GameObject player)
    {
        if (!IsClient || player == null) return;
        if (!IsSpawned) { Debug.LogWarning($"[Vent/{name}] Not spawned yet"); return; }

        var pNo = player.GetComponent<NetworkObject>();
        if (pNo == null) { Debug.LogWarning($"[Vent/{name}] player has no NetworkObject"); return; }

        ToggleEnterExitServerRpc(pNo.NetworkObjectId);
    }

    public void RequestEnterFromPlayer(GameObject player)
    {
        if (!IsClient || player == null) return;

        // 이미 점유 중이면 enter 요청 무시
        if (_occupied.Value) return;

        RequestToggleFromPlayer(player);
    }

    public void RequestExitFromPlayer(GameObject player)
    {
        if (!IsClient || player == null) return;

        var pNo = player.GetComponent<NetworkObject>();
        if (pNo == null) return;

        bool iAmOccupant = (_occupantNetId.Value != 0UL) && (_occupantNetId.Value == pNo.NetworkObjectId);
        if (!_occupied.Value || !iAmOccupant) return;

        RequestToggleFromPlayer(player);
    }

    // =========================
    // Vent 자체 입력/클릭: 기본 OFF 권장
    // =========================
    void OnMouseUpAsButton()
    {
        if (!enableSelfInput) return;
        if (Time.time < _localClickSuppressUntil) return;

        var nm = NetworkManager.Singleton;
        var localPlayerObj = nm?.SpawnManager?.GetLocalPlayerObject();
        if (localPlayerObj == null) return;

        RequestToggleFromPlayer(localPlayerObj.gameObject);
    }

    void Update()
    {
        if (!enableSelfInput) return;
        if (!IsClient || !enableSpacebar) return;

        var nm = NetworkManager.Singleton;
        var localPlayerObj = nm?.SpawnManager?.GetLocalPlayerObject();
        if (localPlayerObj == null) return;

        // self-input도 player 기반으로만 동작하게 통일
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // "근처 or 점유자"만 토글
            if (CanInteract(localPlayerObj.gameObject))
                RequestToggleFromPlayer(localPlayerObj.gameObject);
        }
    }

    void LateUpdate()
    {
        if (!IsClient) return;

        var nm = NetworkManager.Singleton;
        var localPlayerObj = nm?.SpawnManager?.GetLocalPlayerObject();
        if (localPlayerObj == null) return;

        bool iAmOccupant = (_occupantNetId.Value != 0UL) &&
                           (_occupantNetId.Value == localPlayerObj.NetworkObjectId);

        if (_occupied.Value && iAmOccupant)
            localPlayerObj.transform.position = _tr.position;
    }

    // =========================
    // ServerRpc: "sender의 PlayerObject를 찾는 방식" 제거
    //     -> Player가 넘긴 playerNetId를 쓰되, sender가 소유자인지 검증
    // =========================
    [ServerRpc(RequireOwnership = false)]
    private void ToggleEnterExitServerRpc(ulong playerNetId, ServerRpcParams rpc = default)
    {
        var senderClientId = rpc.Receive.SenderClientId;

        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out var playerObj))
            return;

        // 보안: sender가 자기 플레이어만 조작하도록
        if (playerObj.OwnerClientId != senderClientId) return;

        if (!_occupied.Value)
        {
            if (NetworkManager.Singleton.ServerTime.Time - _lastExitServerTime < cooldownSec) return;
            // if (Vector3.Distance(playerObj.transform.position, _tr.position) > interactionRadius) return;

            _occupied.Value = true;
            _occupantNetId.Value = playerObj.NetworkObjectId;

            TeleportPlayerServer(playerObj, _tr.position);
            SetPlayerHiddenClientRpc(_occupantNetId.Value, true);

            SpawnArrowsClientRpc(NetworkObjectId, BuildTargetIds(), TargetClient(senderClientId));
            PlayEnterAnimation();
        }
        else
        {
            if (_occupantNetId.Value != playerObj.NetworkObjectId) return;

            _occupied.Value = false;
            _lastExitServerTime = NetworkManager.Singleton.ServerTime.Time;

            SetPlayerHiddenClientRpc(_occupantNetId.Value, false);
            DespawnArrowsClientRpc(TargetClient(senderClientId));

            playerObj.transform.position = _tr.position + (Vector3)exitOffset;
            PlayExitAnimation();
        }
    }

    // (기존) 벤트간 이동은 occupant & sender로 검증하므로 유지
    public void RequestMoveToSelected()
    {
        if (!IsClient || _selectedTargetVent == null) return;

        _localClickSuppressUntil = Mathf.Max(_localClickSuppressUntil, Time.time + 0.2f);
        MoveToVentServerRpc(_selectedTargetVent.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveToVentServerRpc(ulong targetVentNetId, ServerRpcParams rpc = default)
    {
        var sender = rpc.Receive.SenderClientId;

        var playerObj = NetworkManager.Singleton.ConnectedClients[sender].PlayerObject;
        if (playerObj == null)
        {
            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (netObj != null && netObj.OwnerClientId == sender && netObj.gameObject.CompareTag("Player"))
                {
                    playerObj = netObj;
                    break;
                }
            }
            if (playerObj == null) return;
        }

        if (_occupantNetId.Value != playerObj.NetworkObjectId) return;

        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetVentNetId, out var targetNetObj)) return;
        var targetVent = targetNetObj.GetComponent<VentController>();
        if (targetVent == null) return;

        _occupied.Value = false;
        targetVent._occupied.Value = true;
        targetVent._occupantNetId.Value = playerObj.NetworkObjectId;

        TeleportPlayerServer(playerObj, targetVent.transform.position);

        var onlySender = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } };
        DespawnArrowsClientRpc(onlySender);
        targetVent.SpawnArrowsClientRpc(targetVent.NetworkObjectId, targetVent.BuildTargetIds(), onlySender);
    }

    private void TeleportPlayerServer(NetworkObject playerObj, Vector3 pos)
    {
        playerObj.transform.position = pos;
    }

    [ClientRpc]
    private void SetPlayerHiddenClientRpc(ulong playerNetId, bool hidden)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject networkObject)) return;

        foreach (Renderer renderer in networkObject.GetComponentsInChildren<Renderer>(true)) renderer.enabled = !hidden;
        foreach (Collider2D collider in networkObject.GetComponentsInChildren<Collider2D>(true)) collider.enabled = !hidden;

        try
        {
            var ownerClientId = networkObject.OwnerClientId;
            var playerView = PlayerHelperManager.Instance?.GetPlayerViewlByClientId(ownerClientId);
            if (playerView != null) playerView.SetNicknameVisibility(!hidden);
        }
        catch { }
    }

    [ClientRpc]
    private void SpawnArrowsClientRpc(ulong ventNetId, ulong[] linkedVentIds, ClientRpcParams target = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ventNetId, out var no)) return;
        var vent = no.GetComponent<VentController>();
        if (!vent) return;

        vent.LocalSpawnArrowsByNetworkIds(linkedVentIds);
    }

    [ClientRpc]
    private void DespawnArrowsClientRpc(ClientRpcParams target = default)
    {
        LocalDespawnArrows();
    }

    private void LocalSpawnArrowsByNetworkIds(ulong[] linkedIds)
    {
        LocalDespawnArrows();
        if (arrowPrefab == null || linkedIds == null) return;

        foreach (var id in linkedIds)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var no)) continue;
            var target = no.GetComponent<VentController>();
            if (!target) continue;

            var go = Instantiate(arrowPrefab, transform.parent);
            var arrow = go.GetComponent<VentArrowController>();

            Vector3 a = _tr.position;
            Vector3 b = target.transform.position;
            Vector3 ab = b - a;
            float d = ab.magnitude;
            if (d < 0.001f) continue;

            Vector3 dir = ab / d;
            Vector3 perp = new(-dir.y, dir.x, 0f);

            float place = Mathf.Clamp(arrowDistanceFromSource, 0.05f, Mathf.Max(0.05f, d - keepAwayFromTarget));
            Vector3 pos = a + dir * place + perp * arrowNormalOffset;

            go.transform.position = pos;
            go.transform.rotation =
                Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward);

            arrow.Setup(this, target);
            _spawnedArrows.Add(go);
        }
    }

    private void LocalDespawnArrows()
    {
        for (int i = 0; i < _spawnedArrows.Count; i++)
            if (_spawnedArrows[i]) Destroy(_spawnedArrows[i]);
        _spawnedArrows.Clear();
    }

    public void SetSelectedLinkedVent(VentController target)
    {
        _selectedTargetVent = target;
    }

    private ulong[] BuildTargetIds()
    {
        var ids = new List<ulong>(linkedVents.Count);
        foreach (var v in linkedVents) if (v && v.NetworkObject) ids.Add(v.NetworkObjectId);
        return ids.ToArray();
    }

    private ClientRpcParams TargetClient(ulong clientId) => new ClientRpcParams
    {
        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
    };

    private void PlayEnterAnimation()
    {
        if (animator && enterAnimation) animator.Play(enterAnimation.name);
    }
    private void PlayExitAnimation()
    {
        if (animator && exitAnimation) animator.Play(exitAnimation.name);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (linkedVents.Count > 4) linkedVents.RemoveRange(4, linkedVents.Count - 4);
    }
#endif
}
