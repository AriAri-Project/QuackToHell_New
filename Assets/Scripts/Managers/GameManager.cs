using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
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

    private Transform parent;
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
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

     private void OnLobbySettingButtonClicked()
    {
        // 팝업 띄우기 로직
        UIManager.Instance.ShowPopupUI<LobbySettingPopup>();
    }
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == GameScenes.Lobby && UIManager.Instance.HUDList.OfType<LobbyUI>().FirstOrDefault() != null)
        {
            UIManager.Instance.HUDList.OfType<LobbyUI>().FirstOrDefault().OnClikcedButton_Setting -= OnLobbySettingButtonClicked;
        }
    }
    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
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
            //로비 세팅 버튼이벤트 구독하여, popup 띄우기 
            LobbyUI lobbyUI = UIManager.Instance.HUDList.OfType<LobbyUI>().FirstOrDefault();
            if (lobbyUI != null)
            {
                lobbyUI.OnClikcedButton_Setting += OnLobbySettingButtonClicked;
            }
        }
        if(scene.name == GameScenes.Village)
        {
            UIManager.Instance.ShowHUDUI<VillageUI>("VillageUI");
            UIManager.Instance.ShowHUDUI<SkillButtonUI>("SkillButtonUI");
            
            //시체 청소하기
            CleanPlayerCorpse();
            //움직임 켜기
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            PlayerView playerView= PlayerHelperManager.Instance.GetPlayerViewlByClientId(localClientId);
            playerView.SetIgnoreAllPlayerMoveInputServerRpc(false);
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
        if (assignRoleCanvas != null)
        {
            roleAssignUIReferences = assignRoleCanvas.GetComponent<RoleAssignUIReferences>();
            if (roleAssignUIReferences != null)
            {
                intro = roleAssignUIReferences.Intro;
                showRole = roleAssignUIReferences.ShowRole;
                showRoleText = roleAssignUIReferences.ShowRoleText;
                parent = roleAssignUIReferences.spawnParent;
            }
            assignRoleCanvas.SetActive(false);
        }
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
        PlayerJob myJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerJob();
        TextMeshProUGUI showRoleText = this.showRoleText;
        switch(myJob){
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
        PlayerModel[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        List<PlayerModel> targetPlayers = allPlayers.ToList();

        // 내가 Farmer인 경우, 동료 Farmer만 보여줌
        if (myJob == PlayerJob.Farmer)
        {
            foreach(PlayerModel player in allPlayers){
                if (player.GetPlayerJob() != PlayerJob.Farmer)
                {
                    targetPlayers.Remove(player);    
                }
            }
        }
        
     

        int count = targetPlayers.Count;
        // 최소 5명 ~ 최대 16명 기준으로 현재 인원수의 비율(0.0 ~ 1.0)을 구함
        // 5명이면 t = 0, 16명이면 t = 1, 10명이면 t = 0.45...
        float t = Mathf.InverseLerp(roleAssignUIReferences.minPlayerNum, roleAssignUIReferences.maxPlayerNum, (float)count);
        // 비율에 따라 반지름 결정 (Lerp: Linear Interpolation)
        float currentRadius = Mathf.Lerp(roleAssignUIReferences.minArcRadius, roleAssignUIReferences.maxArcRadius, t);
        float finalGap = roleAssignUIReferences.arcAngleGap; 
        // 전체 각도가 100도를 넘지 않도록 제한 (100도면 화면에 예쁘게 찹니다)
        float maxTotalAngle = 100f;
        
        if (count > 1)
        {
            // 만약 "인원수 * 15도"가 100도를 넘어가면?
            if ((count - 1) * finalGap > maxTotalAngle)
            {
                // 100도 안에 꽉 차게 구겨넣어라 (예: 16명이면 약 6.6도로 자동 축소됨)
                finalGap = maxTotalAngle / (count - 1);
            }
        }
        
        // 계산된 finalGap으로 전체 각도 다시 계산
        float totalAngle = (count - 1) * finalGap;
        float currentAngle = totalAngle / 2f;
        
        for (int i = 0; i < count; i++)
        {
            PlayerModel player = targetPlayers[i];
            
            // 프리팹 생성
            GameObject playerUI  = Instantiate(playerUIPrefab, parent);
            
            // 각도를 라디안으로 변환
            float rad = currentAngle * Mathf.Deg2Rad;
            
            // 위치 계산
            float x = Mathf.Sin(rad) * currentRadius;
            float y = (Mathf.Cos(rad) * currentRadius) - currentRadius;
            
            RectTransform rect = playerUI.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            
            // 데이터 세팅
            playerUI.GetComponentInChildren<TextMeshProUGUI>().text = player.GetPlayerNickname();
            Image playerColor = playerUI.GetComponentInChildren<Image>();
            playerColor.color = AppearanceUtils.GetColorByIndex(player.GetPlayerColorIndex());

            // -----------------------------------------------------------
            // [수정됨] 반드시 계산된 finalGap 하나만 빼야 합니다!
            // -----------------------------------------------------------
            currentAngle -= finalGap; 
        }
        
        //TODO: 시간 늘리기 (테스트용으로 짧게바꿈)
        yield return new WaitForSeconds(2f);
        showRole.SetActive(false);
        onRoleAssignDirectionEnd.Invoke();
    }



    /// <summary>
    /// server전용 로직: server가 호출해야 함
    /// </summary>
    public void AllKillServer()
    {
        if (!IsServer)
        {
            return;
        }

        PlayerModel[] playerModels= PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        foreach (var playerModel in playerModels)
        {
            if (playerModel.GetPlayerJob() != PlayerJob.Animal)
            {
                continue;
            }
            playerModel.HandlePlayerDeathServerRpc();
        }
    }
}
