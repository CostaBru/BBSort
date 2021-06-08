using System;
using Flexols.Data.Collections;

namespace BBsort.Collections
{
    public static class Limits<T> where T : struct, IComparable<T>
    {
        public static T Min = GetMin();
        public static T Max = GetMax();
        
        public static T GetMin()
        {
            if ((typeof(T)) == typeof(float))
            {
                return (T)(object)-float.MaxValue;
            }
            else if ((typeof(T)) == typeof(double))
            {
                return (T)(object)-double.MaxValue;
            }
            else if ((typeof(T)) == typeof(int))
            {
                return (T)(object)-int.MaxValue;
            }
            if ((typeof(T)) == typeof(short))
            {
                return (T)(object)-short.MaxValue;
            }
            else if ((typeof(T)) == typeof(long))
            {
                return (T)(object)-long.MaxValue;
            }
            else if ((typeof(T)) == typeof(byte))
            {
                return (T)(object)0;
            }

            return default(T);
        }
        
        public static T GetMax()
        {
            if ((typeof(T)) == typeof(float))
            {
                return (T)(object)float.MaxValue;
            }
            else if ((typeof(T)) == typeof(double))
            {
                return (T)(object)double.MaxValue;
            }
            else if ((typeof(T)) == typeof(int))
            {
                return (T)(object)int.MaxValue;
            }
            if ((typeof(T)) == typeof(short))
            {
                return (T)(object)short.MaxValue;
            }
            else if ((typeof(T)) == typeof(long))
            {
                return (T)(object)long.MaxValue;
            }
            else if ((typeof(T)) == typeof(byte))
            {
                return (T)(object)byte.MaxValue;
            }

            return default(T);
        }
    }
    
    public class MinMaxMidList<T> where T : struct, IComparable<T>
    {
        public readonly PoolList<T> Storage;
        
        public T Min = Limits<T>.Max;
        public T Max = Limits<T>.Min;
        
        /// <summary>
        /// Has valid and reliable value for case size() == 3 only.
        /// </summary>
        public T Mid = Limits<T>.Min;

        public MinMaxMidList(PoolList<T> storage)
        {
            Storage = storage;
        }

        public void Add(T value)
        {
            if (Storage.m_size == 2){

                if (value.CompareTo(Min) < 0){

                    T temp = Min;
                    Min = value;
                    Mid = temp;
                } else if (value.CompareTo(Max) > 0){

                    T temp = Max;
                    Max = value;
                    Mid = temp;
                }
                else{

                    Mid = value;
                }
            }
            else
            {
                Min = value.CompareTo(Min) > 0 ? Min : value;
                Max = value.CompareTo(Max) > 0 ? value : Max;
            }

            Storage.Add(value);
        }
    }
}