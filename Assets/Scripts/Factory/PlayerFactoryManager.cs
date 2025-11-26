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
        myPlayerStateData.credibility = PlayerStatusData.MaxCredibility;
        myPlayerStateData.spellpower = PlayerStatusData.MaxSpellpower;

        playerModel.PlayerStatusData.Value = myPlayerStateData;

        player.name = myPlayerStateData.Nickname;
        
        playerModel.PlayerAppearanceData.Value = new PlayerAppearanceData
        {
            ColorIndex = 0,
            AlphaValue = 1
        };

        playerModel.PlayerStateData.Value = new PlayerStateData
        {
            aliveState = PlayerLivingState.Alive,
            animationState = PlayerAnimationState.Idle
        };

        DontDestroyOnLoad(player);
        SpawnPlayerResultClientRpc(true);

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