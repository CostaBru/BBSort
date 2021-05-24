#ifndef BBSort_H
#define BBSort_H

#define BUCKET minmax::min_max_heap<sort_item<T>, pool::vector<sort_item<T>>>

#define STACK std::stack<BUCKET>

#define BUCKETS pool::vector_lazy<BUCKET>

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>
#include <min_max_heap.h>
#include "poolable_vector.h"
#include "poolable_vector_lazy.h"
#include <chrono>
#include <iostream>

namespace bb_sort {

    inline float fastLog2(const float val) {
        union { float val; int32_t x; } u = {val};
        const float lg2 = (float) (((u.x >> 23) & 255) - 128);
        u.x &= ~(255 << 23);
        u.x += 127 << 23;
        return lg2 + ((-0.3358287811f) * u.val + 2.0f) * u.val - 0.65871759316667f;
    }

    template<typename T>
    struct sort_item {
    public :
        T value;
        int count = 0;

        sort_item(){
        }

        sort_item(T val) {
            value = val;
            count = 1;
        }

        friend bool operator>(const sort_item &c1, const sort_item &c2) {
            return c1.value > c2.value;
        }

        friend bool operator<=(const sort_item &c1, const sort_item &c2) {
            return c1.value <= c2.value;
        }

        friend bool operator<(const sort_item &c1, const sort_item &c2) {
            return c1.value < c2.value;
        }

        friend bool operator>=(const sort_item &c1, const sort_item &c2) {
            return c1.value >= c2.value;
        }

        friend bool operator==(const sort_item &c1, const sort_item &c2) {
            return c1.value == c2.value;
        }

        friend bool operator!=(const sort_item &c1, const sort_item &c2) {
            return c1.value != c2.value;
        }

        friend std::ostream& operator << (std::ostream& os, const sort_item& m)
        {
            os << "VAL:" << m.value << " , COUNT: " << m.count << std::endl;
            return os ;
        }
    };

    template<typename T>
    inline float getLog(const sort_item<T>* x) {

        const float abs = std::abs(x->value);
        if (abs < 2) {
            return x->value;
        }
        const float lg = fastLog2(abs);
        return x->value < 0 ? -lg : lg;
    }

    inline float getLog(const float x) {

        const float abs = std::abs(x);
        if (abs < 2) {
            return x;
        }
        const float lg = fastLog2(abs);
        return x < 0 ? -lg : lg;
    }

    inline std::tuple<float, float> GetLinearTransformParams(const float x1,
                                                             const float x2,
                                                             const float y1,
                                                             const float y2) {

        const float dx = x1 - x2;

        if (dx != 0.0) [[likely]] {
            const float a = (y1 - y2) / dx;
            const float b = y1 - (a * x1);

            return std::make_tuple(a, b);
        }

        return std::make_tuple(0.0, 0.0);
    }

    template<typename T>
    void getBuckets(const BUCKET &iterable, BUCKETS &buckets, int count) {

        const float minLog = getLog(iterable.findMin().value);
        const float maxLog = getLog(iterable.findMax().value);

        const std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        for (auto const& item : iterable) {
            // ApplyLinearTransform
            int index = ((a * getLog(item.value) + b));
            index = std::min(count - 1, index);
            buckets[index].push(std::move(item));
        }
    }

    template<typename T>
    inline void fillStream(const sort_item<T> &val, std::vector<T> &output, const int index) {

        for (int i = 0; i < val.count; ++i) {

            const int newIndex = index + i;
            if (newIndex >= output.size()){

                break;
            }
            output[newIndex] = val.value;
        }
    }

    template<typename T>
    int case1(STACK &st,
              std::vector<T> &output,
              int index) {

        auto b1 = *(st.top()).begin();
        fillStream<T>(b1, output, index);

        auto count = b1.count;

        st.pop();

        return count;
    }

    template<typename T>
    int case2(STACK &st,
              std::vector<T> &output,
              int index) {

        auto it = (st.top()).begin();

        const sort_item b2 = *it;
        it++;
        const sort_item b1 = *it;

        fillStream<T>(b1, output, index);
        fillStream<T>(b2, output, index + b1.count);

        auto count = b1.count + b2.count;

        st.pop();

        return count;
    }

