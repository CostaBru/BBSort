using System;
using System.Collections.Generic;

namespace Flexols.Data.Collections
{
    public static class Extentions
    {

        public static bool Any<T, V>(this IEnumerable<T> enumerable, V value, Func<T, V> comparer, int start = 0)
        {
            return Any<T, V>(enumerable, value, comparer, start);
        }

        public static bool Any<T, V>(this IEnumerable<T> enumerable, V value, Func<T, V, bool> comparer, int start = 0)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return FindIndex(enumerable, value, comparer, start) >= 0;
        }
      

        public static int FindIndex<T, V>(this IEnumerable<T> collection, V value, Func<T, V, bool> comparer, int start = 0)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            int i = 0;

            foreach (var item in collection)
            {
                if (i < start)
                {
                    continue;
                }

                if (comparer(item, value))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
      

        public static IEnumerable<T> Where<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Where<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Where<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2, value3))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Where<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2, value3, value4))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Where<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2, value3, value4, value5))
                {
                    yield return item;
                }
            }
        }

        public static T FirstOrDefault<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1>(collection, compare, value));

        public static T FirstOrDefault<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2>(collection, compare, value1, value2));

        public static T FirstOrDefault<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        public static T FirstOrDefault<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        public static T FirstOrDefault<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));


        public static T First<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => System.Linq.Enumerable.First(Where<T, V1>(collection, compare, value));

        public static T First<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => System.Linq.Enumerable.First(Where<T, V1, V2>(collection, compare, value1, value2));

        public static T First<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => System.Linq.Enumerable.First(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        public static T First<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => System.Linq.Enumerable.First(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        public static T First<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => System.Linq.Enumerable.First(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));

        public static bool Any<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => System.Linq.Enumerable.Any(Where<T, V1>(collection, compare, value));

        public static bool Any<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => System.Linq.Enumerable.Any(Where<T, V1, V2>(collection, compare, value1, value2));

        public static bool Any<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => System.Linq.Enumerable.Any(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        public static bool Any<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        public static bool Any<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));

        public static bool IsEmpty<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => !System.Linq.Enumerable.Any(Where<T, V1>(collection, compare, value));

        public static bool IsEmpty<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => !System.Linq.Enumerable.Any(Where<T, V1, V2>(collection, compare, value1, value2));

        public static bool IsEmpty<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => !System.Linq.Enumerable.Any(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        public static bool IsEmpty<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => !System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        public static bool IsEmpty<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => !System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));


        public static IEnumerable<T> Where<T, V>(this IEnumerable<T> collection, V value, Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int start = 0)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            int i = 0;

            foreach (var item in collection)
            {
                if (i < start)
                {
                    continue;
                }

                var selector = valueSelector(item);

                if (equalityComparer.Equals(selector, value))
                {
                    yield return item;
                }

                i++;
            }
        }

        public static int FindIndex<T, V>(this IReadOnlyList<T> collection, V value, Func<T, V, bool> equalityComparer, int start = 0)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            int i = 0;

            var collectionCount = collection.Count;

            if (start >= collectionCount || start < 0)
            {
                return -1;
            }

            for (var index = start; index < collectionCount; index++)
            {
                var item = collection[index];

                if (equalityComparer(item, value))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
    }
}
