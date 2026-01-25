using UnityEngine;
using Unity.Netcode;

namespace Court
{
    public class CourtPlayerPresenter : NetworkBehaviour
    {
        // View 연결
        private CourtPlayerView _view;
        
        // 내 리스트 인덱스 캐싱
        private int _myVoteIndex = -1;

        private void Awake()
        {
            _view = GetComponent<CourtPlayerView>();
        }

        public override void OnNetworkSpawn()
        {
            // VoteModel이 준비될 때까지 기다렸다가 구독해야 함
            // 안전하게 Start나 Update에서 체크하거나, 이벤트로 처리
            if (VoteModel.Instance != null)
            {
                SubscribeToModel();
            }
        }
        
        private void Start()
        {
            // OnNetworkSpawn 시점에 Instance가 없을 수도 있으니 Start에서도 시도
            if (_myVoteIndex == -1 && VoteModel.Instance != null)
            {
                SubscribeToModel();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (VoteModel.Instance != null)
            {
                VoteModel.Instance.VoteDataList.OnListChanged -= OnVoteDataChanged;
            }
        }

        private void SubscribeToModel()
        {
            // 1. 내 인덱스 찾기
            _myVoteIndex = VoteModel.Instance.GetPlayerIndex(OwnerClientId);
            
            // 2. 이벤트 구독
            VoteModel.Instance.VoteDataList.OnListChanged += OnVoteDataChanged;
            
            // 3. 초기값 표시
            if (_myVoteIndex != -1 && _myVoteIndex < VoteModel.Instance.VoteDataList.Count)
            {
                int initialScore = VoteModel.Instance.VoteDataList[_myVoteIndex].count;
                _view.UpdateScoreUI(initialScore);
            }
        }

        private void OnVoteDataChanged(NetworkListEvent<VoteData> changeEvent)
        {
            // 내 인덱스의 데이터가 바뀌었을 때만 View 업데이트
            if (_myVoteIndex != -1 && changeEvent.Index == _myVoteIndex)
            {
                _view.UpdateScoreUI(changeEvent.Value.count);
            }
        }
    }
}