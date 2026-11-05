using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Đặt trên collider trigger "vạch cửa"/lòng phòng: báo RoomController khi Player THỰC SỰ bước vào.
    /// Chỉ tính collider thân (không phải trigger như InteractRange r=1.2 / Hurtbox) và chỉ khi
    /// tâm nhân vật đã nằm trong vùng — tránh đóng cửa khi người chơi còn đứng ngoài hành lang.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomTriggerRelay : MonoBehaviour
    {
        public RoomController Room;

        Collider2D area;
        bool playerInside; // báo MỘT lần mỗi lượt vào (như OnTriggerEnter cũ), reset khi thân rời vùng

        void Awake() => area = GetComponent<Collider2D>();

        void OnTriggerEnter2D(Collider2D other) => Check(other);

        // Thân chạm mép vùng trước khi tâm vào hẳn → tiếp tục kiểm tra tới khi tâm qua vạch
        void OnTriggerStay2D(Collider2D other) { if (!playerInside) Check(other); }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.isTrigger && IsPlayer(other)) playerInside = false;
        }

        void Check(Collider2D other)
        {
            if (Room == null || other.isTrigger || playerInside || !IsPlayer(other)) return;
            Vector2 center = other.attachedRigidbody != null ? other.attachedRigidbody.position : (Vector2)other.transform.position;
            if (area != null && !area.OverlapPoint(center)) return;
            playerInside = true;
            Room.OnPlayerEntered();
        }

        static bool IsPlayer(Collider2D c)
        {
            var d = CombatUtil.Resolve(c);
            return d != null && d.Team == Core.Team.Player;
        }
    }
}
