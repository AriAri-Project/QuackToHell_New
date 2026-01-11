using UnityEngine;
using TMPro;
using Unity.Netcode;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement; 

namespace Court
{
    public class CourtPlayerView : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI currentVoteText;
        [SerializeField] private Canvas hudCanvas; // 껐다 켰다 할 캔버스

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer characterRenderer; 

        [Header("Shader Settings")]
        [SerializeField] private string outlineProperty = "_OutlineWidth"; 
        [SerializeField] private float outlineOnValue = 1.0f; 
        
        [Header("UI Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color previewColor = Color.green;
        
        public ulong OwnerId => OwnerClientId;

        private int _realScore = 1;
        private bool _isPreviewing = false;
        private int _myVoteIndex = -1;
        private bool _isSubscribed = false; 
        
        private Material _targetMaterial;

        // ==================================================================================
        // ★ 씬 로딩 이벤트 연결 (이동할 때마다 체크)
        // ==================================================================================
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndEnableUI(scene.name);
        }

        // ==================================================================================
        // ★ 초기화 (Start, OnNetworkSpawn)
        // ==================================================================================
        public override void OnNetworkSpawn()
        {
            // 스폰 직후 내 위치 확인
            CheckAndEnableUI(SceneManager.GetActiveScene().name);
            StartCoroutine(TryConnectRoutine());
        }

        private void Start()
        {
            // 혹시 모르니 Start에서도 체크
            CheckAndEnableUI(SceneManager.GetActiveScene().name);

            if (characterRenderer != null)
            {
                _targetMaterial = characterRenderer.material;
                _targetMaterial.SetFloat(outlineProperty, 0f);
            }

            if (currentVoteText)
            {
                currentVoteText.text = _realScore.ToString();
            }
        }

        // ==================================================================================
        // ★ [핵심] 씬 이름에 따라 UI 끄고 켜기
        // ==================================================================================
        private void CheckAndEnableUI(string sceneName)
        {
            // 1. 캔버스가 연결 안 돼있으면 찾기 (안전장치)
            if (hudCanvas == null)
            {
                hudCanvas = GetComponentInChildren<Canvas>(true);
            }

            // 2. 씬 이름 검사
            if (sceneName.Contains("Court"))
            {
                // 재판장임 -> UI 켜기
                if (hudCanvas != null)
                {
                    hudCanvas.gameObject.SetActive(true);
                    Debug.Log($"[View] '{sceneName}' 도착. 재판장 UI를 켭니다.");
                }
            }
            else
            {
                // 재판장이 아님 (마을 등) -> UI 끄기! (SetFalse)
                if (hudCanvas != null)
                {
                    hudCanvas.gameObject.SetActive(false);
                    Debug.Log($"[View] '{sceneName}' 도착. 재판장 UI를 끕니다.");
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (VoteModel.Instance != null && _isSubscribed)
            {
                VoteModel.Instance.VoteDataList.OnListChanged -= OnVoteDataChanged;
            }
        }

        // ==================================================================================
        // ★ 데이터 연결 로직 (기존 유지)
        // ==================================================================================
        private IEnumerator TryConnectRoutine()
        {
            while (!_isSubscribed)
            {
                // 모델이나 매니저가 없으면 대기
                if (VoteModel.Instance == null || PlayerHelperManager.Instance == null)
                {
                    yield return new WaitForSeconds(1.0f);
                    continue;
                }

                // 내 인덱스 찾기
                int foundIndex = VoteModel.Instance.GetPlayerIndex(OwnerId);

                if (foundIndex != -1)
                {
                    _myVoteIndex = foundIndex;
                    VoteModel.Instance.VoteDataList.OnListChanged += OnVoteDataChanged;
                    _isSubscribed = true;

                    // 연결 성공 시 초기값 갱신
                    if (_myVoteIndex < VoteModel.Instance.VoteDataList.Count)
                    {
                        int currentDataScore = VoteModel.Instance.GetVoteCount(_myVoteIndex);
                        UpdateScoreUI(currentDataScore);
                    }
                    yield break; 
                }
                
                yield return new WaitForSeconds(1.0f);
            }
        }

        private void OnVoteDataChanged(NetworkListEvent<VoteData> changeEvent)
        {
            if (_myVoteIndex != -1 && changeEvent.Index == _myVoteIndex)
            {
                UpdateScoreUI(changeEvent.Value.count);
            }
        }

        public void UpdateScoreUI(int newScore)
        {
            _realScore = newScore;
            
            if (!_isPreviewing && currentVoteText != null)
            {
                currentVoteText.text = _realScore.ToString();
                currentVoteText.color = normalColor;
            }
        }

        // ==================================================================================
        // ★ 프리뷰 & 쉐이더 (기존 유지)
        // ==================================================================================
        public void ShowPreview(int damage)
        {
            if (_targetMaterial != null) _targetMaterial.SetFloat(outlineProperty, outlineOnValue);
            if (currentVoteText == null) return;

            int predictedScore = _realScore + damage;
            if (_isPreviewing && currentVoteText.text == predictedScore.ToString()) return;

            _isPreviewing = true;
            currentVoteText.text = predictedScore.ToString();
            currentVoteText.color = previewColor;
            
            currentVoteText.transform.DOKill();
            currentVoteText.transform.localScale = Vector3.one;
            currentVoteText.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f);
        }

        public void HidePreview()
        {
            if (_targetMaterial != null) _targetMaterial.SetFloat(outlineProperty, 0f);
            if (!_isPreviewing) return;
            
            _isPreviewing = false;
            if (currentVoteText != null)
            {
                currentVoteText.text = _realScore.ToString();
                currentVoteText.color = normalColor;
                currentVoteText.transform.localScale = Vector3.one;
            }
        }
    }
}