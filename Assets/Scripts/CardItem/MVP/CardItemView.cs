using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using TMPro;

public class CardItemView : NetworkBehaviour
{
    #region 외향
    [SerializeField]
    private TextMeshProUGUI cardItemNameTxt;
    [SerializeField]
    private TextMeshProUGUI cardTypeTxt;
    [SerializeField]
    private TextMeshProUGUI descriptionTxt;
    [SerializeField]
    private TextMeshProUGUI costTxt;

    [Header("Card For Sale용 Price 텍스트")]
    [SerializeField]
    private TextMeshProUGUI priceTxtForSale;

    public void SetCardItemNameAppearence(string cardItemName, TierEnum tier)
    {
        //카드 아이템 테이블 –Name string 출력
        cardItemNameTxt.text = cardItemName;
        //TODO: 카드 희귀도 별로 다른 카드 배경 sprite 적용
    }
    public void SetCardItemImageAppearence(TierEnum tier, TypeEnum type) {
        //TODO: 카드 아이템Sprite카드 아이템 테이블 –Image 경로에 있는 Sprite 출력. (뭐가 주어져야하는지 불러올 수 있는지 모르겠음. 경로말고 이름같은 불러올 key 알려달라하기)
        //TODO: 카드 아이템테두리Sprite-카드 희귀도, 카드 타입 별로 다른 카드 배경 sprite 적용
    }
    public void SetCardTypeAppearence(int mapRestriction, TypeEnum type)
    {
        //카드 타입 Txt {카드 아이템 테이블 –Map_Restrictionstring} + {카드 아이템 테이블 –Type string} 출력
        //TODO: Map_Restriction 숫자에 맞는 맵 이름으로 출력
        cardTypeTxt.text = mapRestriction.ToString() + " " + type.ToString();
        //TODO: 카드 타입 BG-사용 가능 직업 별로 다른 카드 배경 sprite 적용
    }
    public void SetCardDefinitionAppearence(string description)
    {
        //TODO: 카드 설명 Txt: 카드 아이템 테이블 –Description string 출력
        descriptionTxt.text = description;
        //TODO: 카드 설명 BG: 사용 가능 직업별로 다른 카드 배경 sprite 적용
    }
    public void SetCardCharacteristicAppearence(int cost, TypeEnum type, int mapRestriction)
    {
        //발언력 표시-발언력 수치는 재판장 공격 카드 or 재판장 방어 카드일 경우에만 {카드 아이템 테이블 -Cost}로 표시
        //TODO: 조건에 따라 코스트 출력할지 안할지 결정
        costTxt.text = cost.ToString();
        //TODO: 카드 특성 아이콘-재판장 공격 카드 or 재판장 방어 카드가 아닌 경우, 카드 타입 및 사용 가능한 장소에 따른 아이콘 표시
        //TODO: 카드 특성 BG-직업 분류 별로 다른 카드 배경 sprite 적용(마피아, 시민, 중립, 공용)
    }

    public void SetCardForSaleAppearence(int price)
    {
        if (priceTxtForSale)
        {
            priceTxtForSale.text = price.ToString();
        }
    }

    #endregion

    #region 구매 클릭 입력 이벤트
    //인자로, 구매하려는 플레이어의 클라이언트 아이디 전달
    public event System.Action<ulong> OnPurchaseClicked;

    private void OnMouseDown()
    {
        //만약 오브젝트가 Card for Sale이라면 구매 클릭 이벤트 전달
        if (gameObject.name == "Card for Sale")
        {
            OnPurchaseClicked?.Invoke(NetworkManager.Singleton.LocalClientId);
        }
    }

    #endregion
}
