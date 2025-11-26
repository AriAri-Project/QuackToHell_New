using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using System.Linq;
using Unity.Multiplayer.Playmode;

/// <summary>
/// 게임 전체를 관리하는 중앙 매니저
/// 
/// 책임:
/// - 게임 상태 및 씬 관리 (씬 전환, 게임 시작/종료)
/// - 플레이어 골드 관리 (차감, 증가, 검증)
/// - 게임 규칙 및 밸런스 관리
/// - 전역 이벤트 및 시스템 간 조율
/// - 게임 데이터 저장/로드 관리
/// 
/// 주의: 플레이어 개별 데이터는 PlayerManager를 통해 접근
/// </summary>
public class GameManager : NetworkBehaviour
{

    #region 변수들

    [Header("Put on your mouse to hosttag to view explaination")]
    [SerializeField] private bool skipLobby = true;
    public bool SkipLobby { get => skipLobby; }
    [Tooltip("multiplay play mode에 들어가면 창마다 태그부여가 가능함. 태그부여 후, 호스트를 부여할 태그를 입력하기.(skipLobby체크했으면 레디여부 체크 안 합니다. 호스트에서 바로 startgame버튼 누르시면 됩니다.)")]

    [SerializeField] private string hostTag = "0";
    //-------------- ----
    [Header("AssignRole UI")]
    private GameObject assignRoleCanvas;
    private RoleAssignUIReferences roleAssignUIReferences;
    private GameObject intro;
    private GameObject showRole;
    private TextMeshProUGUI showRoleText;

    private GameObject[] playerSlot;
    [SerializeField]
    private GameObject playerUIPrefab;

    public Action onRoleAssignDirectionEnd;

    #endregion

    #region 싱글톤
    public static GameManager Instance => SingletonHelper<GameManager>.Instance;

    private void Awake()
    {
        SingletonHelper<GameManager>.InitializeSingleton(this);
    }
    #endregion

