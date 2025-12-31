using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using Court; 

namespace Court.Hand
{
    public class TrialHandPresenter : NetworkBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject trialCardContainerPrefab;
        [SerializeField] private Transform handPivot;
        [SerializeField] private Transform cardsParent;
        [SerializeField] private ArrowController arrowController;

        [Space(10)]
        [Header("1. Basic Layout (기본 배치)")] 
        [Tooltip("부채꼴의 반지름")]
        [SerializeField] private float arcRadius = 1500f;
        
        [Tooltip("카드 사이의 기본 각도 간격")]
        [SerializeField] private float angleSpacing = 5f;

        [Space(10)]
        [Header("2. Interaction (반응 설정)")]
        [Tooltip("호버링/선택 시 카드가 위로 올라오는 정도 (Y축)")]
        [SerializeField] private float hoverYOffset = 120f;
        
        [Tooltip("선택/호버 시 카드가 커지는 배율")]
        [Range(1.0f, 2.0f)]
        [SerializeField] private float selectedScale = 1.3f; 

        [Space(10)]
        [Header("3. Separation (밀어내기)")]
        [Tooltip("호버링 시 밀려나는 각도")]
        [SerializeField] private float hoverSeparationAngle = 3f;
        
        [Tooltip("선택 시 밀려나는 각도")]
        [SerializeField] private float selectedSeparationAngle = 3f;
        
        [Space(10)]
        [Header("4. Input Settings (입력 감도)")]
        [Tooltip("마우스를 누르고 이 거리(픽셀)만큼 움직여야 드래그로 인식합니다.")]
        [SerializeField] private float dragThreshold = 20f; // ★ 추가됨: 드래그 민감도

        // 내부 상태 변수
        private List<TrialCardView> _spawnedCards = new List<TrialCardView>();
        private List<int> _selectedIndices = new List<int>();
        private TrialCardView _hoveredCard;
        
        private bool _isDragging = false;
        private bool _isMouseDownToCheck = false; // ★ 추가됨: 클릭인지 드래그인지 간 보는 중
        private Vector2 _initialMousePos;         // ★ 추가됨: 처음 누른 위치
        
