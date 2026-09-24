using System.Collections.Generic;
using NUnit.Framework;

namespace WTFGames.Hephaestus.PurchasingSystem.Tests
{
    public class PurchaseProcessorTests
    {
        private FakeFulfillment _fulfillment;
        private PurchaseProcessor _processor;
        private List<string> _ownershipEvents;

        [SetUp]
        public void SetUp()
        {
            _fulfillment = new FakeFulfillment();
            _processor = new PurchaseProcessor(new FakeCatalog(), _fulfillment);
            _ownershipEvents = new List<string>();
            _processor.OwnershipChanged += id => _ownershipEvents.Add(id);
        }

        private static OrderLine[] Line(string productId, int quantity = 1) => new[] { new OrderLine(productId, quantity) };

        [Test]
        public void PendingConsumable_IsGrantedWithQuantityAndTransaction()
        {
            var outcome = _processor.ProcessPending("t1", Line(FakeCatalog.Coins, 2));

            Assert.AreEqual(PendingOrderOutcome.Granted, outcome);
            Assert.AreEqual(1, _fulfillment.Grants.Count);
            var grant = _fulfillment.Grants[0];
            Assert.AreEqual(FakeCatalog.Coins, grant.ProductId);
            Assert.AreEqual(StoreProductType.Consumable, grant.Type);
            Assert.AreEqual(2, grant.Quantity);
            Assert.AreEqual("t1", grant.TransactionId);
            Assert.IsFalse(grant.IsRestore);
            Assert.IsFalse(_processor.IsOwned(FakeCatalog.Coins));
        }

        [Test]
        public void ZeroQuantity_CountsAsOne()
        {
            _processor.ProcessPending("t1", Line(FakeCatalog.Coins, 0));
            Assert.AreEqual(1, _fulfillment.Grants[0].Quantity);
        }

        [Test]
        public void RedeliveredOrder_IsAlreadyGranted()
        {
            _processor.ProcessPending("t1", Line(FakeCatalog.Coins));
            var outcome = _processor.ProcessPending("t1", Line(FakeCatalog.Coins));

            Assert.AreEqual(PendingOrderOutcome.AlreadyGranted, outcome);
        }

        [Test]
        public void ProductMissingFromCatalog_StaysPendingAndGrantsNothing()
        {
            var lines = new[] { new OrderLine(FakeCatalog.Coins, 1), new OrderLine("from_a_newer_build", 1) };
            var outcome = _processor.ProcessPending("t1", lines);

            Assert.AreEqual(PendingOrderOutcome.LeftPending, outcome);
            Assert.IsEmpty(_fulfillment.Grants);
        }

        [Test]
        public void FulfillmentUnknown_StaysPending()
        {
            _fulfillment.ForcedResult = FulfillmentResult.Unknown;
            Assert.AreEqual(PendingOrderOutcome.LeftPending, _processor.ProcessPending("t1", Line(FakeCatalog.NoAds)));
            Assert.IsFalse(_processor.IsOwned(FakeCatalog.NoAds));
        }

        [Test]
        public void EmptyOrder_StaysPending()
        {
            Assert.AreEqual(PendingOrderOutcome.LeftPending, _processor.ProcessPending("t1", new OrderLine[0]));
        }

        [Test]
        public void PendingNonConsumable_BecomesOwnedOnce()
        {
            _processor.ProcessPending("t1", Line(FakeCatalog.NoAds));
            _processor.ProcessPending("t1", Line(FakeCatalog.NoAds));

            Assert.IsTrue(_processor.IsOwned(FakeCatalog.NoAds));
            CollectionAssert.AreEqual(new[] { FakeCatalog.NoAds }, _ownershipEvents);
        }

        [Test]
        public void OwnedNonConsumable_IsGrantedAsRestore()
        {
            _processor.ProcessOwned("t9", Line(FakeCatalog.NoAds));

            Assert.AreEqual(1, _fulfillment.Grants.Count);
            Assert.IsTrue(_fulfillment.Grants[0].IsRestore);
            Assert.AreEqual("t9", _fulfillment.Grants[0].TransactionId);
            Assert.IsTrue(_processor.IsOwned(FakeCatalog.NoAds));
            CollectionAssert.AreEqual(new[] { FakeCatalog.NoAds }, _ownershipEvents);
        }

        [Test]
        public void OwnedConsumableAndUnknownProducts_AreIgnored()
        {
            var lines = new[] { new OrderLine(FakeCatalog.Coins, 1), new OrderLine("unknown", 1) };
            _processor.ProcessOwned("t1", lines);

            Assert.IsEmpty(_fulfillment.Grants);
            Assert.IsEmpty(_ownershipEvents);
        }

        [Test]
        public void NullStore_FailsPurchasesAndRestore()
        {
            var store = new NullStoreService();
            PurchaseFailure? failure = null;
            store.PurchaseFailed += f => failure = f;
            bool? restored = null;

            store.Buy(FakeCatalog.Coins);
            store.RestorePurchases((ok, _) => restored = ok);

            Assert.IsFalse(store.IsReady);
            Assert.IsEmpty(store.Products);
            Assert.AreEqual(FakeCatalog.Coins, failure?.ProductId);
            Assert.AreEqual(false, restored);
        }
    }
}
