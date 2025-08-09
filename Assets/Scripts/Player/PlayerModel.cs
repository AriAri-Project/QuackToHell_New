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
        EPlayerState.OnValueChanged += (oldValue, newValue) =>
        {
            SetStateByPlayerStateEnum(newValue);
            ApplyStateChange();
        };        
        
    }

    private void Update()
    {
        if (curState != null)
        {
            curState.OnStateUpdate();
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
    private NetworkVariable<PlayerState> _EplayerState = new NetworkVariable<PlayerState>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<PlayerState> EPlayerState
    {
        get { return _EplayerState; }
        set
        {
            _EplayerState = value;
        }
    }



    private State preState;
    private State tempState;
    private State curState;

    
    private void SetStateByPlayerStateEnum(PlayerState inputPlayerState)
    {
        switch (inputPlayerState)
        {
            case PlayerState.Alive:
                SetState(gameObject.AddComponent<PlayerAliveState>());
                break;
            case PlayerState.Dead:
                SetState(gameObject.AddComponent<PlayerDeadState>());
                break;
            default:
                break;
        }
    }

    private void SetState(State state)
    {
        tempState = curState;
        curState = state;
        preState = tempState;

        //안 쓰는 컴포넌트 삭제
        foreach (var _state in GetComponents<State>())
        {
            if (_state != curState && _state != preState)
            {
                Destroy(_state);
            }
        }
    }
    private void ApplyStateChange()
    {
        if (preState != null)
        {
            preState.OnStateExit();
        }
        curState.OnStateEnter();
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
        
    }

}