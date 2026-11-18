using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonGathering.Lobby
{
    /// <summary>Portal trong sảnh: bấm → bắt đầu run với nhân vật đang chọn. Tự khóa khi bấm (R1).</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LobbyPortal : MonoBehaviour, IPointerClickHandler
    {
        bool used;

        void OnEnable() => used = false;
        void OnDisable() => used = false;

        public void OnPointerClick(PointerEventData eventData) => Enter();

        public void Enter()
        {
            if (used || LobbyEntry.Current == null) return;
            if (Services.Scenes != null && Services.Scenes.IsTransitioning) return;
            used = true;
            // Không gọi thẳng LobbyEntry — phát sự kiện, LobbyEntry thử vào run rồi báo kết quả.
            EventBus<RunStartRequestedEvent>.Raise(new RunStartRequestedEvent { Done = ok => { if (!ok) used = false; } });
        }
    }
}
