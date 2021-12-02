using System;
using System.Collections.Generic;
using System.Linq;

namespace Flexols.Data.Collections
{
    public static class HybridListExtensions
    {
        public static void MergeAscSorted<T>(this HybridList<T> result, HybridList<T> l1, HybridList<T> l2, Func<T, T, int> comparer)
        {
            var list1 = l1;
            var list2 = l2;
            
            var n1 = list1.Count;
            var n2 = list2.Count;

            if (n2 > n1)
            {
                _.Swap(ref list1, ref list2);
                _.Swap(ref n1, ref n2);
            }
            
            result.Ensure(n1 + n2);

            if (result.m_root is HybridList<T>.StoreNode r)
            {
                if (list1.m_root is HybridList<T>.StoreNode s1 && list2.m_root is HybridList<T>.StoreNode s2)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(s1.m_items[i], s2.m_items[j]) < 0)
                        {
                            r.m_items[k++] = s1.m_items[i++];
                        }
                        else
                        {
                            r.m_items[k++] = s2.m_items[j++];
                        }
                    }

                    while (i < n1) r.m_items[k++] = s1.m_items[i++];
                    while (j < n2) r.m_items[k++] = s2.m_items[j++];
                }
                else if (list1.m_root is HybridList<T>.StoreNode ss1)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(ss1.m_items[i], list2[j]) < 0)
                        {
                            r.m_items[k++] = ss1.m_items[i++];
                        }
                        else
                        {
                            r.m_items[k++] = list2[j++];
                        }
                    }

                    while (i < n1) r.m_items[k++] = ss1.m_items[i++];
                    while (j < n2) r.m_items[k++] = list2[j++];
                }
                else
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(list1[i], list2[j]) < 0)
                        {
                            r.m_items[k++] = list1[i++];
                        }
                        else
                        {
                            r.m_items[k++] = list2[j++];
                        }
                    }

                    while (i < n1) r.m_items[k++] = list1[i++];
                    while (j < n2) r.m_items[k++] = list2[j++];
                }
            }
            else
            {
                if (list1.m_root is HybridList<T>.StoreNode s1 && list2.m_root is HybridList<T>.StoreNode s2)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(s1.m_items[i], s2.m_items[j]) < 0)
                        {
                            result[k++] = s1.m_items[i++];
                        }
                        else
                        {
                            result[k++] = s2.m_items[j++];
                        }
                    }

                    while (i < n1) result[k++] = s1.m_items[i++];
                    while (j < n2) result[k++] = s2.m_items[j++];
                }
                else if (list1.m_root is HybridList<T>.StoreNode ss1)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(ss1.m_items[i], list2[j]) < 0)
                        {
                            result[k++] = ss1.m_items[i++];
                        }
                        else
                        {
                            result[k++] = list2[j++];
                        }
                    }

                    while (i < n1) result[k++] = ss1.m_items[i++];
                    while (j < n2) result[k++] = list2[j++];
                }
                else
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(list1[i], list2[j]) < 0)
                        {
                            result[k++] = list1[i++];
                        }
                        else
                        {
                            result[k++] = list2[j++];
                        }
                    }

                    while (i < n1) result[k++] = list1[i++];
                    while (j < n2) result[k++] = list2[j++];
                }
            }
        }

        public static HybridList<T> ToHybridList<T>(this IList<T> source)
        {
            return new HybridList<T>(source);
        }

        public static HybridList<T> ToHybridList<T>(this IEnumerable<T> source)
        {
            return new HybridList<T>(source);
        }

        public static HybridList<T> ToHybridList<T>(this HybridList<T> source)
        {
            return new HybridList<T>(source);
        }

        public static void DisposeList<T>(this HybridList<T> list)
        {
            if (list != null)
            {
                if (list is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    list.Clear();
                }
            }
        }
        

        public static void DisposeBitArray(this HybridBitArray array)
        {
            if (array != null)
            {
                if (array is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    array.Clear();
                }
            }
        }

        public static void DisposeHashset<T>(this HybridHashSet<T> array)
        {
            if (array != null)
            {
                if (array is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    array.Clear();
                }
            }
        }

        public static bool ListNullOrItemAbsent<T>(this IReadOnlyCollection<T> source, T value)
        {
            return source == null || !source.Contains(value);
        }

        public static HybridHashSet<T> ToHybridHashSet<T>(this IReadOnlyCollection<T> source)
        {
            if (source is HybridHashSet<T> hs)
            {
                return new HybridHashSet<T>(hs);
            }
            
            return new HybridHashSet<T>(source);
        }
        
        public static HybridHashSet<T> ToHybridHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HybridHashSet<T>(source, comparer);
        }
        
        public static int BinarySearchExact<T, V>(this IReadOnlyList<T> array, V target, int startIndex, int count, Func<T, V, int> compare)
        {
            int lo = startIndex;
            int hi = count - 1;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = compare(array[index], target);

                if (comp == 0)
                {
                    return index;
                }

                if (comp > 0)
                {
                    lo = index + 1;
                }
                else
                {
                    hi = index - 1;
                }
            }
            return ~lo;
        }

        public static int BinarySearchLeft<T, V>(this IReadOnlyList<T> array, V target, Func<T, V, int> compare)
        {
            return BinarySearchLeft(array, target, 0, array.Count, compare);
        }

        public static int BinarySearchLeft<T, V>(this IReadOnlyList<T> array, V target, int startIndex, int count, Func<T, V, int> compare)
        {
            int lo = startIndex;
            int hi = count - 1;
            int res = -1;
            
            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = compare(array[index], target);

                if (comp > 0)
                {
                    hi = index - 1;
                }
                else if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    res = index;
                    hi = index - 1;
                }
            }

            return res;
        } 
        
        public static int BinarySearchRight<T, V>(this IReadOnlyList<T> array, V target, Func<T, V, int> compare)
        {
            return BinarySearchRight(array, target, 0, array.Count, compare);
        }
        
        public static int BinarySearchRight<T, V>(this IReadOnlyList<T> array, V target, int startIndex, int count, Func<T, V, int> compare)
        {
            int lo = startIndex;
            int hi = count - 1;
            int res = -1;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = compare(array[index], target);

                if (comp > 0)
                {
                    hi = index - 1;
                }
                else if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    res = index;
                    lo = index + 1;
                }
            }

            return res;
        } 
    }
}