using UnityEngine;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class CardCatalogRepository
{
    public static CardCatalogRepository Instance { get; } = new();

    readonly Dictionary<int, CardDef> _byId = new();
    Task _initTask; // 초기화 태스크 보관

    private CardCatalogRepository() { }

    public Task InitializeAsync(ICardCatalogSource source, CancellationToken ct = default)
    {
        if (_initTask != null) return _initTask;
        _initTask = InitializeImplAsync(source, ct);   // <- 백그라운드 스레드 금지
        return _initTask;
    }

    private async Task InitializeImplAsync(ICardCatalogSource source, CancellationToken ct)
    {
        var rows = await source.LoadAsync(ct); // 메인 스레드에서 UnityWebRequest 진행
        Debug.Log($"[CardCatalogRepository] 소스에서 {rows.Count}개 행을 로드했습니다.");
        _byId.Clear();
        foreach (var d in rows) _byId[d.CardID] = d;
    }

    public Task WhenReadyAsync() => _initTask ?? Task.CompletedTask;

    public bool TryGet(int cardId, out CardDef def) => _byId.TryGetValue(cardId, out def);
    public IReadOnlyDictionary<int, CardDef> AsReadOnly() => _byId;
}
