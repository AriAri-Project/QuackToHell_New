using Unity.Netcode;
using UnityEngine;

namespace Court
{

    //Court의 게임 매니저 => 시간 관리
    //[서버 사이드 스크립트]
    public class CourtController : NetworkBehaviour
    {
        [Header("Timer Settings")]
        private NetworkVariable<int> _remainingTime;

        [SerializeField] private int CourtingTime = 180;
        // UI 등에서 접근할 Public 프로퍼티

        private float _timerAccumulator = 0f;
        private bool _isTimerRunning = false;

        private void Awake()
        {
            _remainingTime = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartCourtTimer(CourtingTime);
            }
        }

        /// <summary>
        /// 타이머 시작 함수 (서버 전용)
        /// </summary>
        private void StartCourtTimer(int duration)
        {
            if (!IsServer) return;

            _remainingTime.Value = duration;
            _timerAccumulator = 0f;
            _isTimerRunning = true;
        }

        public float GetTimeRatio()
        {
            return (float)_remainingTime.Value / CourtingTime;
        }

        private void Update()
        {
            if (!IsServer || !_isTimerRunning) return;

            if (_remainingTime.Value > 0)
            {
                _timerAccumulator += Time.deltaTime;

                if (_timerAccumulator >= 1f)
                {
                    _remainingTime.Value -= 1;
                    _timerAccumulator -= 1f;

                    if (_remainingTime.Value <= 0)
                    {
                        OnTimerFinished();
                    }
                }
            }
        }

        private void OnTimerFinished()
        {
            _isTimerRunning = false;
            Debug.Log("[Court] 시간이 종료되었습니다.");

            if (IsServer)
            {
                // TrialManager가 DontDestroyOnLoad라서 Court 씬에서도 Instance 접근 가능
                TrialManager.Instance.EndTrialServer();
            }
        }
    }

}