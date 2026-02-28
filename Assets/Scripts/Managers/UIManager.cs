using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager:MonoBehaviour
{
    #region 싱글톤
    public static UIManager Instance => SingletonHelper<UIManager>.Instance;

    private void Awake()
    {
        SingletonHelper<UIManager>.InitializeSingleton(this);
    }
    #endregion
    

    Stack<UIPopup> popupStack = new Stack<UIPopup>();

	public Stack<UIPopup> PopupStack
	{
		get{ return popupStack; }
	}
	List<UIHUD> hudList = new List<UIHUD>();

	public List<UIHUD> HUDList
	{
		get{ return hudList; }
	}
	List<UISystem> systemList = new List<UISystem>();

	public List<UISystem> SystemList
	{
		get{ return systemList; }
	}
	
	
    GameObject HUDRoot
    {
        get
        {
			GameObject root = GameObject.Find("@UI_HUDRoot");
			if (root == null){
                root = new GameObject { name = "@UI_HUDRoot" };
                SetCanvas(root, UITypes.UIType.HUD);
            }
				
            return root;
		}
    }
    
    GameObject PopupRoot
    {
        get
        {
			GameObject root = GameObject.Find("@UI_PopupRoot");
			if (root == null){
                root = new GameObject { name = "@UI_PopupRoot" };
                SetCanvas(root, UITypes.UIType.Popup);
            }
				
            return root;
		}
    }
    
    GameObject SystemRoot
    {
        get
        {
			GameObject root = GameObject.Find("@UI_SystemRoot");
			if (root == null){
                root = new GameObject { name = "@UI_SystemRoot" };
                SetCanvas(root, UITypes.UIType.System);
                DontDestroyOnLoad(root);
            }
				
            return root;
		}
    }

    
    void SetCanvas(GameObject go, UITypes.UIType uiType)
    {
        Canvas canvas = GameObjectUtils.GetOrAddComponent<Canvas>(go);
        GraphicRaycaster raycaster = GameObjectUtils.GetOrAddComponent<GraphicRaycaster>(go);
        CanvasScaler scaler = GameObjectUtils.GetOrAddComponent<CanvasScaler>(go);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도에 따라 크기 조절
        scaler.referenceResolution = new Vector2(1920, 1080); // 기준 해상도 설정
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // 가로/세로 중간 비율로 맞춤
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = UITypes.GetSortingOrder(uiType);
    }


    /// <summary>
    /// 프리팹 이름넣기(.prefab넣지말고 이름만넣기)
    /// </summary>
	public T ShowHUDUI<T>(string name = null) where T : UIHUD
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;
		//HUD 경로는 Resources/UI/HUD 이하여야 합니다.
        GameObject prefab = Resources.Load<GameObject>($"UI/HUD/{name}");
		GameObject go = Object.Instantiate(prefab);
		go.name = name;
		T hud = GameObjectUtils.GetOrAddComponent<T>(go);
		hudList.Add(hud);

		go.transform.SetParent(HUDRoot.transform,false);
		

		return hud;
	}


    /// <summary>
    /// 프리팹 이름넣기(.prefab넣지말고 이름만넣기)
    /// </summary>
	public T ShowPopupUI<T>(string name = null) where T : UIPopup
    {
	    //팝업 중복생성 막기: 이미 있으면 그걸 보여주기
	    foreach (UIPopup p in popupStack)
		    if (p is T) return (T)p;
	    
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;
        //Popup프리팹의 경로는 Resources/UI/Popup 이하여야 합니다.
        GameObject prefab = Resources.Load<GameObject>($"UI/Popup/{name}");
        GameObject go = Object.Instantiate<GameObject>(prefab);
        go.name = name;
        T popup = GameObjectUtils.GetOrAddComponent<T>(go);
        popupStack.Push(popup);

        go.transform.SetParent(PopupRoot.transform,false);

        //새 팝업이 항상 맨 앞에(하이어라키에서 아래로)
        go.transform.SetAsLastSibling();

		return popup;
    }
    
    /// <summary>
    /// 프리팹 이름넣기(.prefab넣지말고 이름만넣기)
    /// </summary>
	public T ShowSystemUI<T>(string name = null) where T : UISystem
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;
		//System 경로는 Resources/UI/System 이하여야 합니다.
        GameObject prefab = Resources.Load<GameObject>($"UI/System/{name}");
		GameObject go = Object.Instantiate<GameObject>(prefab);
		go.name = name;
		T system = GameObjectUtils.GetOrAddComponent<T>(go);
		systemList.Add(system);

		go.transform.SetParent(SystemRoot.transform,false);

		return system;
	}
    

    /// <summary>
    /// 최상단 팝업 닫는 메서드
    /// </summary>
    public void ClosePopupUI()
    {
	    if (popupStack.Count == 0)
		    return;

	    UIPopup popup = popupStack.Pop();
	    Object.Destroy(popup.gameObject);
	    popup = null;
    }

    public void CloseAllPopupUI()
    {
	    while (popupStack.Count > 0)
		    ClosePopupUI();
    }

    /// <summary>
    /// 해당 HUD를 닫음
    /// GameConstants.UI.HUDName.으로 허드이름 접근가능->이걸 인자로 넣기
    /// </summary>
    public void CloseHUDUI(string hudName)
    {
	    
	    if (hudList.Count == 0)
		    return;

	    UIHUD targetHud = null;
	    for (int i = 0; i < hudList.Count; i++)
	    {
		    if (hudList[i].gameObject.name == hudName)
		    {
			    targetHud = hudList[i];
			    break;
		    }
	    }

	    if (targetHud == null)
	    {
		    Debug.Log("Close hud Failed!");
		    return;
	    }

	    hudList.Remove(targetHud);
	    Object.Destroy(targetHud.gameObject);
	    targetHud = null;
    }

    void CloseHUDUI()
    {
	    if (hudList.Count == 0)
		    return;

	    UIHUD hud = hudList[hudList.Count - 1];
	    hudList.RemoveAt(hudList.Count - 1);
	    Object.Destroy(hud.gameObject);
	    hud = null;
    }

    public void CloseAllHUD()
    {
	    while (hudList.Count > 0)
		    CloseHUDUI();
    }

    /// <summary>
    /// 해당 System UI를 닫음
	/// GameConstants.UI.SystemName.으로 System이름 접근가능->이걸 인자로 넣기
    /// </summary>
    public void CloseSystemUI(string systemName)
    {
	    if (systemList.Count == 0)
		    return;

	    UISystem targetSystem = null;
	    for (int i = 0; i < systemList.Count; i++)
	    {
		    if (systemList[i].gameObject.name == systemName)
		    {
			    targetSystem = systemList[i];
			    break;
		    }
	    }

	    if (targetSystem == null)
	    {
		    Debug.Log("Close system UI Failed!");
		    return;
	    }

	    systemList.Remove(targetSystem);
	    Object.Destroy(targetSystem.gameObject);
	    targetSystem = null;
    }

    void CloseSystemUI()
    {
	    if (systemList.Count == 0)
		    return;

	    UISystem system = systemList[systemList.Count - 1];
	    systemList.RemoveAt(systemList.Count - 1);
	    Object.Destroy(system.gameObject);
	    system = null;
    }

    public void CloseAllSystemUI()
    {
	    while (systemList.Count > 0)
		    CloseSystemUI();
    }
    
    //초기화 함수
    public void Initialize()
    {
	    popupStack.Clear();
	    hudList.Clear();
	    systemList.Clear();
    }
}
