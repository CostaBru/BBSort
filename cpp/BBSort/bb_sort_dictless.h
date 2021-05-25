#ifndef BBSORT_SOLUTION_BB_SORT_DICTLESS_H
#define BBSORT_SOLUTION_BB_SORT_DICTLESS_H

#define BUCKET_D minmax::min_max_heap<T, pool::vector<T>>

#define STACK_D pool::vector<BUCKET_D>

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
    void getBuckets(BUCKET_D & iterable, BUCKETS_D & buckets, int count) {

        float minLog = bb_sort::getLog(iterable.findMin());
        float maxLog = bb_sort::getLog(iterable.findMax());

        std::tuple<float, float> params = bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        float a = std::get<0>(params);
        float b = std::get<1>(params);

        for(int i = 0; i < iterable.size(); ++i) {
            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(iterable.At(i)) + b));
            index = std::min(count - 1, index);
            buckets[index].emplace(iterable.At(i));
        }
    }

    template<typename T>
    int caseAllDuplicates(STACK_D & st,
                          BUCKET_D & top,
                          std::vector<T> & output,
                          int index) {

        auto count = top.size();

        auto val = top.At(0);

        for (int i = 0; i < count; ++i) {

            output[index + i] = val;
        }

        st.pop_back();

        return count;
    }

    template<typename T>
    int case1(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        output[index] = top.At(0);

        st.pop_back();

        return 1;
    }

    template<typename T>
    int case2(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        output[index] = top.At(1);
        output[index + 1] = top.At(0);

        st.pop_back();

        return 2;
    }

    template<typename T>
    int case3(STACK_D & st,
              BUCKET_D & top,
              std::vector<T> & output,
              int index) {

        //single comparison
        const auto maxMidMin = top.getMaxMidMin();

        output[index] = top.At(std::get<2>(maxMidMin));
        output[index + 1] = top.At(std::get<1>(maxMidMin));
        output[index + 2] = top.At(std::get<0>(maxMidMin));

        st.pop_back();

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

        long int count = (top.size() / 2) + 1;

        BUCKETS_D newBuckets(count);

        getBuckets<T>(top, newBuckets, count);

        st.pop_back();

        for (int i = newBuckets.size() - 1; i >= 0; --i) {

            if (newBuckets.hasValue(i)) {

                st.emplace_back(std::move(newBuckets[i]));
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

        while (!st.empty()) {

            const auto caseIndex = std::min(st.back().size() - 1, 3U);
            const auto switchCaseFunc = func_array<int(
                    STACK_D &,
                    BUCKET_D &,
                    std::vector<T> &,
                    int)>
            ::switchCase[caseIndex];

            index += switchCaseFunc(st, st.back(), output, index);
        }
    }

    template<typename T>
    void getTopStackBuckets(std::vector<T> & array, STACK_D & st) {

        T min = array[0];
        T max = array[0];

        for(int i = 1; i < array.size(); ++i){

            min = std::min(min, array[i]);
            max = std::max(max, array[i]);
        }

        if (min == max) {

            return;
        }

        int count = array.size();

        count = std::min(count, 128);

        BUCKETS_D newBuckets(count);

        const float minLog = bb_sort::getLog(min);
        const float maxLog = bb_sort::getLog(max);

        const std::tuple<float, float> params = bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        for(int i = 0; i < array.size(); ++i) {

            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(array[i]) + b));
            index = std::min(count - 1, index);
            newBuckets[index].emplace(array[i]);
        }

        for (int i = count - 1; i >= 0; --i) {

            if (newBuckets.hasValue(i)) {

                st.emplace_back(std::move(newBuckets[i]));
            }
        }
    }

    template<typename T>
    void sort(std::vector<T> & array) {

        long int size = array.size();

        if (size <= 1) {

            return;
        }

        STACK_D st;

        getTopStackBuckets(array, st);

        bbSortToStream<T>(st, array, size);
    }
}
#endif //BBSORT_SOLUTION_BB_SORT_DICTLESS_H
