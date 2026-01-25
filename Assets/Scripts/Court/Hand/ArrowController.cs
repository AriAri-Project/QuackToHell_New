using UnityEngine;

namespace Court.Hand
{
    [RequireComponent(typeof(LineRenderer))]
    public class ArrowController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("곡선을 구성하는 점의 개수 (부드러움)")]
        [SerializeField] private int segmentCount = 50; 
        
        [Tooltip("거리 비례 휨 정도 (0.3 = 거리의 30%만큼 위로 휨)")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float heightFactor = 0.3f; // ★ 고정 높이 대신 비율 사용

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = segmentCount;
            _lineRenderer.useWorldSpace = true; // 월드 좌표 필수
            _lineRenderer.sortingOrder = 30000; // 맨 앞에 그리기
            gameObject.SetActive(false);
        }

        public void ShowArrow(Vector3 startPos, Vector3 endPos)
        {
            gameObject.SetActive(true);
            UpdateCurve(startPos, endPos);
        }

        public void HideArrow()
        {
            gameObject.SetActive(false);
        }

        private void UpdateCurve(Vector3 p0, Vector3 p2)
        {
            // Z축 평탄화 (2D 평면 유지를 위해 시작점 Z로 통일)
            p2.z = p0.z;

            // ★ [핵심 수정] 거리에 비례한 높이 계산
            float distance = Vector3.Distance(p0, p2);
            float dynamicHeight = distance * heightFactor;

            // 꺾이는 지점(P1) 계산
            Vector3 midPoint = (p0 + p2) / 2f;
            Vector3 p1 = midPoint + Vector3.up * dynamicHeight;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 pixel = CalculateQuadraticBezierPoint(t, p0, p1, p2);
                _lineRenderer.SetPosition(i, pixel);
            }
        }

        private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            // 베지에 곡선 공식
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * p0) + (2 * u * t * p1) + (tt * p2);
        }
    }
}