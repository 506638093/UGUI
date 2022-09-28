using System.Collections.Generic;

namespace UnityEngine.UI
{
    internal static class DictionaryPool<T, Value>
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool<Dictionary<T, Value>> s_ListPool = new ObjectPool<Dictionary<T, Value>>(null, Clear);
        static void Clear(Dictionary<T, Value> l) { l.Clear(); }

        public static Dictionary<T, Value> Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(Dictionary<T, Value> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}
