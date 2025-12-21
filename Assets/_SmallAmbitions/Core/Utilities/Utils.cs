using System.Collections.Generic;

namespace SmallAmbitions
{
    public static class Utils
    {
        public static bool IsValidIndex<T>(this ICollection<T> collection, int index)
        {
            return collection != null && (uint)index < (uint)collection.Count;
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static T GetRandomElement<T>(this IList<T> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return default;
            }
            int randomIndex = UnityEngine.Random.Range(0, collection.Count);
            return collection[randomIndex];
        }

        public static T GetRandomElement<T>(this ICollection<T> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return default;
            }

            int randomIndex = UnityEngine.Random.Range(0, collection.Count);
            int currentIndex = 0;

            foreach (var item in collection)
            {
                if (currentIndex == randomIndex)
                {
                    return item;
                }
                ++currentIndex;
            }

            return default;
        }
    }
}
