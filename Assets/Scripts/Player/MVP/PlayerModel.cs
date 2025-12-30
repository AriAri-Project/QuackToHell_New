// PlayerModel.cs
using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// *MVP를 잘 몰랐을 떄 썼으므로 아래로 의미를 재정의함
/// 플레이어에 활용된 MVP의 의미는 기존 UI용 MVP와 조금 다름:
/// <PlayerModel>           <PlayerPresenter>       <PlayerView>
/// [입력들온거로로직처리] <-   [Model/적절한 곳에 전달]   <-    [TriggerEnter, 키입력 감지]
/// [데이터변화]          ->   [View/적절한 곳에 전달]    ->    [변화한 데이터를 외형에 반영]
/// </summary>
public class PlayerModel : NetworkBehaviour
{

    [Header("Network Synchronized Data")]
    private NetworkVariable<FixedString64Bytes> _playerTag = new NetworkVariable<FixedString64Bytes>(
        GameTags.Player, // 기본값
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> PlayerTag => _playerTag;

    private PlayerIdleState idleStateComponent;
    private PlayerWalkState walkStateComponent;
    private PlayerDeadState deadStateComponent;
    private PlayerAliveState aliveStateComponent;
    private PlayerVentEnterState ventEnterStateComponent;
    
    private RoleController _roleController;

    private ulong clientId;

    public ulong ClientId
    {
        get { return clientId; }
        set
        {
            if (!IsHost) return;
            clientId = value;
        }
    }
    

    private void Start()
    {
        // 미리 부착된 컴포넌트들 참조
        _roleController = GetComponent<RoleController>();
        idleStateComponent = GetComponent<PlayerIdleState>();
        walkStateComponent = GetComponent<PlayerWalkState>();
        aliveStateComponent = GetComponent<PlayerAliveState>();
        deadStateComponent = GetComponent<PlayerDeadState>();
        ventEnterStateComponent = GetComponent<PlayerVentEnterState>();

        // 초기 상태 설정
        SetAnimationStateByEnum(PlayerStateData.Value.AnimationState);
        SetAliveStateByEnum(PlayerStateData.Value.AliveState);
        ApplyAliveStateChange();
        ApplyAnimationStateChange();

        PlayerStateData.OnValueChanged += OnPlayerStateDataChanged;
        
        // note cba0898: MVP 규칙 위반 - PlayerPresenter에서 PlayerModel을 참조 할 것
        /*
        // 상태 변경 이벤트 등록 - 개별 처리
        PlayerStateData.OnValueChanged += (oldValue, newValue) =>
        {
           
            // AliveState가 변경된 경우만 처리
            if (oldValue.AliveState != newValue.AliveState)
            {
                SetAliveStateByEnum(newValue.AliveState);
                ApplyAliveStateChange();
                if (newValue.AliveState == PlayerLivingState.Dead)
                {
                    GetComponent<PlayerPresenter>().UpdateVisibilityForAllPlayers();
                }
            }

            // AnimationState가 변경된 경우만 처리
            if (oldValue.AnimationState != newValue.AnimationState)
            {
                SetAnimationStateByEnum(newValue.AnimationState);
                ApplyAnimationStateChange();
            }
        };
        
        // note cba0898: MVP 규칙 위반 - CardInventoryPresenter에서 PlayerModel을 참조 할 
        PlayerStatusData.OnValueChanged += (oldValue, newValue) =>
        {
            //골드 띄우기
            CardInventoryView cardInventoryView = FindAnyObjectByType<CardInventoryView>();
            if (cardInventoryView)
            {
                cardInventoryView.UpdatePlayerGold(_playerStatusData.Value.gold);    
            }
            
        };
        */
    }

    private void OnDestroy()
    {
        // 이벤트 구독 취소
        if (PlayerStateData != null)
        {
            PlayerStateData.OnValueChanged -= OnPlayerStateDataChanged;
        }
    }
    
    private void Update()
    {
        if (curAliveState != null)
        {
            curAliveState.OnStateUpdate();
        }
        if (curAnimationState != null)
        {
            curAnimationState.OnStateUpdate();
        }

    }

    private void FixedUpdate()
    {
        Vector2 moveAmount = direction.Value * _playerStatusData.Value.MoveSpeed*Time.fixedDeltaTime;
        transform.Translate(moveAmount.x, moveAmount.y, 0);
    }

    /// <summary>
    /// 애니메이션 상태 변경 ServerRpc
    /// </summary>
    [ServerRpc]
    public void SetAnimationStateServerRpc(PlayerAnimationState animState)
    {
        PlayerStateData newStateData = PlayerStateData.Value;
        newStateData.animationState = animState;
        PlayerStateData.Value = newStateData;
    }

    #region 플레이어 움직임
    private NetworkVariable<Vector2> direction = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    [Rpc(SendTo.Server)]
    public void MovePlayerServerRpc(int inputXDirection, int inputYDirection)
    {
        direction.Value = new Vector2(inputXDirection, inputYDirection).normalized;

        if (inputXDirection != 0 || inputYDirection != 0)
        {
            PlayerStateData newStateData = PlayerStateData.Value;
            newStateData.animationState = PlayerAnimationState.Walk;
            PlayerStateData.Value = newStateData;
        }
        else
        {
            PlayerStateData newStateData = PlayerStateData.Value;
            newStateData.animationState = PlayerAnimationState.Idle;
            PlayerStateData.Value = newStateData;
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void StopMovementServerRpc()
    {
        direction.Value = Vector2.zero;
    
        // 애니메이션 상태도 Idle로 변경
        PlayerStateData newStateData = PlayerStateData.Value;
        newStateData.animationState = PlayerAnimationState.Idle;
        PlayerStateData.Value = newStateData;
    }
    #endregion


    #region 플레이어 데이터
    [SerializeField]
    [Header("*플레이어 상태 데이터 = 서버만 write 가능*")]
    private NetworkVariable<PlayerStatusData> _playerStatusData = new NetworkVariable<PlayerStatusData>(
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<PlayerStatusData> PlayerStatusData
    {
        get { return _playerStatusData; }
        set { _playerStatusData = value; }
    }

    //플레이어 외형 데이터
    private NetworkVariable<PlayerAppearanceData> _playerAppearanceData = new NetworkVariable<PlayerAppearanceData>(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerAppearanceData> PlayerAppearanceData
    {
        get { return _playerAppearanceData; }
        set { _playerAppearanceData = value; }
    }


    //상태에 따라 행동
    //상태 주입: State를 상속받은 클래스의 인스턴스를 주입받음
    private NetworkVariable<PlayerStateData> _playerStateData { get; set; } = new NetworkVariable<PlayerStateData>(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerStateData> PlayerStateData
    {
        get { return _playerStateData; }
        set
        {
            _playerStateData = value;
        }
    }
    #endregion

    #region 플레이어 상태

    private NetworkStateBase preAliveState;
    private NetworkStateBase tempAliveState;
    private NetworkStateBase curAliveState;
    private NetworkStateBase preAnimationState;
    private NetworkStateBase tempAnimationState;
    private NetworkStateBase curAnimationState;

    // AliveState만 처리하는 메서드
    private void SetAliveStateByEnum(PlayerLivingState aliveState)
    {
        switch (aliveState)
        {
            case PlayerLivingState.Alive:
                SetAliveState(aliveStateComponent);
                break;
            case PlayerLivingState.Dead:
                SetAliveState(deadStateComponent);
                break;
        }
    }

    // AnimationState만 처리하는 메서드
    private void SetAnimationStateByEnum(PlayerAnimationState animationState)
    {
        switch (animationState)
        {
            case PlayerAnimationState.Idle:
                SetAnimationState(idleStateComponent);
                break;
            case PlayerAnimationState.Walk:
                SetAnimationState(walkStateComponent);
                break;
            case PlayerAnimationState.VentEnter:
                SetAnimationState(ventEnterStateComponent);
                break;
        }
    }

    private void SetAliveState(NetworkStateBase state)
    {
        tempAliveState = curAliveState;
        curAliveState = state;
        preAliveState = tempAliveState;

        // 이전 상태 비활성화
        if (preAliveState != null)
        {
            preAliveState.enabled = false;
        }

        // 현재 상태 활성화
        if (curAliveState != null)
        {
            curAliveState.enabled = true;
        }
    }

    private void SetAnimationState(NetworkStateBase state)
    {
        tempAnimationState = curAnimationState;
        curAnimationState = state;
        preAnimationState = tempAnimationState;

        // 이전 상태 비활성화
        if (preAnimationState != null)
        {
            preAnimationState.enabled = false;
        }

        // 현재 상태 활성화
        if (curAnimationState != null)
        {
            curAnimationState.enabled = true;
        }
    }


    // AliveState 변경 적용
    private void ApplyAliveStateChange()
    {
        // 이전 AliveState의 Exit 호출
        if (preAliveState != null)
        {
            preAliveState.OnStateExit();
        }

        // 현재 AliveState의 Enter 호출
        if (curAliveState != null)
        {
            Debug.Log($"상태바뀜: {curAliveState.GetType().Name}");
            curAliveState.OnStateEnter();
        }
    }

    // AnimationState 변경 적용
    private void ApplyAnimationStateChange()
    {
        // 이전 AnimationState의 Exit 호출
        if (preAnimationState != null)
        {
            preAnimationState.OnStateExit();
        }

        // 현재 AnimationState의 Enter 호출
        if (curAnimationState != null)
        {
            curAnimationState.OnStateEnter();
        }
    }
    #endregion

    #region 색깔 변경

    [ServerRpc]
    public void ChangeColorServerRpc(Int32 colorIndex, ulong clientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized color change attempt. Requested: {clientId}, Actual: {requesterClientId}");
            return;
        }
        
        PlayerAppearanceData tempAppearanceData = _playerAppearanceData.Value;
        tempAppearanceData.ColorIndex = colorIndex;
        _playerAppearanceData.Value = tempAppearanceData;
    }
    #endregion

    #region Die
    /// <summary>
    /// 플레이어 사망 처리 (서버에서만 호출되어야 함)
    /// 클라이언트는 이 메서드를 직접 호출할 수 없음
    /// </summary>
    [ServerRpc]
    public void DieServerRpc(ServerRpcParams rpcParams = default)
    {
        // 서버에서만 상태 변경 (권위적 정보)
        PlayerStateData newStateData = PlayerStateData.Value;
        newStateData.AliveState = PlayerLivingState.Dead;
        PlayerStateData.Value = newStateData;
    }
    #endregion

    #region 역할 변경
    public void ChangeRole(PlayerJob newRole){
        PlayerStatusData newStatusData = PlayerStatusData.Value;
        newStatusData.job = newRole;
        PlayerStatusData.Value = newStatusData;
    }
    #endregion

    /// <summary>
    /// 플레이어 상태 데이터 변경 시 호출되는 메서드
    /// </summary>
    private void OnPlayerStateDataChanged(PlayerStateData oldValue, PlayerStateData newValue)
    {
        // AliveState가 변경된 경우만 처리
        if (oldValue.AliveState != newValue.AliveState)
        {
            SetAliveStateByEnum(newValue.AliveState);
            ApplyAliveStateChange();
            
            // note cba0898: MVP 규칙 위반 - PlayerPresenter에서 이 변경사항을 감지하도록 수정 필요
            // if (newValue.AliveState == PlayerLivingState.Dead)
            // {
            //     GetComponent<PlayerPresenter>().UpdateVisibilityForAllPlayers();
            // }
        }

        // AnimationState가 변경된 경우만 처리
        if (oldValue.AnimationState != newValue.AnimationState)
        {
            SetAnimationStateByEnum(newValue.AnimationState);
            ApplyAnimationStateChange();
        }
    }
    
    //-----------public---------
    [ServerRpc]
    public void SetGoldServerRpc(int gold)
    {
        PlayerStatusData temp =  _playerStatusData.Value;
        temp.gold = gold;
        PlayerStatusData.Value = temp;
    }
    public int GetGold()
    {
        return PlayerStatusData.Value.gold;
    }
    
    /// <summary>
    /// 플레이어 생존 상태 조회
    /// </summary>
    public PlayerLivingState GetPlayerAliveState()
    {
        return PlayerStateData.Value.AliveState;
    }
    
    /// <summary>
    /// 플레이어 닉네임 조회
    /// </summary>
    public string GetPlayerNickname()
    {
        return PlayerStatusData.Value.Nickname;
    }

    public int GetPlayerColorIndex()
    {
        return  PlayerAppearanceData.Value.ColorIndex;
    }
    
    /// <summary>
    /// 플레이어 역할 조회
    /// </summary>
    public PlayerJob GetPlayerJob()
    {
        return PlayerStatusData.Value.job;
    }
    
    public void ToggleReady(){
        if(!IsOwner) return;
       
        ToggleReadyServerRpc();
    }
    
    [ServerRpc]
    private void ToggleReadyServerRpc()
    {
        PlayerStatusData statusDataCopy = GetPlayerStatusData();
        statusDataCopy.IsReady = !statusDataCopy.IsReady;
        PlayerStatusData.Value = statusDataCopy;
    }
    
        


    public bool IsReady(){
        return PlayerStatusData.Value.IsReady;
    }

    public PlayerStatusData GetPlayerStatusData()
    {
        return PlayerStatusData.Value;
    }

    public void SubscribeToPlayerReadyStatusChanges(NetworkVariable<PlayerStatusData>.OnValueChangedDelegate handler){
        PlayerStatusData.OnValueChanged += handler;
        Debug.Log($"바인딩된 플레이어의 id는 {NetworkManager.Singleton.LocalClientId}");
        Debug.Log($"바인딩된 함수는 {handler.Method.Name}, 타겟 = {handler.Target}");
    }

    /// <summary>
    /// 플레이어 사망 처리 (죽은 플레이어의 presenter에서 실행)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HandlePlayerDeathServerRpc(ServerRpcParams rpcParams=default)
    {
        // 1. 서버에서 플레이어 상태 검증 (이미 죽었는지 확인)
        if (PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogWarning($"Server: Player {OwnerClientId} is already dead");
            return;
        }
        
        // 시체 프리팹 생성 (서버가 권위적 정보로 처리)
        CorpseFactory.Instance.CreateCorpseServerRpc(transform.position, Quaternion.identity  , _playerAppearanceData.Value, ClientId);
        
        // 죽은 플레이어에게만 유령 상태로 변경하라고 알림
        ChangeToGhostStateServerRpc();
        
        // 로컬: 유령 상태로 전환 (레이어..)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { ClientId }
            }
        };
        ChangeToGhostStateClientRpc(clientRpcParams);
        
        //시각처리: Model의 State.OnValueChanged에서
    }
    
    /// <summary>
    /// 로컬: 유령 상태로 전환 (레이어..)
    /// </summary>
    [ClientRpc]
    private void ChangeToGhostStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // 레이어 변경
        gameObject.layer = GameLayers.GetLayerIndex(GameLayers.PlayerGhost);
    }
    
    //NOTE: 민수님
    //이거는 MODEL건드리는거니까 여기 있어야하는 거 맞음.
    /// <summary>
    /// 유령 상태로 전환 (서버전용함수)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeToGhostStateServerRpc()
    {
        var temp = _playerStatusData.Value;
        // 서버에서 속도 변경 (NetworkVariable로 동기화됨)
        temp.moveSpeed = _playerStatusData.Value.moveSpeed * GameConstants.Player.GhostSpeedMultiplier; // 유령 속도로 설정
        // 서버에서 job변경
        temp.job =  PlayerJob.Ghost;
        _playerStatusData.Value = temp;
        // 서버에서 상태 변경
        PlayerStateData newState = new PlayerStateData
        {
            AliveState = PlayerLivingState.Dead,
            AnimationState = _playerStateData.Value.AnimationState  // 기존 값 유지
        };
        _playerStateData.Value = newState;
        // 서버에서 태그 변경 (NetworkVariable로 동기화됨)
        _playerTag.Value = GameTags.PlayerGhost;
        // 서버에서 투명도 변경
        PlayerAppearanceData currentAppearanceData = _playerAppearanceData.Value;
        currentAppearanceData.AlphaValue = GameConstants.Player.GhostTransparency;
        _playerAppearanceData.Value = currentAppearanceData;
    }
}