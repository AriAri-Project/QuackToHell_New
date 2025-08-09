// PlayerModel.cs
using System;
using System.IO;
using System.Numerics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static PlayerView;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// 로직 관리
/// </summary>
public class PlayerModel : NetworkBehaviour
{
    private void Awake()
    {
        //플레이어 트랜스폼 가져오기
        playerRB = gameObject.GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        //playerstate값 바뀌면 SetStateByPlayerStateEnum() 실행
        PlayerStateData.OnValueChanged += (oldValue, newValue) =>
        {
            SetStateByPlayerStateEnum(newValue.AliveState, newValue.AnimationState);
            ApplyStateChange();
        };        
        
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


    //플레이어 위치
    private Rigidbody2D playerRB;

    //속성값
    private NetworkVariable<PlayerStatusData> _playerStatusData = new NetworkVariable<PlayerStatusData>(
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<PlayerStatusData> PlayerStatusData
    {
        get { return _playerStatusData; }
        set { _playerStatusData = value; }
    }


    //상태에 따라 행동
    //상태 주입: State를 상속받은 클래스의 인스턴스를 주입받음
    private NetworkVariable<PlayerStateData> _playerStateData = new NetworkVariable<PlayerStateData>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<PlayerStateData> PlayerStateData
    {
        get { return _playerStateData; }
        set
        {
            _playerStateData = value;
        }
    }

    

    private State preAliveState;
    private State tempAliveState;
    private State curAliveState;
    private State preAnimationState;
    private State tempAnimationState;
    private State curAnimationState;


    private void SetStateByPlayerStateEnum(PlayerLivingState inputPlayerAliveState = PlayerLivingState.Alive, PlayerAnimationState inputPlayerAnimationState = PlayerAnimationState.Idle)
    {
        switch (inputPlayerAliveState)
        {
            case PlayerLivingState.Alive:
                SetAliveState(gameObject.AddComponent<PlayerAliveState>());
                break;
            case PlayerLivingState.Dead:
                SetAliveState(gameObject.AddComponent<PlayerDeadState>());
                break;
            default:
                break;
        }
        switch (inputPlayerAnimationState)
        {
            case PlayerAnimationState.Idle:
                SetAnimationState(gameObject.AddComponent<PlayerIdleState>());
                break;
            case PlayerAnimationState.Walk:
                SetAnimationState(gameObject.AddComponent<PlayerWalkState>());
                break;
        }
    }

    private void SetAliveState(State state)
    {
        tempAliveState = curAliveState;
        curAliveState = state;
        preAliveState = tempAliveState;

        //안 쓰는 컴포넌트 삭제
        foreach (var _state in GetComponents<State>())
        {
            if (_state != curAliveState && _state != preAliveState)
            {
                Destroy(_state);
            }
        }
    }

    private void SetAnimationState(State state)
    {
        tempAnimationState = curAnimationState;
        curAnimationState = state;
        preAnimationState = tempAnimationState;

        //안 쓰는 컴포넌트 삭제
        foreach (var _state in GetComponents<State>())
        {
            if (_state != curAnimationState && _state != preAnimationState)
            {
                Destroy(_state);
            }
        }
    }


    private void ApplyStateChange()
    {
        if (preAliveState != null)
        {
            preAliveState.OnStateExit();
        }
        if (preAnimationState != null)
        {
            preAnimationState.OnStateExit();
        }
        
        curAliveState.OnStateEnter();
        curAnimationState.OnStateEnter();
    }



    //움직임 로직 실행

    [Rpc(SendTo.Server)]
    public void MovePlayerServerRpc(int inputXDirection, int inputYDirection)
    {
        //이동 : 1초당 움직임
        //방향 벡터
        Debug.Log($"PlayerModel: Moving player with X: {inputXDirection}, Y: {inputYDirection}");
        UnityEngine.Vector2 direction = new UnityEngine.Vector2(inputXDirection, inputYDirection).normalized;
        playerRB.linearVelocity = direction * PlayerStatusData.Value.MoveSpeed;
        //애니메이션 상태변경
        if (inputXDirection != 0 || inputYDirection != 0)
        {
            var newStateData = PlayerStateData.Value;
            newStateData.AnimationState = PlayerAnimationState.Walk;
            PlayerStateData.Value = newStateData;
            Debug.Log("PlayerModel: Walking");
        }
        else
        {
            var newStateData = PlayerStateData.Value;
            newStateData.AnimationState = PlayerAnimationState.Idle;
            PlayerStateData.Value = newStateData;
            Debug.Log("PlayerModel: Idle");
        }
    }

}