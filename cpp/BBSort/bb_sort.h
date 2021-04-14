#ifndef BBSort_H
#define BBSort_H

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>

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
std::vector<std::vector<T>> getBuckets(std::vector<T>& iterable, robin_hood::unordered_map<T, float>& logMap) {

    T min_element = *std::min_element(iterable.begin(), iterable.end());
    T max_element = *std::max_element(iterable.begin(), iterable.end());

    float minLog = logMap[min_element];
    float maxLog = logMap[max_element];

    int count = iterable.size();

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<std::vector<T>> buckets(count);

    for (auto item : iterable) {
        float itemLog = logMap[item];
        // ApplyLinearTransform
        int index = ((a * itemLog + b));
        index = std::min(count - 1, index);
        buckets[index].push_back(item);
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
void bb_sort_core_to_stream(std::stack<std::vector<T>>& st, std::vector<T>& output, robin_hood::unordered_map<T, int>& countMap, robin_hood::unordered_map<T, float>& logMap) {

    while (st.size() > 0) {
        auto bucket = st.top();
        st.pop();

        switch (bucket.size()) {
            case 0: {
                continue;
            }
            case 1: {
                fillStream<T>(bucket[0], output, countMap);
                break;
            }
            case 2: {
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

                auto newBuckets = getBuckets<T>(bucket, logMap);

                for (int i = newBuckets.size() - 1; i >= 0; --i){
                    auto nb = newBuckets[i];
                    st.push(nb);
                }

                break;
        }
    }
}

template<typename T>
std::stack<std::vector<T>> prepareTopBuckets(robin_hood::unordered_map<T, float>& logMap, const T& min_element, const T& max_element) {

    float minLog = logMap[min_element];
    float maxLog = logMap[max_element];

    int count = logMap.size();

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<std::vector<T>> buckets(count);

    //pushing distinct items only for sorting routine
    for (auto const& key : logMap) {
        // ApplyLinearTransform
        int index = ((a * logMap[key.first] + b));
        index = std::min(count - 1, index);
        buckets[index].push_back(key.first);
    }

    std::stack<std::vector<T>> st;

    for (int i = buckets.size() - 1; i >= 0; --i){
        auto nb = buckets[i];
        st.push(nb);
    }
    return st;
}

template <typename T>
void bb_sort(const std::vector<T>& array, std::vector<T>& outArray){

    if (array.size() == 0){
        return;
    }

    robin_hood::unordered_map<T, int> countMap;
    robin_hood::unordered_map<T, float> logMap;

    T min_element = array[0];
    T max_element = array[0];

    for (int i = 0; i < array.size(); ++i) {

        T item = array[i];

        if (!countMap.contains(item)){

            logMap[item] = getLog(item);

            if (item < min_element){
                min_element = item;
            }

            if (item > max_element){
                max_element = item;
            }
        }

        countMap[item] += 1;
    }

    std::stack<std::vector<T>> topBucketsStack = prepareTopBuckets(logMap, min_element, max_element);

    bb_sort_core_to_stream<T>(topBucketsStack, outArray, countMap, logMap);
}

#endif
