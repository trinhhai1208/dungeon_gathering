using System;
using DungeonGathering.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonGathering.UI
{
    /// <summary>
    /// One item square used everywhere an item is shown (equipment slots, weapon slots, bags, shop, run end, HUD).
    /// Layers, back to front: rarity colour square, icon, rarity letter (top-left, inside the square),
    /// cooldown overlay, selection frame (9-sliced corner brackets around the square).
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemTile : MonoBehaviour, IPointerClickHandler
    {
        [Header("Parts")]
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] Image rarityLetter;
        [SerializeField] Image cooldownOverlay;
        [SerializeField] TMP_Text cooldownText;
        [SerializeField] Image selectionFrame;

        [Header("Sprites, indexed by Rarity (S, A, B, C, F)")]
        [SerializeField] Sprite[] raritySquares = new Sprite[5];
        [SerializeField] Sprite[] rarityLetters = new Sprite[5];

        [Header("Empty slot")]
        [SerializeField] Color emptySquareTint = new(0.55f, 0.55f, 0.6f, 1f);
        [SerializeField] Color emptyIconTint = new(0.09f, 0.1f, 0.17f, 0.6f);

        [Header("Display")]
        [Tooltip("Off for in-run UI (HUD, Pause): only the colour square is shown.")]
        [SerializeField] bool showRarityLetter = true;

        public event Action<ItemTile> Clicked;

        public bool IsEmpty { get; private set; } = true;
        public bool IsSelected => selectionFrame != null && selectionFrame.enabled;
        public Rarity Rarity { get; private set; } = Rarity.D;

        void Awake()
        {
            SetSelected(false);
            ClearCooldown();
        }

        /// <summary>Show an item. Icons are drawn with preserveAspect so 16px items and 32px swords share one slot size.</summary>
        public void SetItem(Sprite itemIcon, Rarity rarity)
        {
            IsEmpty = false;
            Rarity = rarity;
            if (background != null)
            {
                background.sprite = Pick(raritySquares, rarity);
                background.color = Color.white;
            }
            if (icon != null)
            {
                icon.sprite = itemIcon;
                icon.color = Color.white;
                icon.enabled = itemIcon != null;
            }
            RefreshLetter();
        }

        /// <summary>Empty slot: darkened F square plus an optional faint silhouette of the slot type.</summary>
        public void SetEmpty(Sprite slotSilhouette = null)
        {
            IsEmpty = true;
            Rarity = Rarity.D;
            if (background != null)
            {
                background.sprite = Pick(raritySquares, Rarity.D);
                background.color = emptySquareTint;
            }
            if (icon != null)
            {
                icon.sprite = slotSilhouette;
                icon.color = emptyIconTint;
                icon.enabled = slotSilhouette != null;
            }
            RefreshLetter();
        }

        public void SetShowRarityLetter(bool show)
        {
            showRarityLetter = show;
            RefreshLetter();
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null) selectionFrame.enabled = selected;
        }

        /// <summary>Radial cooldown for item active skills on the HUD. remaining &lt;= 0 clears it.</summary>
        public void SetCooldown(float remaining, float total)
        {
            if (cooldownOverlay == null) return;
            bool on = remaining > 0f && total > 0f;
            cooldownOverlay.enabled = on;
            cooldownOverlay.fillAmount = on ? Mathf.Clamp01(remaining / total) : 0f;
            if (cooldownText != null)
            {
                cooldownText.enabled = on;
                // Gọi mỗi frame từ HUD: chỉ đổi text (cấp phát chuỗi) khi số giây nguyên thay đổi.
                int seconds = on ? Mathf.CeilToInt(remaining) : -1;
                if (seconds != shownCooldownSeconds)
                {
                    shownCooldownSeconds = seconds;
                    cooldownText.text = on ? seconds.ToString() : string.Empty;
                }
            }
        }

        int shownCooldownSeconds = int.MinValue;   // giá trị đã ghi vào cooldownText (-1 = trống)

        public void ClearCooldown() => SetCooldown(0f, 0f);

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);

        void RefreshLetter()
        {
            if (rarityLetter == null) return;
            bool show = showRarityLetter && !IsEmpty;
            rarityLetter.enabled = show;
            if (show) rarityLetter.sprite = Pick(rarityLetters, Rarity);
        }

        /// <summary>Sprite theo Rarity; mảng thiếu/ngắn hoặc giá trị enum lạ → null thay vì ném lỗi.</summary>
        static Sprite Pick(Sprite[] sprites, Rarity rarity)
        {
            int i = (int)rarity;
            return sprites != null && i >= 0 && i < sprites.Length ? sprites[i] : null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (raritySquares == null || raritySquares.Length != 5) Array.Resize(ref raritySquares, 5);
            if (rarityLetters == null || rarityLetters.Length != 5) Array.Resize(ref rarityLetters, 5);
        }
#endif
    }
}
