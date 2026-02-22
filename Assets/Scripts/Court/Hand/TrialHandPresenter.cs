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
        [SerializeField] private GameObject endSpeechCardPrefab; 
        
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
        
        // 드래그 상태 관리
        private bool _isDraggingEvidence = false;  // 증거물(2장) 드래그 중인가?
        private bool _isDraggingEndSpeech = false; // 발언 마치기 카드 드래그 중인가?
        private TrialCardView _draggingEndCardView; // 현재 드래그 중인 발언 마치기 카드
        
        private bool _isMouseDownToCheck = false;
        private Vector2 _initialMousePos;
        private Vector3 _currentDragStartPos; 
        private CourtPlayerView _lastHoveredPlayer; 
        
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

            // 1. 발언 마치기 카드 생성 (인덱스 -1)
            if (endSpeechCardPrefab != null)
            {
                GameObject endCardObj = Instantiate(endSpeechCardPrefab, cardsParent);
                TrialCardView endCardView = endCardObj.GetComponent<TrialCardView>();
                
                if (endCardView != null)
                {
                    // 데이터 없음, 인덱스 -1
                    // visualPrefab에 null을 넣거나 자기 자신을 처리하는 로직에 따름
                    endCardView.Initialize(default(CardItemData), -1, null); 

                    // 호버링 이벤트 연결
                    endCardView.OnHoverEnter += (v) => { _hoveredCard = v; }; 
                    endCardView.OnHoverExit += (v) => { if (_hoveredCard == v) _hoveredCard = null; };
                    
                    // * 발언 마치기 카드는 클릭 선택(HandleCardClick)을 연결하지 않습니다.

                    _spawnedCards.Add(endCardView);
                }
            }

            // 2. 인벤토리 카드 생성
            int index = 0;
            foreach (var cardData in _myInventory.OwnedCards)
            {
                GameObject container = Instantiate(trialCardContainerPrefab, cardsParent);
                TrialCardView view = container.GetComponent<TrialCardView>();
                
                GameObject visual = null;
                if (CardItemFactoryManager.Instance != null)
                {
                    visual = CardItemFactoryManager.Instance.CreateCardForInventory(cardData);
                }

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
            if (_isDraggingEvidence || _isDraggingEndSpeech) return;
            
            // 인덱스가 -1인 카드(발언 마치기 카드)는 클릭 선택 로직 무시
            if (cardView.InventoryIndex < 0) return;

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

            // 첫 번째 카드 선택
            if (_selectedIndices.Count == 0)
            {
                _selectedIndices.Add(clickedIndex);
                UpdateFilterVisuals();
                return;
            }

            // 두 번째 카드 선택 (호환성 체크)
            if (_selectedIndices.Count == 1)
            {
                int firstIndex = _selectedIndices[0];
                if (firstIndex >= _myInventory.OwnedCards.Count) return;

                CardItemData firstCard = _myInventory.OwnedCards[firstIndex];
                CardItemData secondCard = cardView.Data; 

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
                if (cardView.InventoryIndex < 0) continue;

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

        // ==================================================================================
        // ★ [핵심 수정] 드래그 입력 처리 분기 (2장 선택 vs 발언마치기 카드)
        // ==================================================================================
        private void UpdateDragInput()
        {
            // 1. 마우스 누름 (드래그 시작 감지)
            if (Input.GetMouseButtonDown(0))
            {
                TrialCardView cardUnderMouse = GetCardUnderMouse(); // 마우스 아래 카드 찾기

                if (cardUnderMouse != null)
                {
                    // A. 발언 마치기 카드 드래그 조건: 선택된 카드가 0장이고, 클릭한 카드가 발언 마치기 카드일 때
                    if (_selectedIndices.Count == 0 && cardUnderMouse.InventoryIndex < 0)
                    {
                        _isMouseDownToCheck = true;
                        _initialMousePos = Input.mousePosition;
                        _draggingEndCardView = cardUnderMouse; // 드래그 대상 저장
                    }
                    // B. 증거 제출 드래그 조건: 선택된 카드가 2장이고, 클릭한 카드가 선택된 카드 중 하나일 때
                    else if (_selectedIndices.Count == 2 && _selectedIndices.Contains(cardUnderMouse.InventoryIndex))
                    {
                        _isMouseDownToCheck = true;
                        _initialMousePos = Input.mousePosition;
                        
                        // 화살표 시작점 계산
                        float heightOffset = 150f; 
                        RectTransform rect = cardUnderMouse.GetComponent<RectTransform>();
                        if (rect) heightOffset = rect.rect.height * cardUnderMouse.transform.lossyScale.y * 0.5f;
                        _currentDragStartPos = cardUnderMouse.transform.position + (cardUnderMouse.transform.up * heightOffset);
                    }
                    else
                    {
                        _isMouseDownToCheck = false;
                    }
                }
                else
                {
                    _isMouseDownToCheck = false;
                }
            }

            // 2. 마우스 이동 (드래그 중)
            if (Input.GetMouseButton(0))
            {
                if (_isMouseDownToCheck && !_isDraggingEvidence && !_isDraggingEndSpeech)
                {
                    float dist = Vector2.Distance(_initialMousePos, Input.mousePosition);
                    if (dist > dragThreshold)
                    {
                        // 드래그 시작! 어떤 모드인지 확인
                        if (_draggingEndCardView != null)
                        {
                            _isDraggingEndSpeech = true;
                            // ★ 자동 정렬 끄기 -> 마우스 따라다니게 함
                            _draggingEndCardView.IsAutoLayoutEnabled = false; 
                        }
                        else
                        {
                            _isDraggingEvidence = true;
                        }
                    }
                }

                // A. 발언 마치기 카드 드래그 중 -> 카드가 마우스 따라다님
                if (_isDraggingEndSpeech && _draggingEndCardView != null)
                {
                    Vector3 mouseWorldPos = GetMouseWorldPosition(0f);
                    _draggingEndCardView.transform.position = mouseWorldPos;
                }
                // B. 증거 제출 드래그 중 -> 화살표 표시
                else if (_isDraggingEvidence)
                {
                    Vector3 mouseWorldPos = GetMouseWorldPosition(_currentDragStartPos.z);
                    if (arrowController != null) arrowController.ShowArrow(_currentDragStartPos, mouseWorldPos);
                    HandleDragHoverPreview(mouseWorldPos);
                }
            }

            // 3. 마우스 뗌 (드롭)
            if (Input.GetMouseButtonUp(0))
            {
                if (_isDraggingEndSpeech)
                {
                    StopEndSpeechDrag();
                }
                else if (_isDraggingEvidence)
                {
                    StopEvidenceDrag();
                }

                // 상태 초기화
                _isDraggingEndSpeech = false;
                _isDraggingEvidence = false;
                _isMouseDownToCheck = false;
                _draggingEndCardView = null;
            }
        }

        // ==================================================================================
        // 드래그 종료 처리 메서드들
        // ==================================================================================

        private void StopEndSpeechDrag()
        {
            if (_draggingEndCardView != null)
            {
                // 발언 종료 실행!
                // 여기서 유효성 검사(특정 위치에 놓았는지 등)를 추가할 수 있습니다.
                // 지금은 "드래그했다 놓으면 무조건 발동"으로 처리합니다.
                
                Debug.Log("[TrialHand] 발언 종료 요청!");
                
                if (TrialManager.Instance != null && TrialManager.Instance.LocalPlayer != null)
                {
                    TrialManager.Instance.LocalPlayer.EndSpeech();
                }

                // 카드 파괴 (사용했으므로)
                _spawnedCards.Remove(_draggingEndCardView);
                Destroy(_draggingEndCardView.gameObject);
            }
        }

        private void StopEvidenceDrag()
        {
            if (arrowController != null) arrowController.HideArrow();
            if (_lastHoveredPlayer != null) { _lastHoveredPlayer.HidePreview(); _lastHoveredPlayer = null; }
            
            // 제출 시도
            TrySubmitEvidence();
        }

        // ==================================================================================
        // 헬퍼 메서드
        // ==================================================================================

        // ★ [수정] 마우스 아래에 있는 "모든" 카드를 찾습니다 (선택 여부 상관없이)
        private TrialCardView GetCardUnderMouse()
        {
            // 역순으로 탐색 (화면상 위에 그려진 카드를 먼저 잡기 위함)
            for (int i = _spawnedCards.Count - 1; i >= 0; i--)
            {
                TrialCardView cardView = _spawnedCards[i];
                if (cardView != null)
                {
                    RectTransform rectTransform = cardView.GetComponent<RectTransform>();
                    if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main)) 
                        return cardView;
                }
            }
            return null;
        }

        private void TrySubmitEvidence()
        {
            Vector3 mousePos = GetMouseWorldPosition(0f); 
            var targetView = GetTargetPlayerAtPoint(mousePos);
            if (targetView != null && targetView.OwnerId != NetworkManager.Singleton.LocalClientId)
            {
                if(_myInventory != null)
                {
                    Debug.Log($"[TrialHand] 증거 제출! Target: {targetView.OwnerId}");
                    _myInventory.SubmitEvidenceServerRpc(_selectedIndices[0], _selectedIndices[1], targetView.OwnerId);
                }
                _selectedIndices.Clear();
                UpdateFilterVisuals();
                return;
            }
            Debug.Log("[System] 더 적절한 타겟을 찾아보자.");
        }
        
        // ... (나머지 Layout, Preview 로직 등은 기존 유지) ...
        
        private void HandleDragHoverPreview(Vector3 mousePos)
        {
            CourtPlayerView targetView = TryGetValidTarget(mousePos);
            if (targetView == null) { ClearLastHoveredPlayer(); return; }
            if (_lastHoveredPlayer != null && _lastHoveredPlayer != targetView) _lastHoveredPlayer.HidePreview();
            if (!TryGetSelectedCards(out CardItemData card1, out CardItemData card2)) return; 
            UpdateTargetPreview(targetView, card1, card2);
            _lastHoveredPlayer = targetView;
        }

        private CourtPlayerView TryGetValidTarget(Vector3 mousePos)
        {
            var view = GetTargetPlayerAtPoint(mousePos);
            if (view == null || view.OwnerId == NetworkManager.Singleton.LocalClientId) return null;
            return view;
        }

        private CourtPlayerView GetTargetPlayerAtPoint(Vector3 mousePos)
        {
            Vector2 point = new Vector2(mousePos.x, mousePos.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);
            if (hits == null || hits.Length == 0) return null;

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                // 콜라이더가 자식 오브젝트에 붙은 경우까지 고려
                var view = hit.GetComponentInParent<CourtPlayerView>();
                if (view != null) return view;
            }

            return null;
        }
        private void ClearLastHoveredPlayer()
        {
            if (_lastHoveredPlayer != null) { _lastHoveredPlayer.HidePreview(); _lastHoveredPlayer = null; }
        }
        private bool TryGetSelectedCards(out CardItemData c1, out CardItemData c2)
        {
            c1 = default; c2 = default;
            if (_myInventory == null) return false;
            if (_selectedIndices.Count < 2) return false;
            int idx1 = _selectedIndices[0]; int idx2 = _selectedIndices[1];
            if (idx1 >= _myInventory.OwnedCards.Count || idx2 >= _myInventory.OwnedCards.Count) return false;
            c1 = _myInventory.OwnedCards[idx1]; c2 = _myInventory.OwnedCards[idx2];
            return true;
        }
        private void UpdateTargetPreview(CourtPlayerView targetView, CardItemData c1, CardItemData c2)
        {
            if (CourtGameRules.IsUnknownResult(c1, c2)) targetView.ShowPreview("?");
            else
            {
                int targetIndex = VoteModel.Instance.GetPlayerIndex(targetView.OwnerId);
                int currentVote = (targetIndex != -1) ? VoteModel.Instance.GetVoteCount(targetIndex) : 0;
                int damage = CourtGameRules.CalculatePreviewScore(c1, c2, currentVote);
                targetView.ShowPreview(damage);
            }
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
                // 선택된 카드는 호버링 계산에서 제외 (이미 솟아있으므로)
                if (_hoveredCard.InventoryIndex < 0 || !_selectedIndices.Contains(_hoveredCard.InventoryIndex)) 
                    hoveredIndex = rawIndex;
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

                // ★ 중요: 현재 드래그 중인 발언 마치기 카드는 레이아웃 계산을 무시 (SetTargetState 호출해도 View 내부에서 무시됨)
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
    }
}
