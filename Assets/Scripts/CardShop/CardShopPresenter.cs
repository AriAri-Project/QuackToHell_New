using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public sealed class CardShopPresenter : NetworkBehaviour
{
    [SerializeField] private ICardShopView viewBehaviour;
    [SerializeField] private float rerollCooldown = 0.2f;

    private ICardShopView _view;
    private ICardShopModel _model;
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

        if (IsOwner && _view != null)
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


    public void TryPurchaseCard(InventoryCard card, ulong inputClientId)
    {
        Debug.Log("[CardShopPresenter] TryPurchaseCard 실행됨");
        var clientId = inputClientId == 0UL ? OwnerClientId : inputClientId;
        _model.RequestPurchase(card, inputClientId);
    }

    [ClientRpc]
    public void PurchaseCardResultClientRpc(bool success, ClientRpcParams sendParams = default)
    {
        if (_view == null) return;
        _view.ShowLoading(false);
        _view.ShowResult(success, success ? "구매 성공" : "구매 실패");
    }

    private void OnClickLock()
    {
        _model.IsLocked = !_model.IsLocked;
        _view.SetLockedVisual(_model.IsLocked);
        _view.ShowResult(true, _model.IsLocked ? "목록 고정됨" : "목록 고정 해제");
    }
    private void OnClickReRoll()
    {
        if (_cooldown)
            return;

        if (!_model.TryReRoll())
        {
            _view.ShowResult(false, "잠금 상태에서는 새로고침 불가");
            return;
        }

        StartCoroutine(RerollCooldown());
        _view.ShowResult(true, "새로고침 완료");

        // 실제 카드목록 UI를 갱신하려면 여기서 View.Render(...)를 호출하도록
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
    
    ////////////////////////////////
    /// test: 유진
    /// /////////////////////////////////////////////////
    private void Start()
    {
        //_model.DisplayCardsForSale();
    }
}
