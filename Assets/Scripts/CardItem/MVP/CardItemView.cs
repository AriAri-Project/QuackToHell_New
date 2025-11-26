using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardItem.MVP
{
    public class CardItemView : MonoBehaviour, IPointerClickHandler
    {
        [Header("For CardForSale SFX")]
        public AudioSource mouseEnterSFX;
    
        #region 외향
        [SerializeField]
        private TextMeshProUGUI cardItemNameTxt;
        [SerializeField]
        private TextMeshProUGUI cardTypeTxt;
        [SerializeField]
        private TextMeshProUGUI descriptionTxt;
        [SerializeField]
        private TextMeshProUGUI costTxt;
        [SerializeField]
        private TextMeshProUGUI cardItemIdTxt;

        [Header("Card For Sale용 Price 텍스트")]
        [SerializeField]
        private TextMeshProUGUI priceTxtForSale;

        public void SetCardItemNameAppearence(string cardItemName, TierEnum tier)
        {
            cardItemNameTxt.text = cardItemName;
            //TODO: 카드 희귀도 별로 다른 카드 배경 sprite 적용
        }
        public void SetCardItemImageAppearence(TierEnum tier, TypeEnum type) {
            //TODO: 카드 아이템Sprite카드 아이템 테이블 –Image 경로에 있는 Sprite 출력. (뭐가 주어져야하는지 불러올 수 있는지 모르겠음. 경로말고 이름같은 불러올 key 알려달라하기)
            //TODO: 카드 아이템테두리Sprite-카드 희귀도, 카드 타입 별로 다른 카드 배경 sprite 적용
        }
        public void SetCardTypeAppearence(int mapRestriction, TypeEnum type)
        {
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
            //TODO: 조건에 따라 코스트 출력할지 안할지 결정
            if(gameObject.tag == GameTags.CardForSale)
            {
                costTxt.text = cost.ToString();
            }
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

        public void SetCardItemIdAppearence(int cardItemId)
        {
            if (cardItemIdTxt)
            {
                //카드 아이템 ID 텍스트 출력
                cardItemIdTxt.text = "card item id: \n"+cardItemId.ToString();
            }
        }

        #endregion

        #region 구매 클릭 입력 이벤트
        //인자로, 구매하려는 플레이어의 클라이언트 아이디 전달
        public event System.Action<ulong> OnPurchaseClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            //만약 오브젝트가 Card for Sale이라면 구매 클릭 이벤트 전달
            if (gameObject.CompareTag(GameTags.CardForSale))
            {
                OnPurchaseClicked?.Invoke(NetworkManager.Singleton.LocalClientId);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (gameObject.CompareTag(GameTags.CardForSale))
            {
                SoundManager.Instance.SFXPlay(mouseEnterSFX.name, mouseEnterSFX.clip);
            }
        }

        #endregion
    }
}
