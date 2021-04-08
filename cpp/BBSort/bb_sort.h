#ifndef BBSort_H
#define BBSort_H

#include "bb_sort.h"
#include "fast_map.h"
#include <vector>
#include <bit>
#include <tuple>
#include <cmath>

template <typename T>
float getLog(T x)
{
    if (x == 0){
        return 0;
    }

    if (x < 0){
        return -std::log2(std::abs(x));
    }

    return std::log2(x);
}

template <typename T>
std::tuple<float , float> GetLinearTransformParams(T x1, T x2, T y1, T y2) {
    T dx = x1 - x2;
    if (dx == 0)
    {
        return std::make_tuple(0, 0);
    }

    float a = (y1 - y2) / dx;
    float b = y1 - (a * x1);

    return std::make_tuple(a, b);
}

template <typename T>
std::vector<std::vector<T>> getBuckets(std::vector<T> array, int count, robin_hood::unordered_map<T, int> countMap) {

    T min_element = *std::min_element(array.begin(), array.end());
    T max_element = *std::max_element(array.begin(), array.end());

    for (int i = 0; i < count; ++i) {

        T item = array[i];

        if (countMap.contains(item)) {
            countMap[item] += 1;
        } else {
            countMap[item] = 1;
        }
    }

    std::tuple<float, float> params = GetLinearTransformParams<T>(getLog(min_element), getLog(max_element), 0, count);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<std::vector<T>> buckets(count);

    for (auto const& key : countMap) {
        // ApplyLinearTransform
        int index = int((a * getLog(key.first)) + b);
        buckets[index].push_back(key.first);
    }

    return buckets;
}

template <typename T>
void fillStream(T val, std::vector<T> output, robin_hood::unordered_map<T, int> countMap){
    int valCount = countMap[val];
    for (int i = 0; i < valCount; ++i) {
        output.push_back(val);
    }
}

template <typename T>
void bb_sort_core_to_stream(std::vector<T> array, int count, std::vector<T> output) {

    robin_hood::unordered_map<T, int> countMap(count);

    auto buckets = getBuckets<T>(array, count, countMap);

    for (const std::vector<T> bucket : buckets) {
        int bucketCount = bucket.size();

        if (bucketCount == 1)
        {
            fillStream<T>(bucket[0], output, countMap);
        }
        else if (bucketCount == 2) {
            T b1 = bucket[0];
            T b2 = bucket[1];
            if (b1 > b2) {
                std::swap(b1, b2);
            }
            fillStream<T>(b1, output, countMap);
            fillStream<T>(b2, output, countMap);
        } else if (bucketCount > 0) {
            bb_sort_core_to_stream<T>(bucket, bucketCount, output);
        }
    }
}

template <typename T>
void bb_sort(std::vector<T> array, std::vector<T> outArray){
    bb_sort_core_to_stream<T>(array, array.size(), outArray);
}

#endif
