using System.Collections.Generic;

namespace WTFGames.Hephaestus.PurchasingSystem.Tests
{
    /// <summary>Grants each transaction once, like a game that saves transaction ids.</summary>
    public class FakeFulfillment : IPurchaseFulfillment
    {
        public readonly List<PurchaseGrant> Grants = new List<PurchaseGrant>();
        public FulfillmentResult? ForcedResult;

        private readonly HashSet<string> _transactions = new HashSet<string>();

        public FulfillmentResult Fulfill(PurchaseGrant grant)
        {
            Grants.Add(grant);
            if (ForcedResult.HasValue) return ForcedResult.Value;
            return _transactions.Add(grant.TransactionId) ? FulfillmentResult.Granted : FulfillmentResult.AlreadyGranted;
        }
    }
}
