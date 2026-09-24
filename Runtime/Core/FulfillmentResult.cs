namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>The game's answer to a <see cref="PurchaseGrant"/>.</summary>
    public enum FulfillmentResult
    {
        /// <summary>Granted now: the order is confirmed and <see cref="IStoreService.PurchaseSucceeded"/> fires.</summary>
        Granted,

        /// <summary>This transaction was granted before: the order is confirmed silently.</summary>
        AlreadyGranted,

        /// <summary>This build cannot grant the product: the order stays pending so a later build can.</summary>
        Unknown
    }
}
