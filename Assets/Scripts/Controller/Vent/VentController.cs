using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private bool enableSpacebar = true;
    [SerializeField, Range(0.5f, 5f)] private float interactionRadius = 1.5f;
    public float InteractionRadius => interactionRadius;
    [SerializeField, Range(0f, 2f)] private float cooldownSec = 0.5f;
    [SerializeField] private Vector2 exitOffset = new(0f, 0.5f);

    [Header("Links (Max 4)")]
    [SerializeField] private List<VentController> linkedVents = new();

    [Header("Arrow Placement")]
    [SerializeField, Min(0.1f)] private float arrowDistanceFromSource = 1.1f; // A에서 떨어질 절대 거리
    [SerializeField, Min(0.1f)] private float keepAwayFromTarget = 0.4f;      // B로부터 최소 이격
    [SerializeField, Range(-0.5f, 0.5f)] private float arrowNormalOffset = 0.0f;

    [Header("Player Handling While Inside (Optional Local Only)")]
    [Tooltip("플레이어 이동/입력 스크립트 등 로컬에서만 비활성화하려면 VentInvisibility 대신 여기에 넣어도 됨")]
    [SerializeField] private List<Behaviour> disableWhileInside = new();
    [SerializeField] private bool freezeRigidbody2D = true;

    // 서버 권한 상태 
    private readonly NetworkVariable<bool> _occupied =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<ulong> _occupantNetId =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private double _lastExitServerTime;

    // 로컬 캐시/화살표
    private Transform _tr;
    [SerializeField] private GameObject arrowPrefab; // 반드시 연결
    private readonly List<GameObject> _spawnedArrows = new();

    // 이동 직후 벤트 클릭을 잠깐 무시하기 위한 로컬 억제 타이머
    private static float _localClickSuppressUntil = 0f;
    
    //스페이스바 입력 상태
    private bool _spaceInput =false;

    public bool SpaceInput
    {
        get { return _spaceInput;}
        set { _spaceInput = value; }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }


    void Awake()
    {
        _tr = transform;

        // 넘버링
        if (string.IsNullOrEmpty(ventId))
            ventId = System.Guid.NewGuid().ToString("N").Substring(0, 6);
        name = $"Vent_{ventId}";
    }

    public bool CanInteract(GameObject player)
    {
        if (_occupied.Value || player == null) return false;
        float dist = Vector3.Distance(player.transform.position, _tr.position);
        return dist <= interactionRadius;
    }

    public void Interact(GameObject player)
    {
        RequestToggleEnterExit();
    }

    // Vent 본체 클릭 → 탑승/탈출 토글 요청 (키 입력 제거)
    void OnMouseUpAsButton()
    {
        if (Time.time < _localClickSuppressUntil) return;

        NetworkManager nm = NetworkManager.Singleton;

        GameObject localPlayerObj = null;
        // note cba0898: 싱글톤은 변수에 저장하지 말고 직접 호출하는 것이 좋습니다. 그것이 싱글톤이니까...
        // NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() 처럼 바로 사용하시는 것을 추천드립니다.
        // 그리고 싱글톤인데 NotNull 체크는 어색합니다. 없을 수 있다면(파괴되었을 수 있다면) ?. 혹은 if문으로 처리하시는 것을 추천드립니다.
        if (DebugUtils.EnsureNotNull(nm, "NetworkManager", this) && 
            DebugUtils.EnsureNotNull(nm.SpawnManager, "SpawnManager", this))
        {
            localPlayerObj = nm.SpawnManager.GetLocalPlayerObject().gameObject;
        }
        
        if (localPlayerObj == null)
        {
            // Fallback: 내 소유 Player 찾기
            if (nm != null)
            {
                foreach (NetworkObject networkObject in nm.SpawnManager.SpawnedObjectsList)
                {
                    if (networkObject.OwnerClientId == nm.LocalClientId && networkObject.CompareTag("Player"))
                    {
                        localPlayerObj = networkObject.gameObject;
                        break;
                    }
                }
            }
        }
        if (localPlayerObj == null) return;

        bool iAmOccupant = (_occupantNetId.Value != 0UL) &&
                           (_occupantNetId.Value == localPlayerObj.GetComponent<NetworkObject>().NetworkObjectId);

        if (!_occupied.Value)
        {
            // 비점유 상태 → 진입 시도
            RequestToggleEnterExit();
        }
        else if (iAmOccupant)
        {
            // 점유 상태 & 내가 탄 상태 → 탈출 시도
            RequestToggleEnterExit();
        }
    }
    void Update()
    {
        if (!IsClient || !enableSpacebar) return;

        // 네트워크 준비 전엔 아무 것도 안 함
        NetworkManager nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || !nm.IsClient) return;

        // 로컬 플레이어 오브젝트 안전하게 얻기
        NetworkObject localPlayerObj = nm.SpawnManager?.GetLocalPlayerObject();
        if (localPlayerObj == null)
        {
            foreach (NetworkObject no in nm.SpawnManager.SpawnedObjectsList)
            {
                if (no.OwnerClientId == nm.LocalClientId && no.CompareTag("Player"))
                            {
                    localPlayerObj = no;
                                break;
                            }
            }
            if (localPlayerObj == null) return;
        }

        bool iAmOccupant = (_occupantNetId.Value != 0UL) &&
                           (_occupantNetId.Value == localPlayerObj.GetComponent<NetworkObject>().NetworkObjectId);

        if (!_occupied.Value)
        {
            float dist = Vector3.Distance(localPlayerObj.transform.position, _tr.position);
            if (dist <= interactionRadius &&  _spaceInput)
            {
                RequestToggleEnterExit();
                _spaceInput = false;
            }
        }
        else
        {
            if (iAmOccupant &&  _spaceInput)
            {
                RequestToggleEnterExit();
                _spaceInput = false;
            }
        }
    }

    void LateUpdate()
    {
        if (!IsClient) return;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;
        
        GameObject localPlayerObj = PlayerHelperManager.Instance?.GetPlayerGameObjectByClientId(nm.LocalClientId);

        if (localPlayerObj == null)
        {
            if (nm != null)
            {
                foreach (NetworkObject networkObject in nm.SpawnManager.SpawnedObjectsList)
                {
                    if (networkObject.OwnerClientId == nm.LocalClientId && networkObject.CompareTag("Player"))
                    {
                        localPlayerObj = networkObject.gameObject;
                        break;
                    }
                }
            }
        }
        if (localPlayerObj == null) return;

        bool iAmOccupant = (_occupantNetId.Value != 0UL) &&
                           (_occupantNetId.Value == localPlayerObj.GetComponent<NetworkObject>().NetworkObjectId);

        if (_occupied.Value && iAmOccupant)
        {
            localPlayerObj.transform.position = _tr.position;
        }
    }



    public void RequestToggleEnterExit()
    {
        Debug.Log("벤트탔음!");
        if (!IsClient) return;
        if (!IsSpawned) { Debug.LogWarning($"[Vent/{name}] Not spawned yet"); return; }
        //디버깅깅
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[RequestToggleEnterExit] LocalClientId: {localClientId}");
        
        ToggleEnterExitServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleEnterExitServerRpc(ServerRpcParams rpc = default)
    {
        ulong sender = rpc.Receive.SenderClientId;
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[sender].PlayerObject;
        if (playerObj == null)
        {
            foreach (NetworkObject no in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (no != null && no.OwnerClientId == sender && no.gameObject.CompareTag("Player"))
                            {
                    playerObj = no;
                                break;
                            }
                    }
                if (playerObj == null)
                    {
                Debug.LogWarning($"[Server] No PlayerObject for client {sender}");
                        return;
            }
        }

        if (!_occupied.Value)
        {
            if (NetworkManager.Singleton.ServerTime.Time - _lastExitServerTime < cooldownSec) return;
            if (Vector3.Distance(playerObj.transform.position, _tr.position) > interactionRadius) return;

            _occupied.Value = true;
            _occupantNetId.Value = playerObj.NetworkObjectId;

            TeleportPlayerServer(playerObj, _tr.position);
            SetPlayerHiddenClientRpc(_occupantNetId.Value, true);

            // 탑승자 본인에게만 화살표 표시
            SpawnArrowsClientRpc(NetworkObjectId, BuildTargetIds(), TargetClient(sender));

            PlayEnterAnimation();

            
        }
        else
        {
            // 점유자만 탈출 가능
            if (_occupantNetId.Value != playerObj.NetworkObjectId) return;

            _occupied.Value = false;
            _lastExitServerTime = NetworkManager.Singleton.ServerTime.Time;

            SetPlayerHiddenClientRpc(_occupantNetId.Value, false);
            DespawnArrowsClientRpc(TargetClient(sender));

            // 탈출 오프셋 위치로
            playerObj.transform.position = _tr.position + (Vector3)exitOffset;

            PlayExitAnimation();
            PlayerModel playerModel = playerObj.GetComponent<PlayerModel>();
            if (playerModel != null)
            {
                PlayerStateData newStateData = playerModel.PlayerStateData.Value;
                newStateData.animationState = PlayerAnimationState.Idle;
                playerModel.PlayerStateData.Value = newStateData;
            }
        }
    }

    public void RequestMoveTo(VentController target)
    {
        if (!IsClient || target == null) return;
        _localClickSuppressUntil = Mathf.Max(_localClickSuppressUntil, Time.time + 0.2f);

        MoveToVentServerRpc(target.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveToVentServerRpc(ulong targetVentNetId, ServerRpcParams rpc = default)
    {
        ulong sender = rpc.Receive.SenderClientId;

        // 1) PlayerObject 확보 (fallback 포함)
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[sender].PlayerObject;
        if (playerObj == null)
        {
            foreach (NetworkObject netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (netObj != null && netObj.OwnerClientId == sender && netObj.gameObject.CompareTag("Player"))
                {
                    playerObj = netObj;
                    break;
                }
            }
            if (playerObj == null)
            {
                Debug.LogWarning($"[Server] MoveTo: No PlayerObject for client {sender}");
                return;
            }
        }

        // 2) 현재 벤트의 점유자만 이동 가능
        if (_occupantNetId.Value != playerObj.NetworkObjectId)
        {
            return;
        }

        // 3) 타겟 벤트 조회 (out 변수도 다른 이름 사용)
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetVentNetId, out NetworkObject targetNetObj)) return;
        VentController targetVent = targetNetObj.GetComponent<VentController>();
        if (targetVent == null) return;

        // 4) 점유 상태 A→B 이전
        _occupied.Value = false;
        targetVent._occupied.Value = true;
        targetVent._occupantNetId.Value = playerObj.NetworkObjectId;

        // 5) 플레이어 위치를 B로 이동
        TeleportPlayerServer(playerObj, targetVent.transform.position);

        // 6) 탑승자 본인에게만: A의 화살표 제거 → B의 화살표 생성
        ClientRpcParams onlySender = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } };
        DespawnArrowsClientRpc(onlySender);
        targetVent.SpawnArrowsClientRpc(targetVent.NetworkObjectId, targetVent.BuildTargetIds(), onlySender);

    }

    // 서버 권위 이동 (NetworkTransform 사용 권장)
    private void TeleportPlayerServer(NetworkObject playerObj, Vector3 pos)
    {
        playerObj.transform.position = pos;
    }

    // 가시성 브로드캐스트
    [ClientRpc]
    private void SetPlayerHiddenClientRpc(ulong playerNetId, bool hidden)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject networkObject)) return;

        foreach (Renderer renderer in networkObject.GetComponentsInChildren<Renderer>(true)) renderer.enabled = !hidden;
        foreach (Collider2D collider in networkObject.GetComponentsInChildren<Collider2D>(true)) collider.enabled = !hidden;

        try
        {
            var ownerClientId = networkObject.OwnerClientId;
            var presenter = PlayerHelperManager.Instance?.GetPlayerPresenterByClientId(ownerClientId);
            if (presenter != null)
            {
                // hidden = true(벤트 안) → 닉네임 끔(false)
                // hidden = false(벤트 밖) → 닉네임 켬(true)
                presenter.OnOffNickname(!hidden);
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"[Vent] PlayerPresenter not found for client {ownerClientId}");
            }
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Vent] OnOffNickname failed: {e.Message}");
        }
    }

    // 화살표
    [ClientRpc]
    private void SpawnArrowsClientRpc(ulong ventNetId, ulong[] linkedVentIds, ClientRpcParams target = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ventNetId, out NetworkObject networkObject)) return;
        VentController vent = networkObject.GetComponent<VentController>();
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

        foreach (ulong linkedVentId in linkedIds)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(linkedVentId, out NetworkObject linkedNetworkObject)) continue;
            VentController targetVent = linkedNetworkObject.GetComponent<VentController>();
            if (!targetVent) continue;

            GameObject arrowGameObject = Instantiate(arrowPrefab, transform.parent);
            VentArrowController arrowController = arrowGameObject.GetComponent<VentArrowController>();

            Vector3 sourcePosition = _tr.position;
            Vector3 targetPosition = targetVent.transform.position;
            Vector3 directionVector = targetPosition - sourcePosition;
            float distance = directionVector.magnitude;
            if (distance < 0.001f) continue;

            Vector3 direction = directionVector / distance;
            Vector3 perpendicular = new(-direction.y, direction.x, 0f);

            float placementDistance = Mathf.Clamp(arrowDistanceFromSource, 0.05f, Mathf.Max(0.05f, distance - keepAwayFromTarget));
            Vector3 arrowPosition = sourcePosition + direction * placementDistance + perpendicular * arrowNormalOffset;

            arrowGameObject.transform.position = arrowPosition;
            arrowGameObject.transform.rotation =
                Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward);

            arrowController.Setup(this, targetVent);
            _spawnedArrows.Add(arrowGameObject);
        }
    }

    private void LocalDespawnArrows()
    {
        for (int i = 0; i < _spawnedArrows.Count; i++)
            if (_spawnedArrows[i]) Destroy(_spawnedArrows[i]);
        _spawnedArrows.Clear();
    }

    private ulong[] BuildTargetIds()
    {
        List<ulong> ids = new List<ulong>(linkedVents.Count);
        foreach (VentController v in linkedVents) if (v && v.NetworkObject) ids.Add(v.NetworkObjectId);
        return ids.ToArray();
    }

    private ClientRpcParams TargetClient(ulong clientId) => new ClientRpcParams
    {
        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
    };

    // 애니메이션
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"Vent_{ventId}");
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        foreach (VentController v in linkedVents)
        {
            if (!v) continue;
            Gizmos.DrawLine(transform.position, v.transform.position);
        }
    }
#endif
}
