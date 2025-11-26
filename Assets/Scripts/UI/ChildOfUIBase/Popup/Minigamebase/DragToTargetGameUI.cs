using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
using System.Collections.Generic;

//TODO: 올릴 골드 세팅하고(인스펙터용 열기) 클리어 시 골드증가(server rpc)
public class DragToTargetGameUI : UIPopup
{
    [SerializeField]
    private bool whenFitTransparent = false;
    
    [SerializeField] private float completeDelay = 1f;
    private float completeDelayTimer = 0f;
    
    /// <summary>
    /// 사용하지 않는 오브젝트를 비활성화 할 경우, 반드시 인덱스 뒷번호부터 비활성화할 것.
    /// </summary>
    [Tooltip("Drop Zone Positions: you have to fit index 0 to 4 order like gameObject name: DragItem0, DragItem1, DragItem2, DragItem3, DragItem4")]
    [SerializeField] private RectTransform[] onDroppedFitPositions;
    
    private List<Vector3> _onDroppedFitPositions;
    private List<GameObject> dragItemGameObject;

    private int matchCount = 0;
    
    [Header("Put in here match count")]
    [SerializeField]
    private int TOTAL_MATCH_COUNT = 3;
    enum Images
    {
        DropZone0,    // 아이템을 놓을 영역
        DropZone1,
        DropZone2,
        DropZone3,
        DropZone4,
        DropZone5,
        DropZone6,
        DropZone7,
        DropZone8,
        DropZone9,
        DragItem0,     // 드래그할 아이템
        DragItem1,
        DragItem2,
        DragItem3,
        DragItem4,
        DragItem5,
        DragItem6,
        DragItem7,
        DragItem8,
        DragItem9,
    }

    
    private void Start()
    {
        base.Init();
        
        dragItemGameObject = new List<GameObject>();
        _onDroppedFitPositions = new List<Vector3>();
        
        Bind<Image>(typeof(Images));
        for (int i = 0; i < TOTAL_MATCH_COUNT; i++)
        {
            dragItemGameObject.Add(Get<Image>((int)Images.DragItem0 + i).gameObject);
            BindEvent(dragItemGameObject[i],OnBeginDrag, GameEvents.UIEvent.BeginDrag);
            BindEvent(dragItemGameObject[i],OnEndDrag, GameEvents.UIEvent.EndDrag);
            BindEvent(dragItemGameObject[i],OnDragging, GameEvents.UIEvent.Drag);
            
            _onDroppedFitPositions.Add(dragItemGameObject[i].transform.position);
        
            GameObject dropZoneGameObject = Get<Image>((int)Images.DropZone0 + i).gameObject;
            BindEvent(dropZoneGameObject, OnDropped, GameEvents.UIEvent.Drop);
        }
        
    }

    private void Update()
    {
        if (matchCount == TOTAL_MATCH_COUNT)
        {
            completeDelayTimer += Time.deltaTime;
            if (completeDelayTimer < completeDelay) return;
            OnGameComplete();
        }
    }

    int ReturnDragItemGameObjectIndex(string objectName)
    {
        Match match = Regex.Match(objectName, @"\d+");
        if (match.Success)
        {
            //out int index는 if문 안에서만 사용할 수 있는 index라는 새로운 정수 변수를 선언
            if (int.TryParse(match.Value, out int index))
            {
                return index;
            }
        }
        return -1;
    }
    
    private void OnEndDrag(PointerEventData data)
    {
        string objectName = data.pointerDrag.gameObject.name;
        int index = ReturnDragItemGameObjectIndex(objectName);
        
        if (index!=-1)
        {
            dragItemGameObject[index].transform.position = _onDroppedFitPositions[index];
        }
    }

    private void OnDropped(PointerEventData data)
    {
        int myIndex= ReturnDragItemGameObjectIndex(data.pointerEnter.name);
        int otherIndex = ReturnDragItemGameObjectIndex(data.pointerDrag.name);

        if (myIndex != -1 && otherIndex != -1)
        {
            if (myIndex == otherIndex)
            {
                matchCount++;
                
                //투명컬러
                if (whenFitTransparent)
                {
                    Color color = new Color(1f, 1f, 1f, 0.5f);
                    data.pointerDrag.gameObject.GetComponent<Image>().color = color;
                    data.pointerEnter.gameObject.GetComponent<Image>().color = color;
                }
                SetDragItemPosition(index: otherIndex);
                
            }
            
        }
    }

    void SetDragItemPosition(int index)
    {
        //해당 인덱스로, 매칭위치 덮어씌우기
        _onDroppedFitPositions[index] = onDroppedFitPositions[index].position;
    }

    private void OnBeginDrag(PointerEventData data)
    {
        string objectName = data.pointerDrag.gameObject.name;
        int index = ReturnDragItemGameObjectIndex(objectName);
        if (index!=-1)
        {
            _onDroppedFitPositions[index] = dragItemGameObject[index].transform.position;
        }
    }
    
    private void OnDragging(PointerEventData data)
    {
        string objectName = data.pointerDrag.gameObject.name;
        int index = ReturnDragItemGameObjectIndex(objectName);
        if (index!=-1)
        {
            //위치 움직이기
            dragItemGameObject[index].transform.position = data.position;
        }
    }
    
    private void OnGameComplete()
    {
        Debug.Log("타겟 위치로 드래그하기 성공!");
        gameObject.SetActive(false);
    }

}
