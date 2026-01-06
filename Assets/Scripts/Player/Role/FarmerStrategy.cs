using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using UnityEngine.PlayerLoop;

/// <summary>
/// 농장주 역할 전략
/// Kill, Sabotage 등의 Ability를 다형성으로 구현
/// </summary>
public class FarmerStrategy : NetworkBehaviour, IRoleStrategy
{
    public event Action OnKillSuccess;
    public event Action OnSavotageSuccess;
    public event Action OnKillCooldownReady;
    public event Action OnSavotageCooldownReady;
    
    private PlayerPresenter _playerPresenter;
    private PlayerModel _playerModel;
    private PlayerView _playerView;
    private PlayerInput _playerInput;
    private InputActionMap _farmerActionMap;
    private InputActionMap _commonActionMap;
    
    
    private float killCooltimeMax;
    private float killCooltimer = 0f;
    private bool canKill = false;

    public bool CanKill
    {
        get{return canKill;}
    }
    
    private float savotageCooltimeMax; 
    private float savotageCooltimer = 0f;  
    private bool canSavotage = false;

    private bool isVentEntered = false;

    public bool IsVentEntered
    {
        get { return isVentEntered; }
    }
    private ulong interatingVentNetworkId=0;

    public ulong InteratingVentNetworkId
    {
        get { return interatingVentNetworkId; }
    }
    

    public void Initialize(PlayerModel playerModel, PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerModel = playerModel;
        _playerView = playerModel.GetComponent<PlayerView>();
        _playerPresenter = playerPresenter;
        _playerInput = playerInput;
    }

    
    public void Setup()
    {
        // Action Map 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        _farmerActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Farmer);
        
        if (_commonActionMap != null) _commonActionMap.Enable();
        if (_farmerActionMap != null) _farmerActionMap.Enable();

        killCooltimeMax = LobbyManager.Instance.LobbyData.killCooltime;
        killCooltimer = 0f;
        canKill = false;
        
            
        savotageCooltimeMax = LobbyManager.Instance.LobbyData.savotageCooltime; 
        savotageCooltimer = 0f; 
        canSavotage = false;  
    }
   
    
    public void Update()
    {
        if (!canKill)
        {
            killCooltimer += Time.deltaTime;
            if (killCooltimer >= killCooltimeMax)
            {
                canKill = true;
                OnKillCooldownReady?.Invoke();
            }
        }
        
        if (!canSavotage)
        {
            savotageCooltimer += Time.deltaTime;
            if (savotageCooltimer >= savotageCooltimeMax)
            {
                canSavotage = true;
                OnSavotageCooldownReady?.Invoke();
            }
        }
    }
    
    // 0. 외부 인터페이스
    public void Kill(ulong targetNetworkObjectId)
    {
        CanKillServerRpc(targetNetworkObjectId);
    }

    // 1. Can으로 조건검사: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void CanKillServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        PlayerModel targetPlayerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(targetNetworkObjectId);
        if (targetPlayerModel == null)
        {
            Debug.LogWarning($"[Kill 실패] 타겟 플레이어를 찾을 수 없습니다. TargetNetworkObjectId: {targetNetworkObjectId}, RequesterClientId: {requesterClientId}");
            CanKillResultClientRpc(false, targetNetworkObjectId, new ClientRpcParams 
            { 
                Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
            });
            return;
        }
        
        GameObject requesterPlayerObject = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(requesterClientId);
        if (requesterPlayerObject == null)
        {
            Debug.LogWarning($"[Kill 실패] 요청자 플레이어를 찾을 수 없습니다. RequesterClientId: {requesterClientId}");
            CanKillResultClientRpc(false, targetNetworkObjectId, new ClientRpcParams 
            { 
                Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
            });
            return;
        }
        
        
        
        FarmerStrategy requesterPlayerFarmerStrategy = requesterPlayerObject.GetComponent<FarmerStrategy>();
        if (requesterPlayerFarmerStrategy == null)
        {
            Debug.LogWarning($"[Kill 실패] 요청자가 Farmer가 아닙니다. RequesterClientId: {requesterClientId}");
            CanKillResultClientRpc(false, targetNetworkObjectId, new ClientRpcParams 
            { 
                Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
            });
            return;
        }
        
