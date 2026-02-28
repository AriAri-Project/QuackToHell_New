using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 역할 관리자
/// State 패턴과 유사하게 역할별 전략을 관리
/// </summary>
public class RoleController : MonoBehaviour
{
    [Header("Role Settings")]
    [SerializeField] private PlayerInput playerInput;
    
    private PlayerPresenter _playerPresenter;
    private PlayerModel _playerModel;
    private IRoleStrategy _currentStrategy;
    private PlayerJob _currentRole = PlayerJob.None;
    
    private FarmerStrategy _farmerStrategy;
    private AnimalStrategy _animalStrategy;
    private GhostStrategy _ghostStrategy;
    
    /// <summary>
    /// 현재 전략 (외부에서 접근 가능)
    /// </summary>
    public IRoleStrategy CurrentStrategy => _currentStrategy;
    
    private void Awake()
    {
        _playerPresenter = GetComponent<PlayerPresenter>();
        _playerModel = GetComponent<PlayerModel>();
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        
        _farmerStrategy = GetComponent<FarmerStrategy>();
        _animalStrategy = GetComponent<AnimalStrategy>();
        _ghostStrategy = GetComponent<GhostStrategy>();
        _farmerStrategy.enabled = false;
        _animalStrategy.enabled = false;
        _ghostStrategy.enabled = false;
        
    }
    
    private void Start()
    {
        // 초기 역할 설정
        if (_playerPresenter != null)
        {
            PlayerJob currentJob = _playerModel.GetPlayerCurrentJob();
            ChangeRole(currentJob);
        }
        else
        {
            // 임시로 기본 역할 설정
            ChangeRole(PlayerJob.Farmer);
        }
    }
    
    private void Update()
    {
        // 현재 전략 업데이트
        _currentStrategy?.OnRoleUpdate();
    }
    
    /// <summary>
    /// 역할 변경
    /// </summary>
    public void ChangeRole(PlayerJob newRole)
    {
        if (_currentRole == newRole) return;
        
        // 기존 전략 정리
        if (_currentStrategy != null)
        {
            _currentStrategy.Cleanup();
            (_currentStrategy as MonoBehaviour).enabled = false;
        }
        
        _currentRole = newRole;
        
        // 새로운 전략 생성
        _currentStrategy = GetStrategyForRole(newRole);
        
        // 새 전략 설정
        if (_currentStrategy != null)
        {
            (_currentStrategy as MonoBehaviour).enabled = true;
            _currentStrategy.Setup();
        }

        Debug.Log($"ChangeRole: {newRole}, clientId: {_playerPresenter.NetworkObject.OwnerClientId}");
    }
    
    private IRoleStrategy GetStrategyForRole(PlayerJob role)
    {
        switch (role)
        {
            case PlayerJob.Farmer:
                _farmerStrategy.Initialize(_playerModel, _playerPresenter, playerInput);
                return _farmerStrategy;
            
            case PlayerJob.Animal:
            case PlayerJob.None:
                _animalStrategy.Initialize(_playerModel, _playerPresenter, playerInput);
                return _animalStrategy;
            
            case PlayerJob.Ghost:
                _ghostStrategy.Initialize(_playerPresenter, playerInput);
                return _ghostStrategy;
            
            default:
                return null;
        }
    }

    
    private void OnDestroy()
    {
        _currentStrategy?.Cleanup();
    }
}
