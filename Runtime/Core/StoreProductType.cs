namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>How a product behaves once bought.</summary>
    public enum StoreProductType
    {
        /// <summary>Bought again and again (currency packs); granted on every purchase.</summary>
        Consumable,

        /// <summary>Bought once and owned forever (remove ads, a starter pack); comes back on restore.</summary>
        NonConsumable
    }
}
