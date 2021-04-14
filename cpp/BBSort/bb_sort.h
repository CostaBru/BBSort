#ifndef BBSort_H
#define BBSort_H

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>

inline float mFast_Log2(float val) {
    union { float val; int32_t x; } u = { val };
    float log_2 = (float)(((u.x >> 23) & 255) - 128);
    u.x   &= ~(255 << 23);
    u.x   += 127 << 23;
    log_2 += ((-0.3358287811f) * u.val + 2.0f) * u.val  -0.65871759316667f;
    return (log_2);
}

inline float getLog(float x)
{
    float as = std::abs(x);

    if (as < 2){
        return x;
    }

    if (x < 0){
        return -mFast_Log2(as);
    }

    return mFast_Log2(x);
}

inline std::tuple<float , float> GetLinearTransformParams(float x1, float x2, float y1, float y2) {
    float dx = x1 - x2;
    if (dx == 0.0) [[unlikely]] {
        return std::make_tuple(0.0, 0.0);
    }

    float a = (y1 - y2) / dx;
    float b = y1 - (a * x1);

    return std::make_tuple(a, b);
}

template <typename T>
std::vector<robin_hood::unordered_set<T>> getBuckets(robin_hood::unordered_set<T>& array, int count, robin_hood::unordered_map<T, float>& logMap) {

    T min_element = *std::min_element(array.begin(), array.end());
    T max_element = *std::max_element(array.begin(), array.end());

    float minLog = logMap[min_element];
    float maxLog = logMap[max_element];

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<robin_hood::unordered_set<T>> buckets(count);

    for (auto item : array) {
        float itemLog = getLog(item);
        // ApplyLinearTransform
        int index = ((a * itemLog + b));
        index = std::min(count - 1, index);
        buckets[index].insert(item);
    }

    return buckets;
}

template <typename T>
inline void fillStream(const T& val, std::vector<T>& output, robin_hood::unordered_map<T, int>& countMap){
    int valCount = countMap[val];
    for (int i = 0; i < valCount; ++i) {
        output.push_back(val);
    }
}

template <typename T>
void bb_sort_core_to_stream(robin_hood::unordered_set<T>& array, const int count, std::vector<T>& output, robin_hood::unordered_map<T, int>& countMap, robin_hood::unordered_map<T, float>& logMap) {

    auto buckets = getBuckets<T>(array, count, logMap);
    for (int i = 0; i < buckets.size(); ++i) {
        auto bucket = buckets[i];
        switch (bucket.size()){
            case 0: {
                continue;
            }
            case 1:  {
                fillStream<T>(*bucket.begin(), output, countMap);
                break;
            }
            case 2:  {
                auto it = bucket.begin();
                T b1 = *it;
                it++;
                T b2 = *it;
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
void bb_sort(const std::vector<T>& array, std::vector<T>& outArray){

    robin_hood::unordered_map<T, int> countMap;
    robin_hood::unordered_map<T, float> logMap;

    robin_hood::unordered_set<T> mainBucket(array.size());

    for (int i = 0; i < array.size(); ++i) {

        T item = array[i];

        countMap[item] += 1;

        if (!logMap.contains(item))
        {
            logMap[item] = getLog(item);
        }

        mainBucket.insert(item);
    }

    bb_sort_core_to_stream<T>(mainBucket, mainBucket.size(), outArray, countMap, logMap);
}

#endif
