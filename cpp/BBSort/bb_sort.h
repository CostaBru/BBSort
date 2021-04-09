#ifndef BBSort_H
#define BBSort_H

#include "bb_sort.h"
#include "fast_map.h"
#include <vector>
#include <bit>
#include <tuple>
#include <cmath>

inline float getLog(float x)
{
    if (x == 0){
        return 0;
    }

    double as = std::abs(x);

    if (as < 2){
        return x;
    }

    if (x < 0){
        return -std::log2(as);
    }

    return std::log2(x);
}

inline std::tuple<float , float> GetLinearTransformParams(float x1, float x2, float y1, float y2) {
    float dx = x1 - x2;
    if (dx == 0)
    {
        return std::make_tuple(0, 0);
    }

    float a = ((float)(y1 - y2)) / dx;
    float b = y1 - (a * x1);

    return std::make_tuple(a, b);
}

template <typename T>
std::vector<std::vector<T>> getBuckets(std::vector<T>& array, int count, robin_hood::unordered_map<T, float>& logMap) {

    T min_element = *std::min_element(array.begin(), array.end());
    T max_element = *std::max_element(array.begin(), array.end());

    float minLog = logMap[min_element];
    float maxLog = logMap[max_element];

    std::tuple<float , float> params = GetLinearTransformParams(minLog, maxLog, 0, count);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<std::vector<T>> buckets(count  + 1);

    for (int i = 0; i < count; ++i) {
        T item = array[i];
        // ApplyLinearTransform
        int index = ((a *  logMap[item]) + b);
        buckets[index].push_back(item);
    }

    return buckets;
}

template <typename T>
inline void fillStream(T& val, std::vector<T>& output, robin_hood::unordered_map<T, int>& countMap){
    int valCount = countMap[val];
    for (int i = 0; i < valCount; ++i) {
        output.push_back(val);
    }
}

template <typename T>
void bb_sort_core_to_stream(std::vector<T>& array, int count, std::vector<T>& output, robin_hood::unordered_map<T, int>& countMap, robin_hood::unordered_map<T, float>& logMap) {

    auto buckets = getBuckets<T>(array, count, logMap);
    int bs = buckets.size();
    for (int i = 0; i < bs; ++i) {
        auto bucket = buckets[i];
        switch (bucket.size()){
            case 0: {
                continue;
            }
            case 1:  {
                fillStream<T>(bucket[0], output, countMap);
                break;
            }
            case 2:  {
                T b1 = bucket[0];
                T b2 = bucket[1];
                if (b1 > b2) {
                    std::swap(b1, b2);
                }
                fillStream<T>(b1, output, countMap);
                fillStream<T>(b2, output, countMap);
                break;
            }
            default:
                bb_sort_core_to_stream<T>(bucket, bucket.size(), output, countMap, logMap);
                break;
        }
    }
}

template <typename T>
void bb_sort(std::vector<T> array, std::vector<T>& outArray){

    robin_hood::unordered_map<T, int> countMap;
    robin_hood::unordered_map<T, float> logMap;

    int  count = array.size();

    for (int i = 0; i < count; ++i) {

        T item = array[i];

        countMap[item] += 1;

        if (!logMap.contains(item))
        {
            logMap[item] = getLog(item);
        }
    }

    bb_sort_core_to_stream<T>(array, array.size(), outArray, countMap, logMap);
}

#endif