        bool result = false;
        // 벽에 가려진 플레이어인지 체크 
        ShadowHider requesterShadowHider = requesterPlayerObject?.GetComponentInChildren<ShadowHider>();
        if (requesterShadowHider != null && targetPlayerModel != null)
        {
            if (requesterShadowHider.IsTargetHiddenByShadow(targetPlayerModel.gameObject))
            {
                Debug.Log($"[Kill 실패] 타겟이 벽에 가려져 있습니다.");
                result = false;
            }
        }
        if (targetPlayerModel.GetPlayerJob() != PlayerJob.Animal)
        {
            Debug.Log($"[Kill 실패] 타겟이 동물이 아닙니다. TargetJob: {targetPlayerModel.GetPlayerJob()}, TargetNetworkObjectId: {targetNetworkObjectId}, RequesterClientId: {requesterClientId}");
            result = false;
        }
        else if (targetPlayerModel.GetPlayerAliveState() != PlayerLivingState.Alive)
        {
            Debug.Log($"[Kill 실패] 타겟이 살아있지 않습니다. TargetState: {targetPlayerModel.GetPlayerAliveState()}, TargetNetworkObjectId: {targetNetworkObjectId}, RequesterClientId: {requesterClientId}");
            result = false;
        }
        else if (requesterPlayerFarmerStrategy.canKill == false)
        {
            Debug.Log($"[Kill 실패] 킬 쿨타임이 아직 진행 중입니다. RequesterClientId: {requesterClientId}");
            result = false;
        }
        else
        {
            result = true;
        }
    
        CanKillResultClientRpc(result, targetNetworkObjectId, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }
    
    

    // 2. 결과를 전송: ClientRpc
    [ClientRpc]
    public void CanKillResultClientRpc(bool canKill, ulong targetNetworkObjectId, ClientRpcParams rpcParams = default)
    {
        if (canKill == false) return;
    
        KillServerRpc(targetNetworkObjectId);
    }


    // 3. 실제 작업 수행: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void KillServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        PlayerModel targetPlayerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(targetNetworkObjectId);
        targetPlayerModel.HandlePlayerDeathServerRpc();
        
        // 킬을 실행한 farmer에게만 KillClientRpc 전송
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        KillClientRpc(targetNetworkObjectId, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }
    
    
    public void Cleanup()
    {
        // 입력 이벤트 구독 해제
        if (_farmerActionMap != null)
        {
            _farmerActionMap.Disable();
        }
        
        if (_commonActionMap != null)
        {
            _commonActionMap.Disable();
        }
    }
    
    #region Ability 구현 (다형성)
    

  
    [ClientRpc]
    private void KillClientRpc(ulong targetNetworkObjectId,ClientRpcParams rpcParams = default){
        //쿨타임소모
        killCooltimer = 0f;
        canKill = false;
        
        //킬 성공 action invoke
        OnKillSuccess?.Invoke();
        
        //죽인 애를 OverlappingPlayers에서 제거
        GameObject targetPlayer = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(targetNetworkObjectId);
        if(targetPlayer == null){
            return;
        }
        _playerView?.RemoveDeadPlayerFromOverlappingPlayers(targetPlayer);
    }

    [ClientRpc]
    private void SavotageClientRpc(){  
        savotageCooltimer = 0f;
        canSavotage = false;
        OnSavotageSuccess?.Invoke();
    }
    
    public void Savotage()
    {
        CanSavotageServerRpc();
    }


