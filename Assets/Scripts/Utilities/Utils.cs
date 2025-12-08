using System.Collections.Generic;

namespace SmallAmbitions
{
    public static class Utils
    {
        public static bool IsValidIndex<T>(this ICollection<T> collection, int index)
        {
            return collection != null && index >= 0 && index < collection.Count;
        }
    }
}
