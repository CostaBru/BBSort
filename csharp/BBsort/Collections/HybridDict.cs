using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;


namespace Flexols.Data.Collections
{
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class HybridDict<TKey, TValue> :  IDictionary<TKey, TValue>, 
                                             ICollection<KeyValuePair<TKey, TValue>>,
                                             IEnumerable<KeyValuePair<TKey, TValue>>, 
                                             IReadOnlyDictionary<TKey, TValue>, 
                                             IReadOnlyCollection<KeyValuePair<TKey, TValue>>, 
                                             IAppender<KeyValuePair<TKey, TValue>>,
                                             IDisposable
    {       
        public static HybridDict<TKey, TValue> operator +(HybridDict<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, null))
            {
                return b?.ToHybridDict();
            }
            
            if (ReferenceEquals(b, null))
            {
                return a?.ToHybridDict();
            }

            var dict = new HybridDict<TKey, TValue>(a.m_comparer);
            
            dict.Union(a);
            dict.Union(b);

            return dict;
        }

        public void Union(IReadOnlyDictionary<TKey, TValue> dict)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));

            foreach (var kv in dict)
            {
                this[kv.Key] = kv.Value;
            }
        }

        public static HybridDict<TKey, TValue> operator -(HybridDict<TKey, TValue> a,  IReadOnlyDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToHybridDict();
            }

            var list = new HybridDict<TKey, TValue>(a.m_comparer);
            
            foreach (var item in a)
            {
                if (!(b.TryGetValue(item.Key, out var bValue)))
                {
                    list.Add(item);
                }                
                else if (!(EqualityComparer<TValue>.Default.Equals(item.Value, bValue)))
                {
                    list.Add(item);
                }
            }

            return list;
        }
        
        public static HybridDict<TKey, TValue> operator -(HybridDict<TKey, TValue> a,  IReadOnlyCollection<TKey> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToHybridDict();
            }

            var list = new HybridDict<TKey, TValue>(a.m_comparer);
            
            foreach (var item in a)
            {
                if (!(b.Contains(item.Key)))
                {
                    list.Add(item);
                }  
            }

            return list;
        }

        public static bool operator ==(HybridDict<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return true;

            if (ReferenceEquals(a, null) ||  ReferenceEquals(b, null))
                return false;

            return a.EqualsDict(b);
        }

        public static bool operator !=(HybridDict<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return false;

            if (ReferenceEquals(a, null) ||  ReferenceEquals(b, null))
                return true;

            return !(a.EqualsDict(b));
        }

        protected bool EqualsDict(IReadOnlyDictionary<TKey, TValue> other)
        {
            if (m_count == other.Count)
            {
                foreach (var kv in this)
                {
                    if(!(other.TryGetValue(kv.Key, out var otherValue)))
                    {
                        return false; 
                    }
                    
                    if (!(EqualityComparer<TValue>.Default.Equals(kv.Value, otherValue)))
                    {
                        return false;
                    }
                }
                
                foreach (var kv in other)
                {
                    if(!(this.TryGetValue(kv.Key, out var otherValue)))
                    {
                        return false; 
                    }
                    
                    if (!(EqualityComparer<TValue>.Default.Equals(kv.Value, otherValue)))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public void Append(KeyValuePair<TKey, TValue> value)
        {
            var val = value.Value;
            var key = value.Key;
            
            Insert(ref key, ref val, true);
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
            
            return EqualsDict((HybridDict<TKey, TValue>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 7 ^ m_count.GetHashCode();

                foreach (var item in this)
                {
                    hashCode = (hashCode * 397) ^ EqualityComparer<TValue>.Default.GetHashCode(item.Value) ^ EqualityComparer<TKey>.Default.GetHashCode(item.Key);
                }
              
                return hashCode;
            }
        }
    
    
        [Serializable]
        public class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
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
                        m_current = new KeyValuePair<TKey, TValue>(m_dictionary.m_entries[m_index].Key, m_dictionary.m_entryValues[m_dictionary.m_entries[m_index].ValueRef]);
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

        [Serializable]
        public class ArrayEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly HybridDict<TKey, TValue> m_dictionary;
            private readonly Entry[] m_entries;
            private readonly TValue[] m_values;
            private readonly int m_version;
            private int m_index;
            private KeyValuePair<TKey, TValue> m_current;

            internal ArrayEnumerator(HybridDict<TKey, TValue> dictionary)
            {
                m_entries = ((HybridList<Entry>.StoreNode) dictionary.m_entries.m_root).m_items;
                m_values = ((HybridList<TValue>.StoreNode) dictionary.m_entryValues.m_root).m_items;
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_current = default(KeyValuePair<TKey, TValue>);
            }
            
            public KeyValuePair<TKey, TValue> Current
            {
                get { return m_current; }
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
                    if (m_entries[m_index].HashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(m_entries[m_index].Key, m_values[m_entries[m_index].ValueRef]);
                        m_index++;
                        return true;
                    }

                    m_index++;
                }

                m_index = m_dictionary.m_count + 1;
                m_current = default(KeyValuePair<TKey, TValue>);
                return false;
            }

            public void Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_current = default(KeyValuePair<TKey, TValue>);
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
                if (ReferenceEquals(dictionary, null))
                {
                    throw new ArgumentNullException("dictionary");
                }
                m_dictionary = dictionary;
            }

            public void CopyTo(TKey[] array, int index)
            {
                if (ReferenceEquals(array , null))
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
                if (m_dictionary.m_entries?.m_root is HybridList<Entry>.StoreNode)
                {
                    return new ArrayEnumerator(m_dictionary);
                }
                
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
            
            [Serializable]
            public struct ArrayEnumerator : IEnumerator<TKey>
            {
                private readonly HybridDict<TKey, TValue> m_dictionary;
                private readonly Entry[] m_entries;
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

                internal ArrayEnumerator(HybridDict<TKey, TValue> dictionary)
                {
                    m_entries = ((HybridList<Entry>.StoreNode)dictionary.m_entries.m_root).m_items;
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
                        if (m_entries[m_index].HashCode >= 0)
                        {
                            m_currentKey = m_entries[m_index].Key;
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
                m_dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (ReferenceEquals(array, null))
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
                IList<TValue> entryValues = m_dictionary.m_entryValues;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].HashCode >= 0)
                    {
                        array[index++] = entryValues[entries[i].ValueRef];
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
                if (m_dictionary.m_entries?.m_root is HybridList<Entry>.StoreNode)
                {
                    return new ArrayEnumerator(m_dictionary);
                }
                
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
                            m_currentValue = m_dictionary.m_entryValues[m_dictionary.m_entries[m_index].ValueRef];
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
            
            [Serializable]
            public struct ArrayEnumerator : IEnumerator<TValue>
            {
                private readonly HybridDict<TKey, TValue> m_dictionary;
                private readonly Entry[] m_entries;
                private readonly int m_version;
                private int m_index;
                private TValue m_currentValue;
                private readonly TValue[] m_values;

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

                internal ArrayEnumerator(HybridDict<TKey, TValue> dictionary)
                {
                    m_entries = ((HybridList<Entry>.StoreNode)dictionary.m_entries.m_root).m_items;
                    m_values = ((HybridList<TValue>.StoreNode)dictionary.m_entryValues.m_root).m_items;
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
                        if (m_entries[m_index].HashCode >= 0)
                        {
                            m_currentValue = m_values[m_entries[m_index].ValueRef];
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
            public Entry(int hashCode, int next, TKey key, int value)
            {
                HashCode = hashCode;
                Next = next;
                Key = key;
                ValueRef = value;
            }

            public int HashCode;
            public int Next;
            public TKey Key;
            public int ValueRef;
        }

        private IEqualityComparer<TKey> m_comparer;
        private HybridList<int> m_buckets;
        private HybridList<Entry> m_entries;
        private HybridList<TValue> m_entryValues;
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

        public HybridDict(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public HybridDict(int capacity)
            : this(capacity, null)
        {
        }

        public HybridDict(IReadOnlyDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }
        
        public HybridDict(HybridDict<TKey, TValue> dictionary)
        {
            m_comparer = dictionary.m_comparer;

            m_buckets = new (dictionary.m_buckets);
            m_entries = new (dictionary.m_entries);
            m_entryValues = new (dictionary.m_entryValues);
            
            m_count = dictionary.m_count;
            m_freeCount = dictionary.m_freeCount;
            m_freeList = dictionary.m_freeList;
            m_version = dictionary.m_version;
        }

        public HybridDict(IReadOnlyDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
                    : this((dictionary != null) ? dictionary.Count : 0, comparer)
        {
            if (ReferenceEquals(dictionary, null))
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
            Insert(ref key, ref value, true);
        }
        
        public void Add(KeyValuePair<TKey, TValue> value)
        {
            var val = value.Value;
            var key = value.Key;
            
            Insert(ref key, ref val, true);
        }

        public void Clear()
        {
            if (m_count <= 0)
            {
                return;
            }

            m_buckets?.DisposeList();
            m_entries?.DisposeList();
            m_entryValues?.DisposeList();

            m_buckets = null;
            m_entries = null;
            m_entryValues = null;

            m_freeList = -1;
            m_count = 0;
            m_freeCount = 0;
            m_version++;
        }

        public bool ContainsKey(TKey key)
        {
            ValueByRef(key, out var found);
            
            return found;
        }

        public bool MissingKey(TKey key)
        {
            ValueByRef(key, out var found);
            
            return !found;
        }

        public bool ContainsValue(TValue value)
        {
            if (m_entries.m_root is HybridList<Entry>.StoreNode storeNode)
            {
                var valuesRoot = (HybridList<TValue>.StoreNode)m_entryValues.m_root;

                if (value == null)
                {
                    for (int i = 0; i < m_count && i < storeNode.m_items.Length; i++)
                    {
                        ref var storeNodeItem = ref storeNode.m_items[i];
                        if ((storeNodeItem.HashCode >= 0))
                        {
                                
                            if(valuesRoot.m_items[storeNodeItem.ValueRef] == null)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    var comparer = EqualityComparer<TValue>.Default;
                    for (int j = 0; j < m_count && j < storeNode.m_items.Length; j++)
                    {
                        ref var storeNodeItem = ref storeNode.m_items[j];
                        if (storeNodeItem.HashCode >= 0)
                        {
                            if (comparer.Equals(valuesRoot.m_items[storeNodeItem.ValueRef], value))
                            {
                                return true;
                            }
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
                        ref Entry entry = ref m_entries.ValueByRef(i);
                        if (entry.HashCode >= 0)
                        {
                            if (m_entryValues.ValueByRef(entry.ValueRef) == null)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    var comparer = EqualityComparer<TValue>.Default;
                    for (int j = 0; j < m_count; j++)
                    {
                        ref Entry entry = ref m_entries.ValueByRef(j);
                        if (entry.HashCode >= 0)
                        {
                            if ( comparer.Equals(m_entryValues.ValueByRef(entry.ValueRef), value))
                            {
                                return true;
                            }
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
            var entries = m_entries;
            var values = m_entryValues;
            for (int i = 0; i < count; i++)
            {
                ref Entry entry = ref entries.ValueByRef(i);
                if (entry.HashCode >= 0)
                {
                    ref TValue val = ref values.ValueByRef(entry.ValueRef);
                    
                    destination[index++] = new KeyValuePair<TKey, TValue>(entry.Key, val);
                }
            }
        }

        public ref TValue ValueByRef(TKey key, out bool success)
        {
            success = false;
            
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
                        ref var storeNodeItem = ref storeNode.m_items[i];
                        if ((storeNodeItem.HashCode == num) && m_comparer.Equals(storeNodeItem.Key, key))
                        {
                            success = true;

                            var valuesRoot = (HybridList<TValue>.StoreNode)m_entryValues.m_root;

                            return ref valuesRoot.m_items[storeNodeItem.ValueRef];
                        }
                    }
                }
                else
                {
                    int num = m_comparer.GetHashCode(key) & 0x7fffffff;
                    for (int i = m_buckets[num % m_buckets.Count] - 1; i >= 0; )
                    {
                        ref var currentEntry = ref m_entries.ValueByRef(i);
                        if ((currentEntry.HashCode == num) && m_comparer.Equals(currentEntry.Key, key))
                        {
                            success = true;
                            
                            return ref m_entryValues.ValueByRef(currentEntry.ValueRef);
                        }

                        i = currentEntry.Next;
                    }
                }
            }

            return ref m_nullRef;
        }

        private TValue m_nullRef;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (m_entries?.m_root is HybridList<Entry>.StoreNode)
            {
                return new ArrayEnumerator((HybridDict<TKey, TValue>)this);
            }
            
            return new Enumerator((HybridDict<TKey, TValue>)this);
        }

        private void Initialize(int capacity)
        {
            int prime = Prime.GetPrime(capacity);

            m_buckets = new ();
            m_entries = new ();
            m_entryValues = new ();

            m_buckets.Ensure(prime);
            m_entries.Ensure(prime);
            m_entryValues.Ensure(prime);

            m_freeList = -1;
        }

        private void Insert(ref TKey key, ref TValue value, bool add)
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
                var valuesRoot = (HybridList<TValue>.StoreNode)m_entryValues.m_root;

                for (int i = m_buckets[index] - 1; i >= 0; i = storeNode.m_items[i].Next)
                {
                    ref Entry storeNodeItem = ref storeNode.m_items[i];
                    
                    if ((storeNodeItem.HashCode == hashCode) && m_comparer.Equals(storeNodeItem.Key, key))
                    {
                        if (add)
                        {
                            throw new ArgumentException($"Key '{key}' is already exists.");
                        }
                        
                        valuesRoot.m_items[storeNodeItem.ValueRef] = value;
                        
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
                        if (add)
                        {
                            throw new ArgumentException($"Key '{key}' is already exists.");
                        }

                        m_entryValues.ValueByRef(currentEntry.ValueRef) = value;
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
            m_entries[freeList] = new Entry(hashCode, m_buckets[index] - 1, key, freeList);
            m_entryValues[freeList] = value;
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
                            m_entries.ValueByRef(last).Next = currentEntry.Next;
                        }
                        m_entries[i] = new Entry(-1, m_freeList, default(TKey), 0);
                        m_entryValues[i] = default;
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
            HybridList<int> newBuckets = new ();
            HybridList<Entry> newEntries = new ();
            HybridList<TValue> newEntryValues = new ();
            
            newEntries.Ensure(prime);
            newEntryValues.Ensure(prime);
            newBuckets.Ensure(prime);

            if (m_entries.m_root is HybridList<Entry>.StoreNode storeNode)
            {
                var valuesStoreNode = (HybridList<TValue>.StoreNode)m_entryValues.m_root;
                
                for (var i = 0; i < m_entries.m_count && i < storeNode.m_items.Length; i++)
                {
                    ref var entry = ref storeNode.m_items[i];
                    ref var entryValue = ref valuesStoreNode.m_items[entry.ValueRef];
                    
                    entry.ValueRef = i;

                    if (entry.HashCode >= 0)
                    {
                        var bucket = entry.HashCode % prime;
                        entry.Next = newBuckets[bucket] - 1;
                        newBuckets[bucket] = i + 1;
                    }
                    
                    newEntries.ValueByRef(i) = entry;
                    newEntryValues.ValueByRef(i) = entryValue;
                }
            }
            else
            {
                for (var i = 0; i < m_entries.m_count; i++)
                {
                    ref var entry = ref m_entries.ValueByRef(i);
                    ref var entryValue = ref m_entryValues.ValueByRef(entry.ValueRef);
                    
                    entry.ValueRef = i;
                    
                    if (entry.HashCode >= 0)
                    {
                        var bucket = entry.HashCode % prime;

                        entry.Next = newBuckets[bucket] - 1;
                        
                        newBuckets[bucket] = i + 1;
                    }
                    
                    newEntries.ValueByRef(i) = entry;
                    newEntryValues.ValueByRef(i) = entryValue;
                }
            }

            m_buckets?.DisposeList();
            m_entries?.DisposeList();
            m_entryValues?.DisposeList();

            m_buckets = newBuckets;
            m_entries = newEntries;
            m_entryValues = newEntryValues;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
        {
            var val = ValueByRef(pair.Key, out var found);
            
            return (found && EqualityComparer<TValue>.Default.Equals(val, pair.Value));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            CopyTo(array, index);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
        {
            var value = ValueByRef(pair.Key, out var found);
            
            if (found && EqualityComparer<TValue>.Default.Equals(value, pair.Value))
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
            value = ValueByRef(key, out var found);
            if (found)
            {
                return true;
            }
            value = default;
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
                var value = ValueByRef(key, out var found);

                if (found)
                {
                    return value;
                }
                throw new KeyNotFoundException($"Key '{key}' is not found.");
            }
            set
            {
                Insert(ref key, ref value, false);
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
        public static TSource GetOrDefault<TSource, TKey>([NotNull] this IReadOnlyDictionary<TKey, TSource> dict, TKey key, TSource defaultVal = default(TSource))
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultVal;
        }
        
        public static TSource GetOrAdd<TSource, TKey>([NotNull] this IDictionary<TKey, TSource> dict, TKey key) where TSource : ICollection, new()
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            
            if (!dict.TryGetValue(key, out var value))
            {
                TSource defaultVal;

                dict[key] = defaultVal = new TSource();
                
                return defaultVal;
            }

            return value;
        }
        
        public static TSource GetOrAdd<TSource, TKey>([NotNull] this IDictionary<TKey, TSource> dict, TKey key, Func<TSource> valueFactory)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
            
            if (!dict.TryGetValue(key, out var value))
            {
                TSource defaultVal;

                dict[key] = defaultVal = valueFactory();
                
                return defaultVal;
            }

            return value;
        }
        
         public static HybridDict<TKey, TSource> ToHybridDict<TSource, TKey>([NotNull] this IReadOnlyDictionary<TKey, TSource> source)
         {
             if (ReferenceEquals(source, null)) 
             {
                 return null; 
             }
             
             if (source is HybridDict<TKey, TSource> hd) 
             {
                 return new HybridDict<TKey, TSource>(hd); 
             }
             
             return new HybridDict<TKey, TSource>(source); 
         }
    
        public static HybridDict<TKey, TSource> ToHybridDict<TSource, TKey>([NotNull] this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
                   ToHybridDict(source, keySelector, null);
       
       public static HybridDict<TKey, TSource> ToHybridDict<TSource, TKey>([NotNull] this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
       {
           if (ReferenceEquals(source, null))
           {
               return null;
           }
           
           if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

           int capacity = 0;
           if (source is ICollection<TSource> collection)
           {
               capacity = collection.Count;
               if (capacity == 0)
               {
                   return new HybridDict<TKey, TSource>(comparer);
               }

               if (collection is TSource[] array)
               {
                   return ToHybridDict(array, keySelector, comparer);
               }

               if (collection is List<TSource> list)
               {
                   return ToHybridDict(list, keySelector, comparer);
               }
           }

           var d = new HybridDict<TKey, TSource>(capacity, comparer);
           foreach (TSource element in source)
           {
               d.Add(keySelector(element), element);
           }

           return d;
       }

       private static HybridDict<TKey, TSource> ToHybridDict<TSource, TKey>([NotNull] TSource[] source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] IEqualityComparer<TKey> comparer)
       {
           HybridDict<TKey, TSource> d = new HybridDict<TKey, TSource>(source.Length, comparer);
           for (int i = 0; i < source.Length; i++)
           {
               d.Add(keySelector(source[i]), source[i]);
           }

           return d;
       }

       private static HybridDict<TKey, TSource> ToHybridDict<TSource, TKey>([NotNull] List<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] IEqualityComparer<TKey> comparer)
       {
           HybridDict<TKey, TSource> d = new HybridDict<TKey, TSource>(source.Count, comparer);
           foreach (TSource element in source)
           {
               d.Add(keySelector(element), element);
           }

           return d;
       }

       public static HybridDict<TKey, TElement> ToHybridDict<TSource, TKey, TElement>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] Func<TSource, TElement> elementSelector) =>
           ToHybridDict(source, keySelector, elementSelector, null);

       public static HybridDict<TKey, TElement> ToHybridDict<TSource, TKey, TElement>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
       {
           if (ReferenceEquals(source, null))
           {
               return null;
           }

           int capacity = 0;
           if (source is ICollection<TSource> collection)
           {
               capacity = collection.Count;
               if (capacity == 0)
               {
                   return new HybridDict<TKey, TElement>(comparer);
               }

               if (collection is TSource[] array)
               {
                   return ToHybridDict(array, keySelector, elementSelector, comparer);
               }

               if (collection is List<TSource> list)
               {
                   return ToHybridDict(list, keySelector, elementSelector, comparer);
               }
           }

           HybridDict<TKey, TElement> d = new HybridDict<TKey, TElement>(capacity, comparer);
           foreach (TSource element in source)
           {
               d.Add(keySelector(element), elementSelector(element));
           }

           return d;
       }

       private static HybridDict<TKey, TElement> ToHybridDict<TSource, TKey, TElement>([NotNull] TSource[] source, [NotNull] Func<TSource, TKey> keySelector,[NotNull]  Func<TSource, TElement> elementSelector, [NotNull] IEqualityComparer<TKey> comparer)
       {
           HybridDict<TKey, TElement> d = new HybridDict<TKey, TElement>(source.Length, comparer);
           for (int i = 0; i < source.Length; i++)
           {
               d.Add(keySelector(source[i]), elementSelector(source[i]));
           }

           return d;
       }

       private static HybridDict<TKey, TElement> ToHybridDict<TSource, TKey, TElement>([NotNull] List<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] Func<TSource, TElement> elementSelector, [NotNull] IEqualityComparer<TKey> comparer)
       {
           HybridDict<TKey, TElement> d = new HybridDict<TKey, TElement>(source.Count, comparer);
           foreach (TSource element in source)
           {
               d.Add(keySelector(element), elementSelector(element));
           }

           return d;
       }
    
       public static void DisposeDict<T,V>([CanBeNull] this HybridDict<T, V> dict)
        {
            if (ReferenceEquals(dict, null))
            {
                return;
            }

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
