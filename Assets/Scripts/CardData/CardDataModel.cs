using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Collections;

#region Data Structs (기획서 타입 반영)
public enum TierEnum { None = 0, Common = 1, Rare = 2, Special = 3 }
public enum TypeEnum { None = 0, Attack = 1, Defense = 2, Special = 3 }

// 딕셔너리의 Key와 Value 한 쌍을 담을 컨테이너 struct
public struct Dictionary_CardIdCardDef : INetworkSerializable, IEquatable<Dictionary_CardIdCardDef>
{
    public int Key;
    public CardDef Value;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Key);
        serializer.SerializeValue(ref Value);
    }

    public bool Equals(Dictionary_CardIdCardDef other)
    {
        return Key == other.Key && Value.Equals(other.Value);
    }

    public override bool Equals(object obj)
    {
        return obj is Dictionary_CardIdCardDef pair && Equals(pair);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }
}
/// <summary>
/// 카드 아이템 데이터 (CardDef + CardItemStatusData)
/// </summary>
public struct CardItemData : INetworkSerializable, IEquatable<CardItemData>, IEquatable<CardDef>, IEquatable<CardStatusData>
{
    public int CardIdKey;
    public CardDef CardDef;
    public CardStatusData CardItemStatusData;
    public long AcquiredTicks; // 카드 획득 시점 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CardIdKey);
        serializer.SerializeValue(ref CardDef);
        serializer.SerializeValue(ref CardItemStatusData);
        serializer.SerializeValue(ref AcquiredTicks);
    }

    public bool Equals(CardItemData other)
    {
        return CardIdKey == other.CardIdKey && 
               CardDef.Equals(other.CardDef) && 
               CardItemStatusData.Equals(other.CardItemStatusData) && 
               AcquiredTicks == other.AcquiredTicks;
    }

    public bool Equals(CardDef other)
    {
        return CardDef.Equals(other);
    }

    public bool Equals(CardStatusData other)
    {
        return CardItemStatusData.Equals(other);
    }

    public override bool Equals(object obj)
    {
        if (obj is CardItemData cardItemData)
            return Equals(cardItemData);
        if (obj is CardDef cardDef)
            return Equals(cardDef);
        if (obj is CardStatusData statusData)
            return Equals(statusData);
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CardIdKey, CardDef, CardItemStatusData, AcquiredTicks);
    }
}

public struct CardDef : INetworkSerializable, IEquatable<CardDef>
{
    public int CardID;
    public FixedString64Bytes CardNameKey;
    public TierEnum Tier;      // enum
    public TypeEnum Type;      // enum
    public int SubType;        // 사용 안 하면 0
    public bool IsUniqueCard;
    public bool IsSellableCard;
    public int UsableClass;      // 3bit
    public int Map_Restriction;  // 2bit
    public int BasePrice;
    public int BaseCost;
    public FixedString64Bytes DescriptionKey;
    public FixedString64Bytes ImagePathKey;
    public int AmountOfCardItem;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CardID);
        serializer.SerializeValue(ref CardNameKey);
        serializer.SerializeValue(ref Tier);
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref SubType);
        serializer.SerializeValue(ref IsUniqueCard);
        serializer.SerializeValue(ref IsSellableCard);
        serializer.SerializeValue(ref UsableClass);
        serializer.SerializeValue(ref Map_Restriction);
        serializer.SerializeValue(ref BasePrice);
        serializer.SerializeValue(ref BaseCost);
        serializer.SerializeValue(ref DescriptionKey);
        serializer.SerializeValue(ref ImagePathKey);
        serializer.SerializeValue(ref AmountOfCardItem);
    }

    public bool Equals(CardDef other)
    {
        return CardID == other.CardID && 
               CardNameKey.Equals(other.CardNameKey) && 
               Tier == other.Tier && 
               Type == other.Type && 
               SubType == other.SubType && 
               IsUniqueCard == other.IsUniqueCard && 
               IsSellableCard == other.IsSellableCard && 
               UsableClass == other.UsableClass && 
               Map_Restriction == other.Map_Restriction && 
               BasePrice == other.BasePrice && 
               BaseCost == other.BaseCost && 
               DescriptionKey.Equals(other.DescriptionKey) && 
               ImagePathKey.Equals(other.ImagePathKey) && 
               AmountOfCardItem == other.AmountOfCardItem;
    }

    public override bool Equals(object obj)
    {
        return obj is CardDef def && Equals(def);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CardID);
        hash.Add(CardNameKey);
        hash.Add(Tier);
        hash.Add(Type);
        hash.Add(SubType);
        hash.Add(IsUniqueCard);
        hash.Add(IsSellableCard);
        hash.Add(UsableClass);
        hash.Add(Map_Restriction);
        hash.Add(BasePrice);
        hash.Add(BaseCost);
        hash.Add(DescriptionKey);
        hash.Add(ImagePathKey);
        hash.Add(AmountOfCardItem);
        return hash.ToHashCode();
    }
}