        private CardInventoryModel _myInventory;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) { this.enabled = false; return; }

            ulong myId = NetworkManager.Singleton.LocalClientId;
            var playerModel = global::PlayerHelperManager.Instance.GetPlayerModelByClientId(myId);
            if (playerModel != null)
            {
                _myInventory = playerModel.GetComponent<CardInventoryModel>();
                if (_myInventory != null)
                {
                    _myInventory.OwnedCards.OnListChanged += OnInventoryChanged;
                    InitializeHand();
                }
            }
        }

        private void OnInventoryChanged(NetworkListEvent<CardItemData> changeEvent) => InitializeHand();

        public override void OnNetworkDespawn()
        {
            if (_myInventory != null) _myInventory.OwnedCards.OnListChanged -= OnInventoryChanged;
        }

        private void InitializeHand()
        {
            foreach (var card in _spawnedCards) if (card) Destroy(card.gameObject);
            _spawnedCards.Clear();
            _selectedIndices.Clear();
            
            if (arrowController != null) arrowController.HideArrow();

            int index = 0;
            foreach (var cardData in _myInventory.OwnedCards)
            {
                GameObject container = Instantiate(trialCardContainerPrefab, cardsParent);
                TrialCardView view = container.GetComponent<TrialCardView>();
                GameObject visual = CardItemFactoryManager.Instance.CreateCardForInventory(cardData);

                view.Initialize(cardData, index, visual);

                view.OnHoverEnter += (v) => _hoveredCard = v;
                view.OnHoverExit += (v) => { if (_hoveredCard == v) _hoveredCard = null; };
                view.OnClick += HandleCardClick;

                _spawnedCards.Add(view);
                index++;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;
            UpdateHandLayout();
            UpdateDragInput();
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
                // 선택된 카드는 호버링 대상에서 제외
                if (!_selectedIndices.Contains(rawIndex)) 
                {
                     hoveredIndex = rawIndex;
                }
            }

            for (int i = 0; i < count; i++)
            {
                TrialCardView card = _spawnedCards[i];
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
                Vector3 pos = handPivot.localPosition + new Vector3(Mathf.Sin(rad) * arcRadius, Mathf.Cos(rad) * arcRadius, 0);
                Quaternion rot = Quaternion.Euler(0, 0, -angle);
                float scale = 1f;

                bool isSelected = _selectedIndices.Contains(card.InventoryIndex);

                if (i == hoveredIndex)
                {
                    pos += transform.up * hoverYOffset;
                    rot = Quaternion.identity;
                    scale = selectedScale;
                    card.transform.SetAsLastSibling();
                }
                else if (isSelected)
                {
                    pos += transform.up * hoverYOffset;
                    rot = Quaternion.identity;
                    scale = selectedScale;
                    
                    if (hoveredIndex == -1) card.transform.SetAsLastSibling();
                }
                else
                {
                    if (hoveredIndex == -1) card.transform.SetSiblingIndex(i);
                }

                card.SetTargetState(pos, rot, scale);
            }
        }

        private void HandleCardClick(TrialCardView cardView)
        {
            // 드래그 중이었다면 클릭 이벤트 무시 (드래그 끝내고 손 뗄 때 클릭으로 오인되는 것 방지)
            if (_isDragging) return;

            int idx = cardView.InventoryIndex;
            if (_selectedIndices.Contains(idx)) _selectedIndices.Remove(idx);
            else if (_selectedIndices.Count < 2) _selectedIndices.Add(idx);

            UpdateFilterVisuals();
        }

        private void UpdateFilterVisuals()
        {
            if (_selectedIndices.Count == 0)
            {
                foreach (var c in _spawnedCards) c.SetVisualState(false, false);
                return;
            }

            CardItemData firstCard = _myInventory.OwnedCards[_selectedIndices[0]];

            foreach (var cardView in _spawnedCards)
            {
                bool isSelected = _selectedIndices.Contains(cardView.InventoryIndex);
                bool isDisabled = false;

                if (!isSelected)
                {
                    if (!CourtGameRules.IsCompatible(firstCard, cardView.Data)) isDisabled = true;
                    if (_selectedIndices.Count >= 2) isDisabled = true;
                }
                cardView.SetVisualState(isSelected, isDisabled);
            }
        }

        // ★ [수정됨] 드래그 판정 로직 개선
        private void UpdateDragInput()
        {
            if (_selectedIndices.Count != 2)
            {
                // 조건 불만족 시 모든 상태 초기화
                if (_isDragging) StopDrag();
                _isMouseDownToCheck = false;
                return;
            }

            // 1. 마우스 누름: 드래그인지 클릭인지 감시 시작
            if (Input.GetMouseButtonDown(0))
            {
                _isMouseDownToCheck = true;
                _initialMousePos = Input.mousePosition;
                // 아직 _isDragging = true로 만들지 않음!
            }

            // 2. 마우스 누르고 있는 중
            if (Input.GetMouseButton(0))
            {
                // 아직 드래그 상태가 아니라면, 이동 거리 체크
                if (_isMouseDownToCheck && !_isDragging)
                {
                    float dist = Vector2.Distance(_initialMousePos, Input.mousePosition);
                    // 설정한 임계값(dragThreshold)보다 많이 움직이면 드래그 시작
                    if (dist > dragThreshold)
                    {
                        _isDragging = true;
                        Debug.Log("드래그 시작!");
                    }
                }

                // 진짜 드래그 중일 때만 화살표 그리기
                if (_isDragging)
                {
                    int rightIndex = Mathf.Max(_selectedIndices[0], _selectedIndices[1]);
                    // 인덱스가 범위를 벗어나지 않도록 방어 코드
                    if(rightIndex < _spawnedCards.Count)
                    {
                        TrialCardView rightCard = _spawnedCards[rightIndex]; 
                        Vector3 startPos = rightCard.transform.position;

                        Vector3 mouseScreenPos = Input.mousePosition;
                        if (Camera.main != null)
                        {
                            float distanceToCamera = Mathf.Abs(startPos.z - Camera.main.transform.position.z);
                            mouseScreenPos.z = distanceToCamera; 
                            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                            mouseWorldPos.z = startPos.z;

                            if (arrowController != null) 
                            {
                                arrowController.ShowArrow(startPos, mouseWorldPos);
                            }
                        }
                    }
                }
            }

            // 3. 마우스 뗌
            if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                {
                    StopDrag();
                    TrySubmitEvidence();
                }
                
                // 상태 초기화
                _isDragging = false;
                _isMouseDownToCheck = false;
            }
        }

        private void StopDrag()
        {
            _isDragging = false;
            if (arrowController != null) arrowController.HideArrow();
        }

        private void TrySubmitEvidence()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                var targetPlayer = result.gameObject.GetComponentInParent<global::PlayerView>();
                if (targetPlayer != null && targetPlayer.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log($"[TrialHand] 타겟 발견: {targetPlayer.OwnerClientId}");
                    SubmitEvidenceServerRpc(_selectedIndices[0], _selectedIndices[1], targetPlayer.OwnerClientId);
                    return;
                }
            }
        }

        [ServerRpc]
        public void SubmitEvidenceServerRpc(int idx1, int idx2, ulong targetId)
        {
            CardItemData c1 = _myInventory.OwnedCards[idx1];
            CardItemData c2 = _myInventory.OwnedCards[idx2];
            if (CourtGameRules.IsCompatible(c1, c2))
            {
                Debug.Log($"Player {OwnerClientId} submitted evidence against {targetId}");
            }
        }
    }
}