using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public sealed class CardShopPresenter : NetworkBehaviour
{
    [SerializeField] private CardShopView viewBehaviour;
    [SerializeField] private float rerollCooldown = 0.2f;
    [Header ("Village Scene의 CardForSaleParent 오브젝트를 넣어주세요")]
    [SerializeField] private GameObject CardForSaleParent;
    [Header("Village Scene의 CardShop의 하위 오브젝트인 Row 오브젝트를 넣어주세요")]
    [SerializeField] private Transform CardShopRow;

    private CardShopView _view;
    private CardShopModel _model;
    private static readonly Dictionary<ulong, CardShopPresenter> s_serverByClient = new();
    private bool _cooldown;

    private void Awake()
    {
        _view = viewBehaviour;
        _model = new CardShopModel();
    }
 
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
                
        CardItemFactoryManager.Instance.OnCardForSaleCreated+=CardItemFactory_OnCardForSaleCreated;
        _model.CreateCardsForSale();

        _model.Initiate(CardForSaleParent, CardShopRow);
        _model.DisplayCardForSale();

        if (_view != null)
        {

            _view.OnClickLock += OnClickLock;
            _view.OnClickReRoll += OnClickReRoll;
        }

        if (IsServer)
            s_serverByClient[OwnerClientId] = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner && _view != null)
        {
            _view.OnClickLock -= OnClickLock;
            _view.OnClickReRoll -= OnClickReRoll;
        }

        if (IsServer)
            s_serverByClient.Remove(OwnerClientId);
    }

    public void TryPurchaseCard(CardItemData card, ulong inputClientId)
    {
        Debug.Log("[CardShopPresenter] TryPurchaseCard 실행됨");
        var clientId = inputClientId == 0UL ? OwnerClientId : inputClientId;
        _model.RequestPurchase(card, clientId);
    }

    [ClientRpc]
    public void PurchaseCardResultClientRpc(bool success, ClientRpcParams sendParams = default)
    {
        if (_view == null) return;
    }

    private void OnClickLock()
    {
        _model.IsLocked = !_model.IsLocked;
        _view.SetRefreshInteractable(!_model.IsLocked);
    }
    private void OnClickReRoll()
    {
        if (_model.IsLocked) return;
        if (_cooldown) return;

        StartCoroutine(RerollCooldown());

        // 리롤 실행
        var ok = _model.TryReRoll();
    }

    private IEnumerator RerollCooldown()
    {
        _cooldown = true;
        _view.SetRefreshInteractable(false);
        yield return new WaitForSeconds(rerollCooldown);
        _view.SetRefreshInteractable(true);
        _cooldown = false;
    }

    public static void ServerSendResultTo(ulong clientId, bool success)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return;
        if (s_serverByClient.TryGetValue(clientId, out var presenter))
        {
            var p = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };
            presenter.PurchaseCardResultClientRpc(success, p);
        }
    }

 

}
