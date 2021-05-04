using System;
using System.Collections.Generic;
using Flexols.Data.Collections;


namespace BBsort
{
    public class BBSort<T>
    {
        public static float getLog(float x)
        {

            var abs = Math.Abs(x);
            if (abs < 2)
            {
                return x;
            }
            var lg = Math.Log2(abs);
            return (float)(x < 0 ? -lg : lg);
        }

        Func<T, float> m_getLog;
        public BBSort(Func<T, float> getLog)
        {
            m_getLog = getLog;
        }

        public void Sort(HybridList<T> array)
        {

            if (array.Count <= 1)
            {
                return;
            }

            var st = new Stack<MinMaxHeap<T>>();
            var countMap = new HybridDict<T, int>();

            getTopStackBuckets(array, st, countMap);

            bbSortToStream(st, array, array.Count, countMap);
        }

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

        void getBuckets(T min, T max, IEnumerable<T> iterable, HybridList<MinMaxHeap<T>> buckets, int count)
        {
            var minLog = m_getLog(min);
            var maxLog = m_getLog(max);

            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

            foreach (var item in iterable)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(item) + b));
                index = Math.Min(count - 1, index);
                var bucket = buckets[index];

                if (bucket == null)
                {
                    buckets[index] = bucket = new MinMaxHeap<T>(new HybridList<T>());
                }
                bucket.Add(item);
            }
        }

        void fillStream(T val, HybridList<T> output, int index, int count)
        {

            for (int i = 0; i < count; ++i)
            {

                int newIndex = index + i;
                if (newIndex >= output.Count)
                {
                    break;
                }
                output[newIndex] = val;
            }
        }

        int case1(Stack<MinMaxHeap<T>> st,
                  MinMaxHeap<T> top,
                  HybridList<T> output,
                  HybridDict<T, int> countMap,
                  int index)
        {
            var val = top.At(0);

            fillStream(val, output, index, countMap[val]);

            return top.Count;
        }

        int case2(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      HybridList<T> output,
                      HybridDict<T, int> countMap,
                      int index)
        {
            var val0 = top.At(1);
            var val1 = top.At(0);

            var count0 = countMap[val0];
            var count1 = countMap[val1];

            fillStream(val0, output, index, count0);
            fillStream(val1, output, index + count0, count1);

            return count0 + count1;
        }

        int case3(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      HybridList<T> output,
                      HybridDict<T, int> countMap,
                      int index)
        {

            //single comparison
            var (maxIndex, midIndex, minIndex) = top.GetMaxMidMin();

            var min = top.At(minIndex);
            var mid = top.At(midIndex);
            var max = top.At(maxIndex);

            var count1 = countMap[min];
            var count2 = countMap[mid];
            var count3 = countMap[max];

            fillStream(min, output, index, count1);
            fillStream(mid, output, index + count1, count2);
            fillStream(max, output, index + count1 + count2, count3);

            var count = count1 + count2 + count3;

            return count;
        }

        int caseN(Stack<MinMaxHeap<T>> st,
                      MinMaxHeap<T> top,
                      HybridList<T> output,
                      HybridDict<T, int> countMap,
                      int index)
        {

            var count = (top.Count / 2) + 1;

            var newBuckets = new HybridList<MinMaxHeap<T>>(count);

            newBuckets.Ensure(count);

            getBuckets(top.FindMin(), top.FindMax(), top, newBuckets, count);

            for (int i = newBuckets.Count - 1; i >= 0; --i)
            {
                if (newBuckets[i] != null)
                {
                    st.Push(newBuckets[i]);
                }
            }
            return 0;
        }

        void bbSortToStream(Stack<MinMaxHeap<T>> st, HybridList<T> output, int count, HybridDict<T, int> countMap)
        {
            var caseFunc = new Func<Stack<MinMaxHeap<T>>, MinMaxHeap<T>, HybridList<T>, HybridDict<T, int>, int, int>[] { case1, case2, case3, caseN };

            int index = 0;

            while (st.Count > 0 && index < count)
            {
                var top = st.Pop();
                var caseIndex = Math.Min(top.Count - 1, 3);

                index += caseFunc[caseIndex].Invoke(st, top, output, countMap, index);
            }
        }

        void getTopStackBuckets(HybridList<T> array,
                                Stack<MinMaxHeap<T>> st,
                                HybridDict<T, int> countMap)
        {

            MinMaxHeap<T> distinctItems = new MinMaxHeap<T>(new HybridList<T>());

            foreach (var item in array)
            {
                if (countMap.ContainsKey(item))
                {
                    countMap[item] += 1;
                }
                else
                {
                    distinctItems.Add(item);
                    countMap[item] = 1;
                }
            }

            caseN(st, distinctItems, array, countMap, 0);          
        }
    }
}
