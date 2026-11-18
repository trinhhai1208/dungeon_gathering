using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Cổng: chỉ phát <see cref="LevelExitRequestedEvent"/> (sang level kế / về sảnh) khi Player bước vào hoặc bấm tương
    /// tác — không gọi thẳng code khác; LevelDirector nghe sự kiện và quyết định. Dùng một lần.
    /// Có thời gian "kích hoạt" (armDelay); Player đang đứng trong cổng lúc nó hiện ra phải bước ra rồi vào lại.
    /// Chỉ tính collider thân Player (không tính vùng tương tác / hurtbox là trigger).
    /// </summary>
    public sealed class Portal : MonoBehaviour, IInteractable
    {
        [SerializeField] string promptKey = "hud.portal";
        [SerializeField] bool enterOnTouch = true;
        [SerializeField, Min(0f)] float armDelay = 0.75f;
        [SerializeField] LevelExitDestination destination = LevelExitDestination.NextLevel;

        bool used;
        bool configured;
        bool mustExitFirst;
        int bodiesInside;
        float armedAt;

        public LevelExitDestination Destination => destination;

        /// <summary>Bật cổng (gọi khi phòng boss dọn xong).</summary>
        public void Setup(LevelExitDestination exitTo = LevelExitDestination.NextLevel)
        {
            destination = exitTo;
            configured = true;
            used = false;
            mustExitFirst = false;
            bodiesInside = 0;
            armedAt = Time.time + armDelay;
        }

        bool Armed => Time.time >= armedAt;

        public string GetInteractPrompt() => promptKey;
        public bool CanInteract(GameObject interactor) => configured && !used && Armed;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            used = true;
            Sfx.Play(SfxId.PortalEnter);
            EventBus<LevelExitRequestedEvent>.Raise(new LevelExitRequestedEvent { Destination = destination });
        }

        static bool IsPlayerBody(Collider2D c) => !c.isTrigger && c.GetComponentInParent<PlayerController>() != null;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerBody(other)) return;
            bodiesInside++;
            if (!enterOnTouch) return;
            // Cổng vừa hiện ra ngay dưới chân Player → không vào liền; chờ Player bước ra.
            if (!Armed) { mustExitFirst = true; return; }
            if (mustExitFirst) return;
            Interact(other.gameObject);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerBody(other)) return;
            bodiesInside = Mathf.Max(0, bodiesInside - 1);
            if (bodiesInside == 0) mustExitFirst = false;   // chỉ khi thân Player đã ra hẳn
        }
    }
}
