using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Court;
using Unity.Netcode;

namespace Court.Hand
{
    public class TrialHandPresenter : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject trialCardContainerPrefab;
        [SerializeField] private Transform handPivot;
        [SerializeField] private Transform cardsParent;
        [SerializeField] private ArrowController arrowController;

        [Space(10)]
        [Header("1. Basic Layout")] 
        [SerializeField] private float arcRadius = 1500f;
        [SerializeField] private float angleSpacing = 5f;

        [Space(10)]
        [Header("2. Interaction")]
        [SerializeField] private float hoverYOffset = 120f;
        [Range(1.0f, 2.0f)]
        [SerializeField] private float selectedScale = 1.3f; 

        [Space(10)]
        [Header("3. Separation")]
        [SerializeField] private float hoverSeparationAngle = 3f;
        [SerializeField] private float selectedSeparationAngle = 3f;
        
        [Space(10)]
        [Header("4. Input Settings")]
        [SerializeField] private float dragThreshold = 20f;

        // 내부 상태
        private List<TrialCardView> _spawnedCards = new List<TrialCardView>();
        private List<int> _selectedIndices = new List<int>(); 
        private TrialCardView _hoveredCard;
        
        private bool _isDragging = false;
        private bool _isMouseDownToCheck = false;
        private Vector2 _initialMousePos;
        
        // 드래그 시작 위치 (카드의 상단 중앙) 저장용
        private Vector3 _currentDragStartPos; 
        
        private CardInventoryModel _myInventory;

        private void Awake()
        {
            if (cardsParent == null) cardsParent = transform;
            if (handPivot == null) handPivot = transform;
        }

        private void OnDestroy()
        {
            if (_myInventory != null && _myInventory.OwnedCards != null)
            {
                _myInventory.OwnedCards.OnListChanged -= OnInventoryChanged;
            }
        }

        private void Update()
        {
            if (_myInventory == null) return;

            UpdateHandLayout();    
            UpdateDragInput();     
        }

        public void SetInventory(CardInventoryModel inventory)
        {
            _myInventory = inventory;
            _myInventory.OwnedCards.OnListChanged += OnInventoryChanged;
            InitializeHand(); 
        }

        private void OnInventoryChanged(NetworkListEvent<CardItemData> changeEvent)
        {
            InitializeHand();
        }

        private void InitializeHand()
        {
            foreach (var card in _spawnedCards) if (card) Destroy(card.gameObject);
            _spawnedCards.Clear();
            _selectedIndices.Clear();
            
            if (arrowController != null) arrowController.HideArrow();
            if (_myInventory == null) return;

            int index = 0;
            foreach (var cardData in _myInventory.OwnedCards)
            {
                GameObject container = Instantiate(trialCardContainerPrefab, cardsParent);
                TrialCardView view = container.GetComponent<TrialCardView>();
                GameObject visual = CardItemFactoryManager.Instance.CreateCardForInventory(cardData);

                view.Initialize(cardData, index, visual);

                view.OnHoverEnter += (v) => { _hoveredCard = v; }; 
                view.OnHoverExit += (v) => { if (_hoveredCard == v) _hoveredCard = null; };
                view.OnClick += HandleCardClick;

                _spawnedCards.Add(view);
                index++;
            }
            
            UpdateFilterVisuals();
        }

        private void HandleCardClick(TrialCardView cardView)
        {
            if (_isDragging) return;

            int clickedIndex = cardView.InventoryIndex;

            if (_selectedIndices.Contains(clickedIndex))
            {
                _selectedIndices.Remove(clickedIndex);
                UpdateFilterVisuals();
                return;
            }

            if (_selectedIndices.Count >= 2)
            {
                Debug.Log("[System] 이미 2장을 선택했습니다.");
                return;
            }

            if (_selectedIndices.Count == 0)
            {
                _selectedIndices.Add(clickedIndex);
                UpdateFilterVisuals();
                return;
            }

            if (_selectedIndices.Count == 1)
            {
                int firstIndex = _selectedIndices[0];
                if (firstIndex >= _myInventory.OwnedCards.Count) return;

                CardItemData firstCard = _myInventory.OwnedCards[firstIndex];
                CardItemData secondCard = cardView.Data; // 여기서는 Presenter가 이미 들고 있는 데이터 사용 (안전)

                // 규칙 검사 (수정된 Rule 사용)
                if (CourtGameRules.IsCompatible(firstCard, secondCard))
                {
                    _selectedIndices.Add(clickedIndex);
                    UpdateFilterVisuals();
                }
                else
                {
                    Debug.Log("[System] 호환되지 않는 카드입니다.");
                    cardView.TriggerShake(); 
                }
            }
        }

        private void UpdateFilterVisuals()
        {
            if (_myInventory == null) return;

            bool hasCriteria = (_selectedIndices.Count == 1);
            CardItemData criteriaCard = default;
            
            if (hasCriteria)
            {
                criteriaCard = _myInventory.OwnedCards[_selectedIndices[0]];
            }

            foreach (var cardView in _spawnedCards)
            {
                bool isSelected = _selectedIndices.Contains(cardView.InventoryIndex);
                bool shouldBeDimmed = false;

                if (hasCriteria && !isSelected)
                {
                    if (!CourtGameRules.IsCompatible(criteriaCard, cardView.Data))
                    {
                        shouldBeDimmed = true;
                    }
                }
                
                cardView.SetVisualState(isSelected, shouldBeDimmed);
            }
        }

