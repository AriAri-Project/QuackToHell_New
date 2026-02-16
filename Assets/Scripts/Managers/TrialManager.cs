using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Court;

public class TrialManager : NetworkBehaviour
{
    [Header("UI References")]
    private GameObject convocationOfTrialCanvas;
    private GameObject convocationOfTrialPanel;
    private GameObject corpseTextObject;
    private Image reporterImage;
    private ulong reporterClientId; // 해당 정보는 서버에만 저장됨

    [Header("Execution UI")]
    [SerializeField] private GameObject executionCanvas;
    [SerializeField] private Image executionPlayerImage;
    [SerializeField] private TextMeshProUGUI executionText;

    private ulong executedClientId;

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

        // 4. 재판 시작 (서버가 권위적 정보로 처리)
        TrialResultClientRpc(reporterClientId);
    }

    [ClientRpc]
    public void TrialResultClientRpc(ulong reporterClientId)
    {
        convocationOfTrialPanel.SetActive(true);
        
        InjectReporterColor(reporterClientId);
        InjectReporterPlayerText(reporterClientId);
          
        //모든 플레이어의 움직임 멈춤
        ulong localCliendId = NetworkManager.Singleton.LocalClientId;
        PlayerView playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(localCliendId);
        playerView.SetIgnoreAllPlayerMoveInputServerRpc(true);
        //5초뒤 씬 이동
        Invoke("LoadCourtScene", 5f);
    }

    [ClientRpc]
    private void StartExecutionClientRpc(ulong targetClientId)
    {
        Debug.Log($"[Client] 처형 연출 시작: {targetClientId}");

        if (executionCanvas != null)
            executionCanvas.SetActive(true);

        PlayerModel model = PlayerHelperManager.Instance.GetPlayerModelByClientId(targetClientId);

        if (model != null)
        {
            string nickname = model.PlayerStatusData.Value.Nickname.ToString();

            if (executionText != null)
                executionText.text = $"{nickname}님이 처형되었습니다.";

            if (executionPlayerImage != null)
            {
                int colorIndex = model.PlayerAppearanceData.Value.ColorIndex;
                executionPlayerImage.color = ColorUtils.GetColorByIndex(colorIndex);
            }
        }
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
        PlayerModel reporterModel =  PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterCliendId);
        reporterPlayerText = reporterModel.PlayerStatusData.Value.Nickname.ToString();
        TextMeshProUGUI reporterTextTMP = corpseTextObject.GetComponent<TextMeshProUGUI>();
        reporterTextTMP.text = "ReporterPlayer: " + reporterPlayerText;
    }
    private void InjectReporterColor(ulong reporterClientId)
    {
        PlayerModel reporterModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterClientId);
        PlayerAppearanceData playerAppearanceData = reporterModel.PlayerAppearanceData.Value;
        int colorIndex = playerAppearanceData.ColorIndex;
        reporterImage.color = ColorUtils.GetColorByIndex(colorIndex);               
        
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
    }

    private void HandleAfterExecutionServer()
    {
        if (!IsServer) return;

        Debug.Log("[TrialManager] 처형 이후 후처리 시작");

        // 사망 처리
        PlayerModel targetModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(executedClientId);
        if (targetModel != null)
        {
            targetModel.PlayerStateData.Value =
                new PlayerStateData
                {
                    AliveState = PlayerLivingState.Dead
                };

            Debug.Log($"[TrialManager] {executedClientId} 사망 처리 완료");
        }

        // 임시 테스트 조건
        bool endGame = Random.value > 0.5f;

        if (endGame)
        {
            Debug.Log("[TrialManager] 게임 종료 테스트 이동");
            NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Result, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("[TrialManager] 마을 복귀 테스트 이동");
            NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Village, LoadSceneMode.Single);
        }

        // 다음 재판 대비 투표 리셋
        VoteModel.Instance.ResetVotes();
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
    /// 서버 전용: 모든 플레이어가 발언을 마쳤는지 확인
    /// </summary>
    public void CheckAllPlayersEnded()
    {
        if (!IsServer) return;
        if (_allPlayers.Count == 0) return;

        // 모든 플레이어의 HasEndedSpeech가 true인지 검사
        bool isAllEnded = _allPlayers.All(p => p.HasEndedSpeech.Value);

        if (isAllEnded)
        {
            EndTrial();
        }
    }

    public void EndTrial()
    {
        if (!IsServer) return;

        Debug.Log("<color=yellow>[TrialManager] 모든 플레이어 발언 종료! 처형 대상 선정 시작</color>");

        // 1. 최고 득표자 계산
        if (!VoteModel.Instance.TryGetTopVoted(out ulong topClientId, out int topCount, out bool isTie))
        {
            Debug.LogError("[TrialManager] 투표 데이터 없음");
            return;
        }

        executedClientId = topClientId;

        Debug.Log($"[TrialManager] 처형 대상: {topClientId} / 득표수: {topCount} / 동점:{isTie}");

        // 2. 전 클라이언트에게 처형 연출 시작 알림
        StartExecutionClientRpc(topClientId);

        // 3. 서버에서 4초 후 씬 이동 처리
        Invoke(nameof(HandleAfterExecutionServer), 4f);
    }


    #endregion
}