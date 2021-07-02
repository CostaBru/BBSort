using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Flexols.Data.Collections
{
    /// <summary>Manages a compact array of bit values, which are represented as Booleans, where <see langword="true" /> indicates that the bit is on (1) and <see langword="false" /> indicates the bit is off (0).</summary>
    public sealed class HybridBitArray : ICollection, IEnumerable, ICloneable, IEnumerable<bool>, IReadOnlyList<bool>, IDisposable
    {
        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private const int BitsPerByte = 8;

        private readonly HybridList<int> m_array;

        private uint m_length;

        [NonSerialized] private object m_syncRoot;


        private HybridBitArray()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.BitArray" /> class that can hold the specified number of bit values, which are initially set to <see langword="false" />.</summary>
        /// <param name="length">The number of bit values in the new <see cref="T:System.Collections.BitArray" />. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="length" /> is less than zero. </exception>
        public HybridBitArray(int length)
            : this(length, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.BitArray" /> class that can hold the specified number of bit values, which are initially set to the specified value.</summary>
        /// <param name="length">The number of bit values in the new <see cref="T:System.Collections.BitArray" />. </param>
        /// <param name="defaultValue">The Boolean value to assign to each bit. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="length" /> is less than zero. </exception>
        public HybridBitArray(int length, bool defaultValue)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var capacity = (int)GetArrayLength((uint) length, BitsPerInt32);

            m_array = new HybridList<int>(capacity);

            int num = defaultValue ? -1 : 0;

            m_array.Ensure(capacity, num);

            m_length = (uint) length;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.BitArray" /> class that contains bit values copied from the specified array of Booleans.</summary>
        /// <param name="values">An array of Booleans to copy. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="values" /> is <see langword="null" />. </exception>
        public HybridBitArray(IReadOnlyList<bool> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var capacity =(int) GetArrayLength((uint) values.Count, BitsPerInt32);

            m_array = new HybridList<int>(capacity);
            m_array.Ensure(capacity);

            m_length = (uint) values.Count;
            for (int index = 0; index < values.Count; ++index)
            {
                if (values[index])
                {
                    var valueByRef = m_array.ValueByRef(index / BitsPerInt32);

                    m_array.ValueByRef(index / BitsPerInt32) = valueByRef | (1 << index % BitsPerInt32);
                }
            }
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.BitArray" /> class that contains bit values copied from the specified array of 32-bit integers.</summary>
        /// <param name="values">An array of integers containing the values to copy, where each integer represents 32 consecutive bits. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="values" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">The length of <paramref name="values" /> is greater than <see cref="F:System.Int32.MaxValue" /></exception>
        public HybridBitArray(IReadOnlyList<int> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            m_array = new HybridList<int>(values);
            m_length = (uint) (values.Count * BitsPerInt32);
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.BitArray" /> class that contains bit values copied from the specified <see cref="T:System.Collections.BitArray" />.</summary>
        /// <param name="bits">The <see cref="T:System.Collections.BitArray" /> to copy. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="bits" /> is <see langword="null" />. </exception>

        public HybridBitArray(HybridBitArray bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException(nameof(bits));
            }

            this.m_array = new HybridList<int>(bits.m_array);
            this.m_length = bits.m_length;
        }

        public void Clear()
        {
            m_length = 0;
            m_array.Clear();
        }

        /// <summary>Gets or sets the value of the bit at a specific position in the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <param name="index">The zero-based index of the value to get or set. </param>
        /// <returns>The value of the bit at position <paramref name="index" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.-or-
        /// <paramref name="index" /> is equal to or greater than <see cref="P:System.Collections.BitArray.Count" />. </exception>
        public bool this[int index]
        {
            get { return this.Get(index); }
            set { this.Set(index, value); }
        }

        /// <summary>Gets the value of the bit at a specific position in the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <param name="index">The zero-based index of the value to get. </param>
        /// <returns>The value of the bit at position <paramref name="index" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.-or-
        /// <paramref name="index" /> is greater than or equal to the number of elements in the <see cref="T:System.Collections.BitArray" />. </exception>
        public bool Get(int index)
        {
            if (index < 0 || index >= this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_Index");
            }
            
            if (m_array.m_root is HybridList<int>.StoreNode storeNode)
            {
                return (uint) (storeNode.m_items[index / BitsPerInt32] & 1 << index % BitsPerInt32) > 0U;
            }
            else
            {
                return (uint) (this.m_array.ValueByRef(index / BitsPerInt32) & 1 << index % BitsPerInt32) > 0U;
            }
        }

        public bool HasAndSet(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_Index");
            }

            if (index >= this.Length)
            {
                return false;
            }
            
            if (m_array.m_root is HybridList<int>.StoreNode storeNode)
            {
                return (uint) (storeNode.m_items[index / BitsPerInt32] & 1 << index % BitsPerInt32) > 0U;
            }
            else
            {
                return (uint)(this.m_array.ValueByRef(index / BitsPerInt32) & 1 << index % BitsPerInt32) > 0U;
            }
        }

        /// <summary>Sets the bit at a specific position in the <see cref="T:System.Collections.BitArray" /> to the specified value.</summary>
        /// <param name="index">The zero-based index of the bit to set. </param>
        /// <param name="value">The Boolean value to assign to the bit. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.-or-
        /// <paramref name="index" /> is greater than or equal to the number of elements in the <see cref="T:System.Collections.BitArray" />. </exception>

        public void Set(int index, bool value)
        {
            if (index < 0 || index >= this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_Index");
            }

            if (m_array.m_root is HybridList<int>.StoreNode storeNode)
            {
                int newVal = 0;

                if (value)
                {
                    newVal = storeNode.m_items[(index / BitsPerInt32)] | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = storeNode.m_items[(index / BitsPerInt32)] & ~(1 << index % BitsPerInt32);
                }

                storeNode.m_items[(index / BitsPerInt32)] = newVal;
            }
            else
            {
                var valueByRef = this.m_array.ValueByRef(index / BitsPerInt32);

                int newVal = 0;

                if (value)
                {
                    newVal = valueByRef | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = valueByRef & ~(1 << index % BitsPerInt32);
                }

                m_array.ValueByRef(index / BitsPerInt32) = newVal;
            }
        }

        public bool TrySet(int index, bool value)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_Index");
            }

            if (index >= m_length)
            {
                return false;
            }

            if (m_array.m_root is HybridList<int>.StoreNode storeNode)
            {
                int newVal = 0;

                if (value)
                {
                    newVal = storeNode.m_items[(index / BitsPerInt32)] | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = storeNode.m_items[(index / BitsPerInt32)] & ~(1 << index % BitsPerInt32);
                }

                storeNode.m_items[(index / BitsPerInt32)] = newVal;
            }
            else
            {
                var valueByRef = this.m_array.ValueByRef(index / BitsPerInt32);

                int newVal = 0;

                if (value)
                {
                    newVal = valueByRef | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = valueByRef & ~(1 << index % BitsPerInt32);
                }

                m_array.ValueByRef(index / BitsPerInt32) = newVal;
            }
          
            return true;
        }

        public void SetOrAdd(int index, bool value, int capacity)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_Index");
            }

            if (index >= m_length)
            {
                var validCapacity = Math.Max(index, capacity);

                if (m_length == 0)
                {
                    Length = validCapacity;
                }
                else
                {
                    Length = validCapacity * 2;
                }
            }

            if (m_array.m_root is HybridList<int>.StoreNode storeNode)
            {
                int newVal = 0;

                if (value)
                {
                    newVal = storeNode.m_items[index / BitsPerInt32] | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = storeNode.m_items[index / BitsPerInt32] & ~(1 << index % BitsPerInt32);
                }

                storeNode.m_items[index / BitsPerInt32] = newVal;
            }
            else
            {
                var valueByRef = this.m_array.ValueByRef(index / BitsPerInt32);

                int newVal = 0;

                if (value)
                {
                    newVal = valueByRef | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = valueByRef & ~(1 << index % BitsPerInt32);
                }

                m_array.ValueByRef(index / BitsPerInt32) = newVal;
            }
        }

        /// <summary>Sets all bits in the <see cref="T:System.Collections.BitArray" /> to the specified value.</summary>
        /// <param name="value">The Boolean value to assign to all bits. </param>

        public void SetAll(bool value)
        {
            int num = value ? -1 : 0;
            
            var arrayLength = HybridBitArray.GetArrayLength(this.m_length, BitsPerInt32);

            if (m_array.m_root is HybridList<int>.StoreNode storeNode)
            {
                for (int index = 0; index < arrayLength && index <  storeNode.m_items.Length; ++index)
                {
                    storeNode.m_items[index] = num;
                }
            }
            else
            {
                for (int index = 0; index < arrayLength; ++index)
                {
                    this.m_array.ValueByRef(index) = num;
                }
            }
        }

        /// <summary>
        /// Performs the bitwise AND operation between the elements of the current <see cref="T:System.Collections.BitArray" /> object and the corresponding elements in the specified array. The current <see cref="T:System.Collections.BitArray" /> object will be modified to store the result of the bitwise AND operation.</summary>
        /// <param name="value">The array with which to perform the bitwise AND operation. </param>
        /// <returns>An array containing the result of the bitwise AND operation, which is a reference to the current <see cref="T:System.Collections.BitArray" /> object. </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> and the current <see cref="T:System.Collections.BitArray" /> do not have the same number of elements. </exception>

        public HybridBitArray And(HybridBitArray value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (this.m_length != value.m_length)
            {
                throw new ArgumentException("Arg_ArrayLengthsDiffer");
            }

            var arrayLength = HybridBitArray.GetArrayLength(this.m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = value.m_array.ValueByRef(index);

                this.m_array.ValueByRef(index) &= valueByRef;
            }

            return this;
        }

        /// <summary>
        /// Performs the bitwise OR operation between the elements of the current <see cref="T:System.Collections.BitArray" /> object and the corresponding elements in the specified array. The current <see cref="T:System.Collections.BitArray" /> object will be modified to store the result of the bitwise OR operation.</summary>
        /// <param name="value">The array with which to perform the bitwise OR operation. </param>
        /// <returns>An array containing the result of the bitwise OR operation, which is a reference to the current <see cref="T:System.Collections.BitArray" /> object.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> and the current <see cref="T:System.Collections.BitArray" /> do not have the same number of elements. </exception>

        public HybridBitArray Or(HybridBitArray value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (this.m_length != value.m_length)
            {
                throw new ArgumentException("Arg_ArrayLengthsDiffer");
            }

            var arrayLength = HybridBitArray.GetArrayLength(this.m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = value.m_array.ValueByRef(index);

                this.m_array.ValueByRef(index) |= valueByRef;
            }

            return this;
        }

        /// <summary>
        /// Performs the bitwise exclusive OR operation between the elements of the current <see cref="T:System.Collections.BitArray" /> object against the corresponding elements in the specified array. The current <see cref="T:System.Collections.BitArray" /> object will be modified to store the result of the bitwise exclusive OR operation.</summary>
        /// <param name="value">The array with which to perform the bitwise exclusive OR operation. </param>
        /// <returns>An array containing the result of the bitwise exclusive OR operation, which is a reference to the current <see cref="T:System.Collections.BitArray" /> object. </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> and the current <see cref="T:System.Collections.BitArray" /> do not have the same number of elements. </exception>

        public HybridBitArray Xor(HybridBitArray value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (this.m_length != value.m_length)
            {
                throw new ArgumentException("Arg_ArrayLengthsDiffer");
            }
            var arrayLength = HybridBitArray.GetArrayLength(this.m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = value.m_array.ValueByRef(index);

                this.m_array.ValueByRef(index) ^= valueByRef;
            }
            return this;
        }

        /// <summary>Inverts all the bit values in the current <see cref="T:System.Collections.BitArray" />, so that elements set to <see langword="true" /> are changed to <see langword="false" />, and elements set to <see langword="false" /> are changed to <see langword="true" />.</summary>
        /// <returns>The current instance with inverted bit values.</returns>

        public HybridBitArray Not()
        {
            var arrayLength = HybridBitArray.GetArrayLength(this.m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = this.m_array.ValueByRef(index);

                this.m_array.ValueByRef(index) = ~valueByRef;
            }
            return this;
        }

        /// <summary>Gets or sets the number of elements in the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <returns>The number of elements in the <see cref="T:System.Collections.BitArray" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The property is set to a value that is less than zero. </exception>

        public int Length
        {
            get { return (int) this.m_length; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "ArgumentOutOfRange_NeedNonNegNum");
                }

                m_length = (uint)value;

                var arrayLength = GetArrayLength(m_length, BitsPerInt32);

                m_array.Ensure((int) arrayLength, 0);
            }
        }

        public void Ensure(int count, bool value)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "ArgumentOutOfRange_NeedNonNegNum");
            }

            m_length = (uint)count;

            var arrayLength = GetArrayLength(m_length, BitsPerInt32);

            int num = value ? -1 : 0;

            m_array.Ensure((int)arrayLength, num);
        }

        /// <summary>Copies the entire <see cref="T:System.Collections.BitArray" /> to a compatible one-dimensional <see cref="T:System.Array" />, starting at the specified index of the target array.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.BitArray" />. The <see cref="T:System.Array" /> must have zero-based indexing. </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="array" /> is multidimensional.-or- The number of elements in the source <see cref="T:System.Collections.BitArray" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />. </exception>
        /// <exception cref="T:System.InvalidCastException">The type of the source <see cref="T:System.Collections.BitArray" /> cannot be cast automatically to the type of the destination <paramref name="array" />. </exception>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_NeedNonNegNum");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException("Arg_RankMultiDimNotSupported");
            }

            if (array is int[] ia)
            {
                m_array.CopyTo(ia, index);
            }
            else if (array is byte[])
            {
                var arrayLength = HybridBitArray.GetArrayLength(this.m_length, BitsPerByte);
                if (array.Length - index < arrayLength)
                {
                    throw new ArgumentException("Argument_InvalidOffLen");
                }
                byte[] numArray = (byte[]) array;
                for (int index1 = 0; index1 < arrayLength; ++index1)
                {
                    var m = this.m_array.ValueByRef(index1 / BytesPerInt32);

                    numArray[index + index1] = (byte) (m >> index1 % BytesPerInt32 * 8 & (int) byte.MaxValue);
                }
            }
            else
            {
                if (!(array is bool[]))
                {
                    throw new ArgumentException("Arg_BitArrayTypeUnsupported");
                }

                if (array.Length - index < this.m_length)
                {
                    throw new ArgumentException("Argument_InvalidOffLen");
                }
                bool[] flagArray = (bool[]) array;
                for (int index1 = 0; index1 < this.m_length; ++index1)
                {
                    var m = this.m_array.ValueByRef(index1 / BitsPerInt32);

                    flagArray[index + index1] = (uint) (m >> index1 % BitsPerInt32 & 1) > 0U;
                }
            }
        }

        /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.BitArray" />.</returns>
        public int Count
        {
            get { return (int) this.m_length; }
        }

        /// <summary>Creates a shallow copy of the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <returns>A shallow copy of the <see cref="T:System.Collections.BitArray" />.</returns>
        public object Clone()
        {
            return new HybridBitArray(m_array)
            {
                m_length = this.m_length
            };
        }

        /// <summary>Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.BitArray" />.</returns>
        public object SyncRoot
        {
            get
            {
                if (this.m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), (object) null);
                }
                return this.m_syncRoot;
            }
        }

        /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.BitArray" /> is read-only.</summary>
        /// <returns>This property is always <see langword="false" />.</returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>Gets a value indicating whether access to the <see cref="T:System.Collections.BitArray" /> is synchronized (thread safe).</summary>
        /// <returns>This property is always <see langword="false" />.</returns>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.BitArray" />.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> for the entire <see cref="T:System.Collections.BitArray" />.</returns>
        
        private static uint GetArrayLength(uint n, uint div)
        {
            if (n <= 0)
            {
                return 0;
            }
            return (n - 1) / div + 1;
        }

        public IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < m_length; i++)
            {
               yield return this.Get(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            if (m_array is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}


