using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.PurchasingSystem
{
    /// <summary>A ready-made <see cref="IProductCatalog"/> for games that keep their products in an asset.</summary>
    [CreateAssetMenu(menuName = "Hephaestus/Purchasing/Product Catalog", fileName = "ProductCatalog")]
    public class ProductCatalog : ScriptableObject, IProductCatalog
    {
        [SerializeField] private ProductCatalogEntry[] products = new ProductCatalogEntry[0];

        public IEnumerable<ProductEntry> Entries
        {
            get
            {
                foreach (var p in products)
                {
                    if (p != null && !string.IsNullOrEmpty(p.productId))
                        yield return new ProductEntry(p.productId, p.type, p.fallbackTitle);
                }
            }
        }
    }
}
