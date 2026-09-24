namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>Why a purchase did not go through. Cancelling by the player is not a failure.</summary>
    public readonly struct PurchaseFailure
    {
        /// <summary>The product, or null when the store could not tell.</summary>
        public readonly string ProductId;
        public readonly string Message;

        public PurchaseFailure(string productId, string message)
        {
            ProductId = productId;
            Message = message;
        }
    }
}