    [ServerRpc(RequireOwnership = false)]
    public void SavotageServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        GameObject requesterPlayer = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(requesterClientId);
        SabotageNetworkManager.Instance.TryStartSabotageFromPlayer(requesterPlayer);
        SavotageClientRpc();
    }

    public void Interact(string targetTag ,ulong targetNetworkObjectId = 0)
    {
        CanInteractServerRpc(targetTag, targetNetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanInteractServerRpc(string targetTag,ulong targetNetworkObjectId = 0,ServerRpcParams rpcParams = default)
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
        CanInteractResultClientRpc(result, targetTag, targetNetworkObjectId, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
        
    }

    [ClientRpc]
    public void CanInteractResultClientRpc(bool canInteract, string targetTag,ulong targetNetworkObjectId = 0,ClientRpcParams rpcParams = default)
    {
        if (!canInteract) return;
    
        InteractServerRpc(targetTag, targetNetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(string targetTag,ulong targetNetworkObjectId = 0,ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        PlayerView targetView= PlayerHelperManager.Instance.GetPlayerViewlByClientId(sender);
        
        
        switch (targetTag)
        {
            //벤트
            case GameTags.Vent:
                //벤트타기
                if (targetNetworkObjectId!=0)
                {
                    NetworkObject interactObj = null;
                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                            targetNetworkObjectId, out NetworkObject obj))
                    {
                        interactObj = obj;
                    }
                    if (interactObj != null && interactObj.CompareTag(GameTags.Vent))
                    {
                        VentController vent = interactObj.GetComponent<VentController>();
                        GameObject player = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(sender);
              
                        VentEnterClientRpc(targetNetworkObjectId, true, new ClientRpcParams 
                        { 
                            Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } 
                        });
                    }
                }
                
                break;
            //미니게임
            case  GameTags.MiniGame:
                //미니게임 상호작용
                //미니게임 창 키라고 clientrpc호출
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
    
    [ClientRpc]
    private void VentEnterClientRpc(ulong targetNetworkObjectId,bool isEntering, ClientRpcParams rpcParams = default)
    {
    
        // PlayerVentEnterState에 진입/탈출 정보 전달
        PlayerVentEnterState ventState = GetComponent<PlayerVentEnterState>();
        ventState?.SetVentAction(isEntering, targetNetworkObjectId, this);
        // 상태 전환
        PlayerModel playerModel = GetComponent<PlayerModel>();
        playerModel?.SetAnimationStateServerRpc(PlayerAnimationState.VentEnter);
        
        isVentEntered=true;
        if (isVentEntered)
        {
            interatingVentNetworkId = targetNetworkObjectId;
        }
        else
        {
            interatingVentNetworkId = 0;
        }
    }

    public void ExitVent()
    {
        if (interatingVentNetworkId == 0) return;

        isVentEntered = false;
        
        // PlayerVentEnterState에 탈출 정보 전달
        PlayerVentEnterState ventState = GetComponent<PlayerVentEnterState>();
        ventState?.SetVentAction(false, interatingVentNetworkId, this);
    
        // 탈출 애니메이션 트리거
        ventState?.TriggerExitAnimation();
    }

    public void ReportCorpse(ulong corpseClientId)
    {
        CanReportServerRpc(corpseClientId);
    }
    

    
    
    public bool CanInteract()
    {
        // 모든 역할이 상호작용 가능
        return true;
    }
    

    

    // 1. Can으로 조건검사: ServerRpc
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

    // 2. 결과를 전송: ClientRpc
    [ClientRpc]
    public void CanReportResultClientRpc(bool canReport, ulong corpseClientId, ClientRpcParams rpcParams = default)
    {
        if (canReport==false)
        {
            return;
        }
        Debug.Log($"시체신고 가능여부={canReport}: Server Rpc호출");
        ReportServerRpc(corpseClientId);
    }

    // 3. 실제 작업 수행: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void ReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default)
    {
        ulong reporterClientId = rpcParams.Receive.SenderClientId;
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
    }
    

    [ServerRpc(RequireOwnership = false)]
    public void CanSavotageServerRpc(ServerRpcParams rpcParams = default)
    {
        //TODO: 사보타지 조건구현
        bool result = false;
    
        
        if (canSavotage == false)
        {
            result = false;
        }
        else
        {
            result = true;
        }
        
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        CanSavotageResultClientRpc(true, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanSavotageResultClientRpc(bool canSabotage, ClientRpcParams rpcParams = default)
    {
        if (canSabotage==false)
        {
            return;
        }
        
        SavotageServerRpc();
    }

    
    
    #endregion
}
