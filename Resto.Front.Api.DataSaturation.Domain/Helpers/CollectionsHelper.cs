using Resto.Front.Api.DataSaturation.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Domain.Helpers
{
    public static class CollectionsHelper
    {
        public static bool IsEqualsLists<T>(List<T> firstCollection, List<T> secondCollection) where T : IEqualsObject
        {
            if (firstCollection is null && secondCollection != null)
                return false;

            if (firstCollection != null && secondCollection is null)
                return false;

            if (firstCollection is null && secondCollection is null)
                return true;

            if (firstCollection.Count != secondCollection.Count)
                return false;

            Dictionary<Guid, T> currentModifiers = new Dictionary<Guid, T>();
            foreach (var product in firstCollection)
            {
                if (currentModifiers.ContainsKey(product.id))
                    continue;

                currentModifiers.Add(product.id, product);
            }

            foreach (var product in secondCollection)
            {
                if (!currentModifiers.TryGetValue(product.id, out T orderModifierItem))
                    return false;

                if (!orderModifierItem.Equals(product))
                    return false;
            }
            return true;
        }
    }
}
