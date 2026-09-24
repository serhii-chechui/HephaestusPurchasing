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

## Store setup

Product ids in the catalog must match the store ids exactly, and they are the same on both stores. Use a reverse-domain
scheme in lowercase: `com.studio.game.coins_small`. A deleted id can never be used again on either store.

### iOS: App Store Connect

1. **Agreements.** In **Business** (Agreements, Tax, and Banking), the **Paid Apps** agreement must be *Active*, with
   banking and tax forms filled in. Until then StoreKit returns no products, even in the sandbox.
2. **App record.** Create the app in **Apps** with the bundle id from **Player Settings → Other Settings → Bundle
   Identifier**. The App ID in Certificates, Identifiers & Profiles has the *In-App Purchase* capability on by default.
3. **Products.** In the app, **Monetization → In-App Purchases → +**:
   - **Type** — *Consumable* or *Non-Consumable*, as in the catalog's `StoreProductType`;
   - **Reference Name** — internal only;
   - **Product ID** — the catalog's id;
   - **Availability** and **Price Schedule**;
   - **App Store Localization** — the display name and description the store returns as `Title` and `Description`;
   - **Review Information** — a screenshot of the product in the shop and notes for the reviewer.

   The product reaches *Ready to Submit* when everything is filled in. Products stuck in *Missing Metadata* often do
   not load in the sandbox.
4. **Xcode.** After building the Xcode project from Unity, check that **Signing & Capabilities** lists *In-App
   Purchase*, and add it if it is missing.
5. **Review.** The first in-app purchases are submitted together with an app version: pick them in the version's
   **In-App Purchases and Subscriptions** section. The shop must have a **Restore Purchases** button when it sells
   non-consumables, or the build is rejected.

### Android: Google Play Console

1. **Payments profile.** In **Setup → Payments profile**, link a merchant account. Paid products cannot be created
   without one.
2. **App and signing.** Create the app with the package name from **Player Settings → Other Settings → Package
   Name**. Sign with your own keystore (**Publishing Settings → Custom Keystore**), not the debug one, and build an
   **AAB** (**Build Settings → Build App Bundle (Google Play)**).
3. **Target store.** In Unity, set the Android target store to Google Play: **Services → In-App Purchasing →
   Configure**.
4. **First upload.** The console only allows in-app products once a build containing Google Play Billing has been
   uploaded. Unity IAP adds the library and the `com.android.vending.BILLING` permission, so upload any build to
   **Testing → Internal testing** and roll the release out.
5. **Products.** In **Monetize with Play → Products → In-app products → Create product**: the catalog's id as
   **Product ID**, **Name**, **Description** and **Price**, then **Activate**.
   Google Play does not know consumable from non-consumable: Unity IAP consumes a consumable when the order is
   confirmed and only acknowledges a non-consumable, based on the catalog's type.

New or changed products can take a few hours to reach devices.

## Test purchases

No money is charged in any of the setups below.

### Editor

In the editor Unity IAP runs its **Fake Store**: every catalog product is available, prices are placeholders, and
purchases succeed at once. It checks the game's side (fulfillment, UI, events) but not the store setup.

### iOS

**Sandbox (a real device):**

1. In App Store Connect, **Users and Access → Sandbox → Test Accounts → +**. The email must not be an existing Apple
   Account; an address you own with a `+alias` is fine. Pick the storefront (country) you want to test prices in.
2. Build from Xcode to the device with development signing, or install through **TestFlight**.
3. Buy something in the game. iOS asks you to sign in: use the sandbox account. From then on it is under **Settings →
   Developer → Sandbox Apple Account** (**Settings → App Store → Sandbox Account** on older iOS). The purchase sheet
   says *[Environment: Sandbox]*.
4. To buy a non-consumable again, clear the history: **Users and Access → Sandbox → Test Accounts → Edit → Clear
   Purchase History**, or from the device's sandbox account settings.

TestFlight purchases also go through the sandbox, signed in with the tester's own Apple Account, and are free.

**StoreKit testing in Xcode (a simulator or a device, no App Store Connect needed):**

1. In the Unity-built Xcode project, **File → New → File → StoreKit Configuration File**, check *Sync this file with
   an app in App Store Connect*, or add the products by hand with the catalog's ids.
2. **Product → Scheme → Edit Scheme → Run → Options → StoreKit Configuration**: pick the file.
3. Run from Xcode. **Debug → StoreKit → Manage Transactions** lets you delete, refund and approve transactions.
   The **Editor** menu of the configuration file turns on *Ask to Buy*, interrupted purchases and purchase errors.

### Android

1. **License testers.** In the console, **Setup → License testing**: add the testers' Google accounts, with *License
   response* set to `RESPOND_NORMALLY`.
2. **Internal testing.** Add the same accounts to the **Internal testing** track's testers, open the opt-in link on the
   device and accept the invite.
3. **Install from Google Play** through the opt-in link. The tester's account must be the first account on the device
   (or the one Google Play uses). A build installed another way only works when its package name matches, it is
   signed with the same key, and its version code is already uploaded.
4. **Buy.** The payment sheet offers the test cards:
   - *Test card, always approves* — a normal purchase;
   - *Test card, always declines* — `PurchaseFailed`;
   - *Slow test card, approves after a few minutes* — a deferred order that turns into a paid one;
   - *Slow test card, declines after a few minutes* — a deferred order that never pays.
5. To buy a non-consumable again, refund it with *Revoke entitlement* in **Order management**. Test orders are
   cancelled automatically after a while.

### Checklist

| Scenario                                          | Expected                                                      |
|---------------------------------------------------|---------------------------------------------------------------|
| Buy a consumable                                  | Granted once, `PurchaseSucceeded`; can be bought again         |
| Buy a non-consumable                              | Granted, `OwnershipChanged`; the shop shows it as owned        |
| Cancel the purchase sheet                         | Nothing granted, `PurchaseFailed` is **not** raised            |
| Declined card (Android)                           | `PurchaseFailed`                                               |
| Kill the app right after paying                   | Granted on the next launch, exactly once                       |
| Reinstall, then **Restore Purchases**             | Non-consumables come back; consumables do not                  |
| Deferred payment (slow card, Ask to Buy)          | Nothing at first; granted once approved                        |
| No network                                        | `IsReady` is false; `Buy` raises `PurchaseFailed`              |

### When products do not load

- **iOS:** the Paid Apps agreement is not active; the bundle id differs; the product is in *Missing Metadata*; the
  build runs on a simulator without a StoreKit configuration file.
- **Android:** the product is not active; no build is uploaded to a track; the device account is not a tester or has
  not accepted the opt-in; the package name, signing key or version code differs from the uploaded build; the product
  was created less than a few hours ago.
- **Both:** the id in the catalog differs from the store's, and the product is missing from `Products`. Store errors
  are logged with the `[HephaestusPurchasing]` tag (`Product fetch failed`, `Store connection failed`).

## Notes

- Receipts are not validated. Apple StoreKit 2 verifies transactions locally; Google Play validation needs the
  obfuscated tangle (Services > In-App Purchasing > Receipt Validation Obfuscator).
- Deferred purchases (Ask to Buy, Google pending payments) are granted once they turn into a paid order.
- Subscriptions are not supported yet.

## Tests

Edit mode tests cover the purchase rules. Add the package to `testables` in the project's `Packages/manifest.json`
to see them in the Test Runner.
