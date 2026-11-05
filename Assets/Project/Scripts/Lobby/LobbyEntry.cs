using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// IModuleEntry của Lobby.unity: tải catalog, dựng nhân vật đứng ở các CharacterSpot, mở LobbyHud.
    /// Chọn nhân vật → panel nhân vật; Portal → bắt đầu run với nhân vật đang chọn.
    /// </summary>
    public sealed class LobbyEntry : MonoBehaviour, IModuleEntry
    {
        [Tooltip("Catalog tham chiếu trực tiếp khi chưa có address data/catalog (dev)")]
        [SerializeField] ItemCatalog fallbackCatalog;

        public static LobbyEntry Current { get; private set; }
        public ItemCatalog Catalog { get; private set; }
        public ModuleScope Scope { get; private set; }
        public string SelectedCharacterId { get; private set; }

        readonly List<CharacterSpot> spots = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Current = null;

        public async Awaitable Begin(ModuleContext context)
        {
            Current = this;
            Scope = context.Scope;
            var token = destroyCancellationToken;

            if (AddressablesUtil.KeyExists(AddressKeys.DataCatalog)) Catalog = await ItemCatalog.LoadAsync(Scope);
            else if (fallbackCatalog != null) { fallbackCatalog.Register(); Catalog = fallbackCatalog; }
            if (token.IsCancellationRequested) return;

            SelectedCharacterId = ResolvePlayable(Services.Save?.Data.selectedCharacterId);
            GetComponentsInChildren(true, spots);
            if (spots.Count == 0) spots.AddRange(FindObjectsByType<CharacterSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var s in spots)
            {
                if (s.gameObject.scene != gameObject.scene) continue; // R7: không đụng object của scene khác
                await s.Setup(this);
                if (token.IsCancellationRequested) return;
            }

            if (AddressablesUtil.KeyExists(AddressKeys.UiHudLobby))
                await Services.UI.Open<UIScreen>(AddressKeys.UiHudLobby);
        }

        public CharacterData GetCharacter(string id) => Catalog != null ? Catalog.GetCharacter(id) : null;

        /// <summary>Nhân vật có thể chơi (đã ra mắt) — id không hợp lệ / chưa ra mắt → nhân vật đã ra mắt đầu tiên.</summary>
        string ResolvePlayable(string id)
        {
            var c = GetCharacter(id);
            if (c != null && c.released) return id;
            if (Catalog != null)
                foreach (var ch in Catalog.Characters)
                    if (ch != null && ch.released) return ch.characterId;
            return id;
        }

        /// <summary>
        /// Bấm nhân vật trong sảnh → mở panel (args = characterId). Nhân vật đã ra mắt thì đồng thời thành lựa chọn chơi;
        /// nhân vật chưa ra mắt chỉ mở panel giới thiệu, giữ nguyên lựa chọn cũ (Portal vẫn vào bằng nhân vật đó).
        /// </summary>
        public async void SelectCharacter(string characterId)
        {
            // Chưa Begin (Scope null) → UIService sẽ gán màn cho scope Global và CloseModule(lobby) không đóng được → panel rò sang trận.
            if (Current != this || Scope == null || Scope.IsDisposed) return;
            if (Services.Scenes == null || Services.Scenes.IsTransitioning) return;
            var data = GetCharacter(characterId);
            if (data == null || data.released)
            {
                SelectedCharacterId = characterId;
                foreach (var s in spots) s.SetSelected(s.CharacterId == characterId);
            }
            if (AddressablesUtil.KeyExists(AddressKeys.UiPanelCharacter))
                await Services.UI.Open<UIScreen>(AddressKeys.UiPanelCharacter, characterId);
        }

        /// <summary>Vào Portal: ghi loadout + config vào SessionData rồi Go(play). Trả false nếu chưa chọn / chưa mở khóa.</summary>
        public bool StartRun()
        {
            if (Current != this || Scope == null || Scope.IsDisposed) return false;
            if (Services.Scenes == null || Services.Scenes.IsTransitioning) return false;
            var character = GetCharacter(SelectedCharacterId);
            if (character == null)
            {
                Debug.LogWarning("[Lobby] Chưa chọn nhân vật.");
                return false;
            }
            if (!character.released)
            {
                Debug.LogWarning($"[Lobby] Nhân vật '{character.characterId}' chưa ra mắt.");
                return false;
            }
            if (!Economy.StartRun(character, Catalog)) return false;
            _ = Services.Scenes.Go(AddressKeys.ScenePlay);
            return true;
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
