using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonGathering.Lobby
{
    /// <summary>NPC Thương nhân: bấm → Shop sảnh (ShopContext.Lobby).</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MerchantNpc : MonoBehaviour, IPointerClickHandler
    {
        public async void OnPointerClick(PointerEventData eventData)
        {
            await Services.UI.Open<UIScreen>(AddressKeys.UiScreenShop, Catalog.ShopContext.Lobby);
        }
    }
}
