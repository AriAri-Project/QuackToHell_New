using Court;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CourtUI : UIHUD
{
    private TMP_Text voteNumberText;
    private TMP_Text voteRankingText;
    private Slider timeSlider;

    private CourtController courtController;

    enum Texts
    {
        Votes_Number_Text,
        Votes_Ranking_Text,
    }

    enum Sliders
    {
        Time_Slider
    }

    private void Start()
    {
        var controllerObj = FindFirstObjectByType<CourtController>();
        if (controllerObj != null)
        {
            courtController = controllerObj.GetComponent<CourtController>();
        }

        base.Init();
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Slider>(typeof(Sliders));

        voteNumberText = Get<TextMeshProUGUI>((int)Texts.Votes_Number_Text);
        voteRankingText = Get<TextMeshProUGUI>((int)Texts.Votes_Ranking_Text);

        timeSlider = Get<Slider>((int)Sliders.Time_Slider);

        // [수정 1] 초기 데이터 표시
        UpdateMyVoteInfo();

        // [수정 2] 데이터 변경 감지 (실시간 갱신)
        if (VoteModel.Instance != null)
        {
            VoteModel.Instance.VoteDataList.OnListChanged += OnVoteDataChanged;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 해제 (메모리 누수 방지)
        if (VoteModel.Instance != null)
        {
            VoteModel.Instance.VoteDataList.OnListChanged -= OnVoteDataChanged;
        }
    }

    private void FixedUpdate()
    {
        if (courtController != null)
        {
            timeSlider.value = courtController.GetTimeRatio();
        }
    }

    // 데이터가 변경될 때마다 호출됨
    private void OnVoteDataChanged(NetworkListEvent<VoteData> changeEvent)
    {
        UpdateMyVoteInfo();
    }

    // [수정 3] 내 점수와 순위를 계산하여 UI 갱신
    private void UpdateMyVoteInfo()
    {
        if (VoteModel.Instance == null) return;

        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        
        // 내 데이터가 리스트의 몇 번째에 있는지 찾기
        int myIndex = VoteModel.Instance.GetPlayerIndex(myClientId);

        if (myIndex == -1) return; // 찾지 못함

        // 1. 내 득표수 가져오기
        int myVoteCount = VoteModel.Instance.GetVoteCount(myIndex);
        if (voteNumberText != null)
        {
            voteNumberText.text = myVoteCount.ToString();
        }

        // 2. 내 순위 계산하기 (클라이언트 측 계산)
        // 나보다 표가 많은 사람이 몇 명인지 세어서 순위 결정
        int rank = 1;
        foreach (var data in VoteModel.Instance.VoteDataList)
        {
            if (data.count > myVoteCount)
            {
                rank++;
            }
        }

        if (voteRankingText != null)
        {
            voteRankingText.text = rank.ToString();
        }
    }
}