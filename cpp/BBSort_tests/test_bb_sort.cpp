#include "ut.h"
#include <bb_sort.h>
#include <vector>

//https://github.com/boost-ext/ut#tutorial

template <typename T>
void sort_and_test(std::vector<T> arr){

    std::vector<T> arrCopy(arr);
    std::reverse(arrCopy.begin(), arrCopy.end());

    std::vector<T> goldenArr(arr);
    sort(goldenArr.begin(), goldenArr.end());

    std::vector<T> bb_rez;
    bb_sort(arrCopy, bb_rez);

    for (int i = 0; i < goldenArr.size(); ++i) {
        auto equal = goldenArr[i] == bb_rez[i];
        boost::ut::expect(equal) << "Not equal at: " << i;
    }
}

void test_bucket_worst_1() {

    std::vector<float> arr = {0.0001, 0.0002, 0.0003, 1, 2, 3, 10, 20, 30, 100, 200, 300, 1000, 2000, 3000};

    sort_and_test(arr);
}

void test_bucket_worst_2() {

    std::vector<float> bucket_worse_arr;
    std::vector<float> arrt = {0.000000000001, 0.000000000002, 0.000000000003};
    float cluster = 50.0;

    for (int i = 0; i < 50; ++i) {

        for (auto const val : arrt) {
            bucket_worse_arr.push_back(val * cluster);
            cluster *= 10.0;
        }
    }
    sort_and_test(bucket_worse_arr);
}


void  test_negative_ints() {
    std::vector<int> arr = {-5, -10, 0, -3, 8, 5, -1, 10};
    sort_and_test(arr);
}

void test_huge_gap(){
    std::vector<long> arr = {9, 8, 7, 1, 1000000000};
    sort_and_test(arr);
}

void test_float_huge_gap(){
    std::vector<float> arr = {0.9, 0.8, 0.7, 0.1, 1000000000};
    sort_and_test(arr);
}

int main() {

    test_bucket_worst_1();

    test_bucket_worst_2();

    test_negative_ints();

    test_huge_gap();

    test_float_huge_gap();
}