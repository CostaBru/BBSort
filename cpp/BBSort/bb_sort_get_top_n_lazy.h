#ifndef BBSort_top_n_H
#define BBSort_top_n_H

#define BUCKET_TOPN minmax::min_max_heap<T, pool::vector<T>>

#define STACK_TOPN std::stack<BUCKET_TOPN>

#define MAP_TOPN robin_hood::unordered_map<T, int>

#define BUCKETS_TOPN pool::vector_lazy<BUCKET_TOPN>

#include "fast_map.h"
#include "poolable_vector.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>
#include <min_max_heap.h>
#include <chrono>
#include <bb_sort.h>

namespace bb_sort_top_n_lazy {

    template<typename T>
    void getBuckets(const BUCKET_TOPN &iterable, BUCKETS_TOPN &buckets, int count) {

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
    int case1(STACK_TOPN &st,
              std::vector<T> &output,
              int index,
              MAP_TOPN &countMap) {

        const T b1 = *(st.top()).begin();
        const auto count = countMap[b1];

        fillStream<T>(b1, output, index, count);

        st.pop();

        return count;
    }

    template<typename T>
    int case2(STACK_TOPN &st,
              std::vector<T> &output,
              int index,
              MAP_TOPN &countMap) {

        auto it = st.top().begin();

        const T b2 = *it;
        const auto count2 = countMap[b2];
        it++;
        const T b1 = *it;
        const auto count1 = countMap[b1];

        fillStream<T>(b1, output, index, count1);
        fillStream<T>(b2, output, index + count1, count2);

        st.pop();

        return count1 + count2;
    }

    template<typename T>
    int case3(STACK_TOPN &st,
              std::vector<T> &output,
              int index,
              MAP_TOPN &countMap) {

        //single comparison
        auto &top = st.top();
        auto maxMidMin = top.getMaxMidMin();

        auto maxIndex = std::get<0>(maxMidMin);
        auto midIndex = std::get<1>(maxMidMin);
        auto minIndex = std::get<2>(maxMidMin);

        auto count1 = countMap[top.At(minIndex)];
        auto count2 = countMap[top.At(midIndex)];
        auto count3 = countMap[top.At(maxIndex)];

        fillStream<T>(top.At(minIndex), output, index, count1);
        fillStream<T>(top.At(midIndex), output, index + count1, count2);
        fillStream<T>(top.At(maxIndex), output, index + count1 + count2, count3);

        st.pop();

        return count1 + count2 + count3;
    }

    template<typename T>
    int caseN(STACK_TOPN &st,
              std::vector<T> &output,
              int index,
              MAP_TOPN &countMap) {

        const int count = (st.top().size() / 2) + 1;

        BUCKETS_TOPN newBuckets(count);

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
    void bbSortToStream(STACK_TOPN &st, std::vector<T> &output, const long int count, MAP_TOPN& countMap) {

        int index = 0;

        while (st.size() > 0 && index < count) {

            const auto caseIndex = std::min(st.top().size() - 1, 3U);
            const auto switchCaseFunc = func_array<int(
                    STACK_TOPN &,
                    std::vector<T> &,
                    int,
                    MAP_TOPN &)>
            ::switchCase[caseIndex];

            index += switchCaseFunc(st, output, index, countMap);
        }
    }

    template<typename T>
     void prepareTopBuckets(STACK_TOPN &st,
                            BUCKETS_TOPN &buckets,
                            const MAP_TOPN &map,
                            const T &minSortEl,
                            const T &maxSortEl,
                            int count) {

        const float minLog = bb_sort::getLog(minSortEl);
        const float maxLog = bb_sort::getLog(maxSortEl);

        const std::tuple<float, float> params =  bb_sort::GetLinearTransformParams(minLog, maxLog, 0, count - 1);

        const float a = std::get<0>(params);
        const float b = std::get<1>(params);

        //pushing distinct items only
        for (auto & key : map) {
            // ApplyLinearTransform
            int index = ((a * bb_sort::getLog(key.first) + b));
            index = std::min(count - 1, index);
            buckets[index].push(key.first);
        }

        for (int i = buckets.size() - 1; i >= 0; --i) {

            if (buckets[i].size() > 0)
            {
                //copy
                st.emplace(std::move(buckets[i]));
            }
        }
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

        MAP_TOPN countMap;

        T minEl = array[0];
        T maxEl = array[0];

        for (const auto& item: array) {

            minEl = std::min(item, minEl);
            maxEl = std::max(item, maxEl);
            countMap[item] += 1;
        }

        long int distinctCount = countMap.size();

        const int bucketCount =  std::min(distinctCount, 128l);

        BUCKETS_TOPN buckets(bucketCount);

        STACK_TOPN topBucketsStack;

        prepareTopBuckets(topBucketsStack, buckets, countMap, minEl, maxEl, bucketCount);

        bbSortToStream<T>(topBucketsStack, result, count, countMap);

        return result;
    }
}
#endif