public enum CardItemState
{
    None,
    Sold,
}


public struct CardStatusData : INetworkSerializable, IEquatable<CardStatusData>
{
    public int CardItemID;
    public int CardID;
    public int Price;
    public int Cost;
    public CardItemState State;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CardItemID);
        serializer.SerializeValue(ref CardID);
        serializer.SerializeValue(ref Price);
        serializer.SerializeValue(ref Cost);
    }

    public bool Equals(CardStatusData other)
    {
        return CardItemID == other.CardItemID && CardID == other.CardID && Price == other.Price && Cost == other.Cost && State == other.State;
    }

    public override bool Equals(object obj)
    {
        return obj is CardStatusData data && Equals(data);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CardItemID, CardID, Price, Cost, State);
    }   
}

[Serializable] public struct StringRow { public string Key; public string KR; public string EN; }
[Serializable] public struct ResourceRow { public string Key; public string Path; }

[Serializable]
public struct CardDisplay
{
    public int CardID;
    public string Name;
    public string Description;
    public string ImagePath;
    public TierEnum Tier;
    public TypeEnum Type;
    public int BasePrice;
    public int BaseCost;
}
#endregion

public sealed class CardDataModel : NetworkBehaviour
{
    readonly Dictionary<int, CardDef> _cards = new();
    readonly Dictionary<string, StringRow> _strings = new();
    readonly Dictionary<string, ResourceRow> _resources = new();

    public void LoadCards(IEnumerable<CardDef> rows) { _cards.Clear(); foreach (var r in rows) _cards[r.CardID] = r; }
    public void LoadStrings(IEnumerable<StringRow> rows) { _strings.Clear(); foreach (var r in rows) if (!string.IsNullOrEmpty(r.Key)) _strings[r.Key] = r; }
    public void LoadResources(IEnumerable<ResourceRow> r) { _resources.Clear(); foreach (var x in r) if (!string.IsNullOrEmpty(x.Key)) _resources[x.Key] = x; }

    public bool TryGetCard(int id, out CardDef def) => _cards.TryGetValue(id, out def);
    public IReadOnlyDictionary<int, CardDef> Cards => _cards;

    public string Localize(string key, string locale)
    {
        if (!_strings.TryGetValue(key, out var row)) return key ?? "";
        return locale switch { "ko" => row.KR ?? key, "en" => row.EN ?? key, _ => row.KR ?? row.EN ?? key };
    }
    public string ResolvePath(string key) => _resources.TryGetValue(key, out var r) ? r.Path : key ?? "";

    #region CSV Lite (경량 파서 & 매핑 유틸)
    public static List<string> SplitRows(string csv)
    {
        var t = (csv ?? "").Replace("\r\n", "\n").Replace("\r", "\n"); return new List<string>(t.Split('\n'));
    }
    public static List<string> SplitCols(string line)
    {
        var res = new List<string>(); if (line == null) { res.Add(""); return res; }
        bool q = false; var sb = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\"') { if (q && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; } else q = !q; }
            else if (c == ',' && !q) { res.Add(sb.ToString()); sb.Length = 0; }
            else sb.Append(c);
        }
        res.Add(sb.ToString()); return res;
    }
    public static int ToInt(string s) { s = (s ?? "").Trim(); if (s == "" || s == "-") return 0; return int.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
    public static bool ToBool(string s) { s = (s ?? "").Trim().ToLowerInvariant(); return s == "true" || s == "1" || s == "y"; }
    public static TierEnum ToTier(string s)
    {
        if (int.TryParse(s, out var n)) return (TierEnum)n; s = (s ?? "").Trim().ToLowerInvariant();
        return s switch { "common" => TierEnum.Common, "rare" => TierEnum.Rare, "special" => TierEnum.Special, _ => TierEnum.None };
    }
    public static TypeEnum ToType(string s)
    {
        if (int.TryParse(s, out var n)) return (TypeEnum)n; s = (s ?? "").Trim().ToLowerInvariant();
        return s switch { "attack" => TypeEnum.Attack, "defense" => TypeEnum.Defense, "special" => TypeEnum.Special, _ => TypeEnum.None };
    }
    #endregion
}
