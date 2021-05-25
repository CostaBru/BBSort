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

        void getBuckets(ref T min, ref T max, MinMaxHeap<T> iterable, PoolList<MinMaxHeap<T>> buckets, int count)
        {
            var minLog = m_getLog(min);
            var maxLog = m_getLog(max);

            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

            var cnt = iterable.Count;

            for(int i = 0; i < cnt; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(iterable.At(i)) + b));
                index = Math.Min(count - 1, index);
                var bucket = buckets[index];

                if (bucket == null)
                {
                    const int maxCapacity = int.MaxValue;
                    const int arraySize = 2;
                    buckets[index] = bucket = new MinMaxHeap<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(iterable.At(i));
            }
        }

        int case1(Stack<MinMaxHeap<T>> st,
                  MinMaxHeap<T> top,
                  T[] output,
                  int index)
        {
            if (index < output.Length)
            {
                output[index] = top.At(0);
            }

            return 1;
        }
        
        int caseAllDuplicates(MinMaxHeap<T> top,
            T[] output,
            int index)
        {
            T val = top.At(0);
            
            for (int i = 0; i < top.Count && i < output.Length; ++i)
            {
                int newIndex = index + i;
                if (newIndex >= output.Length)
                {
                    break;
                }
                output[newIndex] = val;
            }

            return top.Count;
        }

        int case2(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      T[] output,
                      int index)
        {
            if (index < output.Length)
            {
                output[index] = top.At(1);
            }
            
            if (index + 1 < output.Length)
            {
                output[index + 1] = top.At(0);
            }

            return 2;
        }

        int case3(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      T[] output,
                      int index)
        {
            //single comparison
            var (maxIndex, midIndex, minIndex) = top.GetMaxMidMin();

            if (index < output.Length)
            {
                output[index] = top.At(minIndex);
            }
            
            if (index + 1 < output.Length)
            {
                output[index + 1] = top.At(midIndex);
            }
            
            if (index + 2 < output.Length)
            {
                output[index + 2] = top.At(maxIndex);
            }

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
            var caseFunc = new Func<Stack<MinMaxHeap<T>>, MinMaxHeap<T>,  T[], int, int>[] { case1, case2, case3, caseN };

            int index = 0;

            while (st.Count > 0 && index < count)
            {
                var top = st.Pop();
                
                var caseIndex = Math.Min(top.Count - 1, 3);

                index += caseFunc[caseIndex].Invoke(st, top, output, index);
            }
        }

        void getTopStackBuckets(T[] array,
                                Stack<MinMaxHeap<T>> st)
        {

            MinMaxHeap<T> items = new MinMaxHeap<T>(new PoolList<T>(array.Length, 4));

            for (var index = 0; index < array.Length; index++)
            {
                items.Add(array[index]);
            }

            caseN(st, items, array, 0);          
        }
    }
}
