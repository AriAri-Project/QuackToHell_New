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
        public Image selectionBorder;     
        public Image dimPanel;            

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

        // ★ 외부에서 자동 정렬 기능을 켜고 끄는 스위치 (발언 마치기 카드용)
        public bool IsAutoLayoutEnabled { get; set; } = true;

        public void Initialize(CardItemData data, int index, GameObject visualPrefab)
        {
            this.Data = data;
            this.InventoryIndex = index;

            if (visualPrefab != null)
            {
                visualPrefab.transform.SetParent(visualContainer, false);
                visualPrefab.transform.localPosition = Vector3.zero;
                visualPrefab.transform.localRotation = Quaternion.identity;
                visualPrefab.transform.localScale = Vector3.one;
                
                // 캔버스 그룹이 있다면 레이캐스트 차단 해제 (클릭 통과 방지)
                var canvasGroup = visualPrefab.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = visualPrefab.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = false; 
            }

            if (selectionBorder) selectionBorder.gameObject.SetActive(false);
            if (dimPanel) dimPanel.gameObject.SetActive(false);
        }

        public void SetTargetState(Vector3 pos, Quaternion rot, float scale)
        {
            _targetPos = pos;
            _targetRot = rot;
            _targetScale = scale;
        }

        // ★ [수정됨] Presenter에서 호출하는 이름에 맞춰 통합
        public void SetVisualState(bool isSelected, bool isDisabled)
        {
            // 1. 선택 테두리
            if (selectionBorder) selectionBorder.gameObject.SetActive(isSelected);

            // 2. 딤 패널 (선택된 카드는 절대 어둡게 하지 않음)
            if (dimPanel)
            {
                if (isSelected) dimPanel.gameObject.SetActive(false);
                else dimPanel.gameObject.SetActive(isDisabled);
            }
        }

        // ★ [수정됨] 이름 변경 (PlayShakeEffect -> TriggerShake)
        public void TriggerShake()
        {
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine());
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
            // 스위치가 꺼져있으면 위치 강제 이동 안 함 (드래그 중)
            if (!IsAutoLayoutEnabled) return;

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