using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using Zenject;

namespace WTFGames.Hephaestus.PurchasingSystem.UnityIAP
{
    /// <summary>
    /// <see cref="IStoreService"/> on Unity IAP 5. Connects at startup, fetches the catalog's products and
    /// the player's purchases. Two-step flow: a paid order (<c>OnPurchasePending</c>) is granted through the
    /// game's <see cref="IPurchaseFulfillment"/>, then confirmed; if the app dies in between, the store
    /// re-delivers it. Owned non-consumables reported by the store are granted again on every launch and
    /// on restore. Receipts are not validated: Apple StoreKit 2 verifies locally; Google Play validation
    /// needs the obfuscated tangle (Services > In-App Purchasing > Receipt Validation Obfuscator).
    /// </summary>
    public class UnityIapStoreService : IStoreService, IInitializable, IDisposable
    {
        private const string LogTag = "[HephaestusPurchasing]";

        private readonly IProductCatalog _catalog;
        private readonly PurchaseProcessor _processor;
        private readonly List<StoreProduct> _products = new List<StoreProduct>();
        private StoreController _store;

        public bool IsReady { get; private set; }
        public IReadOnlyList<StoreProduct> Products => _products;

        public event Action ProductsChanged;
        public event Action<string> PurchaseSucceeded;
        public event Action<PurchaseFailure> PurchaseFailed;
        public event Action<string> OwnershipChanged;

        public UnityIapStoreService(IProductCatalog catalog, IPurchaseFulfillment fulfillment)
        {
            _catalog = catalog;
            _processor = new PurchaseProcessor(catalog, fulfillment);
            _processor.OwnershipChanged += id => OwnershipChanged?.Invoke(id);
        }

        public async void Initialize()
        {
            if (!_catalog.Entries.Any()) return;

            _store = UnityIAPServices.StoreController();

            // Subscribe to everything before Connect: pending purchases from a previous
            // session can be delivered right away.
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;

            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogTag} Store connection failed: {e.Message}");
            }
        }

        public void Dispose()
        {
            if (_store == null) return;
            _store.OnStoreConnected -= OnStoreConnected;
            _store.OnStoreDisconnected -= OnStoreDisconnected;
            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasesFetched -= OnPurchasesFetched;
            _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnPurchaseFailed -= OnPurchaseFailed;
            _store.OnPurchaseDeferred -= OnPurchaseDeferred;
        }

        public bool IsOwned(string productId) => _processor.IsOwned(productId);

        public void Buy(string productId)
        {
            if (!IsReady)
            {
                PurchaseFailed?.Invoke(new PurchaseFailure(productId, "Store is not available"));
                return;
            }
            if (_processor.IsOwned(productId))
            {
                PurchaseFailed?.Invoke(new PurchaseFailure(productId, "Already owned"));
                return;
            }
            _store.PurchaseProduct(productId);
        }

        public void RestorePurchases(Action<bool, string> onFinished = null)
        {
            if (_store == null)
            {
                onFinished?.Invoke(false, "Store is not available");
                return;
            }
            _store.RestoreTransactions((success, error) =>
            {
                // Fetch again so restored non-consumables come through OnPurchasesFetched on every store.
                if (success) _store.FetchPurchases();
                else Debug.LogWarning($"{LogTag} Restore failed: {error}");
                onFinished?.Invoke(success, error);
            });
        }

        private void OnStoreConnected()
        {
            var definitions = _catalog.Entries
                .Select(e => new ProductDefinition(e.Id, ToUnity(e.Type)))
                .ToList();
            _store.FetchProducts(definitions);
            // Re-delivers paid but unconfirmed orders and reports owned non-consumables.
            _store.FetchPurchases();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            IsReady = false;
            Debug.LogWarning($"{LogTag} Store disconnected: {failure.Message}");
            ProductsChanged?.Invoke();
        }

        private void OnProductsFetched(List<Product> fetched)
        {
            _products.Clear();
            foreach (var entry in _catalog.Entries)
            {
                var product = fetched.FirstOrDefault(p => p.definition.id == entry.Id);
                if (product == null) continue;

                var meta = product.metadata;
                // In the editor Unity IAP runs the Fake Store, whose titles are placeholders
                // ("Fake title for <product id>"); use the catalog's title there.
                string title = Application.isEditor || string.IsNullOrEmpty(meta.localizedTitle)
                    ? entry.FallbackTitle
                    : meta.localizedTitle;
                string description = Application.isEditor ? string.Empty : meta.localizedDescription;
                _products.Add(new StoreProduct(entry.Id, entry.Type, title, description,
                    meta.localizedPriceString, meta.localizedPrice, meta.isoCurrencyCode, product.availableToPurchase));
            }
            IsReady = true;
            ProductsChanged?.Invoke();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure) =>
            Debug.LogWarning($"{LogTag} Product fetch failed: {failure.FailureReason}");

        private void OnPurchasesFetched(Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders)
                _processor.ProcessOwned(order.Info.TransactionID, Lines(order));
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) =>
            Debug.LogWarning($"{LogTag} Purchase fetch failed: {failure.Message}");

        private void OnPurchasePending(PendingOrder order)
        {
            var lines = Lines(order);
            var outcome = _processor.ProcessPending(order.Info.TransactionID, lines);
            if (outcome == PendingOrderOutcome.LeftPending)
            {
                // Not a product this build can grant: leave it unconfirmed so a later build can.
                Debug.LogError($"{LogTag} Order {order.Info.TransactionID} " +
                               $"({string.Join(", ", lines.Select(l => l.ProductId))}) left unconfirmed.");
                return;
            }

            _store.ConfirmPurchase(order);
            if (outcome == PendingOrderOutcome.Granted)
            {
                foreach (var line in lines) PurchaseSucceeded?.Invoke(line.ProductId);
            }
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                Debug.LogWarning($"{LogTag} Confirmation failed: {failed.FailureReason} {failed.Details}");
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            if (failed.FailureReason == PurchaseFailureReason.UserCancelled) return;
            Debug.LogWarning($"{LogTag} Purchase failed: {failed.FailureReason} {failed.Details}");
            string productId = failed.CartOrdered?.Items().FirstOrDefault()?.Product?.definition.id;
            PurchaseFailed?.Invoke(new PurchaseFailure(productId, failed.FailureReason.ToString()));
        }

        // Ask to Buy / Google deferred payment: nothing to grant until it turns into a pending order.
        private void OnPurchaseDeferred(DeferredOrder order) =>
            Debug.Log($"{LogTag} Purchase deferred; it is granted once approved.");

        private static List<OrderLine> Lines(Order order) =>
            order.CartOrdered.Items()
                .Select(item => new OrderLine(item.Product.definition.id, item.Quantity))
                .ToList();

        private static ProductType ToUnity(StoreProductType type) =>
            type == StoreProductType.NonConsumable ? ProductType.NonConsumable : ProductType.Consumable;
    }
}
