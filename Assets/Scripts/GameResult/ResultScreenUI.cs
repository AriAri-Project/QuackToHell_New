using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class ResultScreenUI : MonoBehaviour
{
    [Header("Auto-find by name if null")]
    [SerializeField] private TMP_Text winnersText;
    [SerializeField] private TMP_Text losersText;
    [SerializeField] private TMP_Text reasonText;

    private void Awake()
    {
        if (winnersText == null) winnersText = FindTMP("WinnersText");
        if (losersText == null) losersText = FindTMP("LosersText");
        if (reasonText == null) reasonText = FindTMP("ReasonText");
    }

    public void Open(GameResultPayload payload)
    {
        Render(payload);
    }

    private void Start()
    {
        // 혹시 씬 로드 이벤트보다 Start가 먼저 찍혀도 안전하게 표시
        if (ResultBroadcaster.Instance != null && ResultBroadcaster.Instance.HasPayload)
            Render(ResultBroadcaster.Instance.LastPayload);
    }

    private void Render(GameResultPayload payload)
    {
        if (reasonText != null)
            reasonText.text = payload.WinReason.ToString();

        if (winnersText != null)
            winnersText.text = BuildPlayersText("승리", payload, true);

        if (losersText != null)
            losersText.text = BuildPlayersText("패배", payload, false);
    }

    private static string BuildPlayersText(string title, GameResultPayload p, bool isWinner)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{title}]");

        if (isWinner)
        {
            AppendIf(sb, p.HasWinner0, p.Winner0);
            AppendIf(sb, p.HasWinner1, p.Winner1);
            AppendIf(sb, p.HasWinner2, p.Winner2);
            AppendIf(sb, p.HasWinner3, p.Winner3);
        }
        else
        {
            AppendIf(sb, p.HasLoser0, p.Loser0);
            AppendIf(sb, p.HasLoser1, p.Loser1);
            AppendIf(sb, p.HasLoser2, p.Loser2);
            AppendIf(sb, p.HasLoser3, p.Loser3);
        }

        return sb.ToString();
    }

    private static void AppendIf(StringBuilder sb, bool has, ResultPlayerInfo info)
    {
        if (!has) return;

        ExtractTwoStrings(info, out var name, out var job);
        if (string.IsNullOrWhiteSpace(name)) name = "Unknown";
        if (string.IsNullOrWhiteSpace(job)) job = "Unknown";

        sb.AppendLine($"- {name} ({job})");
    }

    // ResultPlayerInfo 멤버명이 뭐든 string 2개를 뽑아서 표시
    private static void ExtractTwoStrings(ResultPlayerInfo info, out string a, out string b)
    {
        a = null; b = null;
        var t = typeof(ResultPlayerInfo);

        a = GetStringMember(info, t, "Nickname", "Name", "PlayerName", "nick", "nickname");
        b = GetStringMember(info, t, "JobName", "Job", "Role", "job", "jobName", "role");

        if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b))
            return;

        var strings =
            t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(m =>
                 (m is FieldInfo fi && fi.FieldType == typeof(string)) ||
                 (m is PropertyInfo pi && pi.PropertyType == typeof(string) && pi.GetIndexParameters().Length == 0))
             .OrderBy(m => m.MetadataToken)
             .ToArray();

        if (string.IsNullOrEmpty(a) && strings.Length >= 1) a = ReadString(info, strings[0]);
        if (string.IsNullOrEmpty(b) && strings.Length >= 2) b = ReadString(info, strings[1]);
    }

    private static string GetStringMember(ResultPlayerInfo info, System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
                return (string)p.GetValue(info);

            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(string))
                return (string)f.GetValue(info);
        }
        return null;
    }

    private static string ReadString(ResultPlayerInfo info, MemberInfo m)
    {
        if (m is FieldInfo fi) return (string)fi.GetValue(info);
        if (m is PropertyInfo pi) return (string)pi.GetValue(info);
        return null;
    }

    private static TMP_Text FindTMP(string goName)
    {
        var go = GameObject.Find(goName);
        return go ? go.GetComponent<TMP_Text>() : null;
    }
}
