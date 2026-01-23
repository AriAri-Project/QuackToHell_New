using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardItem.MVP
{
    public class CardItemView : MonoBehaviour, IPointerClickHandler
    {
        [Header("For CardForSale SFX")]
        public AudioSource mouseEnterSFX;
    
        #region 외향
        [Header("Card For Sale 외향용 참조")]
        [SerializeField] private Image cardIcon;
        [SerializeField] private TextMeshProUGUI cardNumText;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardTypeText;
        [SerializeField] private TextMeshProUGUI cardExplainText;
        [SerializeField] private TextMeshProUGUI cardPriceText;
        [SerializeField] private TextMeshProUGUI cardIconText;
        [SerializeField] private Image cardBG;
        
        public void SetCardIcon(string cardIconResourcePath)
        {
            cardIcon.sprite = Resources.Load<Sprite>(cardIconResourcePath);
        }

        public void SetCardNum(string cardNum, Color textColor)
        {
            cardNumText.text = cardNum;
            cardNumText.color = textColor;
        }

        public void SetCardName(string cardName, Color textColor)
        {
            cardNameText.text = cardName;
            cardNameText.color = textColor;
        }

        public void SetCardType(string cardType, Color textColor)
        {
            cardTypeText.text = cardType;
            cardTypeText.color = textColor;
        }

        public void SetCardExplain(string cardExplain)
        {
            cardExplainText.text = cardExplain;
        }

        public void SetCardBG(string bgPath)
        {
            cardBG.sprite = Resources.Load<Sprite>(bgPath);
        }

        public void SetCardPrice(string price)
        {
            cardPriceText.text = price;
        }

        public void SetIconText(string iconText)
        {
            cardIconText.text = iconText;
        }
        #endregion

        #region 구매 클릭 입력 이벤트
        //인자로, 구매하려는 플레이어의 클라이언트 아이디 전달
        public event System.Action<ulong> OnPurchaseClicked;
        public event System.Action<ulong> OnSellClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            //만약 오브젝트가 Card for Sale이라면 구매 클릭 이벤트 전달
            if (gameObject.CompareTag(GameTags.CardForSale))
            {
                OnPurchaseClicked?.Invoke(NetworkManager.Singleton.LocalClientId);
            }

            if (gameObject.CompareTag(GameTags.CardForInventory))
            {
                OnSellClicked?.Invoke(NetworkManager.Singleton.LocalClientId);
                return;
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
