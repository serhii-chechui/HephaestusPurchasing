namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>What to do with a paid order after <see cref="PurchaseProcessor.ProcessPending"/>.</summary>
    public enum PendingOrderOutcome
    {
        /// <summary>Granted now: confirm the order and report the purchase.</summary>
        Granted,

        /// <summary>Granted before (a re-delivered order): confirm it silently.</summary>
        AlreadyGranted,

        /// <summary>Not granted: do not confirm, so the store delivers it again later.</summary>
        LeftPending
    }
}
