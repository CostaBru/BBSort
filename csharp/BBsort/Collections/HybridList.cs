using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;


namespace Flexols.Data.Collections
{
    internal static class Tool
    {
        public static void Swap<T>(ref T var1, ref T var2)
        {
            var _var3 = var1;
            var1 = var2;
            var2 = _var3;
        }
    }

    [DebuggerDisplay("Count {m_count}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public class HybridList<T> : IList<T>, IReadOnlyList<T>, ICollection, IList, IDisposable
    {
        public static readonly T Default = default(T);

        public const int SmallListCount = 2;

        private static MethodInfo GetSizeOfMethod<T>()
        {
            var assemblyName = new AssemblyName() { Name = "Data.Collections.Internal" };

            var typeBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(
                    "HybridList").DefineType("HybridList", TypeAttributes.Public);

            var methodBuilder = typeBuilder.DefineMethod("SizeOf", MethodAttributes.Public | MethodAttributes.Static, typeof(int), null);
            var strArray = new string[1] { "T" };
            var parameterBuilderArray = methodBuilder.DefineGenericParameters(strArray);

            var ilGenerator = methodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Sizeof, parameterBuilderArray[0]);
            ilGenerator.Emit(OpCodes.Ret);

            return typeBuilder.CreateTypeInfo().GetMethod("SizeOf").MakeGenericMethod(typeof(T));
        }

        public static int GetObjectSize<T>()
        {
            var type = typeof(T);

            if (type.IsValueType == false)
            {
                return IntPtr.Size;
            }

            return (int)GetSizeOfMethod<T>().Invoke(null, null); 
        }
       
        public static int GetSmallHeapSuitableLength<T>()
        {
            return GetSmallHeapSuitableLength<T>(false);
        }
     
        public static int GetSmallHeapSuitableLength<T>(bool toPowerOf2)
        {
            int size = 80000 / GetObjectSize<T>();
            if (toPowerOf2)
            {
                int val = 1;
                while (val <= size)
                {
                    val *= 2;
                }
                size = val / 2;
            }
            return size;
        }

        private static readonly int s_maxSizeOfArray;

        static HybridList()
        {
            var capacity = GetSmallHeapSuitableLength<T>();

            s_maxSizeOfArray = 1 << (int)Math.Log(capacity, 2.0);
        }

        private const int c_minCapacity = 2;
        private readonly int m_intialCapacity;

        public INode GetRoot()
        {
            return m_root;
        }

        protected internal INode m_root;
        protected internal int m_count;

        private T m_val0;
        private T m_val1;

        private bool m_isDisposed;
        private object m_syncRoot;
        private int m_version;

        void ICollection.CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        public int Count
        {
            get
            {
                return m_count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (this.m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), (object)null);
                }
                return this.m_syncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        T IList<T>.this[int index]
        {
            get
            {
                return ValueByRef(index);
            }
            set
            {
                ValueByRef(index) = value;
            }
        }

        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                return ValueByRef(index);
            }
        }

        public virtual T this[int index]
        {
            get
            {
                return ValueByRef(index);
            }
            set
            {
                ValueByRef(index) = value;
            }
        }

        public ref T ValueByRef(int index)
        {
            var simpleNode = m_root as StoreNode;

            if (simpleNode != null)
            {
                if (index >= simpleNode.m_size)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref simpleNode.m_items[index];
            }

            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        throw new IndexOutOfRangeException();

                    case 1:

                        if (index == 0)
                        {
                            return ref m_val0;
                        }

                        throw new IndexOutOfRangeException();
                    case 2:
                    {
                        switch (index)
                        {
                            case 0:
                                return ref m_val0;
                            case 1:
                                return ref m_val1;
                        }

                        throw new IndexOutOfRangeException();
                    }
                }

