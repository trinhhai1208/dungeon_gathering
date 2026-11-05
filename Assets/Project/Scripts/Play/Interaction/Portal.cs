using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Cổng: xuất hiện sau khi hạ Boss; bước vào / bấm tương tác → mở màn kết quả. Dùng một lần.
    /// Có thời gian "kích hoạt" (armDelay); Player đang đứng trong cổng lúc nó hiện ra phải bước ra rồi vào lại.
    /// </summary>
    public sealed class Portal : MonoBehaviour, IInteractable
    {
        [SerializeField] string promptKey = "hud.portal";
        [SerializeField] bool enterOnTouch = true;
        [SerializeField, Min(0f)] float armDelay = 0.75f;

        Action onEnter;
        bool used;
        bool mustExitFirst;
        float armedAt;

        public void Setup(Action enter)
        {
            onEnter = enter;
            used = false;
            mustExitFirst = false;
            armedAt = Time.time + armDelay;
        }

        bool Armed => Time.time >= armedAt;

        public string GetInteractPrompt() => promptKey;
        public bool CanInteract(GameObject interactor) => !used && onEnter != null && Armed;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            used = true;
            Sfx.Play(SfxId.PortalEnter);
            onEnter.Invoke();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!enterOnTouch || other.GetComponentInParent<PlayerController>() == null) return;
            // Cổng vừa hiện ra ngay dưới chân Player → không vào liền; chờ Player bước ra.
            if (!Armed) { mustExitFirst = true; return; }
            if (mustExitFirst) return;
            Interact(other.gameObject);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            mustExitFirst = false;
        }
    }
}