    template<typename T>
    int case3(STACK &st,
              std::vector<T> &output,
              int index) {

        //single comparison
        const auto& top = st.top();
        const auto maxMidMin = top.getMaxMidMin();

        const auto maxIndex = std::get<0>(maxMidMin);
        const auto midIndex = std::get<1>(maxMidMin);
        const auto minIndex = std::get<2>(maxMidMin);

        const auto count1 = top.At(minIndex).count;
        const auto count2 = top.At(midIndex).count;

        fillStream<T>(top.At(minIndex), output, index);
        fillStream<T>(top.At(midIndex), output, index + count1);
        fillStream<T>(top.At(maxIndex), output, index + count1 + count2);

        const auto count = count1 + count2 + top.At(maxIndex).count;

        st.pop();

        return count;
    }

    template<typename T>
    int caseN(STACK &st,
              std::vector<T> &output,
              int index) {

        int count = (st.top().size() / 2) + 1;

        count = std::min(count, 128);

        BUCKETS newBuckets(count);

        getBuckets<T>(st.top(), newBuckets, count);

        st.pop();

        for (int i = newBuckets.size() - 1; i >= 0; --i) {
            if (newBuckets[i].size() > 0)
            {
                st.emplace(std::move(newBuckets[i]));
            }
        }
        return 0;
    }

    template<typename Func>
    struct func_array {
        static Func *const switchCase[];
    };

    template<typename Func>
    Func *const func_array<Func>::switchCase[] = {case1, case2, case3, caseN};

    template<typename T>
    void bbSortToStream(STACK &st, std::vector<T> &output, const long int count) {

        int index = 0;

        while (st.size() > 0 && index < count) {

            const auto caseIndex = std::min(st.top().size() - 1, 3U);
            const auto switchCaseFunc = func_array<int(
                    STACK &,
                    std::vector<T> &,
                    int)>
            ::switchCase[caseIndex];

            index += switchCaseFunc(st, output, index);
        }
    }

    template<typename T>
    void  prepareTopBuckets(STACK &st,
                            BUCKETS &buckets,
                            const std::vector<sort_item<T>> &items,
                            const sort_item<T> &minSortEl,
                            const sort_item<T> &maxSortEl,
                            int count) {

        const float minLog = getLog(&minSortEl);
        const float maxLog = getLog(&maxSortEl);

        const std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        //pushing distinct items only
        for (auto & item : items) {
            // ApplyLinearTransform
            int index = ((a * getLog(&item) + b));
            index = std::min(count - 1, index);
            buckets[index].push(std::move(item));
        }

        for (int i = buckets.size() - 1; i >= 0; --i) {

            if (buckets[i].size() > 0)
            {
                st.emplace(std::move(buckets[i]));
            }
        }
    }

    template<typename T>
    void getTopStackBuckets(const std::vector<T> &array,
                            STACK &st,
                            BUCKETS &buckets,
                            int count) {

        T minEl = array[0];
        T maxEl = array[0];

        for (const auto& item: array) {

            minEl = std::min(item, minEl);
            maxEl = std::max(item, maxEl);
        }

        // following loop is actual bottleneck: we spent here ~70% of execution time, which depend on size of T.
        // capacity reservation does not help.

        std::vector<sort_item<T>> distinctItems;
        robin_hood::unordered_map<T, int> distinctMap;

        for (const auto& item: array) {

            if (distinctMap.contains(item)) {

                distinctItems[distinctMap[item]].count += 1;
            } else {

                sort_item<T> sortItem(item);
                distinctItems.emplace_back(sortItem);
                distinctMap[item] = distinctItems.size() - 1;
            }
        }

        const sort_item minSortEl = distinctItems[distinctMap[minEl]];
        const sort_item maxSortEl = distinctItems[distinctMap[maxEl]];

        prepareTopBuckets(st, buckets, distinctItems, minSortEl, maxSortEl, count);
    }

    template<typename T>
    void sort(std::vector<T> &array) {

        if (array.size() <= 1) {

            return;
        }

        long count = array.size();
        count = std::min(count, 128l);

        BUCKETS buckets(count);
        STACK st;

        getTopStackBuckets(array, st, buckets, count);

        bbSortToStream(st, array, array.size());
    }

    template<typename T>
    std::vector<T> getTopSorted(std::vector<T> &array, long int count) {

        long int size = array.size();

        count = std::min(size, count);
        std::vector<T> result(count);

        if (size <= 1) {

            for (int i = 0; i < size; ++i) {

                result[i] = array[i];
            }

            return result;
        }

        const int bucketCount =  std::min(size, 128l);

        BUCKETS buckets(bucketCount);
        STACK st;

        getTopStackBuckets(array, st, buckets, bucketCount);

        bbSortToStream<T>(st, result, count);

        return result;
    }
}
#endif
