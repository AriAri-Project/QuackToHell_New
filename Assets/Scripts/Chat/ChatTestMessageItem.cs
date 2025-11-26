using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 테스트용 채팅 메시지 한 줄을 표시하는 UI 아이템
/// SetData() 함수 하나로 모든 UI 업데이트 처리
/// </summary>
public class ChatTestMessageItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Image backgroundImage;

    /// <summary>
    /// 채팅 UI 데이터를 받아서 모든 UI 업데이트
    /// </summary>
    public void SetData(ChatTestUIData data)
    {
        Debug.Assert(playerNameText != null, $"[ChatTestMessageItem][{this.name}]] playerNameText is null");
        Debug.Assert(messageText != null, $"[ChatTestMessageItem][{this.name}]] messageText is null");
        Debug.Assert(timestampText != null, $"[ChatTestMessageItem][{this.name}]] timestampText is null");
        Debug.Assert(playerIcon != null, $"[ChatTestMessageItem][{this.name}]] playerIcon is null");
        Debug.Assert(backgroundImage != null, $"[ChatTestMessageItem][{this.name}]] backgroundImage is null");

        playerNameText.text = data.playerName;
        playerNameText.color = data.nameColor;

        messageText.text = data.message;
        messageText.color = data.messageColor;

        timestampText.gameObject.SetActive(data.showTimestamp);
        if (data.showTimestamp)
        {
            timestampText.text = data.timestamp;
        }

        bool hasPlayerIcon = data.playerIcon != null;
        playerIcon.gameObject.SetActive(hasPlayerIcon);
        if (hasPlayerIcon)
        {
            playerIcon.sprite = data.playerIcon;
        }

        backgroundImage.color = data.isOwnMessage ? Color.yellow : Color.gray;
    }
}
