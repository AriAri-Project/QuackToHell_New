using UnityEngine;

namespace Court
{
    public class CourtTargetCollider : MonoBehaviour
    {
        [SerializeField] private CourtPlayerView ownerView;
        public CourtPlayerView OwnerView => ownerView;

        private void Awake()
        {
            if (ownerView == null)
            {
                ownerView = GetComponentInParent<CourtPlayerView>();
            }
        }

        public void SetOwner(CourtPlayerView view)
        {
            ownerView = view;
        }
    }
}
