using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Flexols.Data.Collections
{
    public class HybridHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private static readonly int[] s_primes = new int[72]
        {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
        };

        private const string CapacityName = "Capacity";
        private const string ElementsName = "Elements";
        private const string ComparerName = "Comparer";
        private const string VersionName = "Version";

        private HybridList<int> m_buckets;
        private HybridList<Slot> m_slots;

        private int m_count;
        private int m_lastIndex;
        private int? m_freeList;
        private IEqualityComparer<T> m_comparer;
        private SerializationInfo m_siInfo;

        private static readonly bool IsReferenceType = typeof(T).IsByRef;

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that is empty and uses the default equality comparer for the set type.</summary>
        public HybridHashSet()
          : this((IEqualityComparer<T>)EqualityComparer<T>.Default)
        {
        }

        public HybridHashSet(int capacity)
          : this(capacity, (IEqualityComparer<T>)EqualityComparer<T>.Default)
        {
        }

        public HybridHashSet(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                comparer = (IEqualityComparer<T>)EqualityComparer<T>.Default;
            }
            m_comparer = comparer;
            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;
        }

        public HybridHashSet(IEnumerable<T> collection)
          : this(collection, (IEqualityComparer<T>)EqualityComparer<T>.Default)
        {
        }

        public HybridHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
          : this(comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (collection is HybridHashSet<T> objSet && AreEqualityComparersEqual(this, objSet))
            {
                CopyFrom(objSet);
            }
            else
            {
                Initialize(!(collection is ICollection<T> objs) ? 0 : objs.Count);

                UnionWith(collection);
            }
        }

        private static int ExpandPrime(int oldSize)
        {
            int min = 2 * oldSize;
            if ((uint) min > 2146435069U && 2146435069 > oldSize)
            {
                return 2146435069;
            }
            return GetPrime(min);
        }

        private static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException("CapacityOverflow", nameof(min));
            }
            for (int index = 0; index < s_primes.Length; ++index)
            {
                int prime = s_primes[index];
                if (prime >= min)
                {
                    return prime;
                }
            }
            int candidate = min | 1;
            while (candidate < int.MaxValue)
            {
                if (IsPrime(candidate) && (candidate - 1) % 101 != 0)
                {
                    return candidate;
                }
                candidate += 2;
            }
            return min;
        }

        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
            {
                return candidate == 2;
            }
            int sqrt = (int)Math.Sqrt((double)candidate);
            int i = 3;
            while (i <= sqrt)
            {
                if (candidate % i == 0)
                {
                    return false;
                }
                i += 2;
            }
            return true;
        }

        private void CopyFrom(HybridHashSet<T> source)
        {
            int count = source.m_count;
            if (count == 0)
            {
                return;
            }

            int length = source.m_buckets.Count;

            if (ExpandPrime(count + 1) >= length)
            {
                m_buckets = new HybridList<int>(source.m_buckets);
                m_slots = new HybridList<Slot>(source.m_slots);

                m_lastIndex = source.m_lastIndex;
                m_freeList = source.m_freeList;
            }
            else
            {
                int lastIndex = source.m_lastIndex;
                var slots = source.m_slots;

                Initialize(count);

                int index = 0;
                for (int i = 0; i < lastIndex; ++i)
                {
                    int hashCode = slots.ValueByRef(i).hashCode;
                    if (hashCode >= 0)
                    {
                        AddValue(index, hashCode, slots[i].value);
                        ++index;
                    }
                }
                m_lastIndex = index;
            }
            m_count = count;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class with serialized data.</summary>
        /// <param name="info">A <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object that contains the information required to serialize the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <param name="context">A <see cref="T:System.Runtime.Serialization.StreamingContext" /> structure that contains the source and destination of the serialized stream associated with the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        protected HybridHashSet(SerializationInfo info, StreamingContext context)
        {
            m_siInfo = info;
        }

        public HybridHashSet(int capacity, IEqualityComparer<T> comparer)
          : this(comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity <= 0)
            {
                return;
            }

            Initialize(capacity);
        }

       
        void ICollection<T>.Add(T item)
        {
            AddIfNotPresent(item);
        }

        /// <summary>Removes all elements from a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>

        public void Clear()
        {
            m_slots?.DisposeList();
            m_buckets?.DisposeList();

            m_slots = null;
            m_buckets = null;

            m_lastIndex = 0;
            m_count = 0;
            m_freeList = null;
        }

        /// <summary>Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object contains the specified element.</summary>
        /// <param name="item">The element to locate in the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>
        /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.HashSet`1" /> object contains the specified element; otherwise, <see langword="false" />.</returns>
        public bool Contains(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(item);

                var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

                for (int? index = start - 1; index >= 0; index = m_slots.ValueByRef(index.Value).next)
                {
                    var slot = m_slots.ValueByRef(index.Value);

                    if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Copies the elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to an array, starting at the specified array index.</summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="arrayIndex" /> is greater than the length of the destination <paramref name="array" />.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, m_count);
        }

        /// <summary>Removes the specified element from a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>
        /// <see langword="true" /> if the element is successfully found and removed; otherwise, <see langword="false" />.  This method returns <see langword="false" /> if <paramref name="item" /> is not found in the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</returns>
       
        public bool Remove(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                int index1 = hashCode % m_buckets.Count;
                int i = -1;

                var start = m_buckets.ValueByRef(index1);

                for (int? index = start - 1; index >= 0; index = m_slots.ValueByRef(index.Value).next)
                {
                    var slot = m_slots.ValueByRef(index.Value);

                    if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                    {
                        if (i < 0)
                        {
                            m_buckets.ValueByRef(index1) = (slot.next ?? 0) + 1;
                        }
                        else
                        {
                            m_slots.ValueByRef(i).next = slot.next;
                        }

                        slot.hashCode = -1;
                        slot.value = default(T);
                        slot.next = m_freeList;

                        m_slots.ValueByRef(index.Value) = slot;

                        --m_count;

                        if (m_count == 0)
                        {
                            m_lastIndex = 0;
                            m_freeList = -1;
                        }
                        else
                        {
                            m_freeList = index;
                        }

                        return true;
                    }
                    i = index.Value;
                }
            }
            return false;
        }

        /// <summary>Gets the number of elements that are contained in a set.</summary>
        /// <returns>The number of elements that are contained in the set.</returns>
       
        public int Count => m_count;


        bool ICollection<T>.IsReadOnly => false;

        /// <summary>Returns an enumerator that iterates through a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.HashSet`1.Enumerator" /> object for the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</returns>

        public IEnumerable<T> Values()
        {
            for (int i = 0; i < m_lastIndex; ++i)
            {
                if (m_slots.ValueByRef(i).hashCode >= 0)
                {
                    yield return m_slots.ValueByRef(i).value;
                }
            }
        }
       
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Values().GetEnumerator();
        }

       
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values().GetEnumerator();
        }

        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and returns the data needed to serialize a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
        /// <param name="info">A <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object that contains the information required to serialize the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <param name="context">A <see cref="T:System.Runtime.Serialization.StreamingContext" /> structure that contains the source and destination of the serialized stream associated with the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="info" /> is <see langword="null" />.</exception>
        [SecurityCritical]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue("Comparer", (object)m_comparer, typeof(IEqualityComparer<T>));
            info.AddValue("Capacity", m_buckets?.Count ?? 0);

            if (m_buckets == null)
            {
                return;
            }

            var array = ArrayPool<T>.Shared.Rent(m_count);
            CopyTo(array);
            info.AddValue("Elements", (object)array, typeof(T[]));
            ArrayPool<T>.Shared.Return(array);
        }

        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the deserialization event when the deserialization is complete.</summary>
        /// <param name="sender">The source of the deserialization event.</param>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object associated with the current <see cref="T:System.Collections.Generic.HashSet`1" /> object is invalid.</exception>
        public virtual void OnDeserialization(object sender)
        {
            if (m_siInfo == null)
            {
                return;
            }

            int capacity = m_siInfo.GetInt32(CapacityName);

            m_comparer = (IEqualityComparer<T>)m_siInfo.GetValue(ComparerName, typeof(IEqualityComparer<T>));
            m_freeList = -1;

            if (capacity != 0)
            {
                m_buckets = new HybridList<int>(capacity);
                m_slots = new HybridList<Slot>(capacity);

                T[] objArray = (T[])m_siInfo.GetValue(ElementsName, typeof(T[]));
                if (objArray == null)
                {
                    throw new SerializationException("Serialization_MissingKeys");
                }
            
                for (int index = 0; index < objArray.Length; ++index)
                {
                    AddIfNotPresent(objArray[index]);
                }
            }
            else
            {
                m_buckets = null;
            }
            m_siInfo.GetInt32(VersionName);
            m_siInfo = (SerializationInfo)null;
        }

        /// <summary>Adds the specified element to a set.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>
        /// <see langword="true" /> if the element is added to the <see cref="T:System.Collections.Generic.HashSet`1" /> object; <see langword="false" /> if the element is already present.</returns>
       
        public bool Add(T item)
        {
            return AddIfNotPresent(item);
        }

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            if (m_buckets != null)
            {
                int index = InternalIndexOf(equalValue);
                if (index >= 0)
                {
                    actualValue = m_slots.ValueByRef(index).value;
                    return true;
                }
            }
            actualValue = default(T);
            return false;
        }
       
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (T obj in other)
            {
                AddIfNotPresent(obj);
            }
        }

      
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (m_count == 0)
            {
                return;
            }

            if (other == this)
            {
                Clear();
            }
            else
            {
                foreach (T obj in other)
                {
                    Remove(obj);
                }
            }
        }
       
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is ICollection<T> objs)
            {
                if (objs.Count == 0)
                {
                    return true;
                }

                if (other is HybridHashSet<T> set2 && AreEqualityComparersEqual(this, set2) && set2.Count > m_count)
                {
                    return false;
                }
            }
            return ContainsAllElements(other);
        }

       

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (m_count == 0)
            {
                return false;
            }
            foreach (T obj in other)
            {
                if (Contains(obj))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Copies the elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to an array.</summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.</exception>
      
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0, m_count);
        }

        /// <summary>Copies the specified number of elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to an array, starting at the specified array index.</summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <param name="count">The number of elements to copy to <paramref name="array" />.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than 0.-or-
        /// <paramref name="count" /> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="arrayIndex" /> is greater than the length of the destination <paramref name="array" />.-or-
        /// <paramref name="count" /> is greater than the available space from the <paramref name="index" /> to the end of the destination <paramref name="array" />.</exception>
       
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "ArgumentOutOfRange_NeedNonNegNum");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "ArgumentOutOfRange_NeedNonNegNum");
            }

            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
            }
            int num = 0;
            for (int index = 0; index < m_lastIndex && num < count; ++index)
            {
                if (m_slots.ValueByRef(index).hashCode >= 0)
                {
                    array[arrayIndex + num] = m_slots.ValueByRef(index).value;
                    ++num;
                }
            }
        }

       
        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            int num = 0;
            for (int index = 0; index < m_lastIndex; ++index)
            {
                if (m_slots.ValueByRef(index).hashCode >= 0)
                {
                    T obj = m_slots.ValueByRef(index).value;

                    if (match(obj) && Remove(obj))
                    {
                        ++num;
                    }
                }
            }
            return num;
        }

        /// <summary>Gets the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> object that is used to determine equality for the values in the set.</summary>
        /// <returns>The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> object that is used to determine equality for the values in the set.</returns>
       
        public IEqualityComparer<T> Comparer => m_comparer;

        /// <summary>Sets the capacity of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to the actual number of elements it contains, rounded up to a nearby, implementation-specific value.</summary>
    
        public void TrimExcess()
        {
        }

        /// <summary>Returns an <see cref="T:System.Collections.IEqualityComparer" /> object that can be used for equality testing of a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
        /// <returns>An <see cref="T:System.Collections.IEqualityComparer" /> object that can be used for deep equality testing of the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</returns>
        public static IEqualityComparer<HybridHashSet<T>> CreateSetComparer()
        {
            return new HashSetEqualityComparer<T>();
        }

        [Serializable]
        internal class HashSetEqualityComparer<T> : IEqualityComparer<HybridHashSet<T>>
        {
            private IEqualityComparer<T> m_comparer;

            public HashSetEqualityComparer()
            {
                m_comparer = (IEqualityComparer<T>)EqualityComparer<T>.Default;
            }

            public HashSetEqualityComparer(IEqualityComparer<T> comparer)
            {
                if (comparer == null)
                {
                    m_comparer = (IEqualityComparer<T>)EqualityComparer<T>.Default;
                }
                else
                {
                    m_comparer = comparer;
                }
            }

            public bool Equals(HybridHashSet<T> x, HybridHashSet<T> y)
            {
                return HybridHashSet<T>.HashSetEquals(x, y, m_comparer);
            }

            public int GetHashCode(HybridHashSet<T> obj)
            {
                int num = 0;
                if (obj != null)
                {
                    foreach (T obj1 in obj)
                    {
                        num ^= m_comparer.GetHashCode(obj1) & int.MaxValue;
                    }
                }
                return num;
            }

            public override bool Equals(object obj)
            {
                HashSetEqualityComparer<T> equalityComparer = obj as HashSetEqualityComparer<T>;
                if (equalityComparer == null)
                {
                    return false;
                }
                return m_comparer == equalityComparer.m_comparer;
            }

            public override int GetHashCode()
            {
                return m_comparer.GetHashCode();
            }
        }

        private void Initialize(int capacity)
        {
            int prime = GetPrime(capacity);

            m_buckets = new HybridList<int>(prime);
            m_slots = new HybridList<Slot>(prime);

            m_buckets.Ensure(prime);
            m_slots.Ensure(prime);
        }

        private void IncreaseCapacity()
        {
            int newSize = ExpandPrime(m_count);
            if (newSize <= m_count)
            {
                throw new ArgumentException("Arg_HSCapacityOverflow");
            }

            m_buckets.Clear();
            m_buckets.Ensure(newSize);

            m_slots.Ensure(newSize);

            for (int slotIndex = 0; slotIndex < m_lastIndex; ++slotIndex)
            {
                int bucketIndex = m_slots[slotIndex].hashCode % newSize;

                m_slots.ValueByRef(slotIndex).next = m_buckets.ValueByRef(bucketIndex) - 1;

                m_buckets.ValueByRef(bucketIndex) = slotIndex + 1;
            }
        }

        private bool AddIfNotPresent(T value)
        {
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int hashCode = InternalGetHashCode(value);
            int storageIndex = hashCode % m_buckets.Count;
            int num = 0;

            var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

            for (int? i = start - 1; i >= 0; i = m_slots.ValueByRef(i.Value).next)
            {
                var s = m_slots.ValueByRef(i.Value);

                if (s.hashCode == hashCode && m_comparer.Equals(s.value, value))
                {
                    return false;
                }
                ++num;
            }

            int index;
            if (m_freeList >= 0)
            {
                index = m_freeList.Value;
                m_freeList = m_slots.ValueByRef(index).next;
            }
            else
            {
                if (m_lastIndex == m_slots.Count)
                {
                    IncreaseCapacity();
                    storageIndex = hashCode % m_buckets.Count;
                }
                index = m_lastIndex;
                ++m_lastIndex;
            }

            var bucket = m_buckets.ValueByRef(storageIndex);

            var slot = m_slots.ValueByRef(index);

            slot.hashCode = hashCode;
            slot.value = value;
            slot.next = bucket == 0 ? new int?() : bucket - 1;

            m_slots.ValueByRef(index) = slot;
            m_buckets.ValueByRef(storageIndex) = index + 1;

            ++m_count;
          
            return true;
        }
        
        private void AddValue(int index, int hashCode, T value)
        {
            int storageIndex = hashCode % m_buckets.Count;

            var bucket = m_buckets.ValueByRef(storageIndex);

            var slot = m_slots.ValueByRef(index);

            slot.hashCode = hashCode;
            slot.value = value;
            slot.next = bucket - 1;

            m_slots.ValueByRef(index) = slot;
            m_buckets.ValueByRef(storageIndex) = index + 1;
        }

        private bool ContainsAllElements(IEnumerable<T> other)
        {
            foreach (T obj in other)
            {
                if (!Contains(obj))
                {
                    return false;
                }
            }
            return true;
        }

        private int InternalIndexOf(T item)
        {
            int hashCode = InternalGetHashCode(item);

            var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

            for (int? index = start - 1; index >= 0; index = m_slots.ValueByRef(index.Value).next)
            {
                var slot = m_slots.ValueByRef(index.Value);

                if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                {
                    return index.Value;
                }
            }
            return -1;
        }
        
        internal static bool HashSetEquals(HybridHashSet<T> set1, HybridHashSet<T> set2, IEqualityComparer<T> comparer)
        {
            if (set1 == null)
            {
                return set2 == null;
            }

            if (set2 == null)
            {
                return false;
            }

            if (AreEqualityComparersEqual(set1, set2))
            {
                if (set1.Count != set2.Count)
                {
                    return false;
                }
                foreach (T obj in set2)
                {
                    if (!set1.Contains(obj))
                    {
                        return false;
                    }
                }
                return true;
            }
            foreach (T x in set2)
            {
                bool flag = false;
                foreach (T y in set1)
                {
                    if (comparer.Equals(x, y))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AreEqualityComparersEqual(HybridHashSet<T> set1, HybridHashSet<T> set2)
        {
            return set1.Comparer.Equals((object)set2.Comparer);
        }

        private int InternalGetHashCode(T item)
        {
            if (IsReferenceType)
            {
                if (item == null)
                {
                    return 0;
                }
            }
            return m_comparer.GetHashCode(item) & int.MaxValue;
        }

        internal struct ElementCount
        {
            internal int uniqueCount;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal int? next;
            internal T value;

            public override string ToString()
            {
                return $"{nameof(hashCode)}: {hashCode}, {nameof(next)}: {next}, {nameof(value)}: {value}";
            }
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            UnionWith(enumerable);
        }
    }
}

