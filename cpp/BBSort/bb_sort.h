#ifndef BBSort_H
#define BBSort_H

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>

inline float mFast_Log2(const float val) {
    union { float val; int32_t x; } u = { val };
    float log_2 = (float)(((u.x >> 23) & 255) - 128);
    u.x   &= ~(255 << 23);
    u.x   += 127 << 23;
    log_2 += ((-0.3358287811f) * u.val + 2.0f) * u.val  -0.65871759316667f;
    return (log_2);
}

inline float getLog(const float x)
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

inline std::tuple<float , float> GetLinearTransformParams(const float x1, const float x2, const float y1, const float y2) {
    float dx = x1 - x2;
    if (dx == 0.0) [[unlikely]] {
        return std::make_tuple(0.0, 0.0);
    }

    float a = (y1 - y2) / dx;
    float b = y1 - (a * x1);

    return std::make_tuple(a, b);
}

template <typename T>
class sort_item {
public :
    T value;
    float log;
    int count;
};

template <typename T>
std::vector<std::vector<sort_item<T>>> getBuckets(const std::vector<sort_item<T>>& iterable) {

    sort_item min_element = iterable[0];
    sort_item max_element = iterable[0];

    for (auto item : iterable) {

        if (item.value < min_element.value) {
            min_element = item;
        }

        if (item.value > max_element.value) {
            max_element = item;
        }
    }


    float minLog = min_element.log;
    float maxLog = max_element.log;

    int count = iterable.size();

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<std::vector<sort_item<T>>> buckets(count);

    for (auto item : iterable) {
        // ApplyLinearTransform
        int index = ((a * item.log + b));
        index = std::min(count - 1, index);
        buckets[index].push_back(item);
    }

    return buckets;
}

template <typename T>
inline void fillStream(const sort_item<T>& val, std::vector<T>& output){
    for (int i = 0; i < val.count; ++i) {
        output.push_back(val.value);
    }
}

template <typename T>
void bb_sort_core_to_stream(std::stack<std::vector<sort_item<T>>>& st, std::vector<T>& output) {

    while (st.size() > 0) {
        auto bucket = st.top();
        st.pop();

        switch (bucket.size()) {
            case 0: {
                continue;
            }
            case 1: {
                fillStream<T>(bucket[0], output);
                break;
            }
            case 2: {
                sort_item b1 = bucket[0];
                sort_item b2 = bucket[1];
                if (b1.value > b2.value) {
                    fillStream<T>(b2, output);
                    fillStream<T>(b1, output);
                }
                else{
                    fillStream<T>(b1, output);
                    fillStream<T>(b2, output);
                }

                break;
            }
            default:

                auto newBuckets = getBuckets<T>(bucket);

                for (int i = newBuckets.size() - 1; i >= 0; --i){
                    auto nb = newBuckets[i];
                    st.push(nb);
                }

                break;
        }
    }
}

template <typename T>
std::stack<std::vector<sort_item<T>>> prepareTopBuckets(const std::vector<sort_item<T>>& items, const sort_item<T>& min_element, const sort_item<T>& max_element) {

    float minLog = min_element.log;
    float maxLog = max_element.log;

    int count = items.size();

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<std::vector<sort_item<T>>> buckets(count);

    //pushing distinct items only for sorting routine
    for (auto const& item : items) {
        // ApplyLinearTransform
        int index = ((a * item.log + b));
        index = std::min(count - 1, index);
        buckets[index].push_back(item);
    }

    std::stack<std::vector<sort_item<T>>> st;

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

    T min_element = array[0];
    T max_element = array[0];

    std::vector<sort_item<T>> items;

    items.reserve(array.size());

    for (int i = 0; i < array.size(); ++i) {

        T item = array[i];

        if (!countMap.contains(item)){

            if (item < min_element){
                min_element = item;
            }

            if (item > max_element){
                max_element = item;
            }

            sort_item<T> sortItem;

            sortItem.value = item;
            sortItem.log = getLog(item);
            sortItem.count = 1;

            items.push_back(sortItem);

            countMap[item] = items.size() - 1;
        }
        else{
            items[countMap[item]].count += 1;
        }
    }

    sort_item min_element_sort = items[countMap[min_element]];
    sort_item max_element_sort = items[countMap[max_element]];

    std::stack<std::vector<sort_item<T>>> topBucketsStack = prepareTopBuckets(items, min_element_sort, max_element_sort);

    bb_sort_core_to_stream<T>(topBucketsStack, outArray);
}

#endif
