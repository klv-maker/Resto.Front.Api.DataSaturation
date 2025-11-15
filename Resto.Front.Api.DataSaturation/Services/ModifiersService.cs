using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class ModifiersService : IModifiersService
    {
        private static IModifiersService instance;
        private readonly ConcurrentDictionary<Guid, ProductGroupModifiersInfo> groupModifiersInfoDictionary = new ConcurrentDictionary<Guid, ProductGroupModifiersInfo>();
        private static readonly object lockerCreateInstance = new object();
        private readonly object lockerDictionary = new object();
        private bool isDisposed = false;
        private ModifiersService() { }

        public static IModifiersService Instance
        {
            get
            {
                if (instance is null)
                {
                    lock (lockerCreateInstance)
                    {
                        if (instance is null)
                            instance = new ModifiersService();
                    }
                }
                return instance;
            }
        }

        public IChildModifier GetGroupModifierInfo(IProduct product, Guid modifierId)
        {
            if (isDisposed)
                return null;

            if (product is null)
                return null;

            ProductGroupModifiersInfo productGroupModifiersInfo = GetProductGroupModifiersInfo(product);
            return GetChildModifier(productGroupModifiersInfo, modifierId);
        }

        public void UpdateProductModifierByPriceCategory(IProduct product)
        {
            if (isDisposed)
                return;

            var productGroupModifiersInfo = CreateProductModifierByProduct(product);
            if (productGroupModifiersInfo is null)
                return;

            lock (lockerDictionary)
            {
                groupModifiersInfoDictionary.AddOrUpdate(product.Id, productGroupModifiersInfo, (id, oldValue) => productGroupModifiersInfo);
            }
        }

        private ProductGroupModifiersInfo GetProductGroupModifiersInfo(IProduct product)
        {
            if (isDisposed)
                return null;

            ProductGroupModifiersInfo productGroupModifiersInfo = null;
            if (groupModifiersInfoDictionary.TryGetValue(product.Id, out productGroupModifiersInfo))
                return productGroupModifiersInfo;

            lock (lockerDictionary)
            {
                if (!groupModifiersInfoDictionary.TryGetValue(product.Id, out productGroupModifiersInfo))
                    productGroupModifiersInfo = CreateProductModifierByProduct(product);

                if (productGroupModifiersInfo != null)
                    groupModifiersInfoDictionary.AddOrUpdate(product.Id, productGroupModifiersInfo, (id, oldValue) => productGroupModifiersInfo);

                return productGroupModifiersInfo;
            }
        }

        private IChildModifier GetChildModifier(ProductGroupModifiersInfo productGroupInfo, Guid modifierId)
        {
            if (isDisposed)
                return null;

            if (productGroupInfo is null)
                return null;

            foreach (var modifierGroup in productGroupInfo.GroupModifiers)
            {
                var items = modifierGroup.Value.Items;
                if (items is null || items.Count == 0)
                    continue;
                
                var itemsLikeProductId = items.Where(modif => modif.Product.Id == modifierId).Take(2).ToList();
                var countItems = itemsLikeProductId.Count();
                if (countItems == 0)
                    continue;

                if (countItems > 1)
                    PluginContext.Log.Error($"[{nameof(ModifiersService)}|{nameof(GetChildModifier)}] Found many modifiers by price category for modifier {modifierId}");

                return itemsLikeProductId.FirstOrDefault();
            }
            return null;
        }

        private ProductGroupModifiersInfo CreateProductModifierByProduct(IProduct product)
        {
            if (isDisposed)
                return null;

            var priceCategories = PluginContext.Operations.GetPriceCategories();
            if (priceCategories is null)
                return null;

            ProductGroupModifiersInfo productGroupModifiersInfo = new ProductGroupModifiersInfo(product);
            var productGroupInfos = product.GetGroupModifiers(null);
            if (productGroupInfos is null)
                return null;

            ConcurrentDictionary<Guid, IGroupModifier> childGroups = new ConcurrentDictionary<Guid, IGroupModifier>();
            foreach (var group in productGroupInfos)
            {
                if (group is null || group.ProductGroup == null)
                    continue;

                if (!childGroups.TryAdd(group.ProductGroup.Id, group))
                    PluginContext.Log.Error($"[{nameof(ModifiersService)}|{nameof(CreateProductModifierByProduct)}] Failed to add group with id {group.ProductGroup.Id} to group dictionary for product {product.Id}");
            }
            productGroupModifiersInfo.GroupModifiers = childGroups;
            return productGroupModifiersInfo;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            groupModifiersInfoDictionary.Clear();
        }
    }
}
