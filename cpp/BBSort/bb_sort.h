#ifndef BBSort_H
#define BBSort_H

#include "fast_map.h"
#include <vector>
#include <tuple>
#include <cmath>
#include <stack>
#include <min_max_heap.h>


inline float fastLog2(const float val) {
    union { float val; int32_t x; } u = { val };
    float lg2 = (float)(((u.x >> 23) & 255) - 128);
    u.x   &= ~(255 << 23);
    u.x   += 127 << 23;
    lg2 += ((-0.3358287811f) * u.val + 2.0f) * u.val - 0.65871759316667f;
    return lg2;
}

inline float getLog(const float x)
{
    float abs = std::abs(x);

    if (abs < 2){
        return x;
    }

    float lg = fastLog2(abs);

    return x < 0 ? -lg : lg;
}

inline std::tuple<float , float> GetLinearTransformParams(const float x1, const float x2, const float y1, const float y2) {
    float dx = x1 - x2;

    if (dx != 0.0) [[likely]] {
        float a = (y1 - y2) / dx;
        float b = y1 - (a * x1);

        return std::make_tuple(a, b);
    }

    return std::make_tuple(0.0, 0.0);
}

template <typename T>
struct sort_item {
public :
    T value;
    int count;

    sort_item(T val){
        value = val;
        count = 1;
    }

    friend bool operator> (const sort_item &c1, const sort_item &c2){
        return c1.value > c2.value;
    }
    friend bool operator<= (const sort_item &c1, const sort_item &c2)
    {
        return c1.value <= c2.value;
    }

    friend bool operator< (const sort_item &c1, const sort_item &c2) {
        return c1.value < c2.value;
    }
    friend bool operator>= (const sort_item &c1, const sort_item &c2) {
        return c1.value >= c2.value;
    }

    friend bool operator== (const sort_item &c1, const sort_item &c2) {
        return c1.value == c2.value;
    }
    friend bool operator!= (const sort_item &c1, const sort_item &c2) {
        return c1.value != c2.value;
    }
};

template<typename T>
std::stack<minmax::min_max_heap<sort_item<T>>> getTopStackBuckets(const std::vector<T> &array);

template <typename T>
std::vector<minmax::min_max_heap<sort_item<T>>> getBuckets(const minmax::min_max_heap<sort_item<T>>& iterable) {

    sort_item minEl = iterable.findMin();
    sort_item maxEl = iterable.findMax();

    float minLog = getLog(minEl.value);
    float maxLog = getLog(maxEl.value);

    int count = (iterable.size() / 2) + 1;

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<minmax::min_max_heap<sort_item<T>>> buckets(count);

    for (auto const item : iterable) {
        // ApplyLinearTransform
        int index = ((a * getLog(item.value) + b));
        index = std::min(count - 1, index);
        buckets[index].push(item);
    }

    return buckets;
}

template <typename T>
inline void fillStream(const sort_item<T>& val, std::vector<T>& output, int index){
    for (int i = 0; i < val.count; ++i) {
        output[index + i] = val.value;
    }
}

template <typename T>
int case0(std::stack<minmax::min_max_heap<sort_item<T>>>& st, std::vector<T>& output, minmax::min_max_heap<sort_item<T>>& bucket, int index) {
    return 0;
}

template <typename T>
int case1(std::stack<minmax::min_max_heap<sort_item<T>>>& st, std::vector<T>& output, minmax::min_max_heap<sort_item<T>>& bucket, int index) {
    sort_item b1 = *bucket.begin();
    fillStream<T>(b1, output, index);
    return b1.count;
}

template <typename T>
int case2(std::stack<minmax::min_max_heap<sort_item<T>>>& st, std::vector<T>& output, minmax::min_max_heap<sort_item<T>>& bucket, int index) {
    auto it = bucket.begin();
    sort_item b2 = *it;
    it++;
    sort_item b1 = *it;
    fillStream<T>(b1, output, index);
    fillStream<T>(b2, output, index + b1.count);
    return b1.count + b2.count;
}

template <typename T>
int caseN(std::stack<minmax::min_max_heap<sort_item<T>>>& st, std::vector<T>& output, minmax::min_max_heap<sort_item<T>>& bucket, int index) {
    auto newBuckets = getBuckets<T>(bucket);
    for (int i = newBuckets.size() - 1; i >= 0; --i) {
        auto nb = newBuckets[i];
        st.push(nb);
    }
    return 0;
}

template<typename Func>
struct func_array {
    static Func *const switchCase[];
};

template<typename Func>
Func *const func_array<Func>::switchCase[] = {case0, case1, case2, caseN };

template <typename T>
void bbSortToStream(std::stack<minmax::min_max_heap<sort_item<T>>>& st, std::vector<T>& output) {

    int index = 0;

    while (st.size() > 0) {
        auto bucket = st.top();
        st.pop();

        auto caseIndex = std::min(bucket.size(), 3U);

        auto switchCaseFunc = func_array<int(std::stack<minmax::min_max_heap<sort_item<T>>> &, std::vector<T> &,
                                             minmax::min_max_heap<sort_item<T>> &, int)>::switchCase[caseIndex];

        index += switchCaseFunc(st, output, bucket, index);
    }
}

template <typename T>
std::stack<minmax::min_max_heap<sort_item<T>>> prepareTopBuckets(const std::vector<sort_item<T>>& items, const sort_item<T>& minSortEl, const sort_item<T>& maxSortEl) {

    float minLog = getLog(minSortEl.value);
    float maxLog = getLog(maxSortEl.value);

    int count = (items.size() / 2) + 1;

    std::tuple<float, float> params = GetLinearTransformParams(minLog, maxLog, 0, count - 1);

    float a = std::get<0>(params);
    float b = std::get<1>(params);

    std::vector<minmax::min_max_heap<sort_item<T>>> buckets(count);

    //pushing distinct items only
    for (auto const item : items) {
        // ApplyLinearTransform
        int index = ((a * getLog(item.value) + b));
        index = std::min(count - 1, index);
        buckets[index].push(item);
    }

    std::stack<minmax::min_max_heap<sort_item<T>>> st;

    for (int i = buckets.size() - 1; i >= 0; --i){
        auto nb = buckets[i];
        st.push(nb);
    }
    return st;
}

template <typename T>
void bb_sort(std::vector<T>& array){

    if (array.size() <= 1){
        return;
    }

    std::stack<minmax::min_max_heap<sort_item<T>>> topBucketsStack = getTopStackBuckets(array);

    bbSortToStream<T>(topBucketsStack, array);
}

template<typename T>
std::stack<minmax::min_max_heap<sort_item<T>>> getTopStackBuckets(const std::vector<T> &array) {

    robin_hood::unordered_map<T, int> countMap;

    T minEl = array[0];
    T maxEl = array[0];

    std::vector<sort_item<T>> distinctItems;

    distinctItems.reserve(array.size());

    for (int i = 0; i < array.size(); ++i) {

        T item = array[i];

        if (!countMap.contains(item)){

            if (item < minEl){
                minEl = item;
            }

            if (item > maxEl){
                maxEl = item;
            }

            sort_item<T> sortItem(item);
            distinctItems.push_back(sortItem);

            countMap[item] = distinctItems.size() - 1;
        }
        else{
            distinctItems[countMap[item]].count += 1;
        }
    }

    sort_item minSortEl = distinctItems[countMap[minEl]];
    sort_item maxSortEl = distinctItems[countMap[maxEl]];

    return prepareTopBuckets(distinctItems, minSortEl, maxSortEl);
}

#endif
