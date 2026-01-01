using System.Collections;
using System.Collections.Generic;
using UnityEngine; // NetworkBehaviour 제거됨
using Court;
using Unity.Netcode; // NetworkListEvent 등을 위해 필요

namespace Court.Hand
{
    // [수정] NetworkBehaviour -> MonoBehaviour 변경 (UI는 네트워크 몰라도 됨)
    public class TrialHandPresenter : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject trialCardContainerPrefab;
        [SerializeField] private Transform handPivot;  // 부채꼴의 중심점
        [SerializeField] private Transform cardsParent; // 카드가 생성될 부모
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

        // 내부 상태 변수
        private List<TrialCardView> _spawnedCards = new List<TrialCardView>();
        private List<int> _selectedIndices = new List<int>();
        private TrialCardView _hoveredCard;
        
        private bool _isDragging = false;
        private bool _isMouseDownToCheck = false;
        private Vector2 _initialMousePos;
        
        private CardInventoryModel _myInventory;
        private float _debugTimer = 0f;

        private void Awake()
        {
            if (cardsParent == null) cardsParent = transform;
            if (handPivot == null) handPivot = transform; // 피벗 없으면 내 위치 기준
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
            // 인벤토리 연결 전에는 로직 수행 금지
            if (_myInventory == null) return;

            // [디버그] 3초마다 상태 확인
            _debugTimer += Time.deltaTime;
            if (_debugTimer > 3.0f)
            {
                _debugTimer = 0f;
                int realCount = _myInventory.OwnedCards.Count;
                int uiCount = _spawnedCards.Count;
                // Debug.Log($"[Hand UI] 데이터: {realCount} | UI: {uiCount}");
            }

            UpdateHandLayout();    // 부채꼴 배치 계산
            UpdateDragInput();     // 드래그 입력 처리
            UpdateFilterVisuals(); // 선택/호버 비주얼 처리
        }

        // ★ [핵심] 외부(CardInventoryModel)에서 호출해주는 초기화 함수
        public void SetInventory(CardInventoryModel inventory)
        {
            _myInventory = inventory;
            
            // 데이터 변경 감지 이벤트 연결
            _myInventory.OwnedCards.OnListChanged += OnInventoryChanged;

            Debug.Log($"[UI] 인벤토리 연결됨! 현재 카드 수: {_myInventory.OwnedCards.Count}");
            
            // 초기화: 현재 데이터로 카드 생성
            InitializeHand(); 
        }

        private void OnInventoryChanged(NetworkListEvent<CardItemData> changeEvent)
        {
            InitializeHand();
        }

        private void InitializeHand()
        {
            // 기존 카드 UI 삭제
            foreach (var card in _spawnedCards)
            {
                if (card) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
            _selectedIndices.Clear();
            
            if (arrowController != null) arrowController.HideArrow();

            if (_myInventory == null) return;

            // 데이터 기반으로 카드 UI 생성
            int index = 0;
            foreach (var cardData in _myInventory.OwnedCards)
            {
                GameObject container = Instantiate(trialCardContainerPrefab, cardsParent);
                TrialCardView view = container.GetComponent<TrialCardView>();
                
                // 실제 카드 이미지 생성 (Factory 이용)
                GameObject visual = CardItemFactoryManager.Instance.CreateCardForInventory(cardData);

                view.Initialize(cardData, index, visual);

                // UI 이벤트 연결 (클릭, 호버)
                view.OnHoverEnter += (v) => { _hoveredCard = v; UpdateFilterVisuals(); }; 
                view.OnHoverExit += (v) => { if (_hoveredCard == v) { _hoveredCard = null; UpdateFilterVisuals(); } };
                view.OnClick += HandleCardClick;

                _spawnedCards.Add(view);
                index++;
            }
            
            UpdateFilterVisuals();
        }

        // ... (기존 부채꼴 배치 로직 유지) ...
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
                if (!_selectedIndices.Contains(rawIndex)) hoveredIndex = rawIndex;
            }

