#ifndef BBSORT_SOLUTION_POOLABLE_VECTOR_LAZY_H
#define BBSORT_SOLUTION_POOLABLE_VECTOR_LAZY_H

namespace pool {

    template<typename T>
    class vector_lazy {

    public:

        using value_type = T;
        using reference = T &;
        using const_reference = T const &;
        using array_pointer = T *;
        using const_pointer = T const *;
        using iterator = T *;
        using const_iterator = T const *;
        using riterator = std::reverse_iterator<iterator>;
        using const_riterator = std::reverse_iterator<const_iterator>;
        using difference_type = std::ptrdiff_t;
        using size_type = std::size_t;

    private:

        size_type capacity = 0;
        std::vector<bool> initFlags;
        bool lazyInit = false;
        size_type length = 0;
        array_pointer array = nullptr;

    public:

        vector_lazy(size_type size) :
                capacity(0),
                length(0) {

            reserveCapacity(size);

            lazyInit = true;

            initFlags.resize(size);

            length = size;
        }

        ~vector_lazy() {

            if (capacity > 0) {

                if (length > 0) {

                    clearElements<T>();
                }

                returnArrayToPool(capacity);
            }
        }

        vector_lazy(vector_lazy const &copy)
                : capacity(0), length(0) {

            try {

                reserveCapacity(copy.length);

                for (int loop = 0; loop < copy.length; ++loop) {

                    pushBackInternal(copy.array[loop]);
                }
            }
            catch (...) {

                clearElements<T>();

                if (capacity > 0) {

                    returnArrayToPool(capacity);
                }
                throw;
            }
        }

        vector_lazy &operator=(vector_lazy const &copy) {

            copyAssign<T>(copy);
            return *this;
        }

        vector_lazy(vector_lazy &&move) noexcept
                : capacity(0), length(0), array(nullptr) {

            if (capacity > 0) {

                returnArrayToPool(array, capacity);
            }

            move.swap(*this);
        }

        vector_lazy &operator=(vector_lazy &&move) noexcept {

            move.swap(*this);
            return *this;
        }

        void swap(vector_lazy &other) noexcept {

            using std::swap;
            swap(capacity, other.capacity);
            swap(length, other.length);
            swap(array, other.array);
        }

        // Non-Mutating functions
        size_type size() const { return length; }

        bool empty() const { return length == 0; }

        // Validated element access
        reference at(size_type index) {

            validateIndex(index);

            if (lazyInit && !initFlags[index]) {
                initBack(index);
            }

            return array[index];
        }

        // Non-Validated element access
        reference operator[](size_type index) {

            if (lazyInit && !initFlags[index]) {
                initBack(index);
            }
            return array[index];
        }

        bool hasValue(size_type index) {

            if (lazyInit) {
                return initFlags[index];
            }
            return true;
        }

        friend std::ostream &operator<<(std::ostream &os, const vector_lazy &m) {

            os << "lazy vector:  size: " << m.length << " , capacity: " << m.capacity << " , lazy init: " << m.lazyInit
               << std::endl;

            for (size_type i = 0; i < m.length; ++i) {

                auto good = true;

                if (m.lazyInit) {

                    good = m.initFlags[i];
                }

                os << "[ " << i << "] = " << m.value << " good: " << good << std::endl;
            }

            return os;
        }

        std::string toString() {

            std::ostringstream stream;
            stream << this;
            return stream.str();
        }

        // Comparison
        bool operator!=(vector_lazy const &rhs) const { return !(*this == rhs); }

        bool operator==(vector_lazy const &rhs) const {

            if (size() != rhs.size()) {

                return false;
            }

            for (int i = 0; i < size(); ++i) {

                if (!array[i] != rhs[i]) {

                    return false;
                }
            }

            return true;
        }

        // Mutating functions

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == false>::type
        push_back(value_type const &value) {

            resizeIfRequire();
            pushBackInternal(value);

            if (lazyInit) {

                initFlags.resize(length);
                initFlags[length - 1] = true;
            }
        }

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == false>::type
        push_back(value_type &&value) {

            resizeIfRequire();
            moveBackInternal(std::move(value));

            if (lazyInit) {

                initFlags.resize(length);
                initFlags[length - 1] = true;
            }
        }

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == true>::type
        // Mutating functions
        push_back(value_type const &value) {

            resizeIfRequire();
            pushBackInternal(value);

            if (lazyInit) {

                initFlags.resize(length);
                initFlags[length - 1] = true;
            }
        }

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == true>::type
        push_back(value_type &&value) {

            resizeIfRequire();
            moveBackInternal(std::move(value));

            if (lazyInit) {

                initFlags.resize(length);
                initFlags[length - 1] = true;
            }
        }

