using System;
using System.Collections.Generic;

namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>
    /// Store-independent purchase rules: checks orders against the catalog, asks the game's
    /// <see cref="IPurchaseFulfillment"/> to grant them and tracks owned non-consumables.
    /// Store adapters translate SDK orders into <see cref="OrderLine"/>s and act on the outcome.
    /// </summary>
    public class PurchaseProcessor
    {
        private readonly IPurchaseFulfillment _fulfillment;
        private readonly Dictionary<string, ProductEntry> _entries = new Dictionary<string, ProductEntry>();
        private readonly HashSet<string> _owned = new HashSet<string>();

        /// <summary>Raised with the product id the first time a non-consumable is seen as owned.</summary>
        public event Action<string> OwnershipChanged;

        public PurchaseProcessor(IProductCatalog catalog, IPurchaseFulfillment fulfillment)
        {
            _fulfillment = fulfillment;
            foreach (var entry in catalog.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Id)) _entries[entry.Id] = entry;
            }
        }

        public bool TryGetEntry(string productId, out ProductEntry entry) =>
            _entries.TryGetValue(productId ?? string.Empty, out entry);

        public bool IsOwned(string productId) => productId != null && _owned.Contains(productId);

        /// <summary>A paid order waiting for confirmation: grant every line or none.</summary>
        public PendingOrderOutcome ProcessPending(string transactionId, IReadOnlyList<OrderLine> lines)
        {
            if (lines.Count == 0) return PendingOrderOutcome.LeftPending;
            // Check the whole order first, so it is never granted in part.
            foreach (var line in lines)
            {
                if (!_entries.ContainsKey(line.ProductId ?? string.Empty)) return PendingOrderOutcome.LeftPending;
            }

            bool granted = false;
            foreach (var line in lines)
            {
                var entry = _entries[line.ProductId];
                var grant = new PurchaseGrant(entry.Id, entry.Type, Math.Max(1, line.Quantity), transactionId, false);
                var result = _fulfillment.Fulfill(grant);
                if (result == FulfillmentResult.Unknown) return PendingOrderOutcome.LeftPending;
                if (result == FulfillmentResult.Granted) granted = true;
                if (entry.Type == StoreProductType.NonConsumable) MarkOwned(entry.Id);
            }
            return granted ? PendingOrderOutcome.Granted : PendingOrderOutcome.AlreadyGranted;
        }

        /// <summary>
        /// A confirmed order the store reports at startup or after a restore. Only non-consumables are
        /// granted again (idempotently); a confirmed consumable was already granted when it was bought.
        /// </summary>
        public void ProcessOwned(string transactionId, IReadOnlyList<OrderLine> lines)
        {
            foreach (var line in lines)
            {
                if (!_entries.TryGetValue(line.ProductId ?? string.Empty, out var entry)) continue;
                if (entry.Type != StoreProductType.NonConsumable) continue;

                _fulfillment.Fulfill(new PurchaseGrant(entry.Id, entry.Type, 1, transactionId, true));
                MarkOwned(entry.Id);
            }
        }

        private void MarkOwned(string productId)
        {
            if (_owned.Add(productId)) OwnershipChanged?.Invoke(productId);
        }
    }
}