            for (int i = 0; i < count; i++)
            {
                TrialCardView card = _spawnedCards[i];
                // 카드 UI가 삭제되었거나 비활성 상태면 건너뜀
                if (card == null) continue;

                float angle = startAngle + (i * angleSpacing);
                float pushAmount = 0f;

                if (hoveredIndex != -1)
                {
                    if (i < hoveredIndex) pushAmount -= hoverSeparationAngle;
                    if (i > hoveredIndex) pushAmount += hoverSeparationAngle;
                }
                
                foreach (int selIdx in _selectedIndices)
                {
                    if (i < selIdx) pushAmount -= selectedSeparationAngle;
                    if (i > selIdx) pushAmount += selectedSeparationAngle;
                }
                
                angle += pushAmount;
                float rad = angle * Mathf.Deg2Rad;
                
                // 위치 계산 (피벗 기준)
                Vector3 pos = handPivot.localPosition + new Vector3(Mathf.Sin(rad) * arcRadius, Mathf.Cos(rad) * arcRadius, 0);
                Quaternion rot = Quaternion.Euler(0, 0, -angle);
                float scale = 1f;

                bool isSelected = _selectedIndices.Contains(card.InventoryIndex);

                if (i == hoveredIndex || isSelected)
                {
                    pos += transform.up * hoverYOffset;
                    rot = Quaternion.identity;
                    scale = selectedScale;
                    card.transform.SetAsLastSibling(); // 맨 앞으로 가져오기
                }
                else
                {
                    if (hoveredIndex == -1) card.transform.SetSiblingIndex(i);
                }

                // TrialCardView 내부의 Lerp 이동 함수 호출
                card.SetTargetState(pos, rot, scale);
            }
        }

        // ... (입력 및 인터랙션 로직 유지) ...
        private void HandleCardClick(TrialCardView cardView)
        {
            if (_isDragging) return;

            int idx = cardView.InventoryIndex;

            if (_selectedIndices.Contains(idx))
            {
                _selectedIndices.Remove(idx);
                UpdateFilterVisuals();
                return;
            }

            if (_selectedIndices.Count >= 2)
            {
                Debug.Log("[System] 이미 2장을 선택했습니다.");
                return;
            }

            if (!IsCardSelectable(cardView))
            {
                Debug.Log("[System] 호환되지 않는 카드입니다.");
                return; 
            }

            _selectedIndices.Add(idx);
            UpdateFilterVisuals();
        }

        private bool IsCardSelectable(TrialCardView targetCard)
        {
            if (_selectedIndices.Count == 0) return true;
            if (_selectedIndices[0] >= _myInventory.OwnedCards.Count) return false;

            CardItemData firstCard = _myInventory.OwnedCards[_selectedIndices[0]];
            return CourtGameRules.IsCompatible(firstCard, targetCard.Data);
        }

        private void UpdateFilterVisuals()
        {
            if (_myInventory == null) return;

            bool hasCriteria = _selectedIndices.Count > 0 || _hoveredCard != null;
            CardItemData criteriaCard = default;
            int criteriaIndex = -1;

            if (_selectedIndices.Count > 0)
            {
                criteriaIndex = _selectedIndices[0];
                if(criteriaIndex < _myInventory.OwnedCards.Count)
                    criteriaCard = _myInventory.OwnedCards[criteriaIndex];
            }
            else if (_hoveredCard != null)
            {
                criteriaIndex = _hoveredCard.InventoryIndex;
                criteriaCard = _hoveredCard.Data;
            }

            foreach (var cardView in _spawnedCards)
            {
                bool isSelected = _selectedIndices.Contains(cardView.InventoryIndex);
                bool isDisabled = false;

                if (hasCriteria && !isSelected && _selectedIndices.Count < 2)
                {
                    if (cardView.InventoryIndex != criteriaIndex)
                    {
                        if (!CourtGameRules.IsCompatible(criteriaCard, cardView.Data))
                            isDisabled = true;
                    }
                }
                
                cardView.SetVisualState(isSelected, isDisabled);
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

            if (Input.GetMouseButtonDown(0))
            {
                _isMouseDownToCheck = true;
                _initialMousePos = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                if (_isMouseDownToCheck && !_isDragging)
                {
                    float dist = Vector2.Distance(_initialMousePos, Input.mousePosition);
                    if (dist > dragThreshold) _isDragging = true;
                }

                if (_isDragging)
                {
                    // 화살표 표시
                    if(_selectedIndices.Count > 0) {
                       int firstIdx = _selectedIndices[0];
                       // UI상의 해당 카드 위치 찾기
                       TrialCardView cardView = _spawnedCards.Find(x => x.InventoryIndex == firstIdx);
                       if(cardView != null) {
                           Vector3 startPos = cardView.transform.position;
                           Vector3 mouseWorldPos = GetMouseWorldPosition(startPos.z);
                           if (arrowController != null) arrowController.ShowArrow(startPos, mouseWorldPos);
                       }
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                {
                    StopDrag();
                    //TrySubmitEvidence();
                }
                _isDragging = false;
                _isMouseDownToCheck = false;
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

        /*
        private void TrySubmitEvidence()
        {
            Vector3 mousePos = GetMouseWorldPosition(0f); 
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                var targetView = hit.collider.GetComponent<CourtPlayerView>();
                // 내 자신이 아닌 다른 플레이어에게 드롭했을 때
                if (targetView != null && targetView.OwnerId != NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log($"[TrialHand] 타겟 확정: {targetView.OwnerId}");
                    
                    // ★ [수정] UI는 RPC를 못 쏘므로, 인벤토리(NetworkBehaviour)에게 대신 쏴달라고 요청
                    if(_myInventory != null)
                    {
                        _myInventory.SubmitEvidenceServerRpc(_selectedIndices[0], _selectedIndices[1], targetView.OwnerId);
                    }
                    return;
                }
            }
            Debug.Log("[TrialHand] 유효한 타겟에 드롭하지 않았습니다.");
        }*/
    }
}