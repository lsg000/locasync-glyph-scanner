using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Flattens a font and its fallbacks into the order TextMeshPro searches them: the primary font, its fallback
    /// list depth-first, then the global fallbacks (each depth-first). Each font appears once; cycles are safe.
    /// </summary>
    public static class FontChainResolver
    {
        public static List<T> Resolve<T>(T primary, Func<T, IEnumerable<T>> getFallbacks, IEnumerable<T> globalFallbacks = null)
            where T : class
        {
            var result = new List<T>();
            var visited = new HashSet<T>(ReferenceComparer<T>.Instance);

            Visit(primary, getFallbacks, result, visited);
            if (globalFallbacks != null)
            {
                foreach (T font in globalFallbacks) Visit(font, getFallbacks, result, visited);
            }
            return result;
        }

        static void Visit<T>(T font, Func<T, IEnumerable<T>> getFallbacks, List<T> result, HashSet<T> visited)
            where T : class
        {
            if (font == null || !visited.Add(font)) return;
            result.Add(font);

            IEnumerable<T> fallbacks = getFallbacks(font);
            if (fallbacks == null) return;
            foreach (T fallback in fallbacks) Visit(fallback, getFallbacks, result, visited);
        }

        sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
