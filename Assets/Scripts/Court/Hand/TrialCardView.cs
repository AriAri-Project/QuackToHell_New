using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace Court.Hand
{
    public class TrialCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Visual References")]
        public Transform visualContainer; 
        public Image selectionBorder;     // 선택 시 켜질 초록색 테두리
        public Image dimPanel;            // 비활성 시 켜질 검은 반투명 패널

        // 데이터
        public int InventoryIndex { get; private set; }
        public CardItemData Data { get; private set; }

        // 이벤트
        public event Action<TrialCardView> OnHoverEnter;
        public event Action<TrialCardView> OnHoverExit;
        public event Action<TrialCardView> OnClick;

        // Lerp 타겟
        private Vector3 _targetPos;
        private Quaternion _targetRot;
        private float _targetScale = 1f;
        
        // 쉐이크 효과 변수
        private bool _isShaking = false;
        private Vector3 _shakeOffset = Vector3.zero;

        public void Initialize(CardItemData data, int index, GameObject visualPrefab)
        {
            this.Data = data;
            this.InventoryIndex = index;

            // 비주얼 생성 및 부착
            visualPrefab.transform.SetParent(visualContainer, false);
            visualPrefab.transform.localPosition = Vector3.zero;
            visualPrefab.transform.localRotation = Quaternion.identity;
            visualPrefab.transform.localScale = Vector3.one;
            
            var canvasGroup = visualPrefab.GetOrAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false; 
            
            // 초기화: 모두 끄기
            if(selectionBorder) selectionBorder.gameObject.SetActive(false);
            if(dimPanel) dimPanel.gameObject.SetActive(false);
        }

        public void SetTargetState(Vector3 pos, Quaternion rot, float scale)
        {
            _targetPos = pos;
            _targetRot = rot;
            _targetScale = scale;
        }

        /// <summary>
        /// 카드의 시각적 상태 설정 (선택됨 / 비활성화됨)
        /// </summary>
        public void SetVisualState(bool isSelected, bool isDisabled)
        {
            // 1. 선택 테두리 제어
            if (selectionBorder) 
            {
                selectionBorder.gameObject.SetActive(isSelected);
            }
            
            // 2. 어두운 패널(Dim) 제어
            if (dimPanel) 
            {
                // 선택된 카드는 절대 어둡게 하지 않음
                if (isSelected)
                {
                    dimPanel.gameObject.SetActive(false);
                }
                else
                {
                    // 선택 안 된 카드 중, 호환 안 되는 녀석만 켬
                    dimPanel.gameObject.SetActive(isDisabled);
                }
            }
        }
        
        /// <summary>
        /// 덜덜 떨리는 효과 (선택 불가 피드백)
        /// </summary>
        public void TriggerShake()
        {
            if (!_isShaking) StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            _isShaking = true;
            float elapsed = 0f;
            float duration = 0.3f;
            float magnitude = 10f; 

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-0.5f, 0.5f) * magnitude;
                
                _shakeOffset = new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _isShaking = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime * 12f;
            
            Vector3 finalPos = Vector3.Lerp(transform.localPosition, _targetPos, dt);
            if (_isShaking) finalPos += _shakeOffset;

            transform.localPosition = finalPos;
            transform.localRotation = Quaternion.Lerp(transform.localRotation, _targetRot, dt);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * _targetScale, dt);
        }

        // --- 입력 처리 ---
        public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => OnHoverExit?.Invoke(this);
        public void OnPointerClick(PointerEventData eventData) => OnClick?.Invoke(this);
    }    
}