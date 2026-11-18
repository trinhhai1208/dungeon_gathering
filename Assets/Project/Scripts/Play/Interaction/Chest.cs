using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    public enum ChestKind { Item, Gold }

    /// <summary>Rương (IInteractable, cờ opened — R11). Mở → LootSpawner tung đồ.</summary>
    public sealed class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [SerializeField] string promptKey = "hud.open_chest";
        [SerializeField] int goldAmount = 15;
        [SerializeField] AudioClip openSfx;

        LevelDirector director;
        ChestKind kind = ChestKind.Item;
        bool opened;

        public bool Opened => opened;

        public void Setup(LevelDirector levelDirector, ChestKind chestKind)
        {
            director = levelDirector;
            kind = chestKind;
            opened = false;
            if (visual != null && closedSprite != null) visual.sprite = closedSprite;
        }

        public string GetInteractPrompt() => promptKey;
        public bool CanInteract(GameObject interactor) => !opened && director != null && director.IsPlaying;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            opened = true; // đặt trước khi tung đồ
            if (visual != null && openSprite != null) visual.sprite = openSprite;
            Sfx.PlayOverride(openSfx, SfxId.ChestOpen);
            var loot = director.Loot;
            Vector2 at = (Vector2)transform.position + Vector2.down * 0.6f;
            if (kind == ChestKind.Item) loot?.DropChestItem(at);
            loot?.DropGold(goldAmount, at);
        }
    }
}
