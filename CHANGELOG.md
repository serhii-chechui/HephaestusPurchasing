# Changelog

All notable changes to this project will be documented in this file. The format is based on [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-09-24

### feat
- `IStoreService` on Unity IAP 5 (`UnityIapStoreService`): connect at startup, localized products, buy, restore.
- Consumables and non-consumables; owned non-consumables are granted again at startup and on restore.
- Two-step flow through the game's `IPurchaseFulfillment`: grant, then confirm; unknown products stay pending.
- `IProductCatalog` with a ready-made `ProductCatalog` asset.
- Store-independent `PurchaseProcessor` and `NullStoreService`, with edit mode tests.
- Catalog titles replace the Fake Store's placeholder titles in the editor.

### docs
- README: store setup for App Store Connect and Google Play Console, test purchases in the editor, the iOS sandbox,
  StoreKit testing in Xcode and Google Play license testing, with a test checklist.
