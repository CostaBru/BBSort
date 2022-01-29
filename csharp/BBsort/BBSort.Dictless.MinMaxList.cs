using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBsort.Collections;
using Flexols.Data.Collections;

namespace BBsort.DictLess.MinMaxList
{
    public static class LogHelper
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct ConverterStruct
        {
            [FieldOffset(0)] public int x;
            [FieldOffset(0)] public float val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float fastLog2(float val)
        {
            ConverterStruct u;
            u.x = 0; u.val = val;

            float lg2 = (float)(((u.x >> 23) & 255) - 128);
            u.x &= ~(255 << 23);
            u.x += 127 << 23;
            return lg2 + ((-0.3358287811f) * u.val + 2.0f) * u.val - 0.65871759316667f;
        }
    }

    public class BBSort<T>  where T : struct, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static float getLog(float x)
        {
            var abs = Math.Abs(x);
            if (abs < 2)
            {
                return x;
            }
            var lg = LogHelper.fastLog2(abs);
            return (float)(x < 0 ? -lg : lg);
        }
        
        public static float getLog(int x)
        {
            var abs = Math.Abs(x);
            if (abs < 2)
            {
                return x;
            }
            var lg = LogHelper.fastLog2(abs);
            return (float)(x < 0 ? -lg : lg);
        }
        
        Func<T, float> m_getLog;
        Func<T, double> m_getDouble;

        public BBSort(Func<T, float> getLog, Func<T, double> getDouble)
        {
            m_getLog = getLog;
            m_getDouble = getDouble;
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
        
        (double, double) GetLinearTransformParams(double x1, double x2, double y1, double y2)
        {
            double dx = x1 - x2;

            if (dx != 0.0)
            {
                double a = (y1 - y2) / dx;
                double b = y1 - (a * x1);

                return (a, b);
            }

            return (0.0f, 0.0f);
        }

        void getLogBuckets(ref float minLog, ref float maxLog, MinMaxMidList<T> items, PoolList<MinMaxMidList<T>> buckets, int count)
        {
            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

            var cnt = items.Storage.Count;

            for(int i = 0; i < cnt && i < items.Storage.m_items.Length; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(items.Storage.m_items[i]) + b));
                index = Math.Min(count - 1, index);
                var bucket = buckets.m_items[index];

                if (bucket == null)
                {
                    const int maxCapacity = 1024 * 1024;
                    const int arraySize = 2;
                    buckets[index] = bucket = new MinMaxMidList<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(items.Storage.m_items[i]);
            }
        }
        
        void getBuckets(ref T min, ref T max, MinMaxMidList<T> items, PoolList<MinMaxMidList<T>> buckets, int count)
        {
            var (a, b) = GetLinearTransformParams(m_getDouble(min), m_getDouble(max), 0, count - 1);

            var cnt = items.Storage.Count;

            for(int i = 0; i < cnt && i < items.Storage.m_items.Length; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getDouble(items.Storage.m_items[i]) + b));
                index = Math.Min(count - 1, index);
                var bucket = buckets.m_items[index];

                if (bucket == null)
                {
                    const int maxCapacity = 1024 * 1024;
                    const int arraySize = 2;
                    buckets[index] = bucket = new MinMaxMidList<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(items.Storage.m_items[i]);
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
            
            for (int i = 0; i < topCount && index + i < output.Length; ++i)
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
            var minLog = m_getLog(top.Min);
            var maxLog = m_getLog(top.Max);

            if (maxLog - minLog < 0.1f)
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
                    var minMaxHeap = newBuckets.m_items[i];
                
                    if (minMaxHeap != null)
                    {
                        st.Push(minMaxHeap);
                    }
                }
                
                return 0;
            }

            {
                var count = (top.Storage.Count / 2) + 1;

                var newBuckets = new PoolList<MinMaxMidList<T>>(count, count, count);

                getLogBuckets(ref minLog, ref maxLog, top, newBuckets, count);

                for (int i = newBuckets.Count - 1; i >= 0; --i)
                {
                    var minMaxHeap = newBuckets.m_items[i];

                    if (minMaxHeap != null)
                    {
                        st.Push(minMaxHeap);
                    }
                }

                return 0;
            }
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

            var bucketCount = Math.Min(array.Length, 1024);
            
            var buckets = new PoolList<MinMaxMidList<T>>(bucketCount, bucketCount, bucketCount);

            var minLog = m_getLog(min);
            var maxLog = m_getLog(max);

            var (a, b) = GetLinearTransformParams(minLog, maxLog, 0, bucketCount - 1);

            for(int i = 0; i < array.Length; i++)
            {
                // ApplyLinearTransform
                int index = (int)((a * m_getLog(array[i]) + b));
                index = Math.Min(bucketCount - 1, index);
                var bucket = buckets.m_items[index];

                if (bucket == null)
                {
                    const int maxCapacity = 1024 * 1024;
                    const int arraySize = 2;
                    buckets.m_items[index] = bucket = new MinMaxMidList<T>(new PoolList<T>(maxCapacity, arraySize));
                }
                bucket.Add(array[i]);
            }

            for (int i = buckets.Count - 1; i >= 0; --i)
            {
                var minMaxHeap = buckets.m_items[i];
                
                if (minMaxHeap != null)
                {
                    st.Push(minMaxHeap);
                }
            }
        }
    }
}
