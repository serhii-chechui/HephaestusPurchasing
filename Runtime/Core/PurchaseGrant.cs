namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>What <see cref="IPurchaseFulfillment"/> is asked to grant.</summary>
    public readonly struct PurchaseGrant
    {
        public readonly string ProductId;
        public readonly StoreProductType Type;
        public readonly int Quantity;

        /// <summary>
        /// The store's transaction id. Save it together with what you grant and answer
        /// <see cref="FulfillmentResult.AlreadyGranted"/> when it comes again: the store re-delivers
        /// orders that were paid but not confirmed (e.g. the app was closed mid-purchase).
        /// </summary>
        public readonly string TransactionId;

        /// <summary>
        /// True for a non-consumable the player already owns, reported at startup or by
        /// <see cref="IStoreService.RestorePurchases"/> (new device, reinstall). Granting must be idempotent.
        /// </summary>
        public readonly bool IsRestore;

        public PurchaseGrant(string productId, StoreProductType type, int quantity, string transactionId, bool isRestore)
        {
            ProductId = productId;
            Type = type;
            Quantity = quantity;
            TransactionId = transactionId;
            IsRestore = isRestore;
        }
    }
}
