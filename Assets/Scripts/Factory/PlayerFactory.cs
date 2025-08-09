using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Playables;
using System;
/// <summary>
/// 플레이어 생성 담당
/// </summary>

public class PlayerFactory : NetworkBehaviour
{
    //플레이어스폰
    public GameObject playerPrefab;
    public Transform playerSpawnPoint;



    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(string inputNickName = "Player_", PlayerJob inputPlayerJob = PlayerJob.None, ServerRpcParams rpcParams = default)
    {
        // 스폰 (리플리케이트)
        var player = Instantiate(playerPrefab, playerSpawnPoint);
        player.name = $"{inputNickName}{rpcParams.Receive.SenderClientId}";
        player.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);

        // 서버에서만 값 세팅
        // 속성: NetworkVariable로 관리 (리플리케이트)
        PlayerStatusData playerStatusData = new PlayerStatusData
        {
            Nickname = inputNickName + rpcParams.Receive.SenderClientId,
            Job = inputPlayerJob,
            Credibility = PlayerStatusData.MaxCredibility,
            Spellpower = PlayerStatusData.MaxSpellpower,
            Gold = 25,
            MoveSpeed = 10f
        };
        player.GetComponent<PlayerModel>().PlayerStatusData.Value = playerStatusData;

        // 상태 주입 (모두에게 명령)
        player.GetComponent<PlayerModel>().EPlayerState.Value = PlayerState.Alive;
    }




    //싱글톤로직
    private static PlayerFactory _instance;
    public static PlayerFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerFactory>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerFactory");
                    _instance = go.AddComponent<PlayerFactory>();
                }
            }
            return _instance;
        }
    }


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

}