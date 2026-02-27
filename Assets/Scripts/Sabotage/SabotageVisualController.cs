using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SabotageVisualController : MonoBehaviour
{
    [Header("UI 어둠 오버레이 (1920x1080 PNG Image)")]
    public Image darknessImage;

    [Header("플레이어 시야 SpriteMask (카메라 자식)")]
    public Transform maskTransform;
    public GameObject maskObject;

    [Header("중앙 메시지(Text) - 임시 안내/실패 문구")]
    public TextMeshProUGUI messageText;

    [Header("설정 값")]
    public float fadeDuration = 1f;
    public float targetAlpha = 1.0f;
    public float startMaskScale = 2.0f;
    public float endMaskScale = 0.8f;

    private bool isRunning = false;
    private Coroutine msgRoutine;

    void Awake()
    {
        if (darknessImage != null)
        {
            var c = darknessImage.color;
            c.a = 0f;
            darknessImage.color = c;
            darknessImage.gameObject.SetActive(false);
        }

        if (maskObject != null)
            maskObject.SetActive(false);

        if (maskTransform != null)
            maskTransform.localScale = Vector3.one * startMaskScale;

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    // 기존: 불 끄기 사보타지 연출
    public void PlaySabotageOnce(SabotageType type, float sabotageDuration)
    {
        if (isRunning) return;
        StartCoroutine(SabotageRoutine(sabotageDuration));
    }

    private IEnumerator SabotageRoutine(float sabotageDuration)
    {
        isRunning = true;

        if (darknessImage != null)
            darknessImage.gameObject.SetActive(true);

        if (maskObject != null)
            maskObject.SetActive(true);

        yield return StartCoroutine(FadeAndScale(0f, targetAlpha, startMaskScale, endMaskScale, fadeDuration));
        yield return new WaitForSeconds(sabotageDuration);
        yield return StartCoroutine(FadeAndScale(targetAlpha, 0f, endMaskScale, startMaskScale, fadeDuration));

        if (maskObject != null)
            maskObject.SetActive(false);

        if (darknessImage != null)
            darknessImage.gameObject.SetActive(false);

        isRunning = false;
    }

    private IEnumerator FadeAndScale(float alphaFrom, float alphaTo, float scaleFrom, float scaleTo, float duration)
    {
        if (darknessImage == null)
            yield break;

        float t = 0f;
        var c = darknessImage.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            c.a = Mathf.Lerp(alphaFrom, alphaTo, k);
            darknessImage.color = c;

            if (maskTransform != null)
            {
                float s = Mathf.Lerp(scaleFrom, scaleTo, k);
                maskTransform.localScale = new Vector3(s, s, 1f);
            }

            yield return null;
        }

        c.a = alphaTo;
        darknessImage.color = c;

        if (maskTransform != null)
            maskTransform.localScale = new Vector3(scaleTo, scaleTo, 1f);
    }

    public void ShowCenterMessage(string msg, float seconds)
    {
        if (messageText == null)
        {
            Debug.Log($"[SabotageUI] {msg}");
            return;
        }

        if (msg.Contains("긴급행동"))
        {
            var font = Resources.Load<TMP_FontAsset>("SejongGeulggot SDF 30");
            if (font != null)
            {
                messageText.font = font;
                messageText.fontSize = 30;
            }
        }

        if (msgRoutine != null) StopCoroutine(msgRoutine);
        msgRoutine = StartCoroutine(MessageRoutine(msg, seconds));
    }

    private IEnumerator MessageRoutine(string msg, float seconds)
    {
        messageText.text = msg;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(seconds);

        messageText.gameObject.SetActive(false);
        msgRoutine = null;
    }
}
