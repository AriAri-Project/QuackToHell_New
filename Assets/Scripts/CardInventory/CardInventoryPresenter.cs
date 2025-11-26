using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CardInventoryPresenter : MonoBehaviour
{
    [Header("Components")]
    private CardInventoryModel _cardInventoryModel;
    private CardInventoryView _cardInventoryView;

    private void Awake()
    {
        _cardInventoryModel = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(NetworkManager.Singleton.LocalClientId).GetComponent<CardInventoryModel>();
        DebugUtils.AssertComponent(_cardInventoryModel, "CardInventoryModel", this);
        _cardInventoryView = GetComponent<CardInventoryView>();
        DebugUtils.AssertComponent(_cardInventoryView, "CardInventoryView", this);
    }

    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name == GameScenes.Village)
        {
            var cardShop = FindFirstObjectByType<CardShopPresenter>(FindObjectsInactive.Include);
            DebugUtils.AssertNotNull(cardShop, "CardShopPresenter", this);
            cardShop.RequestShowCardShop();
        }
    }

    private void Start()
    {
        DebugUtils.AssertNotNull(_cardInventoryModel, "CardInventoryModel", this);
        DebugUtils.AssertNotNull(_cardInventoryView, "CardInventoryView", this);

        //초기 뷰 업데이트
        _cardInventoryView.UpdateInventoryView(_cardInventoryModel.OwnedCards);
      
        //TODO: 정렬기능 추가되면, 정렬 enum에 따라 다른 정렬 함수 호출
        /*switch (_cardInventoryModel.SortingOption)
        {
            case InventorySotringOption.RecentlyAcquired:
                _cardInventoryModel.SortCardsByAcquiredTicks();
                break;
            default:
                break;
        }*/

        _cardInventoryModel.OwnedCards.OnListChanged += CardInventoryModel_OwnedCardsOnListChanged;

        _cardInventoryView.OnCloseInventoryClicked += OnClickCloseInventory;
    }

    private void OnDestroy()
    {
        

        if (_cardInventoryModel != null)
        {
            _cardInventoryModel.OwnedCards.OnListChanged -= CardInventoryModel_OwnedCardsOnListChanged;
        }

        if (_cardInventoryView != null)
        {
            _cardInventoryView.OnCloseInventoryClicked -= OnClickCloseInventory;
        }
    }

    private void CardInventoryModel_OwnedCardsOnListChanged(NetworkListEvent<CardItemData> changeEvent)
    {
        // note cba0898: 가지고 있는 _cardInventoryView를 써도 될 것 같습니다 <= 수정완료
        //view 업데이트 함수 호출
        _cardInventoryView.UpdateInventoryView(_cardInventoryModel.OwnedCards);
    }

    private void OnClickCloseInventory()
    {
        DebugUtils.AssertNotNull(_cardInventoryView, "CardInventoryView", this);
        _cardInventoryView.CloseInventory();

        if (SceneManager.GetActiveScene().name == GameScenes.Village)
        {
            CardShopPresenter cardShop = FindFirstObjectByType<CardShopPresenter>(FindObjectsInactive.Include);
            DebugUtils.AssertNotNull(cardShop, "CardShopPresenter", this);
            cardShop.RequestCloseCardShop();
        }
    }

 


}
