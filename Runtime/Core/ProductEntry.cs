namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>A product the game sells, as the catalog declares it.</summary>
    public readonly struct ProductEntry
    {
        /// <summary>Store product id, the same in App Store Connect and Google Play Console.</summary>
        public readonly string Id;
        public readonly StoreProductType Type;

        /// <summary>Shown when the store has no title of its own, and in the editor's Fake Store.</summary>
        public readonly string FallbackTitle;

        public ProductEntry(string id, StoreProductType type, string fallbackTitle)
        {
            Id = id;
            Type = type;
            FallbackTitle = fallbackTitle;
        }
    }
}
