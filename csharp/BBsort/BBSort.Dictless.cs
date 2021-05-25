using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Flexols.Data.Collections;

namespace BBsort.DictLess
{
    public class BBSort<T>
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

            var st = new Stack<MinMaxHeap<T>>();

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

        void getBuckets(ref T min, ref T max, MinMaxHeap<T> items, PoolList<MinMaxHeap<T>> buckets, int count)
        {
            var minLog = m_getLog(min);
            var maxLog = m_getLog(max);

            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

            var cnt = items.Count;

            for(int i = 0; i < cnt; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(items.At(i)) + b));
                index = Math.Min(count - 1, index);
                var bucket = buckets[index];

                if (bucket == null)
                {
                    const int maxCapacity = int.MaxValue;
                    const int arraySize = 2;
                    buckets[index] = bucket = new MinMaxHeap<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(items.At(i));
            }
        }

        int case1(Stack<MinMaxHeap<T>> st,
            MinMaxHeap<T> top,
            T[] output,
            int index)
        {
            output[index] = top.At(0);

            return 1;
        }

        int caseAllDuplicates(MinMaxHeap<T> top,
            T[] output,
            int index)
        {
            T val = top.At(0);

            var topCount = top.Count;
            
            for (int i = 0; i < topCount && i < output.Length; ++i)
            {
                output[index + i] = val;
            }

            return topCount;
        }

        int case2(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      T[] output,
                      int index)
        {
            output[index] = top.At(1);
            output[index + 1] = top.At(0);

            return 2;
        }

        int case3(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      T[] output,
                      int index)
        {
            //single comparison
            var (maxIndex, midIndex, minIndex) = top.GetMaxMidMin();

            output[index] = top.At(minIndex);
            output[index + 1] = top.At(midIndex);
            output[index + 2] = top.At(maxIndex);

            return 3;
        }

        int caseN(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      T[] output,
                      int index)
        {
            if (top.AllDuplicates)
            {
                return caseAllDuplicates(top, output, index);
            }

            var count = (top.Count / 2) + 1;

            var newBuckets = new PoolList<MinMaxHeap<T>>(count, count, count);

            getBuckets(ref top.FindMin(), ref top.FindMax(), top, newBuckets, count);

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

        void bbSortToStream(Stack<MinMaxHeap<T>> st, T[] output, int count)
        {
            var caseFunc = new Func<Stack<MinMaxHeap<T>>, MinMaxHeap<T>,  T[], int, int>[]
            {
                case1, case2, case3, caseN
            };

            int index = 0;

            while (st.Count > 0)
            {
                var top = st.Pop();
                
                var caseIndex = Math.Min(top.Count - 1, 3);

                index += caseFunc[caseIndex].Invoke(st, top, output, index);
            }
        }

        void getTopStackBuckets(T[] array,
                                Stack<MinMaxHeap<T>> st)
        {
            T min = array[0];
            T max = array[0];
            
            for (var index = 1; index < array.Length; index++)
            {
                if (Comparer<T>.Default.Compare(min, array[index]) > 0)
                {
                    min = array[index];
                }
                
                if (Comparer<T>.Default.Compare( array[index], max) > 0)
                {
                    max = array[index];
                }
            }

            if (Comparer<T>.Default.Compare(min, max) == 0)
            {
                return;
            }

            var bucketCount = Math.Min(array.Length, 128);
            
            var buckets = new PoolList<MinMaxHeap<T>>(bucketCount, bucketCount, bucketCount);

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
                    buckets[index] = bucket = new MinMaxHeap<T>(new PoolList<T>(maxCapacity, arraySize));
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
