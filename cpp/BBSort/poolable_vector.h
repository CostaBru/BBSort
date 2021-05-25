#ifndef BBSORT_SOLUTION_POOLABLE_VECTOR_H
#define BBSORT_SOLUTION_POOLABLE_VECTOR_H

#include <type_traits>
#include <memory>
#include <algorithm>
#include <stdexcept>
#include <iterator>
#include <string>
#include <global_array_pool.h>

#include <fastmemcpy.h>

namespace pool {

    template<typename T>
    class vector {

    public:

        using value_type        = T;
        using reference         = T&;
        using const_reference   = T const&;
        using array_pointer           = T*;
        using const_pointer     = T const*;
        using iterator          = T*;
        using const_iterator    = T const*;
        using riterator         = std::reverse_iterator<iterator>;
        using const_riterator   = std::reverse_iterator<const_iterator>;
        using difference_type   = std::ptrdiff_t;
        using size_type         = std::size_t;

    private:

        size_type              capacity = 0;

    public:

        array_pointer          array = nullptr;
        size_type              length = 0;


        vector(size_type size) :
                capacity(0),
                length(0) {

            reserveCapacity(size);

            for (int i = 0; i < size; ++i) {

                initBack(i);
            }
        }

        vector() :
                capacity(0),
                length(0) {
        }

        template<typename I>
        vector(I begin, I end) :
                capacity(0),
                length(0) {

            reserveCapacity(std::distance(begin, end));

            for (auto loop = begin; loop != end; ++loop) {

                pushBackInternal(*loop);
            }
        }

        vector(std::initializer_list<T> const& list)
                : vector(std::begin(list), std::end(list))
        {}

        ~vector() {

            if (capacity > 0) {

                if (length > 0) {

                    clearElements<T>();
                }

                returnArrayToPool(capacity);
            }
        }