    private void Start()
    {
        //persistent씬에서 시작해서 바로 홈씬으로 전환
        if (skipLobby)
        {
            SceneManager.LoadScene(GameScenes.Lobby, LoadSceneMode.Single);
            string[] myTags = CurrentPlayer.ReadOnlyTags();
            bool isHost = myTags.Contains(hostTag);
            if (isHost)
            {
                LobbyManager.Instance.JoinAsHost();
            }
            else
            {
                LobbyManager.Instance.JoinAsClient();
            }
        }
        else
        {
            SceneManager.LoadScene(GameScenes.Home, LoadSceneMode.Single);
        }
        
        //씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }
    
   

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == GameScenes.Home)
        {
            UIManager.Instance.ShowHUDUI<HomeUI>("HomeUI");
        }
        if (scene.name == GameScenes.Lobby) // 또는 해당 씬 이름
        {
            UIManager.Instance.ShowHUDUI<LobbyUI>("LobbyUI");
            FindLobbyUIElements();
        }
        if(scene.name == GameScenes.Village)
        {
            UIManager.Instance.ShowHUDUI<VillageUI>("VillageUI");
            //시체 청소하기
            CleanPlayerCorpse();
            //움직임 켜기
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            PlayerPresenter playerPresenter =  PlayerHelperManager.Instance.GetPlayerPresenterByClientId(localClientId);
            playerPresenter.SetAllPlayerIgnoreMoveInput(true);
        }
    }

    private void CleanPlayerCorpse(){
        //시체찾기: PlayerCorpse 태그가 붙은 오브젝트 찾기
        GameObject[] playerCorpses = GameObject.FindGameObjectsWithTag(GameTags.PlayerCorpse);
        foreach(GameObject playerCorpse in playerCorpses){
            Destroy(playerCorpse);
        }
    }
    private void FindLobbyUIElements()
    {
        assignRoleCanvas = GameObject.FindWithTag(GameTags.UI_RoleAssignCanvas);
        roleAssignUIReferences = assignRoleCanvas.GetComponent<RoleAssignUIReferences>();
        if (assignRoleCanvas != null)
        {
            intro = roleAssignUIReferences.Intro;
            showRole = roleAssignUIReferences.ShowRole;
            showRoleText = roleAssignUIReferences.ShowRoleText;
            playerSlot = roleAssignUIReferences.PlayerSlot;
        }
        assignRoleCanvas.SetActive(false);
    }


    /// <summary>
    /// 서버에서 특정 클라이언트의 골드를 차감하는 RPC
    /// </summary>
    /// <param name="clientId">골드를 차감할 클라이언트 ID</param>
    /// <param name="amount">차감할 골드 양</param>
    [ServerRpc(RequireOwnership = false)]
    public void DeductPlayerGoldServerRpc(ulong clientId, int amount, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized gold deduction attempt. Requested: {clientId}, Actual: {requesterClientId}");
            return;
        }
        
        //플레이어 골드차감
        PlayerModel player = PlayerHelperManager.Instance.GetPlayerModelByClientId(clientId);
        DebugUtils.AssertNotNull(player, "PlayerModel", this);
            
        PlayerStatusData currentStatus = player.PlayerStatusData.Value;
        currentStatus.gold -= amount;
        player.PlayerStatusData.Value = currentStatus;
    }
    
    /// <summary>
    /// 역할 공개 시퀀스 시작
    /// </summary>
    [ClientRpc]
    public void StartRoleRevealSequenceClientRpc(){
        StartCoroutine(RoleRevealCoroutine());
    }
    private IEnumerator RoleRevealCoroutine(){
        //캔버스 켜기
        assignRoleCanvas.SetActive(true);
        //1. 인트로 키기
        intro.SetActive(true);
        //TODO: 시간 늘리기 (테스트용으로 짧게바꿈)
        yield return new WaitForSeconds(1f);
        intro.SetActive(false);
        //2. 역할 공개
        showRole.SetActive(true);
            //2-1. 역할공개 text 세팅하기
            //로컬플레이어 역할에 따라 텍스트 세팅
            PlayerJob playerJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerJob();
            TextMeshProUGUI showRoleText = this.showRoleText;
            switch(playerJob){
                case PlayerJob.Farmer:
                    showRoleText.text = "Farmer";
                    showRoleText.color = Color.red;
                    break;
                case PlayerJob.Animal:
                    showRoleText.text = "Animal";
                    showRoleText.color = Color.blue;
                    break;
                default:
                    showRoleText.text = "UnknownRole";
                    showRoleText.color = Color.white;
                    break;
            }
            //2-2. PlayerSlot에 PlayerUIPrefab 생성하기
            //플레이어 수만큼 플레이어 프리팹 생성
            PlayerModel[] players = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
            int i = 0;
            foreach(PlayerModel player in players){
                if (playerJob == PlayerJob.Farmer)
                {
                    if (playerJob != player.GetPlayerJob())
                        continue;
                }
                GameObject playerUI = Instantiate(playerUIPrefab, playerSlot[i].transform);
                playerUI.transform.position = playerSlot[i].transform.position;
                playerUI.transform.rotation = playerSlot[i].transform.rotation;
                playerUI.transform.localScale = playerSlot[i].transform.localScale;
                //플레이어 닉네임 할당
                playerUI.GetComponentInChildren<TextMeshProUGUI>().text = player.GetPlayerNickname();
                //플레이어 색상 할당
                Image playerColor =  playerUI.GetComponentInChildren<Image>();
                int playerColorIndex = player.GetPlayerColorIndex();
                playerColor.color = ColorUtils.GetColorByIndex(playerColorIndex);
                i++;
            }
            
        //TODO: 시간 늘리기 (테스트용으로 짧게바꿈)
        yield return new WaitForSeconds(2f);
        showRole.SetActive(false);
        onRoleAssignDirectionEnd.Invoke();
    }
}
