using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

/// <summary>
/// 동물 역할 전략
/// 기본적인 이동과 상호작용만 가능
/// </summary>
public class AnimalStrategy : NetworkBehaviour, IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerModel _playerModel;
    private PlayerView _playerView;
    private PlayerInput _playerInput;
    private InputActionMap _commonActionMap;
    
    public void Initialize(PlayerModel playerModel, PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerPresenter = playerPresenter;
        _playerModel = playerModel;
        _playerView = playerModel.GetComponent<PlayerView>();
        _playerInput = playerInput;
    }
    
    public void Setup()
    {
        // 공통 Action Map만 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        if (_commonActionMap != null) _commonActionMap.Enable();
    
    }

    
    
    
    public void OnRoleUpdate()
    {
        // 동물 전용 업데이트 로직
        // 예: 특별한 애니메이션, 효과 등
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
        // 동물은 킬 불가능
        CanKillServerRpc(targetNetworkObjectId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void KillServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        // 동물은 킬 불가능
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void CanKillServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
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
        // 동물은 킬 불가능
        if (!canKill)
        {
            Debug.Log("동물은 킬을 사용할 수 없습니다.");
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void CanReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;

        // 요청자 및 ShadowHider
        GameObject requester = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(requesterClientId);
        ShadowHider shadowHider = requester != null ? requester.GetComponentInChildren<ShadowHider>() : null;

        // ClientId로 시체 GameObject 찾기
        GameObject corpseObject = null;
        var corpses = FindObjectsByType<PlayerCorpse>(FindObjectsSortMode.None);
        for (int i = 0; i < corpses.Length; i++)
        {
            if (corpses[i] != null && corpses[i].ClientId == corpseClientId)
            {
                corpseObject = corpses[i].gameObject;
                break;
            }
        }

        bool canReport = (corpseObject != null);
        if (canReport && shadowHider != null)
        {
            if (shadowHider.IsTargetHiddenByShadow(corpseObject))
            {
                canReport = false;
            }
        }

        CanReportResultClientRpc(canReport, corpseClientId, new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } }
        });
    }

    [ClientRpc]
    public void CanReportResultClientRpc(bool canReport, ulong corpseClientId, ClientRpcParams rpcParams = default)
    {
        if (!canReport) return;
        Debug.Log($"시체신고 가능여부={canReport}: Server Rpc호출");
        ReportServerRpc(corpseClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default)
    {
        ulong reporterClientId = rpcParams.Receive.SenderClientId;
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
    }

    public void Savotage(SabotageType  sabotageType)
    {
        CanSavotageServerRpc(sabotageType);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanSavotageServerRpc(SabotageType  sabotageType, ServerRpcParams rpcParams = default)
    {
        //애니멀이므로 사보타지 못 함
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        CanSavotageResultClientRpc(sabotageType, false, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanSavotageResultClientRpc(SabotageType  sabotageType,bool canSabotage, ClientRpcParams rpcParams = default)
    {
        
        if (!canSabotage)
        {
            return;
        }
        Debug.Log($"사보타지 가능여부={canSabotage}: Server Rpc호출");
        SavotageServerRpc(sabotageType);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SavotageServerRpc(SabotageType  sabotageType, ServerRpcParams rpcParams = default)
    {
        Debug.Log("애니멀이므로 사보타지 못 함");
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
                Debug.Log("동물은 벤트 못 탐");
                break;
            //미니게임
            case  GameTags.MiniGame:
                //미니게임 상호작용
                MinigameClientRpc(new ClientRpcParams 
                { 
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } 
                });
                break;
            //재판소집
            case  GameTags.ConvocationOfTrial:
                //재판소집
                TrialManager.Instance.TryTrialServerRpc(sender);
                break;
        }
    }
    
    [ClientRpc]
    private void MinigameClientRpc(ClientRpcParams rpcParams = default)
    {
        MinigameController minigameController = _playerView.InteractObjCache.GetComponent<MinigameController>();
        minigameController.TryOpenFromPlayer(this.transform);
    }
    
    public void ReportCorpse(ulong corpseClientId)
    {
        CanReportServerRpc(corpseClientId);
    }

    
    #endregion
}
