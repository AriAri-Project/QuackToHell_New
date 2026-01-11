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
        
        // 드래그 시작 위치
        private Vector3 _currentDragStartPos; 
        
        // ★ 추가: 마지막으로 호버링했던 플레이어 (프리뷰 끄기용)
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
            if (_selectedIndices.Count != 2)
            {
                if (_isDragging) StopDrag();
                _isMouseDownToCheck = false;
                return;
            }

            // 1. 마우스 누르기: 선택된 카드 위에서만 시작
            if (Input.GetMouseButtonDown(0))
            {
                TrialCardView startCard = GetMouseOverSelectedCard();
                
                if (startCard != null)
                {
                    _isMouseDownToCheck = true;
                    _initialMousePos = Input.mousePosition;
                    
                    float heightOffset = 150f; 
                    RectTransform rect = startCard.GetComponent<RectTransform>();
                    if (rect) heightOffset = rect.rect.height * startCard.transform.lossyScale.y * 0.5f;

                    _currentDragStartPos = startCard.transform.position + (startCard.transform.up * heightOffset);
                }
                else
                {
                    _isMouseDownToCheck = false;
                }
            }

            // 2. 드래그 중
            if (Input.GetMouseButton(0))
            {
                if (_isMouseDownToCheck && !_isDragging)
                {
                    float dist = Vector2.Distance(_initialMousePos, Input.mousePosition);
                    if (dist > dragThreshold) _isDragging = true;
                }

                if (_isDragging)
                {
                    Vector3 mouseWorldPos = GetMouseWorldPosition(_currentDragStartPos.z);
                    if (arrowController != null) 
                        arrowController.ShowArrow(_currentDragStartPos, mouseWorldPos);

                    // ★ 드래그 중 프리뷰 체크 함수 호출
                    HandleDragHoverPreview(mouseWorldPos);
                }
            }

            // 3. 드래그 종료
            if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                {
                    StopDrag();
                    TrySubmitEvidence(); // 제출 시도
                }
                _isDragging = false;
                _isMouseDownToCheck = false;
            }
        }

        /// <summary>
        /// 드래그 중 마우스 아래 플레이어 감지 및 프리뷰 요청 (리팩토링됨)
        /// </summary>
        private void HandleDragHoverPreview(Vector3 mousePos)
        {
            // 1. 유효한 타겟 플레이어를 찾음 (못 찾으면 null)
            CourtPlayerView targetView = TryGetValidTarget(mousePos);

            // 2. 타겟이 없으면 -> 기존 프리뷰 끄고 종료 (Early Return)
            if (targetView == null)
            {
                ClearLastHoveredPlayer();
                return;
            }

            // 3. 타겟이 바뀌었으면 -> 이전 타겟 프리뷰 끄기
            if (_lastHoveredPlayer != null && _lastHoveredPlayer != targetView)
            {
                _lastHoveredPlayer.HidePreview();
            }

            // 4. 인벤토리나 선택된 카드가 유효한지 확인하고 카드 가져오기
            if (!TryGetSelectedCards(out CardItemData card1, out CardItemData card2))
            {
                return; 
            }

            // 5. 실제 프리뷰 업데이트 로직 수행
            UpdateTargetPreview(targetView, card1, card2);

            // 6. 현재 타겟 캐싱
            _lastHoveredPlayer = targetView;
        }

        // --- ⬇️ 아래는 새로 추가된 헬퍼 함수들입니다 ⬇️ ---

        /// <summary>
        /// 마우스 위치에서 유효한 타겟(나 제외)을 찾습니다.
        /// </summary>
        private CourtPlayerView TryGetValidTarget(Vector3 mousePos)
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider == null) return null;

            var view = hit.collider.GetComponent<CourtPlayerView>();
            
            // 뷰가 없거나, 타겟이 나 자신이면 무효
            if (view == null || view.OwnerId == NetworkManager.Singleton.LocalClientId) return null;

            return view;
        }

        /// <summary>
        /// 마지막으로 호버했던 플레이어의 프리뷰를 끕니다.
        /// </summary>
        private void ClearLastHoveredPlayer()
        {
            if (_lastHoveredPlayer != null)
            {
                _lastHoveredPlayer.HidePreview();
                _lastHoveredPlayer = null;
            }
        }

        /// <summary>
        /// 현재 선택된 2장의 카드를 안전하게 가져옵니다.
        /// </summary>
        private bool TryGetSelectedCards(out CardItemData c1, out CardItemData c2)
        {
            c1 = default;
            c2 = default;

            if (_myInventory == null) return false;
            if (_selectedIndices.Count < 2) return false;

            int idx1 = _selectedIndices[0];
            int idx2 = _selectedIndices[1];

            // 인덱스 범위 체크
            if (idx1 >= _myInventory.OwnedCards.Count || idx2 >= _myInventory.OwnedCards.Count) return false;

            c1 = _myInventory.OwnedCards[idx1];
            c2 = _myInventory.OwnedCards[idx2];
            return true;
        }

        /// <summary>
        /// 타겟 뷰에 실제 프리뷰(N카드 여부 등)를 적용합니다.
        /// </summary>
        private void UpdateTargetPreview(CourtPlayerView targetView, CardItemData c1, CardItemData c2)
        {
            // N 카드가 포함되어 있는지 확인 (Rules 사용)
            if (CourtGameRules.IsUnknownResult(c1, c2))
            {
                targetView.ShowPreview("?");
            }
            else
            {
                // 타겟의 현재 점수 가져오기
                int targetIndex = VoteModel.Instance.GetPlayerIndex(targetView.OwnerId);
                int currentVote = (targetIndex != -1) ? VoteModel.Instance.GetVoteCount(targetIndex) : 0;

                // 확정된 점수 계산
                int damage = CourtGameRules.CalculatePreviewScore(c1, c2, currentVote);
                targetView.ShowPreview(damage);
            }
        }

        private void StopDrag()
        {
            _isDragging = false;
            if (arrowController != null) arrowController.HideArrow();
            
            // 드래그가 끝나면 프리뷰도 확실히 꺼줌
            if (_lastHoveredPlayer != null)
            {
                _lastHoveredPlayer.HidePreview();
                _lastHoveredPlayer = null;
            }
        }
        
        private void TrySubmitEvidence()
        {
            // 드롭 시점의 마우스 위치 확인
            Vector3 mousePos = GetMouseWorldPosition(0f); 
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                var targetView = hit.collider.GetComponent<CourtPlayerView>();
                // 유효한 타겟에게 드롭했는지 확인
                if (targetView != null && targetView.OwnerId != NetworkManager.Singleton.LocalClientId)
                {
                    if(_myInventory != null)
                    {
                        Debug.Log($"[TrialHand] 증거 제출! Target: {targetView.OwnerId}");
                        _myInventory.SubmitEvidenceServerRpc(_selectedIndices[0], _selectedIndices[1], targetView.OwnerId);
                    }
                    
                    // 제출 성공 -> 선택 초기화
                    _selectedIndices.Clear();
                    UpdateFilterVisuals();
                    return;
                }
            }
            
            // 타겟이 없는 곳에 드롭
            Debug.Log("[System] 더 적절한 타겟을 찾아보자.");
        }
        
        private TrialCardView GetMouseOverSelectedCard()
        {
            foreach (int index in _selectedIndices)
            {
                TrialCardView cardView = _spawnedCards.Find(x => x.InventoryIndex == index);
                if (cardView != null)
                {
                    RectTransform rectTransform = cardView.GetComponent<RectTransform>();
                    if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main))
                    {
                        return cardView;
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
    }
}