using System;
using UnityEngine;
using System.Reflection;
public class UITunner : MonoBehaviour
{
    private enum UIBaseType
    {
        UIPopup,
        UIHUD
    }
    
    [SerializeField] private UIBaseType uiBaseType;
    [SerializeField] private string uiName;
    private void Start()
    {
        switch (uiBaseType)
        {
            case UIBaseType.UIHUD:
                ShowByName(uiName, false);
                break;
            case UIBaseType.UIPopup:
                ShowByName(uiName, true);
                break;
            
        }
    }
    private void ShowByName(string typeName, bool isPopup){
        Type uiType = Type.GetType(typeName);
        if(uiType == null){
            Debug.LogError($"타입을 찾을 수 없습니다: {typeName}");
            return;
        }

        string methodName = isPopup?"ShowPopupUI":"ShowHUDUI";
        MethodInfo method = typeof(UIManager).GetMethod(methodName);

        if(method==null){
            Debug.LogError($"메서드를 찾을 없습니다: {methodName}");
            return;
        }

        MethodInfo genericMethod = method.MakeGenericMethod(uiType);
        genericMethod.Invoke(UIManager.Instance, new object[]{uiName});
    }

    
}
