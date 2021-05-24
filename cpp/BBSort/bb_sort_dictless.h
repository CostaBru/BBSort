#ifndef BBSORT_SOLUTION_BB_SORT_DICTLESS_H
#define BBSORT_SOLUTION_BB_SORT_DICTLESS_H

#define BUCKET_D minmax::min_max_heap<T, pool::vector<T>>

#define STACK_D std::stack<BUCKET_D>

#define BUCKETS_D pool::vector_lazy<BUCKET_D>

#include "fast_map.h"
#include "poolable_vector.h"
#include "poolable_vector_lazy.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>
#include <min_max_heap.h>
#include <chrono>

namespace bb_sort_dictless {

    template<typename T>
    void getBuckets(const BUCKET_D & iterable, BUCKETS_D & buckets, int count) {

        const float minLog = bb_sort::getLog(iterable.findMin());
        const float maxLog = bb_sort::getLog(iterable.findMax());

        const std::tuple<float, float> params = bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        for (auto & item : iterable) {
            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(item) + b));
            index = std::min(count - 1, index);
            buckets[index].push(item);
        }
    }

    template<typename T>
    inline void fillStream(const T & val,
                           std::vector<T> & output,
                           const int index,
                           const int count) {

        for (int i = 0; i < count; ++i) {

            const int newIndex = index + i;
            if (newIndex >= output.size()){

                break;
            }
            output[newIndex] = val;
        }
    }

    template<typename T>
    inline void fillStreamSingle(const T & val,
                           std::vector<T> & output,
                           const int index) {

        if (index < output.size()) {

            output[index] = val;
        }
    }

    template<typename T>
    int caseAllDuplicates(STACK_D & st,
                          BUCKET_D & top,
                          std::vector<T> & output,
                          int index) {

        auto count = top.size();

        fillStream<T>(top.At(0), output, index, count);

        st.pop();

        return count;
    }

    template<typename T>
    int case1(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        fillStreamSingle<T>(top.At(0), output, index);

        st.pop();

        return 1;
    }

    template<typename T>
    int case2(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        fillStreamSingle<T>(top.At(1), output, index);
        fillStreamSingle<T>(top.At(0), output, index + 1);

        st.pop();

        return 2;
    }

    template<typename T>
    int case3(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        //single comparison
        const auto maxMidMin = top.getMaxMidMin();

        const auto maxIndex = std::get<0>(maxMidMin);
        const auto midIndex = std::get<1>(maxMidMin);
        const auto minIndex = std::get<2>(maxMidMin);

        fillStreamSingle<T>(top.At(minIndex), output, index);
        fillStreamSingle<T>(top.At(midIndex), output, index + 1);
        fillStreamSingle<T>(top.At(maxIndex), output, index + 2);

        st.pop();

        return 3;
    }

    template<typename T>
    int caseN(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        if (top.allDuplicates()) {

            return caseAllDuplicates(st, top, output, index);
        }

        long int count = top.size();

        count = std::min(count, 128l);

        BUCKETS_D newBuckets(count);

        getBuckets<T>(top, newBuckets, count);

        st.pop();

        for (int i = newBuckets.size() - 1; i >= 0; --i) {

            if (newBuckets.hasValue(i)) {

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
    void bbSortToStream(STACK_D & st, std::vector<T> & output, const long int count) {

        int index = 0;

        while (st.size() > 0 && index < count) {

            const auto caseIndex = std::min(st.top().size() - 1, 3U);
            const auto switchCaseFunc = func_array<int(
                    STACK_D &,
                    BUCKET_D &,
                    std::vector<T> &,
                    int)>
            ::switchCase[caseIndex];

            index += switchCaseFunc(st, st.top(), output, index);
        }
    }

    template<typename T>
    void prepareTopN(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        if (top.allDuplicates()){

            fillStream<T>(top.At(0), output, index, top.size());
        }

        long int count = top.size();

        count = std::min(count, 128l);

        BUCKETS_D newBuckets(count);

        getBuckets<T>(top, newBuckets, count);

        for (int i = newBuckets.size() - 1; i >= 0; --i) {

            if (newBuckets.hasValue(i))
            {
                st.emplace(std::move(newBuckets[i]));
            }
        }
    }

    template<typename T>
    void sort(std::vector<T> & array) {

        long int size = array.size();

        BUCKET_D top;

        if (size <= 1) {

            return;
        }

        for (auto & item: array) {

            top.push(item);
        }

        STACK_D topBucketsStack;

        prepareTopN(topBucketsStack, top, array, size);

        bbSortToStream<T>(topBucketsStack, array, size);
    }

    template<typename T>
    std::vector<T> getTopSortedLazy(std::vector<T> & array, long int count) {

        long int size = array.size();

        count = std::min(size, count);

        std::vector<T> result(count);

        if (size <= 1) {

            for (int i = 0; i < size; ++i) {
                result[i] = array[i];
            }

            return result;
        }

        BUCKET_D top;

        for (auto & item: array) {

            top.push(item);
        }

        STACK_D topBucketsStack;

        prepareTopN(topBucketsStack, top, result, count);

        bbSortToStream<T>(topBucketsStack, result, count);

        return result;
    }
}
#endif //BBSORT_SOLUTION_BB_SORT_DICTLESS_H
