using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerVentEnterState : NetworkStateBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip ventExitAnimationClip;
    
    private bool _isEntering;
    private ulong _targetVentNetworkId;
    private FarmerStrategy _farmerStrategy;
    
    private PlayerView _myView;
    private PlayerModel _playerModel;

    private void Start()
    {
        _myView = GetComponent<PlayerView>();
        _playerModel = GetComponent<PlayerModel>();
    }
    
    public void SetVentAction(bool isEntering, ulong targetVentNetworkId, FarmerStrategy farmerStrategy)
    {
        _isEntering = isEntering;
        _targetVentNetworkId = targetVentNetworkId;
        _farmerStrategy = farmerStrategy;
    }
    
    // 탈출 애니메이션 트리거용 public 메서드 
    public void TriggerExitAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsVent", false);
        }
        
        // 탈출 애니메이션 시작 시점에 입력 차단
        if (IsOwner && _myView != null)
        {
            _myView.SetIgnorePlayerMoveInputServerRpc(true);
        }
    }

    public override void OnStateEnter()
    {
        // Owner 클라이언트에서만 입력 차단
        if (IsOwner && _myView != null)
        {
            if (_isEntering)
            {
                _myView.SetIgnorePlayerMoveInputServerRpc(true);
            }
        }

        // 애니메이션 트리거 (진입만 처리, 탈출은 TriggerExitAnimation에서 처리)
        if (animator != null && _isEntering)
        {
            animator.SetBool("IsVent", true);
        }
    }

    public override void OnStateExit()
    {
        if (animator != null)
        {
            animator.SetBool("IsVent", false);
        }
    }

    public override void OnStateUpdate()
    {

    }
    
    // Animation Event: 벤트 진입 애니메이션 완료 시 호출
    public void OnVentEnterAnimationComplete()
    {
        if (!_isEntering) return;
        if (_farmerStrategy == null) return;

        // 실제 벤트 진입 처리
        NetworkObject interactObj = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                _targetVentNetworkId, out NetworkObject obj))
        {
            interactObj = obj;
        }

        if (interactObj != null && interactObj.CompareTag(GameTags.Vent))
        {
            VentController vent = interactObj.GetComponent<VentController>();
            if (vent != null)
            {
                GameObject player = this.gameObject;
                vent.RequestToggleFromPlayer(player);
            }
        }
    }

    // Animation Event: 벤트 탈출 애니메이션 완료 시 호출
    public void OnVentExitAnimationComplete()
    {
        if (_isEntering) return;
        if (_myView == null || _playerModel == null) return;
        if (_farmerStrategy == null) return;

        // 벤트 탈출 처리
        NetworkObject interactObj = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                _targetVentNetworkId, out NetworkObject obj))
        {
            interactObj = obj;
        }

        if (interactObj != null && interactObj.CompareTag(GameTags.Vent))
        {
            VentController vent = interactObj.GetComponent<VentController>();
            if (vent != null)
            {
                GameObject player = this.gameObject;
                vent.RequestToggleFromPlayer(player);
            }
        }

        // 입력 복구
        _myView.SetIgnorePlayerMoveInputServerRpc(false);

        // 상태를 Idle로 변경
        _playerModel.SetAnimationStateServerRpc(PlayerAnimationState.Idle);
    }
}
