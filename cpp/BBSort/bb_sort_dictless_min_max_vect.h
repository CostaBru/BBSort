#ifndef BBSORT_SOLUTION_BB_SORT_DICTLESS_MIN_MAX_VECT_H
#define BBSORT_SOLUTION_BB_SORT_DICTLESS_MIN_MAX_VECT_H

#define BUCKET_DM  minmax::min_max_mid_vector<T, pool::vector<T>>
#define STACK_DM   pool::vector_lazy<BUCKET_DM>
#define BUCKETS_DM pool::vector_lazy<BUCKET_DM>

#include "fast_map.h"
#include "poolable_vector.h"
#include "poolable_vector_lazy.h"
#include "min_max_mid_vector.h"

#include <vector>
#include <tuple>
#include <cmath>

namespace bb_sort_dictless_min_max_vect {

    template<typename T>
    void getBuckets(BUCKET_DM & minMaxVector, float minLog, float maxLog, STACK_DM & buckets, int count) {

        std::tuple<float, float> params = bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        float a = std::get<0>(params);
        float b = std::get<1>(params);

        for(int i = 0; i < minMaxVector.size(); ++i) {

            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(minMaxVector.Storage[i]) + b));
            index = std::min(count - 1, index);
            buckets[index].push(minMaxVector.Storage[i]);
        }
    }

    template<typename T>
    int caseAllDuplicates(STACK_DM & st,
                          BUCKET_DM & top,
                          std::vector<T> & output,
                          int index) {

        auto count = top.size();

        for (int i = 0; i < count; ++i) {

            output[index + i] = top.Min;
        }

        st.pop_back();

        return count;
    }

    template<typename T>
    int case1(STACK_DM & st,
              BUCKET_DM & top,
              std::vector<T> & output,
              int index) {

        output[index] = top.Min;

        st.pop_back();

        return 1;
    }

    template<typename T>
    int case2(STACK_DM & st,
              BUCKET_DM & top,
              std::vector<T> & output,
              int index) {

        output[index]     = top.Min;
        output[index + 1] = top.Max;

        st.pop_back();

        return 2;
    }

    template<typename T>
    int case3(STACK_DM & st,
              BUCKET_DM & top,
              std::vector<T> & output,
              int index) {

        output[index]     = top.Min;
        output[index + 1] = top.Mid;
        output[index + 2] = top.Max;

        st.pop_back();

        return 3;
    }

    template<typename T>
    int caseN(STACK_DM & st,
              BUCKET_DM & top,
              std::vector<T> & output,
              int index) {

        float minLog = bb_sort::getLog(top.Min);
        float maxLog = bb_sort::getLog(top.Max);

        if(maxLog - minLog < 0.1){

            if (top.Max == top.Min) {

                return caseAllDuplicates(st, top, output, index);
            }

            std::sort (top.Storage.begin(), top.Storage.end());

            auto count = top.size();

            for (int i = 0; i < count; ++i) {

                output[index + i] = top.Storage.at(i);
            }

            st.pop_back();

            return count;
        }

        long int count = (top.size() / 2) + 1;

        BUCKETS_DM newBuckets(count);

        getBuckets<T>(top, minLog, maxLog, newBuckets, count);

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
    void bbSortToStream(STACK_DM & st, std::vector<T> & output, const long int count) {

        int index = 0;

        while (!st.empty()) {

            if (st.hasBack()) {

                const auto caseIndex = std::min(st.back().size() - 1, 3U);
                const auto switchCaseFunc = func_array<int(
                        STACK_DM &,
                        BUCKET_DM &,
                        std::vector<T> &,
                        int)>
                ::switchCase[caseIndex];

                index += switchCaseFunc(st, st.back(), output, index);
            } else {

                st.pop_back();
            }
        }
    }

    template<typename T>
    void getTopStackBuckets(std::vector<T> & array, STACK_DM & st, int count) {

        T min = array[0];
        T max = array[0];

        for(int i = 1; i < array.size(); ++i){

            min = std::min(min, array[i]);
            max = std::max(max, array[i]);
        }

        if (min == max) {

            return;
        }

        const std::tuple<float, float> params = bb_sort::GetLinearTransformParams(bb_sort::getLog(min), bb_sort::getLog(max), 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        for(int i = 0; i < array.size(); ++i) {

            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(array[i]) + b));
            int stackIndex = count - std::min(count - 1, index) - 1;
            st[stackIndex].push(array[i]);
        }
    }

    template<typename T>
    void sort(std::vector<T> & array) {

        long int size = array.size();

        if (size <= 1) {

            return;
        }

        int count = array.size();

        count = std::min(count, 1024);

        STACK_DM st(count);

        getTopStackBuckets(array, st, count);

        bbSortToStream<T>(st, array, size);
    }
}
#endif //BBSORT_SOLUTION_BB_SORT_DICTLESS_MIN_MAX_VECT_H