        private void UpdateDragInput()
        {
            // 2장이 선택되지 않았다면 드래그 불가
            if (_selectedIndices.Count != 2)
            {
                if (_isDragging) StopDrag();
                _isMouseDownToCheck = false;
                return;
            }

            // 1. 마우스 누르는 순간: "선택된 카드 위인가?" 체크
            if (Input.GetMouseButtonDown(0))
            {
                // 선택된 카드들 중에서 마우스가 올라간 카드가 있는지 찾음
                TrialCardView startCard = GetMouseOverSelectedCard();
                
                if (startCard != null)
                {
                    _isMouseDownToCheck = true;
                    _initialMousePos = Input.mousePosition;
                    
                    // 드래그 시작점 설정 (카드 중앙 상단)
                    // transform.up은 카드의 회전을 고려한 '위쪽' 방향입니다.
                    // rect.height * scale * 0.5f = 절반 높이 (상단)
                    float heightOffset = 150f; // 적절한 높이값 (프리팹 크기에 맞춰 조절 필요)
                    RectTransform rect = startCard.GetComponent<RectTransform>();
                    if (rect) heightOffset = rect.rect.height * startCard.transform.lossyScale.y * 0.5f;

                    _currentDragStartPos = startCard.transform.position + (startCard.transform.up * heightOffset);
                }
                else
                {
                    // 빈 공간 클릭 시 드래그 시작 안 함
                    _isMouseDownToCheck = false;
                }
            }

            // 2. 드래그 판정 및 진행
            if (Input.GetMouseButton(0))
            {
                if (_isMouseDownToCheck && !_isDragging)
                {
                    float dist = Vector2.Distance(_initialMousePos, Input.mousePosition);
                    if (dist > dragThreshold) _isDragging = true;
                }

                if (_isDragging)
                {
                    // 저장해둔 시작점에서 마우스 위치까지 화살표 그리기
                    Vector3 mouseWorldPos = GetMouseWorldPosition(_currentDragStartPos.z);
                    if (arrowController != null) 
                        arrowController.ShowArrow(_currentDragStartPos, mouseWorldPos);
                }
            }

            // 3. 드래그 종료
            if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                {
                    StopDrag();
                    // TrySubmitEvidence(); // (삭제됨)
                }
                _isDragging = false;
                _isMouseDownToCheck = false;
            }
        }

        /// <summary>
        /// 현재 마우스 위치에 있는 '선택된(Selected)' 카드를 반환
        /// </summary>
        private TrialCardView GetMouseOverSelectedCard()
        {
            foreach (int index in _selectedIndices)
            {
                // 인덱스로 뷰 찾기
                TrialCardView cardView = _spawnedCards.Find(x => x.InventoryIndex == index);
                if (cardView != null)
                {
                    // UI RectTransform 안에 마우스가 들어왔는지 정밀 검사
                    RectTransform rectTransform = cardView.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // Canvas Render Mode에 따라 카메라가 필요할 수 있음 (Overlay면 null, Camera면 Camera.main)
                        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main))
                        {
                            return cardView;
                        }
                    }
                }
            }
            return null;
        }

        private void UpdateHandLayout()
        {
            int count = _spawnedCards.Count;
            if (count == 0) return;

            float totalAngle = (count - 1) * angleSpacing;
            float startAngle = -totalAngle / 2f;
            
            int hoveredIndex = -1;
            if (_hoveredCard != null)
            {
                int rawIndex = _spawnedCards.IndexOf(_hoveredCard);
                // Contains 체크 시 int 값 비교
                if (!_selectedIndices.Contains(_hoveredCard.InventoryIndex)) 
                {
                    hoveredIndex = rawIndex;
                }
            }

            for (int i = 0; i < count; i++)
            {
                TrialCardView card = _spawnedCards[i];
                if (card == null) continue;

                float angle = startAngle + (i * angleSpacing);
                float pushAmount = 0f;

                if (hoveredIndex != -1)
                {
                    if (i < hoveredIndex) pushAmount -= hoverSeparationAngle;
                    if (i > hoveredIndex) pushAmount += hoverSeparationAngle;
                }
                
                foreach (int selInvIdx in _selectedIndices)
                {
                    int selVisualIdx = _spawnedCards.FindIndex(x => x.InventoryIndex == selInvIdx);
                    if (selVisualIdx != -1)
                    {
                        if (i < selVisualIdx) pushAmount -= selectedSeparationAngle;
                        if (i > selVisualIdx) pushAmount += selectedSeparationAngle;
                    }
                }
                
                angle += pushAmount;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector3 pos = handPivot.localPosition + new Vector3(Mathf.Sin(rad) * arcRadius, Mathf.Cos(rad) * arcRadius, 0);
                Quaternion rot = Quaternion.Euler(0, 0, -angle);
                float scale = 1f;

                bool isSelected = _selectedIndices.Contains(card.InventoryIndex);

                if ((hoveredIndex != -1 && i == hoveredIndex) || isSelected)
                {
                    pos += transform.up * hoverYOffset;
                    rot = Quaternion.identity;
                    scale = selectedScale;
                    card.transform.SetAsLastSibling(); 
                }
                else
                {
                    card.transform.SetSiblingIndex(i);
                }

                card.SetTargetState(pos, rot, scale);
            }
        }

        private Vector3 GetMouseWorldPosition(float depthZ)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            if (Camera.main != null)
            {
                float dist = Mathf.Abs(depthZ - Camera.main.transform.position.z);
                mouseScreenPos.z = dist;
                return Camera.main.ScreenToWorldPoint(mouseScreenPos);
            }
            return Vector3.zero;
        }

        private void StopDrag()
        {
            _isDragging = false;
            if (arrowController != null) arrowController.HideArrow();
        }
    }
}