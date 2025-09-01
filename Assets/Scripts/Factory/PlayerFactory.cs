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
    
    private Transform playerSpawnPoint;
    private void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        playerSpawnPoint = transform;
    }


    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        // 스폰 (리플리케이트)
        var player = Instantiate(playerPrefab, playerSpawnPoint);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);

        // 서버에서만 값 세팅
        // 속성: NetworkVariable로 관리 (리플리케이트)
        // 인스펙터에서 설정한대로 초기화됨
        ulong myClientId =  NetworkManager.Singleton.LocalClientId;
        GameObject myPlayerObj = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(myClientId);
        PlayerModel myPlayerModel =  myPlayerObj.GetComponent<PlayerModel>();
        PlayerStatusData myPlayerStateData = myPlayerModel.PlayerStatusData.Value;
        // 닉네임에 클라이언트 아이디 붙여서 구분
        myPlayerStateData.Nickname = myPlayerStateData.Nickname + myClientId.ToString();
        // 게임오브젝트의 이름을 닉네임으로 변경
        player.name = $"{myPlayerStateData.Nickname}{rpcParams.Receive.SenderClientId}";
        // state 데이터 주입
        player.GetComponent<PlayerModel>().PlayerStatusData.Value = myPlayerStateData;


        // 상태 주입 (모두에게 명령)
        player.GetComponent<PlayerModel>().PlayerStateData.Value = new PlayerStateData
        {
            AliveState = PlayerLivingState.Alive,
            AnimationState = PlayerAnimationState.Idle
        };

        //싱글톤 처리 - 플레이어
        DontDestroyOnLoad(player);
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