        void pop_back() {

            --length;
            array[length].~T();
        }

        void clear() {

            clearElements<T>();

            length = 0;

            lazyInit = false;

            initFlags.clear();
        }

        void reserve(size_type capacityUpperBound) {

            if (capacityUpperBound > capacity) {

                reserveCapacity(capacityUpperBound);
            }
        }

    private:

        vector_lazy(array_pointer array, size_type capacity)
                :
                capacity(capacity),
                array(array),
                length(0) {
        }

        void validateIndex(size_type index) const {

            if (index >= length) {
                throw std::out_of_range("Out of Range");
            }
        }

        void resizeIfRequire() {

            if (length == capacity) {

                size_type newCapacity = std::max(16.0, capacity * 2.0);
                reserveCapacity(newCapacity);
            }
        }

        void reserveCapacity(size_type newCapacity) {

            if (newCapacity > 0) {

                array_pointer rentedArray = global_array_pool<T>::GLOBAL_POOL.rentArray(newCapacity);

                vector_lazy tmpVector = vector_lazy(rentedArray, newCapacity);

                if (length > 0) {

                    simpleCopy<T>(tmpVector);
                }

                tmpVector.swap(*this);
            };
        }

        void returnArrayToPool(size_type size) {

            global_array_pool<T>::GLOBAL_POOL.returnArray(array, size);
        }

        void pushBackInternal(T const &value) {

            new(array + length) T(value);
            ++length;
        }

        void moveBackInternal(T &&value) {

            new(array + length) T(std::move(value));
            ++length;
        }

        void initBack(size_type index) {

            new(array + index) T();
            initFlags[index] = true;
        }

        template<typename X>
        typename std::enable_if<!std::is_nothrow_move_constructible<X>::value && !std::is_trivial<X>::value>::type
        simpleCopy(vector_lazy<T> &dst) {

            std::for_each(array, array + length, [&dst](T const &v) { dst.pushBackInternal(v); });
        }

        template<typename X>
        typename std::enable_if<std::is_nothrow_move_constructible<X>::value && !std::is_trivial<X>::value>::type
        simpleCopy(vector_lazy<T> &dst) {

            std::for_each(array, array + length, [&dst](T &v) { dst.moveBackInternal(std::move(v)); });
        }

        template<typename X>
        typename std::enable_if<std::is_trivial<X>::value == true>::type
        simpleCopy(vector_lazy<X> &dst) {

            memcpy_fast(dst.array, array, sizeof(T) * length);
            dst.length = length;
        }

        template<typename X>
        typename std::enable_if<std::is_trivially_destructible<X>::value == false>::type
        clearElements() {

            for (int loop = 0; loop < length; ++loop) {

                size_type index = length - 1 - loop;

                if (!lazyInit || initFlags[index]) {

                    array[index].~T();
                }
            }
        }

        template<typename X>
        typename std::enable_if<std::is_trivially_destructible<X>::value == true>::type
        clearElements() {
        }

        template<typename X>
        typename std::enable_if<(std::is_nothrow_copy_constructible<X>::value
                                 && std::is_nothrow_destructible<X>::value) == true>::type
        copyAssign(vector_lazy<X> &copy) {

            if (this == &copy) {

                return;
            }

            if (capacity <= copy.length) {

                clearElements<T>();
                length = 0;
                for (int loop = 0; loop < copy.length; ++loop) {

                    pushBackInternal(copy[loop]);
                }

                if (lazyInit) {

                    initFlags.resize(length);
                }
            } else {

                vector_lazy<T> tmp(copy);
                tmp.swap(*this);
            }
        }

        template<typename X>
        typename std::enable_if<(std::is_nothrow_copy_constructible<X>::value
                                 && std::is_nothrow_destructible<X>::value) == false>::type
        copyAssign(vector_lazy<X> &copy) {

            vector_lazy<T> tmp(copy);
            tmp.swap(*this);
        }
    };
}
#endif //BBSORT_SOLUTION_POOLABLE_VECTOR_LAZY_H