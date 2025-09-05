using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;

public class LobbyController : NetworkBehaviour
{
    [SerializeField]
    private TMP_Dropdown colorDropdown;

    #region 카드데이터 로드

    private bool isCardDataLoaded = false;

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //버튼 이벤트 바인딩
        colorDropdown.onValueChanged.AddListener(OnColorDropdownButton);

        
        //호스트만 데이터 로드
        if (!IsHost)
        {
            return;
        }

        // CardDataView가 초기화될 때까지 대기
        while (CardDataView.Presenter == null)
        {
            await Task.Yield();
        }

        // 데이터 로딩 완료까지 대기
        await CardDataView.Presenter.WhenReadyAsync();

        // deckmanager에 카드 데이터 전달
        IReadOnlyDictionary<int, CardDef> cardData = CardDataView.Presenter.Cards;
        //직렬화 가능 타입으로 변환
        Dictionary_CardIdCardDef[] cardKeyValuePairs = new Dictionary_CardIdCardDef[cardData.Count];
        int index = 0;
        foreach (var card in cardData)
        {
            cardKeyValuePairs[index] = new Dictionary_CardIdCardDef { Key = card.Key, Value = card.Value };
            index++;
        }

        
        
    }

    #endregion

    #region 색깔 선택 버튼


    public void OnColorDropdownButton(Int32 colorIndex)
    {
        Debug.Log($"Input color: {colorIndex}");
        PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).ChangeColorServerRpc(colorIndex, NetworkManager.Singleton.LocalClientId);
    }
    #endregion

    #region 게임 버튼

    public void OnJoinAsClientButton()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 클라이언트로 세션에 참여
        NetworkManager.Singleton.StartClient();
    }

    public void OnJoinAsHostButton()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 호스트(서버+클라이언트)로 세션 생성 및 참여
        NetworkManager.Singleton.StartHost();
    }

    public void OnStartGameButton()
    {
        //호스트만 게임 시작 가능
        if (!IsHost)
        {
            Debug.LogError("Only the host can start the game!");
            return;
        }

        // 2명 미만이면 시작 못 함
        if (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
        {
            Debug.LogError("Need at least 2 players to start the game!");
            return;
        }
        
        if (!isCardDataLoaded)
        {
            Debug.LogError("Card data is not loaded!");
            return;
        }
       
        //본인 데이터가 모두 초기화되면, 씬 이동.
         StartCoroutine(DelayedSceneLoad());
    }

    private IEnumerator DelayedSceneLoad()
    {
        // PlayerObject 생성 시간을 확보하기 위해 잠시 대기
        yield return new WaitForSeconds(2f);
        LoadVillageSceneServerRpc();
    }

    private IEnumerator DelayedSceneLoad()
    {
        // PlayerObject 생성 시간을 확보하기 위해 잠시 대기
        yield return new WaitForSeconds(2f);
        LoadVillageSceneServerRpc();
    }


    private async void LoadCardData( Dictionary_CardIdCardDef[] cardKeyValuePairs)
    {
        if(!IsHost)
        {
            return;
        }
        
        //DeckManager에게 데이터 전달
        await DeckManager.Instance.SetTotalCardsOnGame(cardKeyValuePairs);
        
        isCardDataLoaded = true;
    }



    private void OnClientConnected(ulong clientId)
    {

        // 자신의 클라이언트가 연결되었을 때만 플레이어 스폰
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // 이벤트 구독 해제 (한 번만 실행되도록)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

            PlayerSpawn();
        }


    }

    [ServerRpc]
    private void LoadVillageSceneServerRpc()
    {
        // 모든 클라이언트를 VillageScene으로 이동
        NetworkManager.Singleton.SceneManager.LoadScene("VillageScene", LoadSceneMode.Single);
    }

    private void PlayerSpawn()
    {
        PlayerFactory playerFactory = FindObjectOfType<PlayerFactory>();
        if (playerFactory != null)
        {
            playerFactory.SpawnPlayerServerRpc();
        }
        else
        {
            Debug.LogError("PlayerFactory not found in the scene.");
        }
    }
    #endregion
}
