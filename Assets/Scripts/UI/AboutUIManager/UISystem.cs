using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 중요: SystemUI쓸 경우, RootPanel을 생성할 것!
/// 참고: x버튼은 코드상에서 달아줌
/// </summary>
public class UISystem : UIBase
{
    enum GameObjects
    {
        RootPanel
    }
    
    private GameObject rootPanel;
    public override void Init()
    {
       DontDestroyOnLoad(this.gameObject);

       Bind<GameObject>(typeof(GameObjects));
       rootPanel = Get<GameObject>((int)GameObjects.RootPanel).gameObject;
       
       SetXButton();
    }
    
  

    private void SetXButton()
    {
        // 1. 오브젝트 생성 및 부모 설정
        GameObject closeBtnObj = new GameObject("CloseButton");
        // false를 줘야 UI 스케일이 부모에 맞춰지면서 크기가 망가지지 않습니다.
        closeBtnObj.transform.SetParent(rootPanel.transform, false);

        // 2. 50x50 크기 생성 및 앵커 우상단 설정
        RectTransform rect = closeBtnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50f, 50f);
        
        // 앵커(Anchor)와 피벗(Pivot)을 모두 우상단(1, 1)으로 세팅
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        // 화면 완전 구석에 딱 붙으면 안 예쁘니까 안쪽으로 살짝 띄워줍니다. (필요 없으면 0, 0)
        rect.anchoredPosition = new Vector2(-20f, -20f);

        // 3. 이미지 로드 및 적용
        Image img = closeBtnObj.AddComponent<Image>();
        Sprite xIcon = Resources.Load<Sprite>("Sprites/XButton/icon_cross");
        if (xIcon != null)
        {
            img.sprite = xIcon;
        }
        else
        {
            Debug.LogError("[MinigameBaseUI] X버튼 이미지를 찾을 수 없습니다! 경로: Sprites/XButton/icon_cross");
        }

        // 4. 버튼 컴포넌트 추가 및 팝업 닫기 이벤트 바인딩
        Button btn = closeBtnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => 
        {
            gameObject.SetActive(false);
        });
    }

}