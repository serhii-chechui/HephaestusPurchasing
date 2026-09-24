using System;
using System.Collections.Generic;

namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>The in-app store for UI and game code.</summary>
    public interface IStoreService
    {
        /// <summary>True once the store is connected and the products are loaded.</summary>
        bool IsReady { get; }

        /// <summary>The catalog's products the store knows, in catalog order.</summary>
        IReadOnlyList<StoreProduct> Products { get; }

        /// <summary>Raised when products, prices or availability change (and on connect/disconnect).</summary>
        event Action ProductsChanged;

        /// <summary>Raised with the product id after a purchase was granted and confirmed.</summary>
        event Action<string> PurchaseSucceeded;

        /// <summary>Raised when a purchase fails; not raised when the player cancels.</summary>
        event Action<PurchaseFailure> PurchaseFailed;

        /// <summary>Raised with the product id when a non-consumable becomes owned (bought or restored).</summary>
        event Action<string> OwnershipChanged;

        /// <summary>Whether the player owns a non-consumable, as the store reported it this session.</summary>
        bool IsOwned(string productId);

        void Buy(string productId);

        /// <summary>
        /// Asks the store for non-consumables bought on another device or before a reinstall; they arrive
        /// through <see cref="IPurchaseFulfillment"/> with <see cref="PurchaseGrant.IsRestore"/>. Apple requires
        /// a Restore button when a game sells non-consumables.
        /// </summary>
        /// <param name="onFinished">True when the store answered; false with a message otherwise.</param>
        void RestorePurchases(Action<bool, string> onFinished = null);
    }
}
