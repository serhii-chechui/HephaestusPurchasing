# Hephaestus Purchasing

Hephaestus Purchasing is a part of the **Hephaestus Core** framework that sells in-app products in Unity through
[Unity IAP 5](https://docs.unity3d.com/Packages/com.unity.purchasing@latest). It handles the store side of a purchase
(connecting, prices, the two-step pending → confirm flow, re-delivered orders, restore); the game only says what it
sells and what a purchase gives.

## Features

- **Consumables and non-consumables** — currency packs bought again and again; unlocks owned forever.
- **Restore** — non-consumables bought on another device or before a reinstall come back through the same grant path.
- **Safe two-step flow** — a paid order is granted first and confirmed after; if the app dies in between, the store
  re-delivers it. Orders for products this build does not know stay pending for a later build.
- **Localized prices** — title, description, formatted price, numeric price and ISO currency from the store.
- **Editor-friendly** — in the editor Unity IAP runs its Fake Store; the catalog's titles replace its placeholders.
- **Store-independent core** — the interfaces and purchase rules have no Unity IAP dependency
  (`NullStoreService` for platforms without purchases and for tests).

## Requirements

- Unity **2022.3+**
- [Extenject (Zenject)](https://github.com/Mathijs-Bakker/Extenject) `9.2.0` and Unity IAP `5.x` (package dependencies)
- Products created in App Store Connect / Google Play Console with the same ids

## Assemblies

| Assembly                                      | Contents                                                        |
|-----------------------------------------------|-----------------------------------------------------------------|
| `com.wtfgames.hephaestus.purchasing`          | Interfaces, models, `PurchaseProcessor`, `ProductCatalog`, `NullStoreService` |
| `com.wtfgames.hephaestus.purchasing.unityiap` | `UnityIapStoreService`, `HephaestusPurchasingInstaller`         |

## Usage

### 1. Say what you sell

Implement `IProductCatalog` on your own config, or create **Hephaestus → Purchasing → Product Catalog**:

```csharp
public class ShopConfig : ScriptableObject, IProductCatalog
{
    public IEnumerable<ProductEntry> Entries => new[]
    {
        new ProductEntry("com.studio.game.coins_small", StoreProductType.Consumable, "Handful of Coins"),
        new ProductEntry("com.studio.game.no_ads", StoreProductType.NonConsumable, "No Ads"),
    };
}
```

### 2. Say what a purchase gives

`Fulfill` runs before the order is confirmed. Save the transaction id together with the grant, in the same save, and
answer `AlreadyGranted` when it comes again. Non-consumables also arrive with `IsRestore = true` on every launch and
after a restore: granting them must be idempotent.

```csharp
public class ShopFulfillment : IPurchaseFulfillment
{
    public FulfillmentResult Fulfill(PurchaseGrant grant)
    {
        switch (grant.ProductId)
        {
            case "com.studio.game.coins_small":
                return _wallet.Credit(grant.TransactionId, 500 * grant.Quantity)
                    ? FulfillmentResult.Granted : FulfillmentResult.AlreadyGranted;
            case "com.studio.game.no_ads":
                _settings.AdsRemoved = true;   // idempotent
                return FulfillmentResult.Granted;
            default:
                return FulfillmentResult.Unknown;   // stays pending for a newer build
        }
    }
}
```

### 3. Install

```csharp
Container.Bind<IProductCatalog>().FromInstance(shopConfig);
Container.Bind<IPurchaseFulfillment>().To<ShopFulfillment>().AsSingle();
HephaestusPurchasingInstaller.Install(Container);
```

The store connects at startup (`NonLazy`) so orders left unconfirmed last session are granted even if the shop is
never opened.

### 4. Sell

```csharp
[Inject] private IStoreService _store;

foreach (var product in _store.Products)
    card.Show(product.Title, product.PriceText, product.Available && !_store.IsOwned(product.Id));

_store.Buy(product.Id);                          // PurchaseSucceeded / PurchaseFailed
_store.RestorePurchases((ok, error) => { });    // Apple requires a Restore button with non-consumables
```

Listen to `ProductsChanged` (prices arrive, connection lost), `PurchaseSucceeded`, `PurchaseFailed` (not raised when
the player cancels) and `OwnershipChanged`.

## Notes

- Receipts are not validated. Apple StoreKit 2 verifies transactions locally; Google Play validation needs the
  obfuscated tangle (Services > In-App Purchasing > Receipt Validation Obfuscator).
- Deferred purchases (Ask to Buy, Google pending payments) are granted once they turn into a paid order.
- Subscriptions are not supported yet.

## Tests

Edit mode tests cover the purchase rules. Add the package to `testables` in the project's `Packages/manifest.json`
to see them in the Test Runner.
