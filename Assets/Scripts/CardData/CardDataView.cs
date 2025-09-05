using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;

public sealed class CardDataView : NetworkBehaviour
{
    [Header("Google Sheets CSV URLs")]
    [SerializeField] string cardCsvUrl;     // Card_Table
    [SerializeField] string stringCsvUrl;   // String_Table
    [SerializeField] string resourceCsvUrl; // Resource_Table

    public static CardDataPresenter Presenter { get; private set; }
    CancellationTokenSource _cts;

    // 호스트가 접속하면, 데이터 로드 시작
    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //호스트만 데이터 로드
        if (!IsHost)
        {
            return;
        }

        //CardDataView가 중복 생성되는 것을 방지
        var exists = Object.FindObjectsByType<CardDataView>(FindObjectsSortMode.None);

        if (exists.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        _cts = new CancellationTokenSource();

        Presenter ??= new CardDataPresenter();

        try
        {
            await Presenter.PreloadAsync(cardCsvUrl, stringCsvUrl, resourceCsvUrl, _cts.Token);
            Debug.Log($"[CardData] Ready. Cards={Presenter.CardCount}");
        }

        catch (System.Exception ex)
        {
            Debug.LogError($"[CardData] init failed: {ex.Message}");
        }
    }
 
    void OnDestroy() { _cts?.Cancel(); _cts?.Dispose(); }
}
