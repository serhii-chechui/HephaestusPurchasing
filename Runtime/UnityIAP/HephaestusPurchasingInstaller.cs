using Zenject;

namespace WTFGames.Hephaestus.PurchasingSystem.UnityIAP
{
    /// <summary>
    /// Binds <see cref="IStoreService"/> to Unity IAP. The game binds its own <see cref="IProductCatalog"/>
    /// and <see cref="IPurchaseFulfillment"/> in the same container.
    /// </summary>
    public class HephaestusPurchasingInstaller : Installer<HephaestusPurchasingInstaller>
    {
        public override void InstallBindings()
        {
            // NonLazy: connect at startup so orders left unconfirmed last session are granted.
            Container.BindInterfacesTo<UnityIapStoreService>().AsSingle().NonLazy();
        }
    }
}
