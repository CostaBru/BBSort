#ifndef BBSORT_SOLUTION_PTR_VECTOR_H
#define BBSORT_SOLUTION_PTR_VECTOR_H

namespace pool
{
    template <typename T>
    class ptr_vector {

        using value_type        = T;
        using pointer           = T*;

        using reference         = T&;
        using const_reference   = T const&;

        using const_pointer     = T const*;
        using iterator          = T*;
        using const_iterator    = T const*;
        using riterator         = std::reverse_iterator<iterator>;
        using const_riterator   = std::reverse_iterator<const_iterator>;
        using difference_type   = std::ptrdiff_t;
        using size_type         = std::size_t;

    private:
        size_type length   = 0;
        size_type capacity = 0;
        pointer     buffer   = nullptr;

        void deallocate(pointer buffer) {

            free(buffer);
            capacity = 0;
        }

        void construct(pointer buffer, const_reference val) {

            new(buffer) T(val);
        }

        void destroy(pointer buffer) {

            buffer->~T();
        }

    public:

        ptr_vector()
                : length(0), capacity(0), buffer(NULL) {
        }

        ptr_vector(T* buffer, size_type capacity)
                : capacity(capacity), buffer(buffer), length(0) {
        }

        ptr_vector(ptr_vector&& move) noexcept
                : capacity(0), length(0), buffer(NULL) {

                move.swap(*this);
        }

        void swap(ptr_vector& other) noexcept {

            using std::swap;

            swap(capacity, other.capacity);
            swap(length, other.length);
            swap(buffer, other.buffer);
        }

        ptr_vector(const ptr_vector& copyFrom) : length(copyFrom.length), buffer(NULL) {

            if (copyFrom.capacity > 0) {

                reserve(copyFrom.capacity);

                for (size_t i = 0; i < length; ++i) {

                    construct(buffer + i, copyFrom.buffer[i]);
                }
            }
        }

        ~ptr_vector() {

            clear();
            deallocate(buffer);
        }

        void push_back(const_reference inValue) {

            resizeIfRequire();

            construct(buffer + length++, inValue);
        }

        pointer top() {
            return &buffer[length - 1];
        }

        void pop() {

            assert(length > 0);

            --length;
        }

        void clear() {

            length = 0;
        }

        size_t size() const {

            return length;
        }

        bool any() const {

            return length > 0;
        }

        void resizeIfRequire() {

            if (length == capacity) {

                auto newCapacity = std::max(2.0, capacity * 2.0);
                reserve(newCapacity);
            }
        }

        void reserve(size_type newCapacity) {

            auto newBuffer = static_cast<pointer>(::operator new(sizeof(value_type) * newCapacity));

            ptr_vector tmpVector = ptr_vector(newBuffer, newCapacity);

            if (length > 0) {

                simpleCopy<T>(tmpVector);
            }

            tmpVector.swap(*this);
        }

        iterator begin() {
            return &buffer[0];
        }

        iterator end() {
            return &buffer[length];
        }

        template<typename X>
        typename std::enable_if<std::is_nothrow_move_constructible<X>::value == false>::type
        simpleCopy(ptr_vector<value_type>& dst)
        {
            std::for_each(buffer, buffer + length,
                          [&dst](const_reference v){ dst.pushBackInternal(v); }
            );
        }

        template<typename X>
        typename std::enable_if<std::is_nothrow_move_constructible<X>::value == true>::type
        simpleCopy(ptr_vector<value_type>& dst)
        {
            std::for_each(buffer, buffer + length,
                          [&dst](reference v){ dst.moveBackInternal(std::move(v)); }
            );
        }

        // Add new element to the end using placement new
        void pushBackInternal(const_reference value) {

            new(buffer + length) T(value);
            ++length;
        }

        void moveBackInternal(T&& value) {

            new(buffer + length) value_type(std::move(value));
            ++length;
        }
    };
}
#endif //BBSORT_SOLUTION_PTR_VECTOR_H
