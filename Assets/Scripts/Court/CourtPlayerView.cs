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
        [SerializeField] private Collider2D courtOnlyTargetCollider;
        
        public ulong OwnerId => OwnerClientId;

        private int _realScore = 1;
        private bool _isPreviewing = false;
        private int _myVoteIndex = -1;
        private bool _isSubscribed = false; 
        private VoteModel _subscribedVoteModel;
        private Coroutine _connectRoutine;
        
        private Material _targetMaterial;

        // --- (초기화 및 연결 로직 생략, 기존과 동일) ---
        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ClearVoteBinding();
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndEnableUI(scene.name);

            if (scene.name == GameScenes.Court)
            {
                ReconnectVoteModel();
            }
            else
            {
                ClearVoteBinding();
            }
        }

        public override void OnNetworkSpawn()
        {
            CheckAndEnableUI(SceneManager.GetActiveScene().name);
            ReconnectVoteModel();
        }

        private void Start()
        {
            EnsureCourtOnlyTargetCollider();
            CheckAndEnableUI(SceneManager.GetActiveScene().name);
            if (characterRenderer != null)
            {
                _targetMaterial = characterRenderer.material;
                _targetMaterial.SetFloat(outlineProperty, 0f);
            }
            if (currentVoteText) currentVoteText.text = _realScore.ToString();
        }

        private void EnsureCourtOnlyTargetCollider()
        {
            if (courtOnlyTargetCollider == null)
            {
                var marker = GetComponentInChildren<CourtTargetCollider>(true);
                if (marker != null)
                {
                    courtOnlyTargetCollider = marker.GetComponent<Collider2D>();
                    marker.SetOwner(this);
                }
            }

            if (courtOnlyTargetCollider == null)
            {
                GameObject targetObj = new GameObject("CourtTargetCollider");
                targetObj.transform.SetParent(transform, false);
                targetObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                var col = targetObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 3f;
                courtOnlyTargetCollider = col;

                var marker = targetObj.AddComponent<CourtTargetCollider>();
                marker.SetOwner(this);
            }
        }

        private void CheckAndEnableUI(string sceneName)
        {
            if (hudCanvas == null) hudCanvas = GetComponentInChildren<Canvas>(true);
            
            bool isCourtScene = sceneName == GameScenes.Court;
            if (isCourtScene)
            {
                if (hudCanvas != null) hudCanvas.gameObject.SetActive(true);
            }
            else
            {
                if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
            }

            if (courtOnlyTargetCollider != null)
            {
                courtOnlyTargetCollider.enabled = isCourtScene;
            }
        }

        public override void OnNetworkDespawn()
        {
            ClearVoteBinding();
        }

        private void ReconnectVoteModel()
        {
            ClearVoteBinding();

            if (!IsSpawned) return;
            if (_connectRoutine != null) StopCoroutine(_connectRoutine);
            _connectRoutine = StartCoroutine(TryConnectRoutine());
        }

        private void ClearVoteBinding()
        {
            if (_subscribedVoteModel != null && _isSubscribed)
            {
                _subscribedVoteModel.VoteDataList.OnListChanged -= OnVoteDataChanged;
            }

            _isSubscribed = false;
            _myVoteIndex = -1;
            _subscribedVoteModel = null;

            if (_connectRoutine != null)
            {
                StopCoroutine(_connectRoutine);
                _connectRoutine = null;
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

                VoteModel currentVoteModel = VoteModel.Instance;
                int foundIndex = currentVoteModel.GetPlayerIndex(OwnerId);

                if (foundIndex != -1)
                {
                    _myVoteIndex = foundIndex;
                    _subscribedVoteModel = currentVoteModel;
                    _subscribedVoteModel.VoteDataList.OnListChanged += OnVoteDataChanged;
                    _isSubscribed = true;
                    _connectRoutine = null;

                    if (_myVoteIndex < _subscribedVoteModel.VoteDataList.Count)
                    {
                        UpdateScoreUI(_subscribedVoteModel.GetVoteCount(_myVoteIndex));
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
        
        public void ShowPreview(int damage, bool allowZero = false)
        {
            int predictedScore = _realScore + damage;

            int minScore = allowZero ? 0 : 1;
            if (predictedScore < minScore)
            {
                predictedScore = minScore;
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
