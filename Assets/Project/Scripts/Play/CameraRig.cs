using Unity.Cinemachine;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Camera thế giới của Play: CinemachineCamera follow Player, Confiner2D theo phòng hiện tại.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] CinemachineCamera virtualCamera;
        [SerializeField] CinemachineConfiner2D confiner;
        [SerializeField] Camera mainCamera;

        Collider2D currentBounds;

        void Awake()
        {
            if (virtualCamera == null) virtualCamera = GetComponentInChildren<CinemachineCamera>(true);
            if (confiner == null && virtualCamera != null) confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
            if (mainCamera == null) mainCamera = GetComponentInChildren<Camera>(true);
        }

        public Camera Camera => mainCamera != null ? mainCamera : Camera.main;

        public void Follow(Transform target, bool snap = true)
        {
            if (virtualCamera == null) return;
            virtualCamera.Follow = target;
            if (snap && target != null)
                virtualCamera.ForceCameraPosition(new Vector3(target.position.x, target.position.y, virtualCamera.transform.position.z), Quaternion.identity);
        }

        public void SetBounds(Collider2D bounds)
        {
            // Khóa confiner để camera follow mượt mà (smooth damping) giống Soul Knight,
            // tránh giật/khựng màn hình khi người chơi di chuyển qua lại giữa phòng và hành lang.
            if (confiner != null) confiner.enabled = false;
        }
    }
}
