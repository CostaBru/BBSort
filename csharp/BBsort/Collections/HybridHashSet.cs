using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Flexols.Data.Collections
{
    public class HybridHashSet<T> : ICollection<T>, IReadOnlyCollection<T>, IAppender<T>
    {
        public static HybridHashSet<T> operator +(HybridHashSet<T> a, IReadOnlyCollection<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                return b?.ToHybridHashSet();
            }
            
            if (ReferenceEquals(b, null))
            {
                return a?.ToHybridHashSet();
            }

            var set = new HybridHashSet<T>(a.Count + b.Count, a.m_comparer);
            
            set.AddRange(a);
            set.AddRange(b);

            return set;
        }
        
        public static HybridHashSet<T> operator -(HybridHashSet<T> a, IReadOnlyCollection<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToHybridHashSet();
            }

            var list = new HybridHashSet<T>(Math.Max(a.Count - b.Count, 0), a.m_comparer);
            
            foreach (var item in a)
            {
                if (!(b.Contains(item)))
                {
                    list.Add(item);
                }
            }

            return list;
        }
        
        public static bool operator ==(HybridHashSet<T> a, IReadOnlyCollection<T> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return true;

            if ((object) a == null || (object) b == null)
                return false;

            return a.EqualsSet(b);
        }
    
        public static bool operator !=(HybridHashSet<T> a, IReadOnlyCollection<T> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return false;

            if ((object)a == null || (object)b == null)
                return true;

            return !(a.EqualsSet(b));
        }
        
        protected bool EqualsSet(IReadOnlyCollection<T> other)
        {
            if (m_count == other.Count)
            {
                foreach (var item in other)
                {
                    if (!this.Contains(item))
                    {
                        return false;
                    }
                }

                foreach (var item in this)
                {
                    if (!other.Contains(item))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            
            return EqualsSet((HybridList<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 3 ^ m_count.GetHashCode();

                foreach (var item in this)
                {
                    hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(item);
                }
              
                return hashCode;
            }
        }
        
        private static readonly int[] s_primes = new int[]
        {
            HybridList<int>.SmallListCount,
            509,
            1021,
            2039,
            4091,
            8191,
            131071,
            524287,
            2946901
        };

        private const string CapacityName = "Capacity";
        private const string ElementsName = "Elements";
        private const string ComparerName = "Comparer";
        private const string VersionName = "Version";

        private HybridList<int> m_buckets;
        private HybridList<Slot> m_slots;

        private int m_count;
        private int m_lastIndex;
        private int m_freeList;
        private IEqualityComparer<T> m_comparer;
        private SerializationInfo m_siInfo;

        private static readonly bool IsReferenceType = typeof(T).IsByRef;
        private int m_version;

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
                comparer = EqualityComparer<T>.Default;
            }
            m_comparer = comparer;
            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;
        }

        public HybridHashSet(IEnumerable<T> collection)
          : this(collection, EqualityComparer<T>.Default)
        {
        }
        
        public HybridHashSet(HybridHashSet<T> collection)
            : this(collection, collection.m_comparer ?? EqualityComparer<T>.Default)
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
        
        public void Append(T item)
        {
            Add(item);
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
            Add(item);
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
            m_freeList = -1;
            
            ++m_version;
        }

        public bool IsMissing(T item)
        {
            return !Contains(item);
        }
        
        public bool Contains(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = IsReferenceType && item == null ? 0 : m_comparer.GetHashCode(item) & int.MaxValue;

                if (m_slots.m_root is HybridList<Slot>.StoreNode storeNode)
                {
                    var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

                    for (int? index = start - 1; index >= 0; index = storeNode.m_items[index.Value].next)
                    {
                        ref var slot = ref storeNode.m_items[index.Value];
                        
                        if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

                    for (int? index = start - 1; index >= 0; index = m_slots.ValueByRef(index.Value).next)
                    {
                        ref var slot = ref m_slots.ValueByRef(index.Value);

                        if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, m_count);
        }

        public bool Remove(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(ref item);
                int index1 = hashCode % m_buckets.Count;
                int last = -1;

                var start = m_buckets.ValueByRef(index1);

                for (int index = start - 1; index >= 0; index = m_slots.ValueByRef(index).next)
                {
                    ref var currentEntry = ref m_slots.ValueByRef(index);

                    if (currentEntry.hashCode == hashCode && m_comparer.Equals(currentEntry.value, item))
                    {
                        if (last < 0)
                        {
                            m_buckets.ValueByRef(index1) = currentEntry.next  + 1;
                        }
                        else
                        {
                            m_slots.ValueByRef(last).next = currentEntry.next;
                        }

                        currentEntry.hashCode = -1;
                        currentEntry.value = default(T);
                        currentEntry.next = m_freeList;

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
                    last = index;
                }
            }
            return false;
        }

        public int Count => m_count;

        bool ICollection<T>.IsReadOnly => false;

        public IEnumerable<T> Values()
        {
            var version = m_version;
            
            if (m_slots?.m_root is HybridList<Slot>.StoreNode storeNode)
            {
                for (int i = 0; i < m_lastIndex; ++i)
                {
                    if (storeNode.m_items[i].hashCode >= 0)
                    {
                        if (version != m_version)
                        {
                            throw new InvalidOperationException();
                        }
                        
                        yield return storeNode.m_items[i].value;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_lastIndex; ++i)
                {
                    if (m_slots.ValueByRef(i).hashCode >= 0)
                    {
                        if (version != m_version)
                        {
                            throw new InvalidOperationException();
                        }
                        
                        yield return m_slots.ValueByRef(i).value;
                    }
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
            Array.Clear(array, 0, array.Length);
            CopyTo(array);
            info.AddValue("Elements", (object)array, typeof(T[]));
            ArrayPool<T>.Shared.Return(array, true);
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
                    Add(objArray[index]);
                }
            }
            else
            {
                m_buckets = null;
            }
            m_siInfo.GetInt32(VersionName);
            m_siInfo = (SerializationInfo)null;
        }

       

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (T obj in other)
            {
                Add(obj);
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

       
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0, m_count);
        }

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

      
        public IEqualityComparer<T> Comparer => m_comparer;


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
            
            m_freeList = -1;
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
                ref var slot = ref m_slots.ValueByRef(slotIndex);
                
                int bucketIndex = slot.hashCode % newSize;

                slot.next = m_buckets.ValueByRef(bucketIndex) - 1;

                m_buckets.ValueByRef(bucketIndex) = slotIndex + 1;
            }
        }

        /// <summary>Adds the specified element to a set.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>
        /// <see langword="true" /> if the element is added to the <see cref="T:System.Collections.Generic.HashSet`1" /> object; <see langword="false" /> if the element is already present.</returns>
        public bool Add(T value)
        {
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int hashCode = InternalGetHashCode(ref value);
            int storageIndex = hashCode % m_buckets.Count;
            int num = 0;

            var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

            for (int? i = start - 1; i >= 0; i = m_slots.ValueByRef(i.Value).next)
            {
                ref var s = ref m_slots.ValueByRef(i.Value);

                if (s.hashCode == hashCode && m_comparer.Equals(s.value, value))
                {
                    return false;
                }
                ++num;
            }

            int index;
            if (m_freeList >= 0)
            {
                index = m_freeList;
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

            ref var slot = ref m_slots.ValueByRef(index);

            slot.hashCode = hashCode;
            slot.value = value;
            slot.next = bucket - 1;

            m_buckets.ValueByRef(storageIndex) = index + 1;

            ++m_count;
            ++m_version;
          
            return true;
        }
        
        private void AddValue(int index, int hashCode, T value)
        {
            int storageIndex = hashCode % m_buckets.Count;

            var bucket = m_buckets.ValueByRef(storageIndex);

            ref var slot = ref m_slots.ValueByRef(index);

            slot.hashCode = hashCode;
            slot.value = value;
            slot.next = bucket - 1;

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

        private int InternalGetHashCode(ref T item)
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

        internal struct Slot
        {
            internal int hashCode;
            internal int next;
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

