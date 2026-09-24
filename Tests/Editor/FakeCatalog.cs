using System.Collections.Generic;

namespace WTFGames.Hephaestus.PurchasingSystem.Tests
{
    public class FakeCatalog : IProductCatalog
    {
        public const string Coins = "coins_small";
        public const string NoAds = "no_ads";

        public IEnumerable<ProductEntry> Entries { get; } = new[]
        {
            new ProductEntry(Coins, StoreProductType.Consumable, "Coins"),
            new ProductEntry(NoAds, StoreProductType.NonConsumable, "No Ads"),
        };
    }
}
