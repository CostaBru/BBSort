using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BBsort.Collections;
using Flexols.Data.Collections;

namespace BBsort.DictLess.MinMaxList
{
    public class BBSort<T>  where T : struct, IComparable<T>
    {
        Func<T, float> m_getLog;

        public BBSort(Func<T, float> getLog)
        {
            m_getLog = getLog;
        }

        public void Sort(T[] array)
        {
            if (array.Length <= 1)
            {
                return;
            }

            var st = new Stack<MinMaxMidList<T>>();

            getTopStackBuckets(array, st);

            bbSortToStream(st, array, array.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        (float, float) GetLinearTransformParams(float x1, float x2, float y1, float y2)
        {
            float dx = x1 - x2;

            if (dx != 0.0)
            {
                float a = (y1 - y2) / dx;
                float b = y1 - (a * x1);

                return (a, b);
            }

            return (0.0f, 0.0f);
        }

        void getBuckets(ref T min, ref T max, MinMaxMidList<T> items, PoolList<MinMaxMidList<T>> buckets, int count)
        {
            var minLog = m_getLog(min);
            var maxLog = m_getLog(max);

            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

            var cnt = items.Storage.Count;

            for(int i = 0; i < cnt; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(items.Storage.ValueByRef(i)) + b));
                index = Math.Min(count - 1, index);
                var bucket = buckets[index];

                if (bucket == null)
                {
                    const int maxCapacity = int.MaxValue;
                    const int arraySize = 2;
                    buckets[index] = bucket = new MinMaxMidList<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(items.Storage.ValueByRef(i));
            }
        }

        int case1(Stack<MinMaxMidList<T>> st,
            MinMaxMidList<T> top,
            T[] output,
            int index)
        {
            output[index] = top.Min;

            return 1;
        }

        int caseAllDuplicates(MinMaxMidList<T> top,
            T[] output,
            int index)
        {
            var topCount = top.Storage.Count;
            
            for (int i = 0; i < topCount && i < output.Length; ++i)
            {
                output[index + i] = top.Min;
            }

            return topCount;
        }

        int case2(Stack<MinMaxMidList<T>> st,
            MinMaxMidList<T> top,
                      T[] output,
                      int index)
        {
            output[index]     = top.Min;
            output[index + 1] = top.Max;

            return 2;
        }

        int case3(Stack<MinMaxMidList<T>> st,
            MinMaxMidList<T> top,
                      T[] output,
                      int index)
        {
            output[index]     = top.Min;
            output[index + 1] = top.Mid;
            output[index + 2] = top.Max;

            return 3;
        }

        int caseN(Stack<MinMaxMidList<T>> st,
            MinMaxMidList<T> top,
                      T[] output,
                      int index)
        {
            if (top.Min.CompareTo(top.Max) == 0)
            {
                return caseAllDuplicates(top, output, index);
            }

            var count = (top.Storage.Count / 2) + 1;

            var newBuckets = new PoolList<MinMaxMidList<T>>(count, count, count);

            getBuckets(ref top.Min, ref top.Max, top, newBuckets, count);

            for (int i = newBuckets.Count - 1; i >= 0; --i)
            {
                var minMaxHeap = newBuckets[i];
                
                if (minMaxHeap != null)
                {
                    st.Push(minMaxHeap);
                }
            }
            return 0;
        }

        void bbSortToStream(Stack<MinMaxMidList<T>> st, T[] output, int count)
        {
            var caseFunc = new Func<Stack<MinMaxMidList<T>>, MinMaxMidList<T>,  T[], int, int>[]
            {
                case1, case2, case3, caseN
            };

            int index = 0;

            while (st.Count > 0)
            {
                var top = st.Pop();
                
                var caseIndex = Math.Min(top.Storage.Count - 1, 3);

                index += caseFunc[caseIndex].Invoke(st, top, output, index);
            }
        }

        void getTopStackBuckets(T[] array,
                                Stack<MinMaxMidList<T>> st)
        {
            T min = array[0];
            T max = array[0];
            
            for (var index = 1; index < array.Length; index++)
            {
                if (min.CompareTo(array[index])> 0)
                {
                    min = array[index];
                }
                
                if (array[index].CompareTo(max) > 0)
                {
                    max = array[index];
                }
            }

            if (min.CompareTo(max) == 0)
            {
                return;
            }

            var bucketCount = Math.Min(array.Length, 128);
            
            var buckets = new PoolList<MinMaxMidList<T>>(bucketCount, bucketCount, bucketCount);

            var minLog = m_getLog(min);
            var maxLog = m_getLog(max);

            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, bucketCount - 1);

            for(int i = 0; i < array.Length; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(array[i]) + b));
                index = Math.Min(bucketCount - 1, index);
                var bucket = buckets[index];

                if (bucket == null)
                {
                    const int maxCapacity = int.MaxValue;
                    const int arraySize = 2;
                    buckets[index] = bucket = new MinMaxMidList<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(array[i]);
            }

            for (int i = buckets.Count - 1; i >= 0; --i)
            {
                var minMaxHeap = buckets[i];
                
                if (minMaxHeap != null)
                {
                    st.Push(minMaxHeap);
                }
            }
        }
    }
}
