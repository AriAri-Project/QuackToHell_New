using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using TMPro;

public class ChatTestView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ChatTestMessageItem chatTestMessageItemPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentParent;  // ScrollRect의 Content
    [SerializeField] private Button sendButton;
    [SerializeField] private TMP_InputField inputField;

    // note cba0898: const나 SO, json 등으로 별도 분리 추천
    [Header("Settings")]
    [SerializeField] private int maxDisplayMessages = 100;
    [Range(0f, 1f)]
    [Tooltip("Auto Scroll Threshold (0=Bottom, 1=Top). Only scroll when the value is below this threshold.")]
    [SerializeField] private float autoScrollThreshold = 0.1f;
    [SerializeField] private int defaultPoolCapacity = 32;
    [SerializeField] private int maxPoolSize = 128;

    public List<ChatTestMessageItem> displayedMessages = new List<ChatTestMessageItem>();
    private ObjectPool<ChatTestMessageItem> messagePool;

    // 뷰 이벤트들 (Presenter가 구독)
    public System.Action OnSendButtonClicked;
    public System.Action OnInputFieldSubmit;

    private void Awake()
    {
        Debug.Assert(chatTestMessageItemPrefab != null, $"[ChatTestView][{this.name}]] chatTestMessageItemPrefab is null");
        Debug.Assert(scrollRect != null, $"[ChatTestView][{this.name}]] scrollRect is null");
        Debug.Assert(contentParent != null, $"[ChatTestView][{this.name}]] contentParent is null");
        Debug.Assert(sendButton != null, $"[ChatTestView][{this.name}]] sendButton is null");
        Debug.Assert(inputField != null, $"[ChatTestView][{this.name}]] inputField is null");

        sendButton.onClick.AddListener(() => OnSendButtonClicked?.Invoke());
        inputField.onSubmit.AddListener((_) => OnInputFieldSubmit?.Invoke());
    }

    private void Start()
    {
        InitMessagePool();
    }

    private void InitMessagePool()
    {
        messagePool = new ObjectPool<ChatTestMessageItem>(
            createFunc: () => Instantiate(chatTestMessageItemPrefab, contentParent), // 새 메시지 아이템 생성
            actionOnGet: (item) => { // 메시지 아이템 활성화
                item.gameObject.SetActive(true);
                item.transform.SetAsLastSibling(); // 최신 메시지가 맨 아래
            },
            actionOnRelease: (item) => item.gameObject.SetActive(false), // 메시지 아이템 비활성화
            actionOnDestroy: (item) => Destroy(item.gameObject), // 메시지 아이템 삭제
            collectionCheck: true, // 중복 체크
            defaultCapacity: defaultPoolCapacity, // 기본 풀 크기
            maxSize: maxPoolSize // 최대 풀 크기
       );
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
        }
        if (inputField != null)
        {
            inputField.onSubmit.RemoveAllListeners();
        }
        
        messagePool?.Dispose();
    }

    /// <summary>
    /// 새 메시지 추가 (맨 아래에)
    /// </summary>
    public void AddMessage(ChatTestUIData messageData)
    {
        // 메시지 추가 전 스크롤 위치 확인
        bool shouldAutoScroll = messageData.isOwnMessage || scrollRect.verticalNormalizedPosition < autoScrollThreshold;

        ChatTestMessageItem messageItem = messagePool.Get();
        messageItem.transform.SetAsLastSibling();

        messageItem.SetData(messageData);
        displayedMessages.Add(messageItem);
        
        // 최대 개수 초과 시 오래된 메시지 풀로 반환
        if (displayedMessages.Count > maxDisplayMessages)
        {
            ChatTestMessageItem oldestMessage = displayedMessages[0];
            displayedMessages.RemoveAt(0);
            messagePool.Release(oldestMessage);
        }
        
        // 스크롤이 아래쪽에 있었을 때만 자동 스크롤
        if (shouldAutoScroll)
        {
            ScrollToBottom();
        }
    }

    /// <summary>
    /// 모든 메시지 삭제 (ObjectPool로 반환)
    /// </summary>
    public void ClearAllMessages()
    {
        foreach (var item in displayedMessages)
        {
            messagePool.Release(item);
        }
        displayedMessages.Clear();
    }

    /// <summary>
    /// 스크롤을 맨 아래로 이동
    /// </summary>
    public void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public string GetInputText()
    {
        Debug.Assert(inputField != null);
        return inputField.text;
    }

    public void ClearInputText()
    {
        Debug.Assert(inputField != null);
        inputField.text = "";
    }

    public void FocusInputField()
    {
        Debug.Assert(inputField != null);
        inputField.Select();
        inputField.ActivateInputField();
    }
}

