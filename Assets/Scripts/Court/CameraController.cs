using UnityEngine;

namespace Court
{
    [DefaultExecutionOrder(100)] //다른 쪽에서 카메라를 조작하는 로직이 Start에 있어서 미룰 목적으로....
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Vector2 centerPosition = Vector2.zero;
        private Camera mainCam;

        void Start()
        {
            SetCameraFixed();
        }

        public void SetCameraFixed()
        {
            mainCam = Camera.main;

            if (mainCam != null)
            {
                Vector3 newPos = new Vector3(centerPosition.x, centerPosition.y, mainCam.transform.position.z);
                mainCam.transform.position = newPos;
                Debug.Log("카메라 위치 설정이 완료되었습니다." + mainCam.transform.position);
            }
        }

        public void OnDestroy()
        {
            if (mainCam != null)
            {
                mainCam.transform.localPosition = new Vector3(0f, 0f, mainCam.transform.position.z);
            }
        }
    }
}
