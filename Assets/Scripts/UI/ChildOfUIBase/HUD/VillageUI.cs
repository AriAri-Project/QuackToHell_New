using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class VillageUI : UIHUD
{
    private TextMeshProUGUI Text_Gold;
    PlayerModel playerModel;

    enum Images
    {
        Role_Image
    }

    enum Texts
    {
        Gold_Text
    }

private void Start()
    {
        base.Init();
        
        
        Bind<Image>(typeof(Images));
        GameObject Image_Role_gameObject = Get<Image>((int)Images.Role_Image).gameObject;
        if (PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId)
                .GetPlayerJob() == PlayerJob.Animal)
        {
            Image_Role_gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Art/Duck");    
        }
        else
        {
            Image_Role_gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Art/Farmer");    
        }
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        Text_Gold = Get<TextMeshProUGUI>((int)Texts.Gold_Text);
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        playerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
        UpdatePlayerGold(playerModel.GetGold());
        playerModel.PlayerStatusData.OnValueChanged += OnPlayerStatusChanged;
        
    }

    /// <summary>
    /// 플레이어 상태 변경 시 호출되는 메서드
    /// </summary>
    private void OnPlayerStatusChanged(PlayerStatusData previousValue, PlayerStatusData newValue)
    {
        // 골드가 실제로 변경되었을 때만 UI 업데이트
        if (previousValue.gold != newValue.gold)
        {
            UpdatePlayerGold(newValue.gold);
        }
    }

    public void UpdatePlayerGold(int gold)
    {
        Text_Gold.text = "gold: " + gold.ToString();
    }

}
