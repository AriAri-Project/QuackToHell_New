using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QuickCmd;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Netcode;
using System.Threading;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    
    #region Singleton

    public static LobbyManager Instance => SingletonHelper<LobbyManager>.Instance;

    private void Awake()
    {
        SingletonHelper<LobbyManager>.InitializeSingleton(this);
    }

    #endregion


    //카드데이터로드

    [Header("Google Sheets CSV URLs")]
    [SerializeField] string cardCsvUrl;     // Card_Table
    [SerializeField] string stringCsvUrl;   // String_Table
    [SerializeField] string resourceCsvUrl; // Resource_Table

    [SerializeField] bool ToggleForcedAllFarmer = false;

    private bool isCardDataLoaded = false;
    private CancellationTokenSource _cancellationTokenSource;

    //SFX
    [Header("SFX")] 
    [SerializeField] private AudioSource mafiaAssignSFX;
    [SerializeField] private AudioSource citizenAssignSFX;

    public enum LobbySFX{
        mafiaAssignSFX,
        citizenAssignSFX
    }
    
    //로비
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private const float HEART_BEAT_TIMER_MAX = 15f;
    private string _hostLobbyCode="";

    
    public string HostLobbyCode
    {
        get
        {
            return _hostLobbyCode;
        }
    }
    
    private async void Start()
    {
        try
        {
            //익명로그인
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        
    }

    


    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        
        //역할부여 연출이벤트 바인딩
        GameManager.Instance.onRoleAssignDirectionEnd += LoadVillageSceneServerRpc;

        
        //호스트만 데이터 로드
        if (!IsHost)
        {
            return;
        }

        // note cba0898: Assert를 조건처럼 쓰는 형태는 지양해주세요
        // DeckManager가 초기화될 때까지 대기
        while (!DebugUtils.AssertNotNull(DeckManager.Instance, "DeckManager.Instance", this))
        {
            await Task.Yield();
        }

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            
            // DeckManager를 통해 직접 CSV 데이터 로드
            await DeckManager.Instance.LoadCardDataFromCsv(cardCsvUrl, stringCsvUrl, resourceCsvUrl, _cancellationTokenSource.Token);
            
            // 데이터 로딩 완료까지 대기
            await DeckManager.Instance.WhenDataReadyAsync();
            
            isCardDataLoaded = true;
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyController] Card data loading failed: {ex.Message}");
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }



    public override void OnDestroy(){
        //로비 정리 작업
        CleanUpLobby();
        
        _cancellationTokenSource?.Cancel(); 
        _cancellationTokenSource?.Dispose(); 

        base.OnDestroy();
    }
    
    public async void CleanUpLobby(){
        try{
            if (hostLobby != null)
            {
                await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
            }
            else if (joinedLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch(LobbyServiceException e){
            Debug.LogWarning($"로비 정리 중 오류: {e.Message}");
        }
    }
    
    //로비
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer+=Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                
                heartbeatTimer = HEART_BEAT_TIMER_MAX;
                //이 로비 아직 살아있습니다
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                Debug.Log("Sending heartbeat ping to " + hostLobby.Id);
            }
        }
    }


    [Command]
    //for quick command
    public async void CreateLobbyCommand(string lobbyName, bool isPrivate, int maxPlayer)
    {
        await CreateLobby(lobbyName, isPrivate, maxPlayer);
        NetworkManager.Singleton.StartHost();
    }

    
    public async Task CreateLobby(string lobbyName, bool isPrivate, int maxPlayer)
    {
        try
        {
            //릴레이서버 할당요청: host의 로비별로 분리된공간 생성(게임세션)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayer);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                //호스트정보
                Player = GetPlayer(),
                //대기열에 보여줄 커스텀 로비 정보 (기본 필드에 저장 되지 않는 것들 추가로 나열)
                Data= new Dictionary<string, DataObject>
                {
                    //방참가코드
                    {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, "")}, // 일단 빈 문자열로 초기화
                    //Relay참가코드(게임세션연결용)
                    {"RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)},
                    //Private
                    {"IsPrivate", new DataObject(DataObject.VisibilityOptions.Public, isPrivate.ToString())}
                }
            };
            
            //로비생성 요청(대기실)
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptions);      
            
            // LobbyCode가 생성되면 Data에도 저장
            if (!string.IsNullOrEmpty(lobby.LobbyCode))
            {
                UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, lobby.LobbyCode)}
                    }
                };
                lobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, updateOptions);
            }

            //호스트용 릴레이 서버 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,        // 1. 서버 IP 주소
                (ushort)allocation.RelayServer.Port, // 2. 서버 포트 번호
                allocation.AllocationIdBytes,        // 3. 할당 ID (바이트 배열)
                allocation.Key,                      // 4. 암호화 키
                allocation.ConnectionData,           // 5. 클라이언트 연결 데이터
                allocation.ConnectionData        // 6. 호스트 연결 데이터
            );

            hostLobby = lobby;
            joinedLobby = lobby;

            _hostLobbyCode = lobby.LobbyCode; //게임참가코드(ex. E2EE31)
            string gameSessionCode = relayJoinCode;  //게임세션코드(ex.ABC123DEF)

            Debug.Log($"Lobby Created! {lobby.Name}, {lobby.MaxPlayers}, {lobby.LobbyCode}, RelayCode: {gameSessionCode}");
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [Command]
    public async Task<List<Lobby>> ListLobbies()
    {
        List<Lobby> lobbyList = null;
        try
        {
            //TODO: 검색 상세조건 기획 나올 시 QueryLobbiesAsync의 인자로 추가하기
            //QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            var queryResponse =  await LobbyService.Instance.QueryLobbiesAsync();
            lobbyList = queryResponse.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
        return lobbyList;
    }

    private Dictionary<string, string> CreateLobbySnapshot(List<Lobby> lobbies)
    {
        var snapshot = new Dictionary<string, string>();
        foreach (var lobby in lobbies)
        {
            //로비id를 키로, 중요정보를 값으로 저장
            string info = $"{lobby.Name}|{lobby.Players.Count}|{lobby.MaxPlayers}";
            snapshot[lobby.Id]=info;
        }

        return snapshot;
    }

    private bool AreSnapshotsEqual(Dictionary<string, string> old, Dictionary<string, string> newSnap)
    {
        //로비개수가 다르면 변경된 것
        if(old.Count!=newSnap.Count) return false;

        foreach (var lobby in newSnap)
        {
            string lobbyId = lobby.Key;
            string newInfo = lobby.Value;
            
            //이전에 없던 로비가 생겼는지 확인
            bool existInOld = old.TryGetValue(lobbyId, out string oldInfo);
            if (!existInOld)
            {
                return false;
            }
            
            //같은 id의 로비 정보가 변경되었는지 확인
            if(oldInfo!=newInfo) return false;
        }

        return true;
    }

    
    [Command]
    //for quick command
    public async void JoinLobbyByCodeCommand(string lobbyCode)
    {
        bool joinSuccess = await JoinLobbyByCode(lobbyCode);
        if (joinSuccess)
        {
            Debug.Log($"Joined Lobby with code: {lobbyCode}");
            NetworkManager.Singleton.StartClient();
        }
    }
    
    public async Task<bool> JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            // 참여 시 나의 플레이어 정보를 설정하는 옵션.
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            //로비 참가
            Lobby _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            //로비에서 Relay 참가 코드 가져오기
            string relayJoinCode = _joinedLobby.Data["RelayJoinCode"].Value;
            //Relay서버에 클라이언트로 연결 준비
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            //클라이언트용 Relay 서버 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            joinedLobby = _joinedLobby;
            _hostLobbyCode = lobbyCode;
            Debug.Log($"Joined Lobby with code: {lobbyCode}");
            PrintPlayers(joinedLobby);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return false;
        }
    }
    
    private Player GetPlayer()
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        //로비시스템의 플레이어객체임. 인게임 플레이어 객체와 혼동x
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerId) }
            }
        };
    }

    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log($"Players in Lobby: {lobby.Name}, {lobby.MaxPlayers}, {lobby.LobbyCode}");
        foreach (var player in lobby.Players)
        {
            Debug.Log($"Player: {player.Data["PlayerName"].Value}");
        }
    }
    


    //인게임로비 

    public void JoinAsClient()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 클라이언트로 세션에 참여
        NetworkManager.Singleton.StartClient();
    }

    public void JoinAsHost()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 호스트(서버+클라이언트)로 세션 생성 및 참여
        NetworkManager.Singleton.StartHost();
    }

    public void StartGame()
    {
        //호스트만 게임 시작 가능
        if (!IsHost)
        {
            Debug.LogError("Only the host can start the game!");
            return;
        }

        // 2명 미만이면 시작 못 함
        if (NetworkManager.Singleton.ConnectedClientsList.Count < GameConstants.Network.MinPlayersToStart)
        {
            Debug.LogError("Need at least 2 players to start the game!");
            return;
        }
        
        if (!isCardDataLoaded)
        {
            Debug.LogError("Card data is not loaded!");
            return;
        }
        
        //전부 레디해야 시작함
        if (!GameManager.Instance.SkipLobby)
        {
            if(!AreAllPlayersReady()){
                Debug.LogError("Not all players are ready!");
                return;
            }
        }

        //플레이어 역할 부여
        AssignPlayerRolesServerRpc();
       
        //본인 데이터가 모두 초기화되면, 씬 이동 : 바인딩 콜백
        
    }


    private bool AreAllPlayersReady(){
        PlayerModel[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        foreach(var player in allPlayers){
            if(player.IsOwner && IsHost) continue;
            if(!player.IsReady()){
                Debug.LogError("Player " + player.GetPlayerNickname() + " is not ready!");
                return false;
            }
        }
        return true;
    }


    [ServerRpc]
    private void AssignPlayerRolesServerRpc(ServerRpcParams rpcParams = default)
    {
        
        //모든 플레이어 가져오기
        PlayerModel[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        int totalPlayers = allPlayers.Length;
        //농장 수 결정
        int farmerCount = GetFarmerCountByPlayerCount(totalPlayers);

        //플레이어 목록 섞기
        MathUtils.ShuffleArray(allPlayers);

        //역할 부여
        for(int i=0;i<allPlayers.Length;i++){
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { allPlayers[i].OwnerClientId }
                }
            };
            if(ToggleForcedAllFarmer)
            {
                PlaySFXClientRpc(LobbySFX.mafiaAssignSFX, clientRpcParams);
                allPlayers[i].ChangeRole(PlayerJob.Farmer);
                continue;
            }
            if (i<farmerCount){
                PlaySFXClientRpc(LobbySFX.mafiaAssignSFX, clientRpcParams);
                allPlayers[i].ChangeRole(PlayerJob.Farmer);
            }
            else{
                PlaySFXClientRpc(LobbySFX.citizenAssignSFX, clientRpcParams);
                allPlayers[i].ChangeRole(PlayerJob.Animal);
            }
        }

        //연출 시퀀스 시작
        GameManager.Instance.StartRoleRevealSequenceClientRpc();
    }
    
    [ClientRpc]
    private void PlaySFXClientRpc(LobbySFX sfx, ClientRpcParams rpcParams = default )
    {
        switch (sfx)
        {
            case LobbySFX.mafiaAssignSFX:
                SoundManager.Instance.SFXPlay(mafiaAssignSFX.name,mafiaAssignSFX.clip);
                break;
            case LobbySFX.citizenAssignSFX:
                SoundManager.Instance.SFXPlay(citizenAssignSFX.name,citizenAssignSFX.clip);
                break;
        }
        
    }
    

    private int GetFarmerCountByPlayerCount(int playerCount){
        return playerCount switch
        {
            5 or 6 => 1,
            7 or 8 or 9 => 2,
            10 or 11 or 12 => UnityEngine.Random.Range(0, 2) == 0 ? 2 : 3, // 2 or 3 랜덤
            13 or 14 or 15 => 3,
            _ => 1 // 기본값
        };
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

    [ServerRpc(RequireOwnership = false)]
    private void LoadVillageSceneServerRpc()
    {
        // 모든 클라이언트를 VillageScene으로 이동
        NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Village, LoadSceneMode.Single);
    }

    private void PlayerSpawn()
    {
        // note cba0898: PlayerFactoryManager는 싱글톤이라 null체크 상황이 어색합니다.
        // 그리고 PlayerSpawn() 함수를 쓰기보단 PlayerFactoryManager.Instance.SpawnPlayerServerRpc() 처럼 싱글톤 인스턴스를 쓰는게 좋을 것 같아요.
        PlayerFactoryManager.Instance.SpawnPlayerServerRpc();
    }
}
