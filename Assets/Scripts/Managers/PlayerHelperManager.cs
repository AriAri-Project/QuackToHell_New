using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// 플레이어 관련 헬퍼메서드 제공하는 매니저
/// 
/// 책임:
/// - 플레이어 검색 및 조회 (클라이언트 ID로 플레이어 찾기)
/// - 플레이어 데이터 접근 헬퍼 (골드, 상태 등)
/// 
/// 주의: 실제 플레이어 데이터 수정은 하지 않음 (읽기 전용 헬퍼)
/// </summary>
public class PlayerHelperManager : MonoBehaviour
{
    #region 싱글톤
    public static PlayerHelperManager Instance => SingletonHelper<PlayerHelperManager>.Instance;

    private void Awake()
    {
        SingletonHelper<PlayerHelperManager>.InitializeSingleton(this);
    }
    #endregion

    // 캐시된 플레이어 리스트 (성능 최적화)
    private List<PlayerModel> _cachedPlayers = new List<PlayerModel>();
    private bool _isCacheValid = false;

    /// <summary>
    /// 플레이어 캐시 업데이트
    /// </summary>
    private void UpdatePlayerCache()
    {
        if (!_isCacheValid)
        {
            _cachedPlayers.Clear();
            PlayerModel[] allPlayers = FindObjectsByType<PlayerModel>(FindObjectsSortMode.None);
            _cachedPlayers.AddRange(allPlayers);
            _isCacheValid = true;
        }
    }

    /// <summary>
    /// 캐시 무효화 (플레이어가 추가/제거될 때 호출)
    /// </summary>
    public void InvalidateCache()
    {
        _isCacheValid = false;
    }

    /// <summary>
    /// 클라이언트 ID로 플레이어를 찾아서 PlayerModel 반환 (핵심 메서드)
    /// </summary>
    /// <param name="clientId">찾을 플레이어의 클라이언트 ID</param>
    /// <returns>플레이어의 PlayerModel, 찾지 못하면 null</returns>
    public PlayerModel GetPlayerModelByClientId(ulong clientId)
    {
        // note cba0898: Assert 는 배포 빌드에서 해당 상황이 없어야 한다는 가정입니다. 조건문의 일부로 포함하지 말아주세요.
        PlayerModel[] allPlayers = GetAllPlayers<PlayerModel>();
        foreach (PlayerModel player in allPlayers)
        {
            if (player.NetworkObject.OwnerClientId == clientId)
            {
                return player;
            }
        }
        return null;
    }

    /// <summary>
    /// 클라이언트 ID로 플레이어를 찾아서 골드를 반환
    /// </summary>
    /// <param name="clientId">찾을 플레이어의 클라이언트 ID</param>
    /// <returns>플레이어의 골드, 플레이어를 찾지 못하면 0</returns>
    public int GetPlayerGoldByClientId(ulong clientId)
    {
        PlayerModel player = GetPlayerModelByClientId(clientId);
        // note cba0898: Assert 사용 후 바로 return 혹은 일반 if문 사용 후 실패 사유 리포트 형태 추천. 
        if (DebugUtils.AssertNotNull(player, "Player", this))
        {
            return player.PlayerStatusData.Value.gold;
        }
        return 0;
    }

    /// <summary>
    /// 클라이언트 ID로 플레이어를 찾아서 플레이어 게임 오브젝트 반환
    /// </summary>
    /// <param name="clientId">찾을 플레이어의 클라이언트 ID</param>
    /// <returns>플레이어의 GameObject, 찾지 못하면 null</returns>
    public GameObject GetPlayerGameObjectByClientId(ulong clientId)
    {
        PlayerModel player = GetPlayerModelByClientId(clientId);
        // note cba0898: Assert 사용 후 바로 return 혹은 일반 if문 사용 후 실패 사유 리포트 형태 추천. 
        if (DebugUtils.AssertNotNull(player, "Player", this))
        {
            return player.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 현재 씬의 모든 플레이어 수 반환
    /// </summary>
    /// <returns>플레이어 수</returns>
    public int GetPlayerCount()
    {
        return NetworkManager.Singleton.ConnectedClients.Count;
    }

    /// <summary>
    /// 모든 플레이어의 움직임을 멈추는 서버 RPC
    /// </summary>
    [ServerRpc]
    public void StopAllPlayerServerRpc()
    {
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            DebugUtils.AssertNotNull(player, "PlayerView", this);
            player.IgnoreMoveInput = true;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
    /// <summary>
    /// 모든 플레이어를 가져오는 함수
    /// </summary>
    public T[] GetAllPlayers<T> ()where T : Component{
        return FindObjectsByType<T>(FindObjectsSortMode.None);
    }

    public PlayerPresenter GetPlayerPresenterByClientId(ulong clientId){
        PlayerPresenter[] allPlayers = GetAllPlayers<PlayerPresenter>();
        foreach (PlayerPresenter player in allPlayers)
        {
            if (player.NetworkObject.OwnerClientId == clientId)
            {
                return player;
            }
        }
        return null;
    }
}
