using System;
using UnityEngine;

namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>One row of <see cref="ProductCatalog"/>.</summary>
    [Serializable]
    public class ProductCatalogEntry
    {
        [Tooltip("Store product id, the same in App Store Connect and Google Play Console.")]
        public string productId;
        public StoreProductType type;
        [Tooltip("Shown when the store has no title of its own, and in the editor's Fake Store.")]
        public string fallbackTitle;
    }
}
