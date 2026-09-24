namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>One product in a store order, independent of the store SDK.</summary>
    public readonly struct OrderLine
    {
        public readonly string ProductId;
        public readonly int Quantity;

        public OrderLine(string productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }
    }
}