        vector(vector const& copy)
                : capacity(0)
                , length(0) {

            try {

                reserveCapacity(copy.length);

                for (int loop = 0; loop < copy.length; ++loop) {
                    push_back(copy.array[loop]);
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

        vector& operator=(vector const& copy) {

            copyAssign<T>(copy);
            return *this;
        }

        vector(vector&& move) noexcept
                : capacity(0)
                , length(0)
                , array(nullptr) {

            if (capacity > 0) {

                returnArrayToPool(array, capacity);
            }

            move.swap(*this);
        }

        vector& operator=(vector&& move) noexcept {

            move.swap(*this);
            return *this;
        }

        void swap(vector& other) noexcept {

            using std::swap;
            swap(capacity, other.capacity);
            swap(length, other.length);
            swap(array, other.array);
        }

        // Non-Mutating functions
        size_type           size() const                        {return length;}
        bool                empty() const                       {return length == 0;}

        // Validated element access
        reference           at(size_type index)                 { validateIndex(index); return array[index];}
        const_reference     at(size_type index) const           { validateIndex(index); return array[index];}

        // Non-Validated element access

        reference operator[](size_type index)                   { return array[index]; }
        const_reference operator[](size_type index) const       { return array[index]; }
        bool hasValue(size_type index) const                    { return true;  }

        reference           front()                             {return array[0];}
        const_reference     front() const                       {return array[0];}
        reference           back()                              {return array[length - 1];}
        const_reference     back() const                        {return array[length - 1];}

        // Iterators
        iterator            begin()                             {return array;}
        riterator           rbegin()                            {return riterator(end());}
        const_iterator      begin() const                       {return array;}
        const_riterator     rbegin() const                      {return const_riterator(end());}

        iterator            end()                               {return array + length;}
        riterator           rend()                              {return riterator(begin());}
        const_iterator      end() const                         {return array + length;}
        const_riterator     rend() const                        {return const_riterator(begin());}

        const_iterator      cbegin() const                      {return begin();}
        const_riterator     crbegin() const                     {return rbegin();}
        const_iterator      cend() const                        {return end();}
        const_riterator     crend() const                       {return rend();}

        // Comparison
        bool operator!=(vector const& rhs) const {return !(*this == rhs);}

        bool operator==(vector const& rhs) const {

            return (size() == rhs.size())
                   && std::equal(begin(), end(), rhs.begin());
        }

        // Mutating functions

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == false>::type
        push_back(value_type const& value) {

            resizeIfRequire();
            pushBackInternal(value);
        }

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == false>::type
        push_back(value_type&& value) {

            resizeIfRequire();
            moveBackInternal(std::move(value));
        }

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == true>::type
        // Mutating functions
        push_back(value_type const& value) {

            resizeIfRequire();
            pushBackInternal(value);
        }

        template<typename X=T>
        typename std::enable_if<std::is_trivial<X>::value == true>::type
        push_back(value_type&& value) {

            resizeIfRequire();
            moveBackInternal(std::move(value));
        }

        template<typename... Args>
        void emplace_back(Args&&... args) {

            resizeIfRequire();
            emplaceBackInternal(std::move(args)...);
        }

        void pop_back() {

            --length;
            array[length].~T();
        }

        void clear(){

            clearElements<T>();
            length = 0;
        }

        void reserve(size_type capacityUpperBound) {

            if (capacityUpperBound > capacity) {

                reserveCapacity(capacityUpperBound);
            }
        }

        std::string toString() {

            std::ostringstream stream;
            stream << this;
            return stream.str();
        }

        friend std::ostream& operator << (std::ostream& os, const vector& m) {

            os << "vector:  size: " << m.length << " , capacity: " << m.capacity  << std::endl;

            for (size_type i = 0; i < m.length; ++i) {

                os << "[ " << i << "] = " << m.value << std::endl;
            }

            return os;
        }

    private:

        vector(array_pointer array, size_type capacity)
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

                vector tmpVector = vector(rentedArray, newCapacity);

                if (length > 0) {

                    simpleCopy<T>(tmpVector);
                }

                tmpVector.swap(*this);
            };
        }

        void returnArrayToPool(size_type size) {

            global_array_pool<T>::GLOBAL_POOL.returnArray(array, size);
        }

        void pushBackInternal(T const& value) {

            new(array + length) T(value);
            ++length;
        }

        void moveBackInternal(T&& value) {

            new(array + length) T(std::move(value));
            ++length;
        }

        template<typename... Args>
        void emplaceBackInternal(Args&&... args) {

            new(array + length) T(std::move(args)...);
            ++length;
        }

        void initBack(size_type index) {

            new(array + index) T();
        }

        template<typename X>
        typename std::enable_if<!std::is_nothrow_move_constructible<X>::value && !std::is_trivial<X>::value>::type
        simpleCopy(vector<T>& dst) {

            std::for_each(array, array + length, [&dst](T const &v) { dst.pushBackInternal(v); });
        }

        template<typename X>
        typename std::enable_if<std::is_nothrow_move_constructible<X>::value && !std::is_trivial<X>::value>::type
        simpleCopy(vector<T>& dst) {

            std::for_each(array, array + length,    [&dst](T &v) { dst.moveBackInternal(std::move(v)); });
        }

        template<typename X>
        typename std::enable_if<std::is_trivial<X>::value == true>::type
        simpleCopy(vector<X>& dst) {

            memcpy_fast(dst.array, array, sizeof(T) * length);
            dst.length = length;
        }

        template<typename X>
        typename std::enable_if<std::is_trivially_destructible<X>::value == false>::type
        clearElements() {

            for (int loop = 0; loop < length; ++loop) {

                array[length - 1 - loop].~T();
            }
        }

        template<typename X>
        typename std::enable_if<std::is_trivially_destructible<X>::value == true>::type
        clearElements() {
        }

        template<typename X>
        typename std::enable_if<(std::is_nothrow_copy_constructible<X>::value
                                 &&  std::is_nothrow_destructible<X>::value) == true>::type
        copyAssign(vector<X>& copy) {

            if (this == &copy) {

                return;
            }

            if (capacity <= copy.length) {

                clearElements<T>();
                length = 0;
                for (int loop = 0; loop < copy.length; ++loop) {

                    pushBackInternal(copy[loop]);
                }
            } else {

                vector<T> tmp(copy);
                tmp.swap(*this);
            }
        }

        template<typename X>
        typename std::enable_if<(std::is_nothrow_copy_constructible<X>::value
                                 &&  std::is_nothrow_destructible<X>::value) == false>::type
        copyAssign(vector<X>& copy) {

            vector<T> tmp(copy);
            tmp.swap(*this);
        }
    };
}

#endif //BBSORT_SOLUTION_POOLABLE_VECTOR_H
