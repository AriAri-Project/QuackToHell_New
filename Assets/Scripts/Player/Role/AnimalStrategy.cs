using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

/// <summary>
/// 동물 역할 전략
/// 기본적인 이동과 상호작용만 가능
/// </summary>
public class AnimalStrategy : IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerModel _playerModel;
    private PlayerInput _playerInput;
    private InputActionMap _commonActionMap;
    
    public AnimalStrategy(PlayerModel playerModel, PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerPresenter = playerPresenter;
        _playerModel = playerModel;
        _playerInput = playerInput;
    }
    
    public void Setup()
    {
        // 공통 Action Map만 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        if (_commonActionMap != null) _commonActionMap.Enable();
    
    }
    
    public void HandleInput(InputAction.CallbackContext context)
    {
        // 동물은 기본 입력만 처리
        string actionName = context.action.name;
        
        switch (actionName)
        {
            case GameInputs.Actions.Interact:
                TryInteract();
                break;
            case GameInputs.Actions.Report:
                TryReportCorpse();
                break;
        }
    }
    
    public void Update()
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
    
    public void TryKill()
    {
        // 동물은 킬 불가능
        Debug.Log("동물은 킬을 사용할 수 없습니다.");
    }
    
    public void TrySabotage()
    {
        // 동물은 사보타지 불가능
        Debug.Log("다른 상호작용 범위에 없어서, defult키가 사보타지로 세팅됩니다: 동물이 사보타지를 시도했으나, 동물이어서 아무일도 일어나지 않습니다.");
    }
    
    public void TryInteract()
    {
        if (!CanInteract()) return;
        
        // 1단계: 상호작용 오브젝트 확인
        if (HasInteractableObjectsNearby())
        {
            _playerPresenter.TryInteractServerRpc(); // 기존 로직
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
        
        // 시체근처 확인
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
        // 동물은 킬 불가능
        return false;
    }
    
    public bool CanSabotage()
    {
        // 동물은 사보타지 불가능
        return false;
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

    public void TryVent() { /* 아무것도 하지 않음 */ }
    public bool CanVent() { return false; }
    
    #endregion
}
