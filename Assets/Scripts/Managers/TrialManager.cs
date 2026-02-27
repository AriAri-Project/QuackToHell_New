using Court;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TrialManager : NetworkBehaviour
{
    [Header("UI References")]
    private GameObject convocationOfTrialCanvas;
    private GameObject convocationOfTrialPanel;
    private GameObject corpseTextObject;
    private Image reporterImage;
    private ulong reporterClientId; // 해당 정보는 서버에만 저장됨
    private ulong _pendingExecutedClientId = ulong.MaxValue;
    private TextMeshProUGUI _killTextTMP;

    public ulong ReporterClientId
    {
        get => reporterClientId;
    }

    //TODO:  하드코딩 개선

    private string reporterPlayerText = "Not_Set";
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    #region 싱글톤 코드
    //싱글톤 코드
    private static TrialManager _instance;
    public static TrialManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TrialManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TrialManager");
                    _instance = go.AddComponent<TrialManager>();
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
    #endregion


    [ServerRpc(RequireOwnership = false)]
    public void TryTrialServerRpc(ulong reporterClientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;

        if (RebellionVictoryService.TryTriggerRebellion(reporterClientId))
            return;

        //모든 플레이어의 움직임 멈춤
        ulong localCliendId = NetworkManager.Singleton.LocalClientId;
        PlayerView playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(localCliendId);
        playerView.SetIgnoreAllPlayerMoveInputServerRpc(true);

        // 1. 서버에서 리포터 클라이언트 ID 검증
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(reporterClientId))
        {
            Debug.LogError($"Server: Reporter client {reporterClientId} not found in connected clients");
            return;
        }

        // 2. 서버에서 리포터가 실제로 살아있는 플레이어인지 검증
        PlayerModel reporterModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterClientId);
        DebugUtils.AssertNotNull(reporterModel, "ReporterModel", this);

        if (reporterModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogError($"Server: Dead player {reporterClientId} cannot start trial");
            return;
        }

        // 3. 서버에서 이미 재판이 진행 중인지 검증
        if (convocationOfTrialPanel != null && convocationOfTrialPanel.activeInHierarchy)
        {
            Debug.LogWarning($"Server: Trial already in progress, ignoring request from {reporterClientId}");
            return;
        }

        this.reporterClientId = reporterClientId;

        if (RebellionVictoryService.TryTriggerRebellion(reporterClientId)) // 재판 안 열리게 처리(임시)
        {
            return;
        }

        // 4. 재판 시작 (서버가 권위적 정보로 처리)
        TrialResultClientRpc(reporterClientId);
    }

    [ClientRpc]
    public void TrialResultClientRpc(ulong reporterClientId)
    {
        convocationOfTrialPanel.SetActive(true);

        InjectReporterColor(reporterClientId);
        InjectReporterPlayerText(reporterClientId);


        //5초뒤 씬 이동
        Invoke("LoadCourtScene", 5f);
    }

    private void LoadCourtScene()
    {
        if (!IsHost)
        {
            return;
        }

        //재판장 씬으로 이동
        NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Court, LoadSceneMode.Single);
    }
    private void InjectReporterPlayerText(ulong reporterCliendId)
    {
        PlayerModel reporterModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterCliendId);
        reporterPlayerText = reporterModel.PlayerStatusData.Value.Nickname.ToString();
        TextMeshProUGUI reporterTextTMP = corpseTextObject.GetComponent<TextMeshProUGUI>();
        reporterTextTMP.text = "ReporterPlayer: " + reporterPlayerText;
    }
    private void InjectReporterColor(ulong reporterClientId)
    {
        PlayerModel reporterModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterClientId);
        PlayerAppearanceData playerAppearanceData = reporterModel.PlayerAppearanceData.Value;
        int colorIndex = playerAppearanceData.ColorIndex;
        reporterImage.color = AppearanceUtils.GetColorByIndex(colorIndex);

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameScenes.Village)
        {
            convocationOfTrialCanvas = GameObject.FindGameObjectWithTag(GameTags.UI_ConvocationOfTrialCanvas);
            if (DebugUtils.EnsureNotNull(convocationOfTrialCanvas, "convocationOfTrialCanvas", this))
            {
                convocationOfTrialPanel = convocationOfTrialCanvas.transform.GetChild(0).gameObject;
                if (convocationOfTrialPanel != null)
                {
                    reporterImage = convocationOfTrialPanel.transform.GetChild(0).GetComponent<Image>();
                    corpseTextObject = convocationOfTrialPanel.transform.GetChild(1).gameObject;
                }
            }

            // note cba0898: 이것은 무엇..? 그때그때 검증하는 것으로 바꾸시긔.. convocationOfTrialCanvas는 왜 두번..?
            // 검증
            if (DebugUtils.AssertNotNull(convocationOfTrialCanvas, "ConvocationOfTrialCanvas", this))
            {
                if (DebugUtils.AssertNotNull(convocationOfTrialPanel, "ConvocationOfTrialPanel", this))
                {
                    DebugUtils.AssertNotNull(reporterImage, "ReporterImage", this);
                    DebugUtils.AssertNotNull(corpseTextObject, "CorpseTextObject", this);
                }
            }
        }

        if (scene.name == GameScenes.Kill)
        {
            // 모든 플레이어 가져오기
            PlayerView[] players = PlayerHelperManager.Instance.GetAllPlayers<PlayerView>();

            foreach (var player in players)
            {
                if (player == null) continue;

                PlayerModel model = player.GetComponent<PlayerModel>();
                if (model == null) continue;

                // 처형 대상이면
                if (model.ClientId == _pendingExecutedClientId)
                {
                    player.SetPlayerVisibility(true);

                    // 중앙으로 이동
                    player.transform.position = Vector3.zero;
                }
                else
                {
                    // 나머지는 숨김
                    player.SetPlayerVisibility(false);
                }
            }

            if (IsServer)
                StartCoroutine(Co_HandleKillScene());
        }
    }

    private IEnumerator Co_HandleKillScene()
    {
        yield return new WaitForSeconds(1f);

        ShowExecutionClientRpc(_pendingExecutedClientId);

        yield return new WaitForSeconds(1f);

        PlayerModel executedModel =
            PlayerHelperManager.Instance.GetPlayerModelByClientId(_pendingExecutedClientId);

        if (executedModel != null)
        {
            PlayerDeadState deadState =
                executedModel.GetComponent<PlayerDeadState>();

            if (deadState != null)
            {
                deadState.TriggerWalkAnimation();
            }
        }

        executedModel.HandlePlayerDeathServerRpc();

        yield return new WaitForSeconds(0.2f);

        if (GameManager.Instance.CheckLastPlayerAliveAndEndGame())
        {
            yield break; 
        }

        yield return new WaitForSeconds(2f);

        bool ended = GameManager.Instance.TryEndGameServer();

        if (!ended)
        {
            ended = GameManager.Instance.CheckLastPlayerAliveAndEndGame();
        }

        if (!ended)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Village, LoadSceneMode.Single);
        }
    }

    #region 재판 결과 도출을 위한 조건 체크

    private bool IsMafia(ulong clientId)
    {
        PlayerModel model =
            PlayerHelperManager.Instance.GetPlayerModelByClientId(clientId);

        if (model == null) return false;

        return model.GetPlayerJob() == PlayerJob.Farmer;
    }

    private int CountAliveMafia()
    {
        int count = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerModel model =
                PlayerHelperManager.Instance.GetPlayerModelByClientId(client.ClientId);

            if (model == null) continue;

            if (model.GetPlayerAliveState() == PlayerLivingState.Alive &&
                model.GetPlayerJob() == PlayerJob.Farmer)
            {
                count++;
            }
        }

        return count;
    }
    private int CountAliveCitizen()
    {
        int count = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerModel model =
                PlayerHelperManager.Instance.GetPlayerModelByClientId(client.ClientId);

            if (model == null) continue;

            if (model.GetPlayerAliveState() == PlayerLivingState.Alive &&
                model.GetPlayerJob() == PlayerJob.Animal)
            {
                count++;
            }
        }

        return count;
    }
    #endregion 

    [ClientRpc]
    public void RebellionVictoryClientRpc(ulong winnerClientId)
    {
        Debug.Log($"[Rebellion] Winner = {winnerClientId}");

        // 모든 플레이어 이동 잠금 (기존 TrialResultClientRpc 흐름과 유사)
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerView playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(localClientId);
        playerView.SetIgnoreAllPlayerMoveInputServerRpc(true);

        // TODO: 결과 화면/결과 씬이 있으면 여기서 연결
        // if (NetworkManager.Singleton.IsHost)
        //     NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Result, LoadSceneMode.Single);
    }

    [ClientRpc]
    private void ShowExecutionClientRpc(ulong executedClientId)
    {
        PlayerModel model = PlayerHelperManager.Instance.GetPlayerModelByClientId(executedClientId);

        string nickname = (model != null) ? model.GetPlayerNickname() : executedClientId.ToString();
        string msg = $"{nickname}님이 처형되었습니다.";

        Debug.Log($"<color=red>[Execution]</color> {msg}");

        // KillScene에서 텍스트가 잡혀있으면 화면에도 표시
        if (_killTextTMP != null)
            _killTextTMP.text = msg;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerRebelWinServerRpc(ulong winnerClientId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // 1) 반란 승자 기록 (GameManager가 보고 RebelSolo로 끝냄)
        GameManager.Instance.NotifyRebelVictoryServer(winnerClientId);

        // 2) 연출/이동잠금 클라RPC
        RebellionVictoryClientRpc(winnerClientId);

        // 3) 연출 시간 준 뒤 결과 처리 (ResultScene 이동은 TryEndGameServer 내부에서)
        StartCoroutine(Co_EndGameAfterRebel());
    }

    private System.Collections.IEnumerator Co_EndGameAfterRebel()
    {
        yield return new WaitForSeconds(3f); // 연출 시간..

        // TryEndGameServer()가 RebelSolo를 우선 처리해서 payload 만들고
        // ResultBroadcaster.EndGameAndShowResult(payload)까지 호출해줌
        GameManager.Instance.TryEndGameServer();
    }


    #region 재판 진입 후 관리 (서현)

    public PlayerTrialState LocalPlayer { get; private set; }
    private List<PlayerTrialState> _allPlayers = new List<PlayerTrialState>();

    public void SetLocalPlayer(PlayerTrialState player)
    {
        LocalPlayer = player;
    }

    public void RegisterPlayer(PlayerTrialState player)
    {
        if (!_allPlayers.Contains(player)) _allPlayers.Add(player);
    }

    public void UnregisterPlayer(PlayerTrialState player)
    {
        if (_allPlayers.Contains(player)) _allPlayers.Remove(player);
    }

    /// <summary>
    /// 서버 전용: 생존 플레이어가 모두 발언을 마쳤는지 확인
    /// </summary>
    public void CheckAllPlayersEnded()
    {
        if (!IsServer) return;
        if (_allPlayers.Count == 0) return;

        List<PlayerTrialState> alivePlayers = _allPlayers.Where(IsAliveTrialParticipant).ToList();
        if (alivePlayers.Count == 0) return;

        // 생존 플레이어의 HasEndedSpeech가 true인지 검사
        bool isAllAliveEnded = alivePlayers.All(p => p.HasEndedSpeech.Value);

        if (isAllAliveEnded)
        {
            EndTrialServer();
        }
    }

    private bool IsAliveTrialParticipant(PlayerTrialState player)
    {
        if (player == null) return false;
        if (PlayerHelperManager.Instance == null) return false;

        PlayerModel model = PlayerHelperManager.Instance.GetPlayerModelByClientId(player.OwnerClientId);
        if (model == null) return false;

        return model.GetPlayerAliveState() == PlayerLivingState.Alive;
    }

    /*
    private void EndTrial()
    {
        Debug.Log("<color=yellow>[TrialManager] 모든 플레이어 발언 종료! 처형 대상 선정 단계로 진입합니다.</color>");
        // TODO: 처형 씬 전환 또는 투표 결과 집계 로직 호출
    }
    */

    #endregion

    // [추가] 서버에서만 호출되는 재판 종료 진입점
    public void EndTrialServer()
    {
        if (!IsServer) return;

        var top = GetHighestVotedClientIds();

        // 득표 리셋(다음 재판 대비) - 아래 4) 참고해서 함수로 빼도 됨
        ResetVotesServer();

        if (top.Count == 0 || top.Count >= 2)
        {
            Debug.Log("[TrialManager] 동점/무효 → Village 복귀");
            NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Village, LoadSceneMode.Single);
            return;
        }

        _pendingExecutedClientId = top[0];
        Debug.Log($"[TrialManager] 처형 대상 확정: {_pendingExecutedClientId} → KillScene");

        NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Kill, LoadSceneMode.Single);
    }

    private void ResetVotesServer()
    {
        if (!IsServer) return;
        if (VoteModel.Instance == null) return;

        var list = VoteModel.Instance.VoteDataList;
        for (int i = 0; i < list.Count; i++)
        {
            var v = list[i];
            v.count = 1;
            list[i] = v;
        }

        Debug.Log("[TrialManager] VoteDataList reset 완료");
    }

    // 최고 득표자 계산
    private List<ulong> GetHighestVotedClientIds()
    {
        if (VoteModel.Instance == null) return new List<ulong>();

        var list = VoteModel.Instance.VoteDataList;
        if (list.Count == 0) return new List<ulong>();

        int maxVote = int.MinValue;
        for (int i = 0; i < list.Count; i++)
            if (list[i].count > maxVote) maxVote = list[i].count;

        var result = new List<ulong>();
        for (int i = 0; i < list.Count; i++)
            if (list[i].count == maxVote) result.Add(list[i].clientId);

        return result;
    }
}