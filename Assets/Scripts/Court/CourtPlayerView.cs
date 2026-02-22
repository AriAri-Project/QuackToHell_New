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
        [SerializeField] private Canvas hudCanvas; 

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer characterRenderer; 

        [Header("Shader Settings")]
        [SerializeField] private string outlineProperty = "_OutlineWidth"; 
        [SerializeField] private float outlineOnValue = 1.0f; 
        
        [Header("UI Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color previewColor = Color.green;
        [SerializeField] private float courtTargetColliderRadius = 3f;
        [SerializeField] private Vector2 courtTargetColliderOffset = new Vector2(0f, 0.5f);
        
        public ulong OwnerId => OwnerClientId;

        private int _realScore = 1;
        private bool _isPreviewing = false;
        private int _myVoteIndex = -1;
        private bool _isSubscribed = false; 
        
        private Material _targetMaterial;

        // --- (초기화 및 연결 로직 생략, 기존과 동일) ---
        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CheckAndEnableUI(scene.name);

        public override void OnNetworkSpawn()
        {
            CheckAndEnableUI(SceneManager.GetActiveScene().name);
            StartCoroutine(TryConnectRoutine());
        }

        private void Start()
        {
            CheckAndEnableUI(SceneManager.GetActiveScene().name);
            if (characterRenderer != null)
            {
                _targetMaterial = characterRenderer.material;
                _targetMaterial.SetFloat(outlineProperty, 0f);
            }
            if (currentVoteText) currentVoteText.text = _realScore.ToString();

            EnsureCourtTargetCollider();
        }

        private void EnsureCourtTargetCollider()
        {
            var colliders = GetComponents<CircleCollider2D>();
            foreach (var col in colliders)
            {
                if (col == null) continue;
                // 기존에 충분히 큰 트리거 콜라이더가 있으면 재사용
                if (col.isTrigger && col.radius >= courtTargetColliderRadius * 0.9f)
                {
                    return;
                }
            }

            var targetCol = gameObject.AddComponent<CircleCollider2D>();
            targetCol.isTrigger = true;
            targetCol.offset = courtTargetColliderOffset;
            targetCol.radius = courtTargetColliderRadius;
        }

        private void CheckAndEnableUI(string sceneName)
        {
            if (hudCanvas == null) hudCanvas = GetComponentInChildren<Canvas>(true);
            
            if (sceneName.Contains("Court"))
            {
                if (hudCanvas != null) hudCanvas.gameObject.SetActive(true);
            }
            else
            {
                if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (VoteModel.Instance != null && _isSubscribed)
            {
                VoteModel.Instance.VoteDataList.OnListChanged -= OnVoteDataChanged;
            }
        }

        private IEnumerator TryConnectRoutine()
        {
            while (!_isSubscribed)
            {
                if (VoteModel.Instance == null || PlayerHelperManager.Instance == null)
                {
                    yield return new WaitForSeconds(1.0f);
                    continue;
                }

                int foundIndex = VoteModel.Instance.GetPlayerIndex(OwnerId);

                if (foundIndex != -1)
                {
                    _myVoteIndex = foundIndex;
                    VoteModel.Instance.VoteDataList.OnListChanged += OnVoteDataChanged;
                    _isSubscribed = true;

                    if (_myVoteIndex < VoteModel.Instance.VoteDataList.Count)
                    {
                        UpdateScoreUI(VoteModel.Instance.GetVoteCount(_myVoteIndex));
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
        // ★ [핵심 수정] 프리뷰 0 미만 방지
        // ==================================================================================
        
        public void ShowPreview(int damage)
        {
            int predictedScore = _realScore + damage;

            // ★ 0점 미만이면 0점으로 고정 (Clamp)
            if (predictedScore < 0) 
            {
                predictedScore = 0;
            }

            ShowPreviewInternal(predictedScore.ToString());
        }

        public void ShowPreview(string text)
        {
            ShowPreviewInternal(text);
        }

        private void ShowPreviewInternal(string textToDisplay)
        {
            if (_targetMaterial != null) _targetMaterial.SetFloat(outlineProperty, outlineOnValue);
            if (currentVoteText == null) return;

            if (_isPreviewing && currentVoteText.text == textToDisplay) return;

            _isPreviewing = true;
            currentVoteText.text = textToDisplay;
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