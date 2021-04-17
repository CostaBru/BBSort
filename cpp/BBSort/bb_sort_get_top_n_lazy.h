#ifndef BBSort_top_n_H
#define BBSort_top_n_H

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>
#include <min_max_heap.h>
#include <chrono>
#include <bb_sort.h>

namespace bb_sort_top_n_lazy {

    template<typename T>
    std::vector<minmax::min_max_heap<T>> getBuckets(const minmax::min_max_heap<T> &iterable) {

        const T minEl = iterable.findMin();
        const T maxEl = iterable.findMax();

        const float minLog = bb_sort::getLog(minEl);
        const float maxLog = bb_sort::getLog(maxEl);

        const int count = (iterable.size() / 2) + 1;

        const std::tuple<float, float> params = bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        std::vector<minmax::min_max_heap<T>> buckets(count);

        for (auto const item : iterable) {
            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(item) + b));
            index = std::min(count - 1, index);
            buckets[index].push(item);
        }

        return buckets;
    }

    template<typename T>
    inline void fillStream(const T &val,
                           std::vector<T> &output,
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
    int case1(std::stack<minmax::min_max_heap<T>> &st,
              std::vector<T> &output,
              const minmax::min_max_heap<T> &bucket,
              int index,
              robin_hood::unordered_map<T, int>& countMap) {

        const T b1 = *bucket.begin();
        const auto count = countMap[b1];
        fillStream<T>(b1, output, index, count);
        return count;
    }

    template<typename T>
    int case2(std::stack<minmax::min_max_heap<T>> &st,
              std::vector<T> &output,
              const minmax::min_max_heap<T> &bucket,
              int index,
              robin_hood::unordered_map<T, int>& countMap) {

        auto it = bucket.begin();
        const T b2 = *it;
        const auto count2 = countMap[b2];
        it++;
        const T b1 = *it;
        const auto count1 = countMap[b1];
        fillStream<T>(b1, output, index, count1);
        fillStream<T>(b2, output, index + count1, count2);
        return count1 + count2;
    }

    template<typename T>
    int caseN(std::stack<minmax::min_max_heap<T>> &st,
              std::vector<T> &output,
              const minmax::min_max_heap<T> &bucket,
              int index,
              robin_hood::unordered_map<T, int>& countMap) {

        const auto newBuckets = getBuckets<T>(bucket);
        for (int i = newBuckets.size() - 1; i >= 0; --i) {
            const auto nb = newBuckets[i];
            if (nb.size() > 0)
            {
                st.push(nb);
            }
        }
        return 0;
    }

    template<typename Func>
    struct func_array {
        static Func *const switchCase[];
    };

    template<typename Func>
    Func *const func_array<Func>::switchCase[] = {case1, case2, caseN};

    template<typename T>
    void bbSortToStream(std::stack<minmax::min_max_heap<T>> &st, std::vector<T> &output, const long int count, robin_hood::unordered_map<T, int>& countMap) {

        int index = 0;

        while (st.size() > 0 && index < count) {

            const auto bucket = st.top();
            st.pop();

            const auto caseIndex = std::min(bucket.size() - 1, 2U);
            const auto switchCaseFunc = func_array<int(
                    std::stack<minmax::min_max_heap<T>> &,
                    std::vector<T> &,
                    const minmax::min_max_heap<T> &,
                    int,
                    robin_hood::unordered_map<T, int>&)>
                            ::switchCase[caseIndex];

            index += switchCaseFunc(st, output, bucket, index, countMap);
        }
    }

    template<typename T>
    std::stack<minmax::min_max_heap<T>>  prepareTopBuckets(
            const robin_hood::unordered_map<T, int> &map,
            const T &minSortEl,
            const T &maxSortEl) {

        const float minLog = bb_sort::getLog(minSortEl);
        const float maxLog = bb_sort::getLog(maxSortEl);

        const int count = (map.size() / 2) + 1;

        const std::tuple<float, float> params =  bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        std::vector<minmax::min_max_heap<T>> buckets(count);

        //pushing distinct items only
        for (auto const key : map) {
            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(key.first) + b));
            index = std::min(count - 1, index);
            buckets[index].push(key.first);
        }

        std::stack<minmax::min_max_heap<T>> st;

        for (int i = buckets.size() - 1; i >= 0; --i) {

            const auto nb = buckets[i];
            if (nb.size() > 0)
            {
                st.push(nb);
            }
        }

        return st;
    }

    template<typename T>
    std::stack<minmax::min_max_heap<T>> getTopStackBuckets(const std::vector<T> &array, robin_hood::unordered_map<T, int>& countMap) {

        T minEl = array[0];
        T maxEl = array[0];

        for (const auto item: array) {

            minEl = std::min(item, minEl);
            maxEl = std::max(item, maxEl);
            countMap[item] += 1;
        }

        return prepareTopBuckets(countMap, minEl, maxEl);
    }

    template<typename T>
    std::vector<T> getTopSortedLazy(std::vector<T> &array, long int count) {

        long int size = array.size();

        count = std::min(size, count);
        std::vector<T> result(count);

        if (size <= 1) {

            for (int i = 0; i < size; ++i) {
                result[i] = array[i];
            }

            return result;
        }
        robin_hood::unordered_map<T, int> distinctMap;

        std::stack<minmax::min_max_heap<T>> topBucketsStack = getTopStackBuckets(array, distinctMap);
        bbSortToStream<T>(topBucketsStack, result, count, distinctMap);

        return result;
    }
}
#endif
