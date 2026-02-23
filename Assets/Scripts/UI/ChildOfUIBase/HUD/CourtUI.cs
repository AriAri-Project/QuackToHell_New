using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ★ 네임스페이스를 Court로 감싸서 VoteModel과 같은 공간에 둡니다.
namespace Court
{
    public class CourtUI : UIHUD
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI voteNumberText;
        [SerializeField] private TextMeshProUGUI voteRankingText;
        [SerializeField] private Slider timeSlider;

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

        enum Images
        {
            Player_Role_Icon,
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
            Bind<Image>(typeof(Images));

            // Inspector에서 연결 안 했을 경우를 대비해 Bind로 가져오기
            if (voteNumberText == null) voteNumberText = Get<TextMeshProUGUI>((int)Texts.Votes_Number_Text);
            if (voteRankingText == null) voteRankingText = Get<TextMeshProUGUI>((int)Texts.Votes_Ranking_Text);
            if (timeSlider == null) timeSlider = Get<Slider>((int)Sliders.Time_Slider);
            
            //역할 이미지 초기화
            GameObject Image_Role_gameObject = Get<Image>((int)Images.Player_Role_Icon).gameObject;
            if (PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId)
                    .GetPlayerJob() == PlayerJob.Animal)
            {
                Image_Role_gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Art/Duck");    
            }
            else
            {
                Image_Role_gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Art/Farmer");    
            }
            
            // 1. 초기 데이터 표시
            UpdateMyVoteInfo();

            // 2. 이벤트 구독 (안전하게 연결)
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
            if (courtController != null && timeSlider != null)
            {
                timeSlider.value = courtController.GetTimeRatio();
            }
        }

        // ★ 에러가 나던 부분: 네임스페이스 안으로 들어왔으므로 VoteData를 정확히 인식합니다.
        private void OnVoteDataChanged(NetworkListEvent<VoteData> changeEvent)
        {
            UpdateMyVoteInfo();
        }

        // 내 점수와 순위를 계산하여 UI 갱신
        private void UpdateMyVoteInfo()
        {
            if (VoteModel.Instance == null) return;
            if (NetworkManager.Singleton == null) return;

            ulong myClientId = NetworkManager.Singleton.LocalClientId;
            
            // 1. 내 인덱스 찾기
            int myIndex = VoteModel.Instance.GetPlayerIndex(myClientId);
            if (myIndex == -1) return; 

            // 2. 내 득표수 갱신
            int myVoteCount = VoteModel.Instance.GetVoteCount(myIndex);
            if (voteNumberText != null)
            {
                voteNumberText.text = myVoteCount.ToString();
            }

            // 3. 내 순위 계산 (나보다 표 많은 사람 수 + 1)
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
}