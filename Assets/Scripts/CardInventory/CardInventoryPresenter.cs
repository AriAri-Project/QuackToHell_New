using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class CardInventoryPresenter : MonoBehaviour
{
    private CardInventoryModel _cardInventoryModel;
    private CardInventoryView _cardInventoryView;

    private void Awake()
    {
        _cardInventoryModel = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(NetworkManager.Singleton.LocalClientId).GetComponent<CardInventoryModel>();
        _cardInventoryView = gameObject.GetComponent<CardInventoryView>();
    }

    private void Start()
    {
        _cardInventoryModel.OwnedCards.OnListChanged += CardInventoryModel_OwnedCardsOnListChanged;
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
    }

    private void CardInventoryModel_OwnedCardsOnListChanged(NetworkListEvent<CardItemData> changeEvent)
    {
        //view 업데이트 함수 호출
        CardInventoryView cardInventoryView = gameObject.GetComponent<CardInventoryView>();
        cardInventoryView.UpdateInventoryView(_cardInventoryModel.OwnedCards);
        
        // 주의: 이벤트 핸들러에서 정렬을 호출하면 무한 재귀가 발생할 수 있습니다.
        // 정렬은 명시적으로 요청될 때만 수행해야 합니다.
    }
}
