using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

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

        // 이벤트 (Presenter가 구독함)
        public event Action<TrialCardView> OnHoverEnter;
        public event Action<TrialCardView> OnHoverExit;
        public event Action<TrialCardView> OnClick;

        // Lerp 타겟
        private Vector3 _targetPos;
        private Quaternion _targetRot;
        private float _targetScale = 1f;

        public void Initialize(CardItemData data, int index, GameObject visualPrefab)
        {
            this.Data = data;
            this.InventoryIndex = index;

            // 비주얼 생성 및 부착
            visualPrefab.transform.SetParent(visualContainer, false);
            visualPrefab.transform.localPosition = Vector3.zero;
            visualPrefab.transform.localRotation = Quaternion.identity;
            visualPrefab.transform.localScale = Vector3.one;
            
            // 중요: 생성된 비주얼 프리팹이 Raycast를 막지 않도록 설정
            // (이 TrialCardView 컴포넌트가 달린 부모 객체가 입력을 받아야 함)
            var canvasGroup = visualPrefab.GetOrAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false; 
            
            // 카드 내용(Visual)이 테두리/배경보다 항상 위에 그려지도록 순서 정리
            // (Hierarchy 상에서 맨 아래에 있어야 화면상 맨 앞에 그려짐)
            visualContainer.SetAsLastSibling();
        }
        

        public void SetTargetState(Vector3 pos, Quaternion rot, float scale)
        {
            _targetPos = pos;
            _targetRot = rot;
            _targetScale = scale;
        }

        public void SetVisualState(bool isSelected, bool isDisabled)
        {
            if (selectionBorder) selectionBorder.enabled = isSelected;
            if (dimPanel) dimPanel.enabled = isDisabled;
        }
        

        private void Update()
        {
            // 매 프레임 부드럽게 이동 (Lerp)
            float dt = Time.deltaTime * 12f; // 속도 조절
            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetPos, dt);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, _targetRot, dt);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * _targetScale, dt);
        }

        // --- 입력 처리 ---
        public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => OnHoverExit?.Invoke(this);
        public void OnPointerClick(PointerEventData eventData) => OnClick?.Invoke(this);
    }    
}