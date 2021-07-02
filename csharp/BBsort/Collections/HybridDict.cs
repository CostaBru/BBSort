using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;

namespace Flexols.Data.Collections
{
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class HybridDict<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IDisposable
    {
        
        public const int HashCollisionThreshold = 100;
        
        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly HybridDict<TKey, TValue> m_dictionary;
            private readonly int m_version;
            private int m_index;
            private KeyValuePair<TKey, TValue> m_current;

            internal Enumerator(HybridDict<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    return m_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return new KeyValuePair<TKey, TValue>(m_current.Key, m_current.Value);
                }
            }

            public bool MoveNext()
            {
                CheckVersion();

                while (m_index < m_dictionary.m_count)
                {
                    if (m_dictionary.m_entries[m_index].HashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(m_dictionary.m_entries[m_index].Key, m_dictionary.m_entries[m_index].Value);
                        m_index++;
                        return true;
                    }
                    m_index++;
                }
                m_index = m_dictionary.m_count + 1;
                m_current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            public void Dispose()
            {
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException();
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        [NotNull]
        public IReadOnlyList<TKey> KeyList
        {
            get
            {
                return Keys.ToHybridList();
            }
        }

        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
        {
            private readonly HybridDict<TKey, TValue> m_dictionary;

            public KeyCollection(HybridDict<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException("dictionary");
                }
                m_dictionary = dictionary;
            }

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < m_dictionary.Count)
                {
                    throw new ArgumentException();
                }
                int count = m_dictionary.m_count;
                IList<Entry> entries = m_dictionary.m_entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].HashCode >= 0)
                    {
                        array[index++] = entries[i].Key;
                    }
                }
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return m_dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new Enumerator(m_dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((ICollection<TKey>)this).GetEnumerator();
            }

            public int Count
            {
                get
                {
                    return m_dictionary.Count;
                }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public struct Enumerator : IEnumerator<TKey>
            {
                private readonly HybridDict<TKey, TValue> m_dictionary;
                private readonly int m_version;
                private int m_index;
                private TKey m_currentKey;

                public TKey Current
                {
                    get
                    {
                        return m_currentKey;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        CheckState();

                        return m_currentKey;
                    }
                }

                internal Enumerator(HybridDict<TKey, TValue> dictionary)
                {
                    m_dictionary = dictionary;
                    m_version = dictionary.m_version;
                    m_index = 0;
                    m_currentKey = default(TKey);
                }

                public bool MoveNext()
                {
                    CheckVersion();

                    while (m_index < m_dictionary.m_count)
                    {
                        if (m_dictionary.m_entries[m_index].HashCode >= 0)
                        {
                            m_currentKey = m_dictionary.m_entries[m_index].Key;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentKey = default(TKey);
                    return false;
                }

                public void Dispose()
                {
                }

                void IEnumerator.Reset()
                {
                    CheckVersion();

                    m_index = 0;
                    m_currentKey = default(TKey);
                }

                private void CheckVersion()
                {
                    if (m_version != m_dictionary.m_version)
                    {
                        throw new InvalidOperationException();
                    }
                }

                private void CheckState()
                {
                    if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
        {
            private readonly HybridDict<TKey, TValue> m_dictionary;

            public ValueCollection(HybridDict<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException("dictionary");
                }
                m_dictionary = dictionary;
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < m_dictionary.Count)
                {
                    throw new ArgumentException();
                }
                int count = m_dictionary.m_count;
                IList<Entry> entries = m_dictionary.m_entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].HashCode >= 0)
                    {
                        array[index++] = entries[i].Value;
                    }
                }
            }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return m_dictionary.ContainsValue(item);
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return new Enumerator(m_dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((ICollection<TValue>)this).GetEnumerator();
            }

            public int Count
            {
                get
                {
                    return m_dictionary.Count;
                }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly HybridDict<TKey, TValue> m_dictionary;
                private readonly int m_version;
                private int m_index;
                private TValue m_currentValue;

                public TValue Current
                {
                    get
                    {
                        return m_currentValue;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        CheckState();

                        return m_currentValue;
                    }
                }

                internal Enumerator(HybridDict<TKey, TValue> dictionary)
                {
                    m_dictionary = dictionary;
                    m_version = dictionary.m_version;
                    m_index = 0;
                    m_currentValue = default(TValue);
                }

                public bool MoveNext()
                {
                    CheckVersion();

                    while (m_index < m_dictionary.m_count)
                    {
                        if (m_dictionary.m_entries[m_index].HashCode >= 0)
                        {
                            m_currentValue = m_dictionary.m_entries[m_index].Value;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentValue = default(TValue);
                    return false;
                }

                public void Dispose()
                {
                }

                void IEnumerator.Reset()
                {
                    CheckVersion();

                    m_index = 0;
                    m_currentValue = default(TValue);
                }

                private void CheckVersion()
                {
                    if (m_version != m_dictionary.m_version)
                    {
                        throw new InvalidOperationException();
                    }
                }

                private void CheckState()
                {
                    if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        private static class Prime
        {
            private static readonly int[] s_primes =
                new int[]
                {
                    2, 3, 7, 11, 0x11, 0x17, 0x1d, 0x25, 0x2f, 0x3b, 0x47, 0x59, 0x6b, 0x83, 0xa3, 0xc5, 0xef,
                    0x125, 0x161, 0x1af, 0x209, 0x277, 0x2f9, 0x397, 0x44f, 0x52f, 0x63d, 0x78b, 0x91d, 0xaf1, 0xd2b, 0xfd1, 0x12fd,
                    0x16cf, 0x1b65, 0x20e3, 0x2777, 0x2f6f, 0x38ff, 0x446f, 0x521f, 0x628d, 0x7655, 0x8e01, 0xaa6b, 0xcc89, 0xf583, 0x126a7, 0x1619b,
                    0x1a857, 0x1fd3b, 0x26315, 0x2dd67, 0x3701b, 0x42023, 0x4f361, 0x5f0ed, 0x72125, 0x88e31, 0xa443b, 0xc51eb, 0xec8c1, 0x11bdbf, 0x154a3f, 0x198c4f,
                    0x1ea867, 0x24ca19, 0x2c25c1, 0x34fa1b, 0x3f928f, 0x4c4987, 0x5b8b6f, 0x6dda89
                };


            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static int GetPrime(int min)
            {
                if (min < 0)
                {
                    throw new ArgumentException();
                }
                for (int i = 0; i < s_primes.Length; i++)
                {
                    int prime = s_primes[i];
                    if (prime >= min)
                    {
                        return prime;
                    }
                }
                for (int j = min | 1; j < 0x7fffffff; j += 2)
                {
                    if (IsPrime(j))
                    {
                        return j;
                    }
                }
                return min;
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            private static bool IsPrime(int candidate)
            {
                if ((candidate & 1) == 0)
                {
                    return (candidate == 2);
                }

                int num = (int)Math.Sqrt(candidate);
                for (int i = 3; i <= num; i += 2)
                {
                    if ((candidate % i) == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [Serializable]
        private struct Entry
        {
            public Entry(int hashCode, int next, TKey key, TValue value)
            {
                HashCode = hashCode;
                Next = next;
                Key = key;
                Value = value;
            }

            public int HashCode;
            public int Next;
            public TKey Key;
            public TValue Value;
        }

        private IEqualityComparer<TKey> m_comparer;
        private HybridList<int> m_buckets;
        private HybridList<Entry> m_entries;
        private int m_count;
        private int m_freeCount;
        private int m_freeList;
        private KeyCollection m_keys;
        private ValueCollection m_values;
        private int m_version;
        private bool m_isDisposed;

        public HybridDict()
            : this(0, null)
        {
        }

        public HybridDict(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        public HybridDict(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public HybridDict(int capacity)
            : this(capacity, null)
        {
        }

        public HybridDict(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : this((dictionary != null) ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }

        public HybridDict(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            if (capacity > 0)
            {
                Initialize(capacity);
            }
            if (comparer == null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }
            m_comparer = comparer;
        }

        public void Add(TKey key, TValue value)
        {
            Insert(key, value);
        }

        public void Clear()
        {
            if (m_count <= 0)
            {
                return;
            }

            m_buckets?.DisposeList();
            m_entries?.DisposeList();

            m_buckets = null;
            m_entries = null;

            m_freeList = -1;
            m_count = 0;
            m_freeCount = 0;
            m_version++;
        }

        public bool ContainsKey(TKey key)
        {
            KeyValuePair<TKey, TValue> entry;
            return (FindEntry(key, out entry));
        }

        public bool ContainsValue(TValue value)
        {
            if (m_entries.m_root is HybridList<Entry>.StoreNode storeNode)
            {
                if (value == null)
                {
                    for (int i = 0; i < m_count && i < storeNode.m_items.Length; i++)
                    {
                        if ((storeNode.m_items[i].HashCode >= 0) && (storeNode.m_items[i].Value == null))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
                    for (int j = 0; j < m_count && j < storeNode.m_items.Length; j++)
                    {
                        if ((storeNode.m_items[j].HashCode >= 0) && comparer.Equals(storeNode.m_items[j].Value, value))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (value == null)
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        Entry entry = m_entries[i];
                        if ((entry.HashCode >= 0) && (entry.Value == null))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
                    for (int j = 0; j < m_count; j++)
                    {
                        Entry entry = m_entries[j];
                        if ((entry.HashCode >= 0) && comparer.Equals(entry.Value, value))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void CopyTo(IList<KeyValuePair<TKey, TValue>> destination, int index)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if ((index < 0) || (index > destination.Count))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((destination.Count - index) < Count)
            {
                throw new ArgumentException();
            }
            int count = m_count;
            IList<Entry> entries = m_entries;
            for (int i = 0; i < count; i++)
            {
                Entry entry = entries[i];
                if (entry.HashCode >= 0)
                {
                    destination[index++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                }
            }
        }

        private bool FindEntry(TKey key, out KeyValuePair<TKey, TValue> entry)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (m_buckets != null)
            {
                if (m_entries.m_root is HybridList<Entry>.StoreNode storeNode)
                {
                    int num = m_comparer.GetHashCode(key) & 0x7fffffff;
                    for (int i = m_buckets[num % m_buckets.Count] - 1; i >= 0 && i < storeNode.m_items.Length; i = storeNode.m_items[i].Next)
                    {
                        if ((storeNode.m_items[i].HashCode == num) && m_comparer.Equals(storeNode.m_items[i].Key, key))
                        {
                            entry = new KeyValuePair<TKey, TValue>(storeNode.m_items[i].Key, storeNode.m_items[i].Value);
                            return true;
                        }
                    }
                }
                else
                {
                    int num = m_comparer.GetHashCode(key) & 0x7fffffff;
                    Entry currentEntry;
                    for (int i = m_buckets[num % m_buckets.Count] - 1; i >= 0; i = currentEntry.Next)
                    {
                        currentEntry = m_entries[i];
                        if ((currentEntry.HashCode == num) && m_comparer.Equals(currentEntry.Key, key))
                        {
                            entry = new KeyValuePair<TKey, TValue>(currentEntry.Key, currentEntry.Value);
                            return true;
                        }
                    }
                }
            }
            entry = default(KeyValuePair<TKey, TValue>);
            return false;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private void Initialize(int capacity)
        {
            int prime = Prime.GetPrime(capacity);

            m_buckets = new HybridList<int>(prime);
            m_entries = new HybridList<Entry>(prime);

            m_buckets.Ensure(prime);
            m_entries.Ensure(prime);

            m_freeList = -1;
        }

        private void Insert(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int freeList;
            int hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;
            int index = hashCode % m_buckets.Count;
            
            if (m_entries.m_root is HybridList<Entry>.StoreNode storeNode)
            {
                for (int i = m_buckets[index] - 1; i >= 0; i = storeNode.m_items[i].Next)
                {
                    ref Entry storeNodeItem = ref storeNode.m_items[i];
                    
                    if ((storeNodeItem.HashCode == hashCode) && m_comparer.Equals(storeNodeItem.Key, key))
                    {
                        storeNodeItem.Value = value;
                        
                        m_version++;
                        return;
                    }
                }
            }
            else
            {
                for (int i = m_buckets[index] - 1; i >= 0;)
                {
                    ref Entry currentEntry = ref m_entries.ValueByRef(i);
                    if ((currentEntry.HashCode == hashCode) && m_comparer.Equals(currentEntry.Key, key))
                    {
                        currentEntry.Value = value;
                        m_entries[i] = currentEntry;
                        m_version++;
                        return;
                    }

                    i = currentEntry.Next;
                }
            }
          
            if (m_freeCount > 0)
            {
                freeList = m_freeList;
                m_freeList = m_entries[freeList].Next;
                m_freeCount--;
            }
            else
            {
                if (m_count == m_entries.Count)
                {
                    int prime = Prime.GetPrime(m_count * 2);

                    Resize(prime);
                    index = hashCode % m_buckets.Count;
                }
                freeList = m_count;
                m_count++;
            }
            m_entries[freeList] = new Entry(hashCode, m_buckets[index] - 1, key, value);
            m_buckets[index] = freeList + 1;
            m_version++;
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (m_buckets != null)
            {
                int hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;
                
                int index = hashCode % m_buckets.Count;
                int last = -1;
                
                for (int i = m_buckets[index] - 1; i >= 0; )
                {
                    ref var currentEntry = ref m_entries.ValueByRef(i);
                    if ((currentEntry.HashCode == hashCode) && m_comparer.Equals(currentEntry.Key, key))
                    {
                        if (last < 0)
                        {
                            m_buckets[index] = currentEntry.Next + 1;
                        }
                        else
                        {
                            Entry entry = m_entries[last];
                            entry.Next = currentEntry.Next;
                            m_entries[last] = entry;
                        }
                        m_entries[i] = new Entry(-1, m_freeList, default(TKey), default(TValue));
                        m_freeList = i;
                        m_freeCount++;
                        m_version++;
                        return true;
                    }
                    last = i;
                    i = currentEntry.Next;
                }
            }
            return false;
        }

        private void Resize(int prime, bool forceNewHashCodes = false)
        {
            HybridList<int> newBuckets = new HybridList<int>(prime);
            HybridList<Entry> newEntries = new HybridList<Entry>(prime);
            
            newBuckets.Ensure(prime);

            if (m_entries.m_root is HybridList<Entry>.StoreNode storeNode)
            {
                for (var i = 0; i < m_entries.m_count && i < storeNode.m_items.Length; i++)
                {
                    ref var entry = ref storeNode.m_items[i];
                    
                    if (entry.HashCode >= 0)
                    {
                        var bucket = entry.HashCode % prime;

                        entry.Next = newBuckets[bucket] - 1;
                        
                        newBuckets[bucket] = i + 1;
                        
                        newEntries.Add(entry);
                    }
                    else
                    {
                        newEntries.Add(entry);
                    }
                }
            }
            else
            {
                for (var i = 0; i < m_entries.m_count; i++)
                {
                    ref var entry = ref m_entries.ValueByRef(i);
                    
                    if (entry.HashCode >= 0)
                    {
                        var bucket = entry.HashCode % prime;

                        entry.Next = newBuckets[bucket] - 1;
                        
                        newBuckets[bucket] = i + 1;
                        
                        newEntries.Add(entry);
                    }
                    else
                    {
                        newEntries.Add(entry);
                    }
                }
            }

            int unusedCount = prime - m_entries.Count;
            for (int i = 0; i < unusedCount; i++)
            {
                newEntries.Add(new Entry());
            }

            m_buckets?.DisposeList();
            m_entries?.DisposeList();

            m_buckets = newBuckets;
            m_entries = newEntries;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
        {
            KeyValuePair<TKey, TValue> entry;
            return (FindEntry(pair.Key, out entry) && EqualityComparer<TValue>.Default.Equals(entry.Value, pair.Value));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            CopyTo(array, index);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
        {
            KeyValuePair<TKey, TValue> entry;
            if (FindEntry(pair.Key, out entry) && EqualityComparer<TValue>.Default.Equals(entry.Value, pair.Value))
            {
                Remove(pair.Key);
                return true;
            }
            return false;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            KeyValuePair<TKey, TValue> entry;
            if (FindEntry(key, out entry))
            {
                value = entry.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }


        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                return m_comparer;
            }
        }

        public int Count
        {
            get
            {
                return (m_count - m_freeCount);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                KeyValuePair<TKey, TValue> entry;
                if (FindEntry(key, out entry))
                {
                    return entry.Value;
                }
                throw new KeyNotFoundException();
            }
            set
            {
                Insert(key, value);
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public KeyCollection Keys
        {
            get
            {
                return m_keys ?? (m_keys = new KeyCollection(this));
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get { return Keys; }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return Keys;
            }
        }

        public ValueCollection Values
        {
            get
            {
                return m_values ?? (m_values = new ValueCollection(this));
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return Values;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get { return Values; }
        }

        ~HybridDict()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!(m_isDisposed))
            {
                Clear();

                m_isDisposed = true;
            }
        }
    }

    public static class HybridDictExtensions
    {
       public static void DisposeDict<T,V>(this HybridDict<T, V> dict)
        {
            if (dict != null)
            {
                if (dict is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    dict.Clear();
                }
            }
        }
    }
}
