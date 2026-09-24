using System.Collections.Generic;

namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>
    /// What the game sells. Supply your own (e.g. from a game config) or use <see cref="ProductCatalog"/>.
    /// The order of <see cref="Entries"/> is the order of <see cref="IStoreService.Products"/>.
    /// </summary>
    public interface IProductCatalog
    {
        IEnumerable<ProductEntry> Entries { get; }
    }
}
