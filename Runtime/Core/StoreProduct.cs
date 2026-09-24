namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>A product as the store sells it right now: localized title and price, availability.</summary>
    public readonly struct StoreProduct
    {
        public readonly string Id;
        public readonly StoreProductType Type;
        public readonly string Title;
        public readonly string Description;

        /// <summary>Formatted for display in the player's currency, e.g. "$0.99" or "49,99 ₴".</summary>
        public readonly string PriceText;

        /// <summary>The same price as a number, for analytics or "best value" math.</summary>
        public readonly decimal Price;

        /// <summary>ISO 4217 code, e.g. "USD", "UAH".</summary>
        public readonly string CurrencyCode;

        public readonly bool Available;

        public StoreProduct(string id, StoreProductType type, string title, string description,
            string priceText, decimal price, string currencyCode, bool available)
        {
            Id = id;
            Type = type;
            Title = title;
            Description = description;
            PriceText = priceText;
            Price = price;
            CurrencyCode = currencyCode;
            Available = available;
        }
    }
}
