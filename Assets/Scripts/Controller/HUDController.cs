using UnityEngine;
/// <summary>
/// 책임: HUD(Heads-Up Display) 관련 UI를 관리하는 매니저
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField]
    private GameObject inventoryPrefab;

    private GameObject inventoryCanvas;
    #region 버튼 바인딩
    public void InventoryButton_OnClick()
    {
        if (inventoryCanvas == null)
        {
            inventoryCanvas = GameObject.Find("CardInventoryCanvas");     
        }
        if(inventoryCanvas==null){
            inventoryCanvas = Instantiate(inventoryPrefab, Vector3.zero, Quaternion.identity);    
        }
        else
        {
            inventoryCanvas.transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    #endregion
}
