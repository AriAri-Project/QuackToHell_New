using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private TextMeshProUGUI RoleExplainText;
    private Image FootholdImage;
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
            //UIManager.Instance.ShowHUDUI<MobileJoystickUI>("MobileJoystickUI");
            FindLobbyUIElements();
            //데이터 초기화
            if (IsServer)
            {
                RunServerSideGameRestartInitialization();
                GameRestartClientRpc();
            }
        }
        if(scene.name == GameScenes.Village)
        {
            //유아이 초기화
            UIManager.Instance.ShowHUDUI<VillageUI>("VillageUI");
            UIManager.Instance.ShowHUDUI<SkillButtonUI>("SkillButtonUI");
            //UIManager.Instance.ShowHUDUI<MobileJoystickUI>("MobileJoystickUI");
            //시체 청소하기
            CleanPlayerCorpse();
            //움직임 켜기
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            PlayerView playerView= PlayerHelperManager.Instance.GetPlayerViewlByClientId(localClientId);
            playerView.SetIgnoreAllPlayerMoveInputServerRpc(false);
            //레디 풀기
            PlayerModel localPlayer = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
            localPlayer.ToggleReady();
            //쿨타임 zero로 시작
            if (localPlayer.GetPlayerCurrentJob()==PlayerJob.Farmer)
            {
                FarmerStrategy farmerStrategy = localPlayer.GetComponent<FarmerStrategy>();
                farmerStrategy.SetCooltimeZero();
            }
        }
        if (scene.name == GameScenes.Court)
        {
            // 서버에서만 Trial 라운드 초기화
            if (IsServer && TrialManager.Instance != null)
            {
                TrialManager.Instance.Initialize();
            }
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
                RoleExplainText =  roleAssignUIReferences.RoleExplainText;
                FootholdImage = roleAssignUIReferences.FootholdImage;
                
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
    /// 서버에서 특정 클라이언트의 골드를 증가시키는 RPC (판매용)
    /// </summary>
    /// <param name="clientId">골드를 증가시킬 클라이언트 ID</param>
    /// <param name="amount">증가할 골드 양</param>
    public void AddPlayerGoldServer(ulong clientId, int amount)
    {
        if (!IsServer) return;

        PlayerModel player = PlayerHelperManager.Instance.GetPlayerModelByClientId(clientId);
        if (player == null) return;

        PlayerStatusData currentStatus = player.PlayerStatusData.Value;
        currentStatus.gold += amount;
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
        PlayerJob myJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerCurrentJob();
        TextMeshProUGUI showRoleText = this.showRoleText;
        switch(myJob){
            case PlayerJob.Farmer:
                showRoleText.text = "당신은 농장주입니다";
                RoleExplainText.text = "동물들을 처치하고 재판에서 끝까지 살아남아 당신의 농장을 되찾으세요.";
                showRoleText.color = new Color(1f, 0.3608f, 0.3608f, 1f);
                FootholdImage.sprite = Resources.Load<Sprite>("Sprites/AssignRole/FarmerFoothold");
                break;
            case PlayerJob.Animal:
                showRoleText.text = "당신은 동물입니다";
                RoleExplainText.text = "농장에 숨어든 농장주를 찾아내 재판에서 처형시켜 동물농장의 평화를 되찾으세요.";
                showRoleText.color = new Color(0.3608f, 1f, 0.4039f, 1f);
                FootholdImage.sprite = Resources.Load<Sprite>("Sprites/AssignRole/AnimalFoothold");
                break;
            default:
                showRoleText.text = "UnknownRole";
                showRoleText.color = Color.white;
                break;
        }
        //2-2. 플레이어 수만큼 프리팹 생성하기 (아래로 뾰족한 V자 배치)
        PlayerModel[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        List<PlayerModel> targetPlayers = allPlayers.ToList();

        // 내가 Farmer인 경우, 동료 Farmer만 보여줌
        if (myJob == PlayerJob.Farmer)
        {
            foreach(PlayerModel player in allPlayers)
            {
                if (player.GetPlayerCurrentJob() != PlayerJob.Farmer)
                {
                    targetPlayers.Remove(player);    
                }
            }
        }

        int count = targetPlayers.Count;
        
        // 화면 크기 기준 (1920x1080 기준, 안전 영역)
        float screenWidth = 1600f;
        float screenHeight = 800f;
        
        // V자 배치에서 최대 깊이 (몇 층까지 내려가는지)
        int maxDepth = (count - 1 + 1) / 2;
        
        // 동적 스케일 및 간격 계산
        float baseXSpacing = 180f;
        float baseYSpacing = 55f;
        float nicknameYJitter = 30f;
        
        // 필요한 총 너비/높이 계산
        float neededWidth = maxDepth * baseXSpacing * 2;
        float neededHeight = maxDepth * baseYSpacing;
        
        // 화면에 맞추기 위한 스케일 계산
        float widthScale = (neededWidth > 0) ? Mathf.Min(1f, screenWidth / neededWidth) : 1f;
        float heightScale = (neededHeight > 0) ? Mathf.Min(1f, screenHeight / neededHeight) : 1f;
        float scaleFactor = Mathf.Min(widthScale, heightScale);
        scaleFactor = Mathf.Clamp(scaleFactor, 0.3f, 1f);
        
        float xSpacing = baseXSpacing * scaleFactor;
        float ySpacing = baseYSpacing * scaleFactor;
        float uiScale = scaleFactor;
        
        // 전체 배치의 시작 y 위치 (아래로 뾰족한 V자이므로 아래에서 시작)
        float startYOffset = 100f * scaleFactor;
        
        for (int i = 0; i < count; i++)
        {
            SpawnPlayerUIVShape(targetPlayers[i], i, xSpacing, ySpacing, uiScale, startYOffset, nicknameYJitter);
        }
        
        void SpawnPlayerUIVShape(PlayerModel player, int index, float xSpace, float ySpace, float scale, float yOffset, float nickYJitter)
        {
            GameObject playerUI = Instantiate(playerUIPrefab, parent);
            
            float x, y;
            
            if (index == 0)
            {
                // 맨 아래 중앙 (뾰족한 부분)
                x = 0;
                y = yOffset;
            }
            else
            {
                int pairIndex = (index - 1) / 2;
                bool isLeft = (index - 1) % 2 == 0;
                
                x = (pairIndex + 1) * xSpace * (isLeft ? -1 : 1);
                y = yOffset + (pairIndex + 1) * ySpace;
            }
            
            RectTransform rect = playerUI.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = new Vector3(scale, scale, 1f);
            
            TextMeshProUGUI nicknameText = playerUI.GetComponentInChildren<TextMeshProUGUI>();
            nicknameText.text = player.GetPlayerNickname();
            
            // 닉네임 y 지그재그 (겹침 방지)
            RectTransform nicknameRect = nicknameText.GetComponent<RectTransform>();
            float baseNicknameY = nicknameRect.anchoredPosition.y;
            float adjustedNicknameY = baseNicknameY + (index % 2 == 0 ? nickYJitter : -nickYJitter);
            nicknameRect.anchoredPosition = new Vector2(nicknameRect.anchoredPosition.x, adjustedNicknameY);
            
            Image playerColor = playerUI.GetComponentInChildren<Image>();
            playerColor.color = AppearanceUtils.GetColorByIndex(player.GetPlayerColorIndex());
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
            if (playerModel.GetPlayerCurrentJob() != PlayerJob.Animal)
            {
                continue;
            }
            playerModel.HandlePlayerDeathServerRpc();
        }
    }

    #region 게임 결과 확인

    // =========================
    //  게임 종료 상태 관리
    // =========================
    private bool _gameEnded = false;

    // 반란승(솔로승) 승자(서버에서만 세팅). 없으면 ulong.MaxValue
    private ulong _rebelWinnerClientId = ulong.MaxValue;

    /// <summary>
    /// (서버) 반란승 성공한 플레이어를 기록해둠.
    /// </summary>
    public void NotifyRebelVictoryServer(ulong rebelClientId)
    {
        if (!IsServer) return;
        _rebelWinnerClientId = rebelClientId;
    }

    /// <summary>
    /// (서버) 승리 조건을 체크하고, 조건 만족 시 결과 UI를 전체 클라에 표시.
    /// </summary>
    public bool TryEndGameServer()
    {
        if (!IsServer) return false;
        if (_gameEnded) return false;

        // 1) 전체 플레이어 수집
        PlayerModel[] players = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        Debug.Log($"All Player Count: {players.Length}");
        if (players == null || players.Length == 0) return false;

        // 2) 생존자 분류
        var aliveFarmers = new List<PlayerModel>();
        var aliveAnimals = new List<PlayerModel>();
        var deadPlayers = new List<PlayerModel>();

        foreach (var p in players)
        {
            if (p == null) continue;

            bool isAlive = (p.GetPlayerAliveState() == PlayerLivingState.Alive);
            if (!isAlive)
            {
                deadPlayers.Add(p);
                continue;
            }
            
            var job = GetFactionJob(p); 
            if (job == PlayerJob.Farmer) aliveFarmers.Add(p);
            else if (job == PlayerJob.Animal) aliveAnimals.Add(p);
            else
            {
                // 직업이 더 늘어나면 여기서 시민/농장주 진영으로 분류 규칙 추가
                // 일단 "시민측"으로 취급하고 싶으면 aliveFarmers에 넣는 방식도 가능
                // ↑ ?? alive Animals아닐까요. 바꿀게요(유진)
                aliveAnimals.Add(p);
            }
        }

        // 3) 승리 조건 체크 (우선순위: 반란승 > 진영 승)
        if (_rebelWinnerClientId != ulong.MaxValue)
        {
            var rebel = FindByClientId(players, _rebelWinnerClientId);
            if (rebel != null && rebel.GetPlayerAliveState() == PlayerLivingState.Alive)
            {
                _gameEnded = true;

                var payload = BuildPayload(
                    winType: EWinType.RebelSolo,
                    winReason: "반란승 조건 달성",
                    winners: new List<PlayerModel> { rebel },
                    losers: players,
                    winnerFilter: (pm) => pm == rebel
                );

                BroadcastResult(payload);
                return true;
            }
        }

        // 3-2) "살아있는 플레이어" 기준 진영 승리
        // - 살아있는 Animal이 0이고, 살아있는 Farmer가 1 이상이면 => Farmer 승 (Farmer가 2명이어도 OK)
        // - 살아있는 Farmer가 0이고, 살아있는 Animal이 1 이상이면 => Animal 승
        if (aliveAnimals.Count == 0 && aliveFarmers.Count > 0)
        {
            _gameEnded = true;

            // 승자는 "진영(Farmer)" 기준으로 전체(죽은 Farmer 포함) 다 넣기
            var winners = new List<PlayerModel>();
            foreach (var p in players)
            {
                if (p == null) continue;
                if (GetFactionJob(p) == PlayerJob.Farmer)
                    winners.Add(p);
            }

            var payload = BuildPayload(
                winType: EWinType.Mafia,
                winReason: "농장주만 생존",
                winners: winners,
                losers: players,
                winnerFilter: (pm) => pm != null && GetFactionJob(pm) == PlayerJob.Farmer
            );

            BroadcastResult(payload);
            return true;
        }

        if (aliveFarmers.Count == 0 && aliveAnimals.Count > 0)
        {
            _gameEnded = true;

            // 승자는 "진영(Animal)" 기준으로 전체(죽은 Animal 포함) 다 넣기
            var winners = new List<PlayerModel>();
            foreach (var p in players)
            {
                if (p == null) continue;
                if (GetFactionJob(p) == PlayerJob.Animal)
                    winners.Add(p);
            }

            var payload = BuildPayload(
                winType: EWinType.Citizens,
                winReason: "동물만 생존",
                winners: winners,
                losers: players,
                winnerFilter: (pm) => pm != null && GetFactionJob(pm) == PlayerJob.Animal
            );

            BroadcastResult(payload);
            return true;
        }

        if (CheckLastPlayerAliveAndEndGame())
            return true;

        // 아직 게임 안 끝남
        return false;
    }

    public bool CheckLastPlayerAliveAndEndGame()
    {
        if (!IsServer) return false;
        if (_gameEnded) return false;

        // 1) 전체 플레이어 수집
        PlayerModel[] players = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        if (players == null || players.Length == 0) return false;

        // 2) 생존자 찾기
        PlayerModel lastAlive = null;
        int aliveCount = 0;

        foreach (var p in players)
        {
            if (p == null) continue;
            if (p.GetPlayerAliveState() == PlayerLivingState.Alive)
            {
                aliveCount++;
                lastAlive = p;
            }
        }

        Debug.Log($"[LastCheck] aliveCount = {aliveCount}");

        // 3) 1명 이하일 때 종료 처리
        if (aliveCount <= 1)
        {
            _gameEnded = true;

            // 승리 진영 결정: lastAlive가 있으면 그 진영 ALL
            PlayerJob winningJob = PlayerJob.Animal;
            EWinType winType = EWinType.Citizens;
            string reason = "최후의 생존자";

            if (lastAlive != null)
            {
                winningJob = GetFactionJob(lastAlive);

                if (winningJob == PlayerJob.Farmer) winType = EWinType.Mafia;
                else winType = EWinType.Citizens;
            }
            else
            {
                reason = "생존자 없음";
                winningJob = PlayerJob.Animal;  // 전원 사망 시 규칙(임시, 사용X)
                winType = EWinType.Citizens;
            }

            // winners: 진영 기준으로 전원 모으기
            var winners = new List<PlayerModel>();
            foreach (var p in players)
            {
                if (p == null) continue;
                if (GetFactionJob(p) == winningJob)
                    winners.Add(p);
            }

            var payload = BuildPayload(
                winType: winType,
                winReason: reason,
                winners: winners,
                losers: players,
                winnerFilter: (pm) => pm != null && GetFactionJob(pm) == winningJob
            );

            BroadcastResult(payload);
            return true;
        }

        return false;
    }

    // =========================
    //  내부 헬퍼들
    // =========================

    private void BroadcastResult(GameResultPayload payload)
    {
        // ResultBroadcaster는 씬에 있어야 함 (ResultScene이든 현재 씬이든)
        var broadcaster = ResultBroadcaster.Instance;
        if (broadcaster == null)
        {
            broadcaster = FindFirstObjectByType<ResultBroadcaster>(FindObjectsInactive.Include);
        }
        if (broadcaster == null)
        {
            Debug.LogError("[GameManager] ResultBroadcaster not found in scene!");
            return;
        }

        broadcaster.EndGameAndShowResult(payload);
    }

    private static PlayerModel FindByClientId(PlayerModel[] players, ulong clientId)
    {
        foreach (var p in players)
        {
            if (p == null) continue;
            var no = p.GetComponent<NetworkObject>();
            if (no != null && no.OwnerClientId == clientId) return p;
        }
        return null;
    }

    /// <summary>
    /// PlayerModel의 생존 여부를 "최대한 안전하게" 추정
    /// - 프로젝트마다 필드명이 달라서, 흔한 이름(IsDead/IsAlive/isDead/isAlive)을 반사(reflection)로 탐색
    /// - 못 찾으면 "일단 살아있다"로 처리
    /// </summary>
    private static bool IsAliveGuess(PlayerModel p)
    {
        if (p == null) return false;

        // 1) public property 우선 탐색
        var t = p.GetType();

        // IsAlive
        var propAlive = t.GetProperty("IsAlive", BindingFlags.Public | BindingFlags.Instance);
        if (propAlive != null && propAlive.PropertyType == typeof(bool))
            return (bool)propAlive.GetValue(p);

        // IsDead
        var propDead = t.GetProperty("IsDead", BindingFlags.Public | BindingFlags.Instance);
        if (propDead != null && propDead.PropertyType == typeof(bool))
            return !(bool)propDead.GetValue(p);

        // 2) field 탐색
        var fieldAlive = t.GetField("isAlive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldAlive != null && fieldAlive.FieldType == typeof(bool))
            return (bool)fieldAlive.GetValue(p);

        var fieldDead = t.GetField("isDead", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldDead != null && fieldDead.FieldType == typeof(bool))
            return !(bool)fieldDead.GetValue(p);

        // 3) fallback
        return true;
    }

    // ① 진영 판단용 직업 반환
    private static PlayerJob GetFactionJob(PlayerModel p)
    {
        if (p == null) return PlayerJob.None;

        PlayerStatusData status = p.PlayerStatusData.Value;

        // Ghost인데 초기 직업이 설정돼 있으면 → 초기 직업 기준
        if (status.currentJob == PlayerJob.Ghost && status.initialJob != PlayerJob.None)
        {
            Debug.Log($"{p.ClientId}의 직업은 {status.initialJob}");
            return status.initialJob;
        }

        // Ghost인데 초기 직업이 None이면 → 시민 진영(Animal)으로 취급
        if (status.currentJob == PlayerJob.Ghost && status.initialJob == PlayerJob.None)
        {
            Debug.Log($"{p.ClientId}의 직업은 Animal");
            return PlayerJob.Animal;
        }

        // 그 외엔 현재 job 그대로
        Debug.Log($"{p.ClientId}의 직업은 {status.currentJob}");
        return status.currentJob;
    }
    
    // ② 결과 화면 표시용 직업 문자열
    private static string GetDisplayJobName(PlayerModel p)
    {
        if (p == null) return "Unknown";

        PlayerStatusData status = p.PlayerStatusData.Value;

        // Ghost라도 초기 직업이 있으면 → 초기 직업 이름으로 표시
        if (status.currentJob == PlayerJob.Ghost && status.initialJob != PlayerJob.None)
        {
            return status.initialJob.ToString();
        }

        // 그 외에는 현재 job 이름 그대로
        return status.currentJob.ToString();
    }
    
    private static GameResultPayload BuildPayload(
        EWinType winType,
        string winReason,
        List<PlayerModel> winners,
        PlayerModel[] losers,
        Func<PlayerModel, bool> winnerFilter
    )
    {
        var payload = new GameResultPayload
        {
            WinType = winType,
            WinReason = new Unity.Collections.FixedString128Bytes(winReason),
        };

        // winners 슬롯 채우기 (최대 4명까지)
        var w = new List<PlayerModel>();
        foreach (var p in winners)
        {
            if (p == null) continue;
            if (!w.Contains(p)) w.Add(p);
            if (w.Count >= 4) break;
        }

        SetWinnerSlot(ref payload, 0, w, 0);
        SetWinnerSlot(ref payload, 1, w, 1);
        SetWinnerSlot(ref payload, 2, w, 2);
        SetWinnerSlot(ref payload, 3, w, 3);

        // losers 슬롯 채우기: winnerFilter가 false인 사람 중 최대 4명
        var l = new List<PlayerModel>();
        foreach (var p in losers)
        {
            if (p == null) continue;
            if (winnerFilter != null && winnerFilter(p)) continue;
            l.Add(p);
            if (l.Count >= 4) break;
        }

        SetLoserSlot(ref payload, 0, l, 0);
        SetLoserSlot(ref payload, 1, l, 1);
        SetLoserSlot(ref payload, 2, l, 2);
        SetLoserSlot(ref payload, 3, l, 3);

        return payload;
    }

    private static void SetWinnerSlot(ref GameResultPayload payload, int slot, List<PlayerModel> list, int idx)
    {
        bool has = idx < list.Count && list[idx] != null;
        var info = has
            ? new ResultPlayerInfo(list[idx].GetPlayerNickname(), GetDisplayJobName(list[idx]))
            : default;

        switch (slot)
        {
            case 0: payload.HasWinner0 = has; payload.Winner0 = info; break;
            case 1: payload.HasWinner1 = has; payload.Winner1 = info; break;
            case 2: payload.HasWinner2 = has; payload.Winner2 = info; break;
            case 3: payload.HasWinner3 = has; payload.Winner3 = info; break;
        }
    }

    private static void SetLoserSlot(ref GameResultPayload payload, int slot, List<PlayerModel> list, int idx)
    {
        bool has = idx < list.Count && list[idx] != null;
        var info = has
            ? new ResultPlayerInfo(list[idx].GetPlayerNickname(), GetDisplayJobName(list[idx]))
            : default;

        switch (slot)
        {
            case 0: payload.HasLoser0 = has; payload.Loser0 = info; break;
            case 1: payload.HasLoser1 = has; payload.Loser1 = info; break;
            case 2: payload.HasLoser2 = has; payload.Loser2 = info; break;
            case 3: payload.HasLoser3 = has; payload.Loser3 = info; break;
        }
    }

    #endregion

    /// <summary>
    /// 서버에서만 호출. NetworkVariable/NetworkList 초기화 (동기화됨).
    /// </summary>
    private void RunServerSideGameRestartInitialization()
    {
        if (!IsServer) return;

        _gameEnded = false;
        _rebelWinnerClientId = ulong.MaxValue;
        
        if (Court.VoteModel.Instance != null)
            Court.VoteModel.Instance.Initialize();
        
        if (DeckManager.Instance != null)
            DeckManager.Instance.Initialize();

        PlayerModel[] players = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
        if (players == null) return;

        foreach (PlayerModel playerModel in players)
        {
            if (playerModel == null) continue;
            playerModel.Initialize();

            CardInventoryModel inv = playerModel.GetComponent<CardInventoryModel>();
            if (inv != null)
                inv.Initialize();
        }
    }
    /// <summary>
    /// Village 로드 시 모든 클라이언트에서 로컬 전용 상태/UI 초기화.
    /// </summary>
    [ClientRpc]
    private void GameRestartClientRpc()
    {
        Debug.Log("클라상태초기화");

        if (TrialManager.Instance != null)
            TrialManager.Instance.Initialize();

        if (UIManager.Instance != null)
            UIManager.Instance.Initialize();

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerModel localPlayer = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
        Debug.Log($"GameRestartClientRpc: localPlayer == null? {localPlayer == null}");  // 추가

        if (localPlayer != null)
        {
            PlayerView playerView = localPlayer.GetComponent<PlayerView>();
            Debug.Log($"GameRestartClientRpc: playerView == null? {playerView == null}"); 
            if (playerView != null) playerView.Initialize();

            MinigameController minigame = localPlayer.GetComponent<MinigameController>();
            if (minigame != null) minigame.Initialize();

            SabotageNetworkManager sabotage = localPlayer.GetComponent<SabotageNetworkManager>();
            if (sabotage != null) sabotage.Initialize();
        }
    }
}
