using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Collections.Generic;

//TODO: 나중에 수정하기
public class LobbyController : NetworkBehaviour
{
    [Header("Card System")]
    [SerializeField] private CardCatalogBootstrap cardCatalogBootstrap;
    
    private bool isCardDataLoaded = false;
    private bool isGameReadyToStart = false;

    /// <summary>
    /// 카드 데이터 로딩 상태 확인: 읽기 전용 프로퍼티
    /// </summary>
    public bool IsCardDataLoaded => isCardDataLoaded;

    /// <summary>
    /// 게임 시작 준비 상태 확인: 읽기 전용 프로퍼티
    /// </summary>
    public bool IsGameReadyToStart => isGameReadyToStart;


    private void Start()
    {
        // 씬에서 해당 타입의 오브젝트를 찾아서 참조 설정
        cardCatalogBootstrap = FindObjectOfType<CardCatalogBootstrap>();
        
        if (cardCatalogBootstrap == null)
        {
            Debug.LogError("[LobbyController] CardCatalogBootstrap을 씬에서 찾을 수 없습니다.");
        }

        // 카드 데이터 로딩 시작
        LoadCardData();        
    }

    /// <summary>
    /// 1. 카드 데이터를 불러온다 (카드구조체)
    /// </summary>
    private async void LoadCardData()
    {
        Debug.Log("[LobbyController] 카드 데이터 로딩 시작...");
        
        try
        {
            // CardCatalogRepository가 준비될 때까지 대기
            await CardCatalogRepository.Instance.WhenReadyAsync();
            
            // 카드 데이터 로딩 완료
            isCardDataLoaded = true;
            Debug.Log($"[LobbyController] 카드 데이터 로딩 완료. 총 {CardCatalogRepository.Instance.AsReadOnly().Count}개 카드");
            
            // 2. 데이터 불러오는 거 끝나면, 게임 시작 가능하게 만든다
            SetGameReadyToStart();
            
            // 3. 팩토리한테 데이터(카드구조체)를 넘겨준다
            PassCardDataToFactory();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyController] 카드 데이터 로딩 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 2. 데이터 불러오는 거 끝나면, 게임 시작 가능하게 만든다
    /// </summary>
    private void SetGameReadyToStart()
    {
        isGameReadyToStart = true;
        Debug.Log("[LobbyController] 카드 데이터 로딩 완료!");
        
        // UI 업데이트나 다른 게임 시작 관련 로직을 여기에 추가할 수 있습니다
        // 예: 시작 버튼 활성화, 로딩 화면 숨기기 등
    }

    /// <summary>
    /// 3. 팩토리한테 데이터(카드구조체)를 넘겨준다
    /// </summary>
    private void PassCardDataToFactory()
    {
        // 싱글톤 인스턴스를 통해 CardItemFactory에 접근
        if (CardItemFactory.Instance != null)
        {
            // 모든 카드 데이터를 딕셔너리로 가져와서 팩토리에 전달
            var allCardData = CardCatalogRepository.Instance.AsReadOnly();
            
            // 팩토리에 카드 데이터 전달
            CardItemFactory.Instance.SetCardData(allCardData);
            
            Debug.Log($"[LobbyController] 팩토리에 {allCardData.Count}개 카드 데이터 전달 완료");
        }
        else
        {
            Debug.LogWarning("[LobbyController] CardItemFactory 싱글톤 인스턴스를 찾을 수 없습니다.");
        }
    }

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
        if (!IsOwner)//default 오너: 호스트
        {
            Debug.LogError("Only the owner can start the game!");
            return;
        }

        // 카드 데이터가 로드되지 않았거나 게임 시작 준비가 안 되었다면 시작 불가
        if (!isCardDataLoaded || !isGameReadyToStart)
        {
            Debug.LogWarning("[LobbyController] 게임 시작 준비가 완료되지 않았습니다.");
            return;
        }

        //클라이언트가 2명 이상이면, 마을 씬 이동: invoke를 3초뒤에
        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
        {
            LoadVillageSceneServerRpc();
        }
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

}
