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
    }
    
    private void Start()
    {
        // 초기 역할 설정
        if (_playerPresenter != null)
        {
            PlayerJob currentJob = _playerModel.GetPlayerJob();
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
        _currentStrategy?.Update();
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
        }
        
        _currentRole = newRole;
        
        // 새로운 전략 생성
        _currentStrategy = CreateStrategyForRole(newRole);
        
        // 새 전략 설정
        if (_currentStrategy != null)
        {
            _currentStrategy.Setup();
        }

        Debug.Log($"ChangeRole: {newRole}, clientId: {_playerPresenter.NetworkObject.OwnerClientId}");
    }
    
    private IRoleStrategy CreateStrategyForRole(PlayerJob role)
    {
        switch (role)
        {
            case PlayerJob.Farmer:
                return new FarmerStrategy(_playerModel, _playerPresenter, playerInput);
            case PlayerJob.Animal:
                return new AnimalStrategy(_playerModel, _playerPresenter, playerInput);
            case PlayerJob.Ghost:
                return new GhostStrategy(_playerPresenter, playerInput);
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 현재 역할의 입력 처리: 키보드용
    /// </summary>
    public void HandleInput(InputAction.CallbackContext context)
    {
        _currentStrategy?.HandleInput(context);
    }
    
    private void OnDestroy()
    {
        _currentStrategy?.Cleanup();
    }
}
