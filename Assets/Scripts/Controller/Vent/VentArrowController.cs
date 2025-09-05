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
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended)
                TryRaycastClick(t.position);
        }
    }

    private void TryRaycastClick(Vector3 screenPos)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || _col == null) return;

        var wp = _cam.ScreenToWorldPoint(screenPos);
        var p2 = new Vector2(wp.x, wp.y);
        // 같은 지점에 여러 콜라이더가 있으면 가장 가까운 것만 잡히므로 OverlapPointNonAlloc 사용
        var hit = Physics2D.OverlapPoint(p2);
        if (hit != null && hit.gameObject == gameObject)
            TryRequest();
    }

    private void TryRequest()
    {
        if (_source != null && _target != null)
        {
            Debug.Log($"Arrow clicked: {_source.name} -> {_target.name}");
            _source.RequestMoveTo(_target);
        }
    }
}
