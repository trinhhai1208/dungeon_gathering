using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Điểm đứng của một nhân vật trong sảnh (bấm qua Physics2DRaycaster trên camera Lobby).
    /// Hình: address char/&lt;id&gt; (prefab CharLobby_&lt;id&gt;) → con có sẵn trong visualRoot → tự dựng từ HeroDefinitionSO.portrait.
    /// Nhân vật chưa ra mắt (HeroDefinitionSO.released = false): dáng tối + nhãn "CHƯA RA MẮT", ẩn ổ khóa; bấm vẫn mở panel giới thiệu.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CharacterSpot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] string characterId = "knight";
        [SerializeField] Transform visualRoot;
        [SerializeField] GameObject selectedMarker;   // mũi tên trên đầu
        [SerializeField] GameObject lockedMarker;     // ổ khóa + giá (world-space)
        [SerializeField] GameObject comingSoonMarker; // nhãn "CHƯA RA MẮT" (world-space)
        [Tooltip("Màu nhân vào hình khi chưa ra mắt (dáng tối)")]
        [SerializeField] Color comingSoonTint = new(0.16f, 0.13f, 0.2f, 1f);
        [SerializeField] string sortingLayer = "Characters";

        LobbyEntry lobby;
        readonly List<SpriteRenderer> visuals = new();

        public string CharacterId => characterId;
        public bool IsReleased { get; private set; } = true;

        public async Awaitable Setup(LobbyEntry entry)
        {
            lobby = entry;
            var data = entry.GetCharacter(characterId);
            IsReleased = data == null || data.released;

            var root = visualRoot != null ? visualRoot : transform;
            string address = AddressKeys.LobbyCharacter(characterId);
            if (AddressablesUtil.KeyExists<GameObject>(address))
            {
                try { await entry.Scope.Instantiate(address, root); }
                catch (System.Exception e) when (e is not System.OperationCanceledException) { Debug.LogException(e); }
                if (this == null) return;
            }
            else EnsurePortrait(root, data != null ? data.portrait : null);

            bool unlocked = Services.Save == null || Services.Save.Data.IsUnlocked(characterId);
            if (lockedMarker != null) lockedMarker.SetActive(false); // Không hiện giá tiền trên sàn, bấm vào nhân vật mới xem trong panel
            if (comingSoonMarker != null) comingSoonMarker.SetActive(!IsReleased);
            ApplyTint(root, IsReleased ? Color.white : comingSoonTint);
            SetSelected(IsReleased && entry.SelectedCharacterId == characterId);
        }

        /// <summary>visualRoot chưa có hình nào → dựng SpriteRenderer từ portrait (nhân vật chưa có prefab sảnh).</summary>
        void EnsurePortrait(Transform root, Sprite portrait)
        {
            if (portrait == null || HasOwnRenderer(root)) return;
            var go = new GameObject("Portrait");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = portrait;
            sr.sortingLayerName = sortingLayer;
        }

        bool HasOwnRenderer(Transform root)
        {
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (!IsMarker(sr.transform)) return true;
            return false;
        }

        void ApplyTint(Transform root, Color tint)
        {
            visuals.Clear();
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (!IsMarker(sr.transform)) visuals.Add(sr);
            foreach (var sr in visuals) sr.color = tint;
        }

        bool IsMarker(Transform t)
        {
            for (; t != null && t != transform.parent; t = t.parent)
                if ((selectedMarker != null && t == selectedMarker.transform) || (lockedMarker != null && t == lockedMarker.transform)
                    || (comingSoonMarker != null && t == comingSoonMarker.transform)) return true;
            return false;
        }

        public void SetSelected(bool on) { if (selectedMarker != null) selectedMarker.SetActive(on); }

        public void OnPointerClick(PointerEventData eventData) => lobby?.SelectCharacter(characterId);
    }
}
