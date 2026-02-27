using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// 플레이어 생성 담당
/// </summary>

public class PlayerFactoryManager : NetworkBehaviour
{
    public GameObject playerPrefab;
    public Action onPlayerSpawned;
    
    private Transform playerSpawnPoint;
    private void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        playerSpawnPoint = transform;
    }


    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        // note cba0898: Assert 상황에서 코드 실행 사유 체크 필요. Assert가 아니라 일반적인 if문으로 변경하는게 맞아 보임.
        if (!DebugUtils.AssertNotNull(playerPrefab, "playerPrefab", this))
        {
            SpawnPlayerResultClientRpc(false);
            return;
        }
            
            
        GameObject player = Instantiate(playerPrefab, playerSpawnPoint);
        PlayerModel playerModel = player.GetComponent<PlayerModel>();
        // note cba0898: Assert 상황에서 코드 실행 사유 체크 필요. Assert가 아니라 일반적인 if문으로 변경하는게 맞아 보임.
        if (!DebugUtils.AssertNotNull(playerModel, "PlayerModel", this))
        {
            SpawnPlayerResultClientRpc(false);
            return;
        }

        NetworkObject networkObject = player.GetComponent<NetworkObject>();
        // note cba0898: Assert 상황에서 코드 실행 사유 체크 필요. Assert가 아니라 일반적인 if문으로 변경하는게 맞아 보임.
        // assert는 빌드할 때 빠지니까 조건이 비어버림
        /*if (!DebugUtils.AssertNotNull(networkObject, "NetworkObject", this))
        {
            SpawnPlayerResultClientRpc(false);
            return;
        }*/

        if (networkObject == null)
        {
            SpawnPlayerResultClientRpc(false);
            //assert는 디버그로그처럼 쓰기
            DebugUtils.AssertNotNull(false, "NetworkObject", this);
            return;
        }

        networkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        
        //클라이언트 아이디 부여
        playerModel.ClientId = rpcParams.Receive.SenderClientId;

        PlayerStatusData myPlayerStateData = playerModel.PlayerStatusData.Value;
        string baseNickname = myPlayerStateData.Nickname.Split('_')[0];
        myPlayerStateData.Nickname = $"{baseNickname}_{rpcParams.Receive.SenderClientId}";        
        myPlayerStateData.job = PlayerJob.None;
        myPlayerStateData.moveSpeed = GameConstants.Player.DefaultMoveSpeed;
        myPlayerStateData.gold = GameConstants.Player.DefaultGold;
        myPlayerStateData.IsReady = false;
        myPlayerStateData.credibility = GameConstants.Player.MaxCredibility;
        myPlayerStateData.spellpower = GameConstants.Player.MaxSpellpower;
        
        playerModel.PlayerStatusData.Value = myPlayerStateData;

        player.name = myPlayerStateData.Nickname;
        
        playerModel.PlayerAppearanceData.Value = new PlayerAppearanceData
        {
            ColorIndex = 0,
            AlphaValue = 1,
        };

        playerModel.PlayerStateData.Value = new PlayerStateData
        {
            aliveState = PlayerLivingState.Alive,
            animationState = PlayerAnimationState.Idle
        };
        
        
        DontDestroyOnLoad(player);
        SpawnPlayerResultClientRpc(true);
    }
    
    // 클라이언트가 나갈 때 호출하는 메서드
    public void DespawnPlayerAsClient()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        GameObject playerGameObject = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(localClientId);

        if (playerGameObject != null)
        {
            // 카메라 먼저 분리
            PlayerView playerView = playerGameObject.GetComponent<PlayerView>();
            if (playerView != null)
            {
                playerView.DetachCamera();
            }
        
            NetworkObject networkObject = playerGameObject.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn(false);
            }
        }

        if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    
        // Shutdown 후 남은 오브젝트 파괴
        if (playerGameObject != null)
        {
            Destroy(playerGameObject);
        }

        PlayerHelperManager.Instance.InvalidateCache();
    }
    
    /// <summary>
    /// warning: 호스트만 호출 가능
    /// 플레이어만 Despawn (Shutdown은 하지 않음)
    /// </summary>
    public void DespawnAllPlayers()
    {
        if (!IsHost)
        {
            Debug.LogWarning($"클라이언트가 요청했으나 거부됨: 호스트만 모든 플레이어를 디스폰 할 권한이 있습니다.");
            return;
        }
    
        //모든 플레이어를 디스폰
        NetworkObject[] networkObjects = PlayerHelperManager.Instance.GetAllPlayers<NetworkObject>();
        foreach (var value in networkObjects)
        {
            if (value != null)
            {
                value.Despawn(false);
            }
        }
    
        // 캐시 무효화
        PlayerHelperManager.Instance.InvalidateCache();
    }

    /// <summary>
    /// Shutdown 후 남은 플레이어 오브젝트를 파괴
    /// </summary>
    public void DestroyAllPlayerObjects()
    {
        PlayerModel[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        foreach (var player in allPlayers)
        {
            if (player != null && player.gameObject != null)
            {
                // 플레이어 오브젝트에 붙어있는 카메라를 먼저 분리
                PlayerView playerView = player.gameObject.GetComponent<PlayerView>();
                if (playerView != null)
                {
                    playerView.DetachCamera();
                }
            
                Destroy(player.gameObject);
            }
        }

        PlayerHelperManager.Instance.InvalidateCache();
    }
    
    /// <summary>
    /// warning: 호스트만 호출 가능
    /// NetworkManager 종료
    /// </summary>
    public void ShutdownNetworkManager()
    {
        if (!IsHost)
        {
            Debug.LogWarning($"클라이언트가 요청했으나 거부됨: 호스트만 NetworkManager를 종료할 권한이 있습니다.");
            return;
        }
    
        NetworkManager.Singleton.Shutdown();
    }
    
    [ClientRpc]
    public void SpawnPlayerResultClientRpc(bool success)
    {
        if (success)
        {
            onPlayerSpawned?.Invoke();
        }
        else
        {
            Debug.LogError("Error spawning player");
        }
    }

    #region 싱글톤
    public static PlayerFactoryManager Instance => SingletonHelper<PlayerFactoryManager>.Instance;

    private void Awake()
    {
        SingletonHelper<PlayerFactoryManager>.InitializeSingleton(this);
    }
    #endregion

}