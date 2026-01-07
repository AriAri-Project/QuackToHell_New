using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SabotageVisualController : MonoBehaviour
{
    [Header("UI 어둠 오버레이 (1920x1080 PNG Image)")]
    public Image darknessImage;

    [Header("플레이어 시야 SpriteMask (카메라 자식)")]
    public Transform maskTransform;       
    public GameObject maskObject; 

    [Header("설정 값")]
    public float fadeDuration = 1f;      // 어두워지기/밝아지기 시간
    public float targetAlpha = 0.9f;     // 최종 어두운 정도
    public float startMaskScale = 2.0f;  // 평상시 마스크 크기
    public float endMaskScale = 0.8f;    // 사보타지 때 마스크 크기

    private bool isRunning = false;

    void Awake()
    {
        // 시작 상태 : 사보타지 꺼져 있음
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
    }

    /// <summary>
    /// 이 클라이언트 화면에 사보타지 연출 1번 실행
    /// </summary>
    public void PlaySabotageOnce(SabotageType type, float sabotageDuration)
    {
        if (isRunning) return;
        StartCoroutine(SabotageRoutine(type, sabotageDuration));
    }

    private IEnumerator SabotageRoutine(SabotageType type, float sabotageDuration)
    {
        isRunning = true;

        if (darknessImage != null)
        {
            darknessImage.gameObject.SetActive(true);
        }

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

            float a = Mathf.Lerp(alphaFrom, alphaTo, k);
            c.a = a;
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
}
