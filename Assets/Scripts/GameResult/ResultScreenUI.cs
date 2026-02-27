using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultScreenUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject intro;
    public GameObject showResult;

    [Header("Text")]
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultExplainText;

    [Header("Players")]
    public GameObject playerUIPrefab;
    public Transform spawnParent;

    [Header("Foothold Sprites")]
    [SerializeField] private Image spawnParentImage;
    [SerializeField] private Sprite farmerFootholdSprite;
    [SerializeField] private Sprite animalFootholdSprite;

    [SerializeField] private Button goToLobbyButton;
    [SerializeField] private Button goToStartButton;

    public void Open(GameResultPayload payload)
    {
        gameObject.SetActive(true);
        StartCoroutine(ResultCoroutine(payload));
    }
    private void Awake()
    {
        Debug.Log("ResultScreenUI Awake");

        goToLobbyButton.onClick.AddListener(OnClickGoToLobby);
        goToStartButton.onClick.AddListener(OnClickGoToStart);
    }

    private System.Collections.IEnumerator ResultCoroutine(GameResultPayload payload)
    {
        intro.SetActive(true);
        yield return new WaitForSeconds(1f);
        intro.SetActive(false);

        showResult.SetActive(true);

        bool animalWin = payload.WinType == EWinType.Citizens;

        // 발판 교체
        if (spawnParentImage != null)
        {
            spawnParentImage.sprite = animalWin
                ? animalFootholdSprite
                : farmerFootholdSprite;
        }

        // ===== 텍스트 세팅 =====
        if (animalWin)
        {
            resultTitleText.text = "동물 승리";
            resultTitleText.color = new Color(0.3608f, 1f, 0.4039f, 1f);

            resultExplainText.text =
                "농장의 평화는 당분간 이어질 겁니다. 적어도... 다음 침입 전까지는요.";
        }
        else
        {
            resultTitleText.text = "농장주 승리";
            resultTitleText.color = new Color(1f, 0.3608f, 0.3608f, 1f);

            resultExplainText.text =
                "농장은 잠시 빼앗겼을 뿐입니다. 그리고 마침내 다시 제 주인을 찾았습니다.";
        }

        SpawnWinners(payload);

        yield return new WaitForSeconds(4f);
    }

    // ===== Winner만 V자 배치 =====
    private void SpawnWinners(GameResultPayload payload)
    {
        List<ResultPlayerInfo> winners = new();

        if (payload.HasWinner0) winners.Add(payload.Winner0);
        if (payload.HasWinner1) winners.Add(payload.Winner1);
        if (payload.HasWinner2) winners.Add(payload.Winner2);
        if (payload.HasWinner3) winners.Add(payload.Winner3);

        float xSpacing = 180f;
        float ySpacing = 100f;

        for (int i = 0; i < winners.Count; i++)
        {
            SpawnPlayerUIVShape(winners[i], i, xSpacing, ySpacing);
        }
    }

    private void SpawnPlayerUIVShape(ResultPlayerInfo player, int index,
        float xSpace, float ySpace)
    {
        GameObject playerUI = Instantiate(playerUIPrefab, spawnParent);

        float x = 0f;
        float y = 150f; // 중앙 기준으로 시작

        if (index != 0)
        {
            int pairIndex = (index - 1) / 2;
            bool isLeft = (index - 1) % 2 == 0;

            x = (pairIndex + 1) * xSpace * (isLeft ? -1 : 1);
            y = (pairIndex + 1) * ySpace; // 위로 올라가게
        }

        RectTransform rect = playerUI.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);

        var nicknameText = playerUI.GetComponentInChildren<TextMeshProUGUI>();
        nicknameText.text = player.Name.ToString();
    }

    private void OnClickGoToLobby()
    {
        Debug.Log("GoToLobby 버튼 눌림");

        ResultBroadcaster.Instance.RequestGoToLobby();
    }
    private void OnClickGoToStart()
    {
        Debug.Log("GoToStart 버튼 눌림");

        // 네트워크 연결 끊고 홈으로
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(GameScenes.Home, LoadSceneMode.Single);
    }
}