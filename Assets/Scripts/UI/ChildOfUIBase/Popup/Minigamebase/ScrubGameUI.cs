using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
//TODO: 올릴 골드 세팅하고(인스펙터용 열기) 클리어 시 골드증가(server rpc)
public class ScrubGameUI : UIPopup
{
    /* 사용법: 
    /* Target은 10개있음.
     * 안 사용하는 건 비활성화하고, 사용하는 건 활성화하기
     */
    /*
     * 주의사항:
     * 1. 하이어라키창의 오브젝트명 바꾸지 말기
     * 2. 이미 있는 오브젝트에 대해서 추가 / 삭제x
     */
    private List<float> totalScrubDistances;
    
    [Header("Write the goal distance(pixels) to complete the game")]
    [SerializeField] 
    private float distanceToComplete = 1000f;
    [Tooltip("if true, the target image will be transparent while scrubbing.")]
    [SerializeField] 
    private bool isTransparent = false;
    [SerializeField] private float completeDelay = 1f;
    
    
    private Vector3 startPosition;
    private GameObject eraserGameObject;
    private RectTransform eraserRectTransform;
    private List<RectTransform> targetRectTransforms;
    private List<Image> targetImages;
    private bool isComplete = false;
    private List<float> progress;
    private int removeCount = 0;
    private float completeDelayTimer = 0f;
    
    
    enum Images
    {
        Eraser,
        Target1,
        Target2,
        Target3,
        Target4,
        Target5,
        Target6,
        Target7,
        Target8,
        Target9,
        Target10,
    }

    private void Start()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        
        //리스트 초기화
        targetRectTransforms = new List<RectTransform>();
        targetImages = new List<Image>();
        totalScrubDistances = new List<float>();
        progress = new List<float>();

        foreach (Images image in Enum.GetValues(typeof(Images)))
        {
            if (!Get<Image>((int)image).gameObject.activeSelf)
            {
                continue;
            }
            GameObject gameObject = Get<Image>((int)image).gameObject;
            if (image == Images.Eraser)
            {
                eraserGameObject = gameObject;
                eraserRectTransform = Get<Image>((int)image).GetComponent<RectTransform>();
                BindEvent(gameObject,OnRubbing, GameEvents.UIEvent.Drag);
                BindEvent(gameObject,OnBeginDrag, GameEvents.UIEvent.BeginDrag);
                BindEvent(gameObject,OnEndDrag, GameEvents.UIEvent.EndDrag);
                continue;
            }
            targetRectTransforms.Add(Get<Image>((int)image).GetComponent<RectTransform>()); 
            targetImages.Add(Get<Image>((int)image).GetComponent<Image>());
            totalScrubDistances.Add(0f);
            progress.Add(1f);
        }
        
    }

    private void Update()
    {
        if (removeCount == totalScrubDistances.Count)
        {
            completeDelayTimer += Time.deltaTime;
            if (completeDelayTimer < completeDelay) return;
            OnGameComplete();
        }
    }

    /*eraser rect랑 target rect 겹치는지 확인 : (xmin,xmax,ymin,ymax)사이에 포인터가 있는지 체크*/
    private bool IsRectTransformOverlapping(RectTransform rectEraser, RectTransform rectB)
    {
        if(rectB.gameObject.activeSelf == false) {return false;}
        //각 렉트트랜스폼의 월드좌표기준 4개 코너 가져오기
        Vector3[] cornersA = new Vector3[4];
        rectEraser.GetWorldCorners(cornersA);
        //xmin, xmax, ymin, ymax로 사각형 만들기
        Rect rect1 = new Rect(cornersA[0].x, cornersA[0].y, cornersA[2].x - cornersA[0].x, cornersA[2].y - cornersA[0].y);
                
        Vector3[] cornersB = new Vector3[4];
        rectB.GetWorldCorners(cornersB);
        Rect rect2 = new Rect(cornersB[0].x, cornersB[0].y, cornersB[2].x - cornersB[0].x, cornersB[2].y - cornersB[0].y);
        
        return rect1.Overlaps(rect2);
    }
    
    private void OnEndDrag(PointerEventData data)
    {

        eraserGameObject.transform.position = startPosition;
    }

    private void OnBeginDrag(PointerEventData data)
    {
        startPosition = eraserGameObject.transform.position;
    }
    
    private void OnRubbing(PointerEventData data)
    {
        if(isComplete) return;
        
        //위치 움직이기
        eraserGameObject.transform.position = data.position;
        
        
        
        int overlappedIndex = -1;
        for (int i=0;i<targetRectTransforms.Count;i++)
        {
            if (IsRectTransformOverlapping(eraserRectTransform, targetRectTransforms[i]))
            {
                overlappedIndex = i;                    
                break;
            }
        }
        
        //안겹치면 로직x
        if (overlappedIndex == -1)
        {
            return;
        }
        
        
        //움직인거리
        totalScrubDistances[overlappedIndex] += data.delta.magnitude;
        Debug.Log($"overlapped index: {overlappedIndex}, totalScrubDistance: {totalScrubDistances}");
        progress[overlappedIndex] = 1f - Mathf.Clamp01(totalScrubDistances[overlappedIndex] /distanceToComplete) ;
        
        
        //클리어 조건
        for(int i=0;i<totalScrubDistances.Count;i++){
            if (totalScrubDistances[i] >= distanceToComplete)
            {
                if (!targetImages[i].gameObject.activeSelf)
                {
                    continue;
                }
                progress[i] = 0f;
                removeCount++;
                //note: 얘는 레이캐스트를 사용하지않음
                targetImages[overlappedIndex].gameObject.SetActive(false);
            }
        }
        
        
        
        //투명해지기 (움직이는 거리에 비례해서 a값 조정)
        if (!isTransparent)
        {
            return;
        }
        Color color = targetImages[overlappedIndex].color;
        color.a = progress[overlappedIndex];
        targetImages[overlappedIndex].color = color;
    }
    


    private void OnGameComplete()
    {
        Debug.Log("문지르기 성공!");
        isComplete = true;
        gameObject.SetActive(false);
    }
    
}
