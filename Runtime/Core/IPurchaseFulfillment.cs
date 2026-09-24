namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>
    /// The game's side of a purchase: turn a paid product into what it gives (currency, an unlock).
    /// Called before the order is confirmed, so if the app dies in between the store re-delivers it;
    /// keep grants idempotent by <see cref="PurchaseGrant.TransactionId"/>.
    /// </summary>
    public interface IPurchaseFulfillment
    {
        FulfillmentResult Fulfill(PurchaseGrant grant);
    }
}
