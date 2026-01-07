using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

/// <summary>
/// 유령 역할 전략
/// 시체를 볼 수 있지만 리포트는 불가능
/// </summary>
public class GhostStrategy : NetworkBehaviour, IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerInput _playerInput;
    private InputActionMap _commonActionMap;
    public Action onDead;
    
    public void Initialize(PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerPresenter = playerPresenter;
        _playerInput = playerInput;
    }
    
    public void Setup()
    {
        // 공통 Action Map만 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        if (_commonActionMap != null) _commonActionMap.Enable();
        
        // 유령 전용 UI 활성화
        onDead?.Invoke();
    }
    

    
    public void Update()
    {
        // 유령 전용 업데이트 로직
        // 예: 투명도, 특별한 효과 등
    }
    
    public void Cleanup()
    {
        if (_commonActionMap != null)
        {
            _commonActionMap.Disable();
        }
    }
    
    #region Ability 구현 (다형성)
    public void Kill(ulong targetNetworkObjectId)
    {
        // 유령은 킬 불가능
        CanKillServerRpc(targetNetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        // 유령은 킬 불가능
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanKillServerRpc(ulong targetNetworkObjectId,  ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        CanKillResultClientRpc(false, targetNetworkObjectId,  new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanKillResultClientRpc(bool canKill, ulong targetNetworkObjectId, ClientRpcParams rpcParams = default)
    {
        // 유령은 킬 불가능
        if (!canKill)
        {
            Debug.Log("유령은 킬을 사용할 수 없습니다.");
        }
    }

    public void Convocation(ulong corpseNetworkObjectId)
    {
        throw new NotImplementedException();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        //유령은 report못함
        CanReportResultClientRpc(false, corpseClientId, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanReportResultClientRpc(bool canReport, ulong corpseClientId, ClientRpcParams rpcParams = default)
    {
        Debug.Log("유령은 시체 리포트 못 함");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default)
    {
        Debug.Log("유령은 시체 리포트 못 함");
        return;
    }

    public void Savotage(SabotageType sabotageType)
    {
        CanSavotageServerRpc(sabotageType);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanSavotageServerRpc(SabotageType sabotageType, ServerRpcParams rpcParams = default)
    {
        //ghost는 사보타지 불가
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        CanSavotageResultClientRpc(sabotageType,false, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanSavotageResultClientRpc(SabotageType sabotageType, bool canSabotage, ClientRpcParams rpcParams = default)
    {
        if (!canSabotage)
        {
            return;
        }
        Debug.Log($"사보타지 가능여부={canSabotage}: Server Rpc호출");
        SavotageServerRpc(sabotageType);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SavotageServerRpc(SabotageType sabotageType, ServerRpcParams rpcParams = default)
    {
        Debug.Log("유령은 사보타지 불가");
    }

    public void Interact(string targetTag,ulong targetNetworkObjectId = 0)
    {
        CanInteractServerRpc(targetTag,targetNetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanInteractServerRpc(string targetTag,ulong targetNetworkObjectId = 0, ServerRpcParams rpcParams = default)
    {
        bool result = false;
        //인터랙트 가능한 태그가 아니면 fasle
        if (targetTag == GameTags.Vent || targetTag == GameTags.Interactable ||
            targetTag == GameTags.ConvocationOfTrial || targetTag == GameTags.MiniGame)
        {
            result = true;
        }
        
        
        // 요청한 클라이언트 ID 가져오기
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
    
        // 해당 클라이언트에게만 결과 전송
        CanInteractResultClientRpc(result, targetTag,targetNetworkObjectId, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanInteractResultClientRpc(bool canInteract, string targetTag,ulong targetNetworkObjectId = 0, ClientRpcParams rpcParams = default)
    {
        if (!canInteract) return;
    
        InteractServerRpc(targetTag,targetNetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(string targetTag,ulong targetNetworkObjectId = 0, ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        switch (targetTag)
        {
            //벤트
            case GameTags.Vent:
                //벤트타기
                Debug.Log("유령은 벤트 못 탐");
                break;
            //미니게임
            case  GameTags.MiniGame:
                //미니게임 상호작용
                Debug.Log("유령은 미니게임 못 함");
                break;
            //재판소집
            case  GameTags.ConvocationOfTrial:
                //재판소집
                Debug.Log("유령은 재판소집 못 함");
                break;
        }
    }



    public void Sabotage()
    {
        // 유령은 사보타지 불가능
        Debug.Log("유령은 사보타지를 사용할 수 없습니다.");
    }
    

    
    public void ReportCorpse(ulong corpseClientId)
    {
        CanReportServerRpc(corpseClientId);
    }
    
    public bool CanKill(GameObject target)
    {
        // 유령은 킬 불가능
        return false;
    }
    
    public bool CanSabotage()
    {
        // 유령은 사보타지 불가능
        return false;
    }
    
    public bool CanInteract()
    {
        // 유령은 상호작용 불가능
        return false;
    }
    
    public bool CanReportCorpse()
    {
        // 유령은 시체 리포트 불가능
        return false;
    }
    public void TryVent() { /* 아무것도 하지 않음 */ }
    public bool CanVent() { return false; }
    
    #endregion
}
