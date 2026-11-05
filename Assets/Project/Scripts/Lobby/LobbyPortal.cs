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

        public void OnPointerClick(PointerEventData eventData) => Enter();

        public void Enter()
        {
            if (used || LobbyEntry.Current == null) return;
            used = true;
            if (!LobbyEntry.Current.StartRun()) used = false;
        }
    }
}