                throw new IndexOutOfRangeException();
            }
            else
            {
                return ref m_root[index];
            }
        }

        public HybridList()
        {
            m_intialCapacity = SmallListCount;
        }
      
        public HybridList(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            m_intialCapacity = 1 << (int)Math.Log(capacity < c_minCapacity ? 2.0 : (capacity > s_maxSizeOfArray ? s_maxSizeOfArray : capacity), 2.0);

            if (m_intialCapacity > SmallListCount)
            {
                m_root = new StoreNode(s_maxSizeOfArray, m_intialCapacity);
            }
        }
        
        public HybridList(IEnumerable<T> enumerable)
        {
            if (enumerable is HybridList<T> hybridList)
            {
                m_intialCapacity = hybridList.Count;

                CreateFromHybridList(hybridList);
            }
            else if (enumerable is IList<T> list)
            {
                var capacity = list.Count;

                m_intialCapacity = 1 << (int)Math.Log(capacity < c_minCapacity ? 2.0 : (capacity > s_maxSizeOfArray ? s_maxSizeOfArray : capacity), 2.0);

                if (m_intialCapacity > SmallListCount)
                {
                    m_root = new StoreNode(s_maxSizeOfArray, m_intialCapacity);
                }

                AddRange(list);
            }
            else
            {
                m_intialCapacity = s_maxSizeOfArray;

                AddRange(enumerable);
            }
        }

        public HybridList([NotNull] HybridList<T> source)
        {
            m_intialCapacity = source.Count;

            CreateFromHybridList(source);
        }

        private void CreateFromHybridList(HybridList<T> source)
        {
            if (source.m_root != null)
            {
                if (source.m_root is StoreNode simpleNode)
                {
                    m_root = new StoreNode(simpleNode);

                    m_count = source.m_count;
                }
                else if (source.m_root is LinkNode linkNode)
                {
                    m_root = new LinkNode(linkNode);

                    m_count = source.m_count;
                }
            }
            else
            {
                m_val0 = source.m_val0;
                m_val1 = source.m_val1;

                m_count = source.m_count;
            }
        }

        public void Reverse()
        {
            //common case
            if (m_root is StoreNode simpleNode)
            {
                Array.Reverse(simpleNode.m_items, 0, m_count);

                return;
            }

            for (int upTo = 0; upTo < m_count; upTo++)
            {
                var downTo = m_count - upTo - 1;

                if (downTo + 1 <= upTo)
                {
                    break;
                }

                var uptoValue = ValueByRef(upTo);

                ValueByRef(upTo) = ValueByRef(downTo);

                ValueByRef(downTo) = uptoValue;
            }
        }

        internal int BinarySearchCore(T item, int startIndex, int count, IComparer<T> comparer)
        {
            //common case
            if (m_root is StoreNode storeNode)
            {
                int lo = startIndex;
                int hi = count - 1;

                while (lo <= hi)
                {
                    int index = lo + (hi - lo >> 1);

                    int order = comparer.Compare(item, storeNode[index]);

                    if (order == 0)
                    {
                        return index;
                    }

                    if (order > 0)
                    {
                        lo = index + 1;
                    }
                    else
                    {
                        hi = index - 1;
                    }
                }
                return ~lo;
            }
            else
            {
                int lo = startIndex;
                int hi = count - 1;

                while (lo <= hi)
                {
                    int index = lo + (hi - lo >> 1);

                    int order = comparer.Compare(item, ValueByRef(index));

                    if (order == 0)
                    {
                        return index;
                    }

                    if (order > 0)
                    {
                        lo = index + 1;
                    }
                    else
                    {
                        hi = index - 1;
                    }
                }
                return ~lo;
            }
        }

        public int BinarySearch(T value, int startIndex, int count, IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            if (startIndex < 0 || startIndex >= m_count)
            {
                return -1;
            }

            if (count < 0 || count > m_count)
            {
                return -1;
            }

            if (startIndex >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            return BinarySearchCore(value, startIndex, count, comparer);
        }

        public int BinarySearch(T value, int startIndex, int count)
        {
            return BinarySearch(value, startIndex, count, Comparer<T>.Default);
        }

        // ReSharper disable once UnusedMember.Global
        public virtual int BinarySearch(T value)
        {
            var startIndex = 0;
            var endIndex = m_count - 1;

            return BinarySearchCore(value, startIndex, endIndex, Comparer<T>.Default);
        }

        public int IndexOf(T item, int startIndex = 0)
        {
            if (startIndex < 0)
            {
                throw new ArgumentException(nameof(startIndex));
            }

            if (startIndex >= m_count)
            {
                return -1;
            }

            var comparer = EqualityComparer<T>.Default;

            //common case
            if (m_root is StoreNode simpleNode)
            {
                var nodeItems = simpleNode.m_items;

                for (int index = startIndex; index < simpleNode.m_size && index < nodeItems.Length; ++index)
                {
                    if (comparer.Equals(nodeItems[index], item))
                    {
                        return index;
                    }
                }

                return -1;
            }

            int num = 0;

            for (int i = startIndex; i < m_count; i++)
            {
                if (comparer.Equals(ValueByRef(i), item))
                {
                    return num;
                }

                ++num;
            }

            return -1;
        }

        public int IndexOf(T item)
        {
            return IndexOf(item, 0);
        }

        public virtual void Insert(int index, T item)
        {
            if (m_root is StoreNode node)
            {
                //inlined method StoreNode.Insert 
                if (node.m_items == null)
                {
                    T[] vals = ArrayPool<T>.Shared.Rent(Math.Max(4, node.m_size * 2));

                    Array.Clear(vals, 0, vals.Length);

                    node.m_items = vals;
                }
                else  if (node.m_size < node.m_maxCapacity - 1)
                {
                    if (node.m_size == node.m_items.Length)
                    {
                        int newCapacity = Math.Min(node.m_items.Length == 0 ? 4 : node.m_items.Length * 2, node.m_maxCapacity);

                        T[] vals = ArrayPool<T>.Shared.Rent(newCapacity);

                        Array.Clear(vals, 0, vals.Length);

                        if (node.m_size > 0)
                        {
                            Array.Copy(node.m_items, 0, vals, 0, node.m_size);
                        }

                        ArrayPool<T>.Shared.Return(node.m_items, clearArray: true);

                        node.m_items = vals;
                    }

                    if (index < node.m_size)
                    {
                        Array.Copy(node.m_items, index, node.m_items, index + 1, node.m_size - index);
                    }

                    node.m_items[index] = item;
                    node.m_size = node.m_size + 1;


                    m_count++;
                    m_version++;
                    return;
                }
            }

            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        if (index == 0)
                        {
                            Add(item);
                        }
                        break;
                    case 1:
                        switch (index)
                        {
                            case 0:
                                {
                                    Tool.Swap(ref m_val0, ref item);
                                    Add(item);
                                    break;
                                }
                            case 1:
                                {
                                    Add(item);
                                    break;
                                }
                            default: throw new IndexOutOfRangeException();
                        }
                        break;
                    case 2:
                        switch (index)
                        {
                            case 0:
                                Tool.Swap(ref m_val0, ref item);
                                Tool.Swap(ref m_val1, ref item);
                                Add(item);
                                break;
                            case 1:
                                Tool.Swap(ref m_val1, ref item);
                                Add(item);
                                break;
                            case 2:
                                Add(item);
                                break;
                            default: throw new IndexOutOfRangeException();
                        }
                        break;
                }
            }
            else
            {
                T current = item;

                for (int i = index; i < m_count; ++i)
                {
                    T toMove = ValueByRef(i);
                    ValueByRef(i) = current;
                    current = toMove;
                }

                Add(current);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T obj in items)
            {
                Add(obj);
            }
        }

        public virtual void Add(T item)
        {
            if (m_root is StoreNode node)
            {
                //inlined method StoreNode.Add 
                if (node.m_size < node.m_maxCapacity)
                {
                    if (node.m_items == null)
                    {
                        node.m_items = ArrayPool<T>.Shared.Rent(Math.Max(4, node.m_size * 2));

                        Array.Clear(node.m_items, 0, node.m_items.Length);
                    }
                    else if (node.m_size == node.m_items.Length)
                    {
                        int newCapacity = Math.Min(node.m_items.Length == 0 ? 4 : node.m_items.Length * 2, node.m_maxCapacity);
                            
                        T[] vals = ArrayPool<T>.Shared.Rent(newCapacity);

                        Array.Clear(vals, 0, vals.Length);

                        if (node.m_size > 0)
                        {
                            Array.Copy(node.m_items, 0, vals, 0, node.m_size);
                        }

                        ArrayPool<T>.Shared.Return(node.m_items, clearArray: true);

                        node.m_items = vals;
                    }

                    node.m_items[node.m_size] = item;

                    node.m_size++;

                    ++m_version;
                    ++m_count;
                    return;
                }
            }

            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        m_val0 = item;
                        ++m_version;
                        ++m_count;
                        return;
                    case 1:
                        m_val1 = item;
                        ++m_version;
                        ++m_count;
                        return;
                    default:
                        {
                            var storeNode = new StoreNode(s_maxSizeOfArray, SmallListCount * 2);

                            storeNode.m_items[0] = m_val0;
                            storeNode.m_items[1] = m_val1;

                            storeNode.m_items[SmallListCount] = item;

                            storeNode.m_size = SmallListCount + 1;

                            m_val0 = Default;
                            m_val1 = Default;

                            m_root = storeNode;

                            ++m_version;
                            ++m_count;
                            return;
                        }
                }
            }

            if (m_root != null)
            {
                INode node1 = m_root;
                INode node2;
                if (!node1.Add(item, out node2))
                {
                    m_root = new LinkNode(node1.Level + 1, s_maxSizeOfArray, node1, node2);
                }
            }

            ++m_version;
            ++m_count;
        }

        public void Ensure(int size, T defaultValue = default(T))
        {
            if (m_count > size)
            {
                return;
            }


            if (m_root == null && size <= SmallListCount)
            {
                switch (m_count)
                {
                    case 0:
                        {
                            m_val0 = defaultValue;
                            m_val1 = defaultValue;

                            m_count = size;
                            ++m_version;
                            return;
                        }
                    case 1:

                        m_val1 = defaultValue;

                        m_count = size;
                        ++m_version;
                        return;
                    case 2:

                        m_count = size;
                        return;
                }
            }
            else
            {
                if (m_root == null)
                {
                    //common case
                    var storeNode = new StoreNode(s_maxSizeOfArray, size);

                    if (m_count > 0)
                    {
                        storeNode.m_items[0] = m_val0;
                        storeNode.m_items[1] = m_val1;

                        m_val0 = Default;
                        m_val1 = Default;
                    }

                    storeNode.m_size = Math.Min(s_maxSizeOfArray, size);

                    m_count = storeNode.m_size;
                    ++m_version;

                    m_root = storeNode;

                    var setupDefaultValueForArray = EqualityComparer<T>.Default.Equals(defaultValue, Default) == false;

                    if (setupDefaultValueForArray)
                    {
                        for (int i = SmallListCount; i < storeNode.m_items.Length; i++)
                        {
                            storeNode.m_items[i] = defaultValue;
                        }
                    }

                    //not common case
                    if (size > storeNode.m_items.Length)
                    {
                        var toAdd = size - storeNode.m_items.Length;

                        for (int i = 0; i < toAdd; i++)
                        {
                            Add(defaultValue);
                        }
                    }


                    return;
                }
            }

            var toAddDefault = size - m_count;

            for (int i = 0; i < toAddDefault; i++)
            {
                Add(defaultValue);
            }
        }

        int IList.Add(object value)
        {
            Add((T)value);

            return m_count - 1;
        }

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        public void Clear()
        {
            if (m_count == 0)
            {
                return;
            }

            m_root?.Clear();
            m_count = 0;
            ++m_version;

            m_root = null;

            m_val0 = Default;
            m_val1 = Default;
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            Remove((T)value);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int num = arrayIndex;

            if (m_root is StoreNode storeNode)
            {
                Array.Copy(storeNode.m_items, 0, array, num, storeNode.m_size);

                return;
            }

            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        break;
                    case 1:
                        {
                            array[num++] = m_val0;
                            return;
                        }
                    case 2:
                        {
                            array[num++] = m_val0;
                            array[num++] = m_val1;
                            return;
                        }
                    default:
                        {
                            throw new IndexOutOfRangeException();
                        }
                }
            }
            else
            {
                for (int i = 0; i < m_count; i++)
                {
                    array[num++] = m_root[i];
                }
            }
        }

        public bool Remove(T item)
        {
            var indexOf = IndexOf(item);

            if (indexOf < 0)
            {
                return false;
            }

            RemoveAt(indexOf);

            return true;
        }

        public int RemoveAll<V>(V value, Func<T, V> valueSelector)
        {
            return RemoveAll<V>(value, valueSelector, EqualityComparer<V>.Default);
        }

        public int RemoveAll<V>(V value, Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            int counter = 0;
            int matchedIndex;
            do
            {
                matchedIndex = -1;

                for (int i = 0; i < m_count; i++)
                {
                    var listItem = ValueByRef(i);

                    var selector = valueSelector(listItem);

                    if (equalityComparer.Equals(selector, value))
                    {
                        matchedIndex = i;
                        break;
                    }
                }

                if (matchedIndex >= 0)
                {
                    RemoveAt(matchedIndex);
                    counter++;
                }
            }
            while (m_count > 0 && matchedIndex >= 0);

            return counter;
        }

        public int RemoveAll(T item)
        {
            return RemoveAll(item, Comparer<T>.Default);
        }

        public int RemoveAll(T item, IComparer<T> comparer)
        {
            if (m_root is StoreNode storeNode)
            {
                return storeNode.RemoveAll(item, comparer);
            }
            else
            {
                int counter = 0;
                int matchedIndex;
                do
                {
                    matchedIndex = -1;

                    for (int i = 0; i < m_count; i++)
                    {
                        var listItem = this.ValueByRef(i);

                        if (comparer.Compare(listItem, item) == 0)
                        {
                            matchedIndex = i;
                            break;
                        }
                    }

                    if (matchedIndex >= 0)
                    {
                        RemoveAt(matchedIndex);
                        counter++;
                    }
                }
                while (m_count > 0 && matchedIndex >= 0);

                return counter;
            }
        }

        public void RemoveAt(int index)
        {
            if (m_root is StoreNode simpleNode)
            {
                simpleNode.RemoveAt(index);

                m_count--;
                ++m_version;

                if (m_count == 0)
                {
                    m_root = null;
                }

                return;
            }

            if (m_root == null)
            {
                switch (index)
                {
                    case 0: m_val0 = m_val1; m_val1 = Default;  break;
                    case 1: m_val1 = Default; break;
                 
                    default: throw new IndexOutOfRangeException();
                }

                m_count--;
                ++m_version;
            }
            else
            {
                for (int i = index + 1; i < m_count; ++i)
                {
                    ValueByRef(i - 1) = ValueByRef(i);
                }

                RemoveLast();
            }
        }

        object IList.this[int index]
        {
            get
            {
                return ValueByRef(index);
            }
            set
            {
                var typedValue = (T)value;

                ValueByRef(index) = typedValue;
            }
        }

        private void RemoveLast()
        {
            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        break;
                    case 1:
                        m_val0 = Default;
                        break;
                    case 2:
                        m_val1 = Default;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                var isEmpty = m_root.RemoveLast();

                if (isEmpty)
                {
                    m_root = null;
                }
            }

            ++m_version;
            m_count--;
        }

        public void Sort()
        {
            Sort(this);
        }

        public void Sort(IComparer<T> comparer)
        {
            Sort(this, comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            Sort(this, comparison);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var version = m_version;

            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0: yield break;

                    case 1: yield return m_val0; break;
                    case 2: yield return m_val0; CheckState(ref version); yield return m_val1; break;
                 }
            }
            else
            {
                var enumerator = m_root.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    CheckState(ref version);

                    yield return enumerator.Current;
                }

                enumerator.Dispose();
            }
        }

        private void CheckState(ref int version)
        {
            if (version != m_version)
            {
                throw new InvalidOperationException();
            }
        }

        public bool HasList
        {
            get
            {
                return m_root != null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static void Sort(HybridList<T> list)
        {
            if (list.Count == 0)
            {
                return;
            }
            Sort(list, Comparer<T>.Default.Compare);
        }

        public static void Sort(HybridList<T> list, IComparer<T> comparer)
        {
            if (list.Count == 0)
            {
                return;
            }
            Sort(list, 0, list.Count - 1, comparer.Compare);
        }

        private class Comparer : IComparer<T>
        {
            private readonly Comparison<T> m_comparison;

            public Comparer(Comparison<T> comparison)
            {
                m_comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return m_comparison(x, y);
            }
        }

        public static void Sort(HybridList<T> list, Comparison<T> comparison)
        {
            if (list.Count < 2)
            {
                return;
            }

            if (list.m_root is StoreNode storeNode)
            {
                Array.Sort(storeNode.m_items, 0, storeNode.m_size, new Comparer(comparison));

                ++list.m_version;
                return;
            }

            if (list.m_root == null && list.m_count <= 2)
            {
                switch (list.m_count)
                {
                    case 2:
                    {
                        if (comparison(list.m_val0 , list.m_val1) > 0)
                        {
                            Tool.Swap(ref list.m_val0, ref list.m_val1);
                        }
                          
                        ++list.m_version;
                        return;
                    }
                }
            }

            Sort(list, 0, list.Count - 1, comparison);

            ++list.m_version;
        }

        private static void Sort(HybridList<T> list, int left, int right, Comparison<T> comparison)
        {
            do
            {
                int leftIdx = left;
                int rightIdx = right;
                int currentIdx = leftIdx + (rightIdx - leftIdx >> 1);

                if (leftIdx != currentIdx)
                {
                    var x = list.ValueByRef(leftIdx);
                    var y = list.ValueByRef(currentIdx);

                    if (comparison(x, y) > 0)
                    {
                        T obj = x;
                        list.ValueByRef(leftIdx) = y;
                        list.ValueByRef(currentIdx) = obj;
                    }
                }

                if (leftIdx != rightIdx)
                {
                    var x = list.ValueByRef(leftIdx);
                    var y = list.ValueByRef(rightIdx);

                    if (comparison(x, y) > 0)
                    {
                        T obj = x;
                        list.ValueByRef(leftIdx) = y;
                        list.ValueByRef(rightIdx) = obj;
                    }
                }

                if (currentIdx != rightIdx)
                {
                    var x = list.ValueByRef(currentIdx);
                    var y = list.ValueByRef(rightIdx);

                    if (comparison(x, y) > 0)
                    {
                        T obj = x;
                        list.ValueByRef(currentIdx) = y;
                        list.ValueByRef(rightIdx) = obj;
                    }
                }

                T value = list.ValueByRef(currentIdx);
                do
                {
                    while (comparison(list.ValueByRef(leftIdx), value) < 0)
                    {
                        ++leftIdx;
                    }
                    while (comparison(value, list.ValueByRef(rightIdx)) < 0)
                    {
                        --rightIdx;
                    }
                    if (leftIdx <= rightIdx)
                    {
                        if (leftIdx < rightIdx)
                        {
                            T obj2 = list.ValueByRef(leftIdx);
                            list.ValueByRef(leftIdx) = list.ValueByRef(rightIdx);
                            list.ValueByRef(rightIdx) = obj2;
                        }
                        ++leftIdx;
                        --rightIdx;
                    }
                    else
                    {
                        break;
                    }
                }
                while (leftIdx <= rightIdx);

                if (rightIdx - left <= right - leftIdx)
                {
                    if (left < rightIdx)
                    {
                        Sort(list, left, rightIdx, comparison);
                    }
                    left = leftIdx;
                }
                else
                {
                    if (leftIdx < right)
                    {
                        Sort(list, leftIdx, right, comparison);
                    }
                    right = rightIdx;
                }
            }
            while (left < right);
        }

        public interface INode : IEnumerable<T>
        {
            int Level { get; }

            ref T this[int index] { get; }

            bool Add(T item, out INode node);

            void Clear();

            bool RemoveLast();
        }

        [DebuggerDisplay("Link. Nodes: {m_nodes.Count}, Level: {Level}")]
        private sealed class LinkNode : INode
        {
            private const int c_intermediateCapacity = 128;
            private readonly PoolListBase<INode> m_nodes;
            private readonly int m_level;
            private readonly int m_stepBase;
            private readonly int m_leafCapacity;

            public int Level
            {
                get
                {
                    return m_level;
                }
            }

            public ref T this[int index]
            {
                get
                {
                    var current = index >> m_stepBase;
                    var next = index - (current << m_stepBase);

                    if (current < 0 || current > m_nodes.m_size)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return ref m_nodes[current][next];
                }
            }

            public LinkNode(int level, int leafCapacity, INode child1, INode child2 = null)
            {
                m_level = level;
                m_leafCapacity = leafCapacity;

                m_stepBase = (int)Math.Log(Math.Pow(c_intermediateCapacity, m_level - 1) * m_leafCapacity, 2);

                m_nodes = new PoolListBase<INode>(int.MaxValue, 4);
                m_nodes.Add(child1);

                if (child2 != null)
                {
                    m_nodes.Add(child2);
                }
            }

            public LinkNode(LinkNode linkNode)
            {
                m_level = linkNode.m_level;
                m_leafCapacity = linkNode.m_leafCapacity;
                m_stepBase = linkNode.m_stepBase;

                m_nodes = new PoolListBase<INode>(int.MaxValue, linkNode.m_nodes.m_size);

                foreach (var node in linkNode.m_nodes)
                {
                    if (node is LinkNode ln)
                    {
                        m_nodes.Add(new LinkNode(ln));
                    }
                    else if (node is StoreNode sn)
                    {
                        m_nodes.Add(new StoreNode(sn));
                    }
                }
            }

            public bool Add(T item, out INode node)
            {
                INode node1;
                if (m_nodes[m_nodes.Count - 1].Add(item, out node1) == false)
                {
                    if (m_nodes.m_size == c_intermediateCapacity)
                    {
                        node = new LinkNode(m_level, m_leafCapacity, node1);
                        return false;
                    }
                    m_nodes.Add(node1);
                }
                node = this;
                return true;
            }

            public void Clear()
            {
                foreach (INode node in m_nodes)
                {
                    node.Clear();
                }

                m_nodes.Clear();
            }

            public bool RemoveLast()
            {
                var node = m_nodes[m_nodes.m_size - 1];

                var isEmpty = node.RemoveLast();

                if (isEmpty)
                {
                    m_nodes.RemoveAt(m_nodes.m_size - 1);
                }

                return m_nodes.m_size == 0;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (INode node in m_nodes)
                {
                    if (node is StoreNode storeNode)
                    {
                        for (int i = 0; i < storeNode.m_size && i < storeNode.m_items.Length; i++)
                        {
                            yield return storeNode.m_items[i];
                        }
                    }
                    else
                    {
                        foreach (T obj in node)
                        {
                            yield return obj;
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [DebuggerDisplay("Store. Size: {m_size}")]
        public sealed class StoreNode : PoolListBase<T>, INode
        {
            public int Level
            {
                get { return 0; }
            }

            public StoreNode(int maxCapacity, int arraySize) : base(maxCapacity, arraySize)
            {
            }

            public StoreNode(StoreNode poolList) : base(poolList)
            {
            }

            public StoreNode(int maxCapacity, int arraySize, T item)
              : base(maxCapacity, arraySize)
            {
                AddItem(item);
            }

            public bool Add(T item, out INode node)
            {
                if (m_size == m_maxCapacity)
                {
                    node = new StoreNode(m_maxCapacity, 4, item);
                    return false;
                }
                AddItem(item);
                node = this;
                return true;
            }
        }

        public int RemoveAll(Func<T, bool> match)
        {
            var simpleNode = m_root as StoreNode;

            if (simpleNode != null)
            {
                return simpleNode.RemoveAll(match);
            }
            else
            {
                int counter = 0;
                int matchedIndex;
                do
                {
                    matchedIndex = -1;

                    for (int i = 0; i < m_count; i++)
                    {
                        var item = this.ValueByRef(i);

                        if (match(item))
                        {
                            matchedIndex = i;
                            break;
                        }
                    }

                    if (matchedIndex >= 0)
                    {
                        RemoveAt(matchedIndex);
                        counter++;
                    }
                }
                while (m_count > 0 && matchedIndex >= 0);

                return counter;
            }
        }

        public int FindIndex(Predicate<T> match, int start = 0)
        {
            if (start >= m_count || start < 0)
            {
                return -1;
            }

            if (m_root is StoreNode simpleNode)
            {
                return simpleNode.FindIndex(match, start);
            }
            else
            {
                for (int index = start; index < m_count; ++index)
                {
                    if (match(ValueByRef(index)))
                    {
                        return index;
                    }
                }
                return -1;
            }
        }

        public int FindIndex<V>(V value, Func<T, V> valueSelector, int start = 0)
        {
            return FindIndex<V>(value, valueSelector, EqualityComparer<V>.Default, start);
        }

        public int FindIndex<V>(V value, Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int start = 0)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            if (start >= m_count || start < 0)
            {
                return -1;
            }

            for (int index = start; index < m_count; ++index)
            {
                var valueByRef = ValueByRef(index);

                var selector = valueSelector(valueByRef);

                if (equalityComparer.Equals(selector, value))
                {
                    return index;
                }
            }

            return -1;
        }


        ~HybridList()
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

    public static class HybridListExtensions
    {
        public static HybridList<T> ToHybridList<T>(this IList<T> source)
        {
            return new HybridList<T>(source);
        }

        public static HybridList<T> ToHybridList<T>(this IEnumerable<T> source)
        {
            return new HybridList<T>(source);
        }

        public static HybridList<T> ToHybridList<T>(this HybridList<T> source)
        {
            return new HybridList<T>(source);
        }

        public static void DisposeList<T>(this HybridList<T> list)
        {
            if (list != null)
            {
                if (list is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    list.Clear();
                }
            }
        }

        public static void DisposeBitArray(this HybridBitArray array)
        {
            if (array != null)
            {
                if (array is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    array.Clear();
                }
            }
        }

        public static void DisposeHashset<T>(this HybridHashSet<T> array)
        {
            if (array != null)
            {
                if (array is IDisposable d)
                {
                    d.Dispose();
                }
                else
                {
                    array.Clear();
                }
            }
        }

        public static bool ListNullOrItemAbsent<T>(this IReadOnlyCollection<T> source, T value)
        {
            return source == null || !source.Contains(value);
        }

        public static HybridHashSet<T> ToHybridHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HybridHashSet<T>(source, comparer);
        }
    }
}

