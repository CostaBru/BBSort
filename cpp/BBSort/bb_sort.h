#ifndef BBSort_H
#define BBSort_H

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <chrono>

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
    float abs = std::abs(x);

    if (abs < 2){
        return x;
    }

    if (x < 0){
        return -mFast_Log2(abs);
    }

    return mFast_Log2(x);
}

template <typename T>
inline std::vector<std::vector<T>> getBuckets(const std::vector<T>& array, int count) {

    T min_element = *std::min_element(array.begin(), array.end());
    T max_element = *std::max_element(array.begin(), array.end());

    float minLog = getLog(min_element);
    float maxLog = getLog(max_element);

    // GetLinearTransform
    float x1 = minLog;
    float x2 = maxLog;
    float y1 = 0.0;
    float y2 = count - 1;

    float a = 0.0;
    float b = 0.0;

    float dx = x1 - x2;
    if (dx != 0.0) [[likely]] {
         a = (y1 - y2) / dx;
         b = y1 - (a * x1);
    }

    std::vector<std::vector<T>> buckets(count);

    for (int i = 0; i < count; ++i) {
        T item = array[i];
        // ApplyLinearTransform
        int index = ((a *  getLog(item) + b));
        buckets[index].push_back(item);
    }

    return buckets;
}


template <typename T>
void bb_sort_core_to_stream(const std::vector<T>& array, const int count, std::vector<T>& output) {

    auto buckets = getBuckets<T>(array, count);
    for (int i = 0; i < buckets.size(); ++i) {
        auto bucket = buckets[i];
        switch (bucket.size()){
            case 0: {
                continue;
            }
            case 1:  {
                output.push_back(bucket[0]);
                break;
            }
            case 2:  {
                T b1 = bucket[0];
                T b2 = bucket[1];
                if (b1 > b2) {
                    std::swap(b1, b2);
                }
                output.push_back(b1);
                output.push_back(b2);
                break;
            }
            default:
                bb_sort_core_to_stream<T>(bucket, bucket.size(), output);
                break;
        }
    }
}

template <typename T>
void bb_sort(const std::vector<T>& array, std::vector<T>& outArray){

    bb_sort_core_to_stream<T>(array, array.size(), outArray);
}

#endif
