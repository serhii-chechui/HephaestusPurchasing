using System;
using System.Collections.Generic;

namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>A store that sells nothing: for platforms without in-app purchases and for tests.</summary>
    public class NullStoreService : IStoreService
    {
        private const string Unavailable = "In-app purchases are not available";

        public bool IsReady => false;
        public IReadOnlyList<StoreProduct> Products { get; } = new StoreProduct[0];

        public event Action ProductsChanged { add { } remove { } }
        public event Action<string> PurchaseSucceeded { add { } remove { } }
        public event Action<PurchaseFailure> PurchaseFailed;
        public event Action<string> OwnershipChanged { add { } remove { } }

        public bool IsOwned(string productId) => false;

        public void Buy(string productId) => PurchaseFailed?.Invoke(new PurchaseFailure(productId, Unavailable));

        public void RestorePurchases(Action<bool, string> onFinished = null) => onFinished?.Invoke(false, Unavailable);
    }
}
