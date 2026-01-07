using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class VentArrowController : MonoBehaviour
{
    private VentController _source;
    private VentController _target;
    private Camera _cam;
    private Collider2D _col;

    public void Setup(VentController source, VentController target)
    {
        _source = source;
        _target = target;

        if (_cam == null) _cam = Camera.main;
        _col = GetComponent<Collider2D>();
        if (_col != null) _col.isTrigger = true; // 트리거로 사용
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    // 기본 방식(될 수 있으면 이걸로)
    void OnMouseUpAsButton()
    {
        TryRequest();
    }

    // 폴백: 마우스/터치 레이캐스트로 직접 판정
    void Update()
    {
        // 마우스 업
        if (Input.GetMouseButtonUp(0))
            TryRaycastClick(Input.mousePosition);

        // 터치 업
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
                TryRaycastClick(touch.position);
        }
    }

    private void TryRaycastClick(Vector3 screenPos)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || _col == null) return;

        Vector3 worldPosition = _cam.ScreenToWorldPoint(screenPos);
        Vector2 point2D = new Vector2(worldPosition.x, worldPosition.y);
        // 같은 지점에 여러 콜라이더가 있으면 가장 가까운 것만 잡히므로 OverlapPointNonAlloc 사용
        Collider2D hit = Physics2D.OverlapPoint(point2D);
        if (hit != null && hit.gameObject == gameObject)
            TryRequest();
    }

    private void TryRequest()
    {
        if (_source != null && _target != null)
        {
            Debug.Log($"Arrow clicked: {_source.name} -> {_target.name}");

            _source.SetSelectedLinkedVent(_target);

            _source.RequestMoveToSelected();
        }
    }
}
