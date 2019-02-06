using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class CollectionsUtils
    {
        /// <summary>
        /// Useful when you don't need unique empty collection instance per usage - object graph optimization
        /// </summary>
        /// <typeparam name="T">Type of collection</typeparam>
        /// <param name="list">Existing collection or null</param>
        /// <returns>When <paramref name="list"/> is null - empty collection, otherwise - <paramref name="list"/></returns>
        public static IList<T> EmptyCollectionIfNull<T>(this IList<T> list)
        {
            return list ?? CollectionsUtils<T>.EmptyCollection;
        }

        public static int IndexOf<T>(this IEnumerable<T> seq, Func<T, bool> cond)
        {
            int index = 0;
            foreach (var item in seq)
            {
                if (cond(item))
                    return index;

                index++;
            }

            return -1;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> collection, T newItem)
        {
            yield return newItem;

            foreach (var item in collection)
                yield return item;
        }
    }

    public static class CollectionsUtils<T>
    {
        public static readonly T[] EmptyCollection = new T[0];
    }
}
