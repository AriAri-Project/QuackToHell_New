using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// 농장주 역할 전략
/// Kill, Sabotage 등의 Ability를 다형성으로 구현
/// </summary>
public class FarmerStrategy : IRoleStrategy
{
    
    private PlayerPresenter _playerPresenter;
    private PlayerModel _playerModel;
    private PlayerInput _playerInput;
    private InputActionMap _farmerActionMap;
    private InputActionMap _commonActionMap;
    private float _ventAnimationDuration = -1f;
    
    public FarmerStrategy(PlayerModel playerModel , PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerModel = playerModel;
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
        CacheVentAnimationDuration();
    }
    private void CacheVentAnimationDuration()
    {
        Animator animator = _playerPresenter.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            _ventAnimationDuration = 0.5f;
            return;
        }

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name.Contains(GameConstants.Animation.VentEnter)) 
            {
                _ventAnimationDuration = clip.length;
                return;
            }
        }
        _ventAnimationDuration = 0.5f;
    }
    
    /// <summary>
    /// 농장주 전용 입력 처리: 키보드용
    /// </summary>
    /// <param name="context"></param>
    public void HandleInput(InputAction.CallbackContext context)
    {
        // 농장주 전용 입력 처리
        string actionName = context.action.name;
        
        switch (actionName)
        {
            case GameInputs.Actions.Kill:
                TryKill();
                break;
            case GameInputs.Actions.Interact:
                TryInteract();
                break;
            case GameInputs.Actions.Report:
                TryReportCorpse();
                break;
            case GameInputs.Actions.Savotage:
                TrySabotage();
                break;
        }
    }
    
    public void Update()
    {
        // 농장주 전용 업데이트 로직
        // 예: 특별한 효과, 타이머 등
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

    public void TryVent()
    {
        if (!CanVent()) return;
        
        // 벤트 근처 확인
        if (!HasVentNearby())
        {
            Debug.LogError("벤트가 근처에 없음");
        }
    }
    public bool CanVent()
    {
        // 농장주만 벤트 사용 가능
        return _playerModel.GetPlayerAliveState() == PlayerLivingState.Alive;
    }

    private bool HasVentNearby()
    {
        VentController[] allVents = UnityEngine.Object.FindObjectsByType<VentController>(FindObjectsSortMode.None);
        foreach (VentController vent in allVents)
        {
            float dist = Vector3.Distance(_playerPresenter.transform.position, vent.transform.position);
            if (dist <= vent.InteractionRadius)
            {
                //애니메이션 재생
                _playerModel.SetAnimationStateServerRpc(PlayerAnimationState.VentEnter);
                //애니메이션 재생 완료 후 진입
                _playerPresenter.StartCoroutine(EnterVentAfterAnimation(vent));
                return true;
            }
        }
        return false;
    }

    private System.Collections.IEnumerator EnterVentAfterAnimation(VentController vent)
    {
        yield return new WaitForSeconds(_ventAnimationDuration);
        vent.Interact(_playerPresenter.gameObject);
    }
    
    public void TryKill()
    {
        if (!CanKill()) return;
        
        _playerPresenter.TryKillServerRpc();
    }
    
    public void TrySabotage()
    {
        if (!CanSabotage()) return;
        
        // TODO: 사보타지 액션 구현
        Debug.Log("사보타지기능 아직 없듬");
        
    }
    
    public void TryInteract()
    {
        if (!CanInteract()) return;
        
        if (HasInteractableObjectsNearby())
        {
            _playerPresenter.TryInteractServerRpc(); 
        }
    }

    public bool HasInteractableObjectsNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.MiniGame) ||
                collider.CompareTag(GameTags.RareCardShop) ||
                collider.CompareTag(GameTags.Exit) ||
                collider.CompareTag(GameTags.Teleport) ||
                collider.CompareTag(GameTags.Vent)||
                collider.CompareTag(GameTags.ConvocationOfTrial))
            {
                return true;
            }
        }
        return false;
    }
    
    public void TryReportCorpse()
    {
        if (!CanReportCorpse()) return;
        
        // 시체 오브젝트 근처 확인
        if (HasCorpseNearby())
        {
            _playerPresenter.ReportCorpseServerRpc(_playerPresenter.OwnerClientId);
        }
        
    }
    

    private bool HasCorpseNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.PlayerCorpse))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasTrialConvocationNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.ConvocationOfTrial))
            {
                return true;
            }
        }
        return false;
    }


    
    public bool CanKill()
    {
        // 농장주는 킬 가능
        return true;
    }
    
    public bool CanSabotage()
    {
        // 농장주는 사보타지 가능
        return true;
    }
    
    public bool CanInteract()
    {
        // 모든 역할이 상호작용 가능
        return true;
    }
    
    public bool CanReportCorpse()
    {
        // 유령이 아닌 경우에만 시체 리포트 가능
        return _playerModel.GetPlayerAliveState() == PlayerLivingState.Alive;
    }
    
    #endregion
}
