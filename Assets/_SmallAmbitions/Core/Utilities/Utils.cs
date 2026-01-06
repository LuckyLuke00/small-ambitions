using System.Collections.Generic;

namespace SmallAmbitions
{
    public static class Utils
    {
        public static bool IsValidIndex<T>(this IReadOnlyList<T> list, int index)
        {
            return list != null && (uint)index < (uint)list.Count;
        }

        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static T GetRandomElement<T>(this IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default;
            }
            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            return list[randomIndex];
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
