#include "ut.h"
#include "qsortrand.h"
#include <bb_sort.h>
#include <vector>
#include <random>
#include <chrono>

//https://github.com/boost-ext/ut#tutorial

template <typename T>
void test_arrays(std::vector<T> bb_rez, std::vector<T> goldenArr){

    boost::ut::expect(goldenArr.size() == bb_rez.size()) << "sizes not equal";

    for (int i = 0; i < goldenArr.size(); ++i) {
        auto equal = goldenArr[i] == bb_rez[i];
        boost::ut::expect(equal) << "Not equal at: " << i;
    }
}

template <typename T>
void sort_and_test(std::vector<T> arr){

    std::vector<T> arrCopy(arr);
    std::reverse(arrCopy.begin(), arrCopy.end());

    std::vector<T> goldenArr(arr);
    sort(goldenArr.begin(), goldenArr.end());

    std::vector<T> bb_rez;
    bb_sort(arrCopy, bb_rez);

    test_arrays<T>(bb_rez, goldenArr);
}

void test_bucket_worst_1() {

    std::cout << "test_bucket_worst_1" << std::endl;

    std::vector<float> arr = {0.0001, 0.0002, 0.0003, 1, 2, 3, 10, 20, 30, 100, 200, 300, 1000, 2000, 3000};

    sort_and_test(arr);
}

void test_bucket_worst_2() {

    std::cout << "test_bucket_worst_2" << std::endl;

    std::vector<double> bucket_worse_arr;
    std::vector<double> arrt = {0.000000000001, 0.000000000002, 0.000000000003};
    double cluster = 10.0;

    for (int i = 0; i < 15; ++i) {

        for (auto const val : arrt) {
            bucket_worse_arr.push_back(val * cluster);
            cluster *= 10.0;
        }
    }
    sort_and_test(bucket_worse_arr);
}


void  test_negative_ints() {

    std::cout << "test_negative_ints" << std::endl;

    std::vector<int> arr = {-5, -10, 0, -3, 8, 5, -1, 10};
    sort_and_test(arr);
}

void test_huge_gap(){

    std::cout << "test_huge_gap" << std::endl;

    std::vector<long> arr = {9, 8, 7, 1, 1000000000};
    sort_and_test(arr);
}

void test_float_huge_gap(){

    std::cout << "test_float_huge_gap" << std::endl;

    std::vector<float> arr = {0.9, 0.8, 0.7, 0.1, 1000000000};
    sort_and_test(arr);
}

std::vector<long> range(long start, long end){

    std::vector<long> t;
    for (int i = start; i < end; ++i) {
        t.push_back(i);
    }

    return t;
}

std::vector<long> sample(std::vector<long> population, int count){

    std::vector<long> sampled;

    std::sample(population.begin(),
                population.end(),
                std::back_inserter(sampled),
                count,
                std::mt19937{std::random_device{}()});

    return sampled;
}

void test_reports(){

    std::cout << "test_reports" << std::endl;

    std::vector<std::vector<long>> tests;

    tests.push_back(range(0, 3000));

    for(int i = 0; i < 2; ++i) {

        srand(i);

        tests.push_back(sample(range(-100000, 100000), 10));
        tests.push_back(sample(range(-100000, 100000), 1000));
        tests.push_back(sample(range(-100000, 100000), 100000));
        tests.push_back(sample(range(-100000, 100000), 10000000));
    }

    int caseNumber = 1;
    bool allGood = true;

    for (auto test : tests) {

        std::cout << caseNumber << std::endl;

        std::random_device rd;
        std::mt19937 g(rd());

        std::shuffle(test.begin(), test.end(), g);

        std::vector<long> qsortTest(test);
        {
            const auto start = std::chrono::high_resolution_clock::now();
            quicksortrand(qsortTest.begin(), qsortTest.end(), std::less<long>());
            const auto stop = std::chrono::high_resolution_clock::now();
            const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
            std::cout << "[" << "qsort" << "] " << ns.count() << " ns\n" << std::endl;
        }

        std::vector<long> result;
        result.reserve(test.size());
        {
            const auto start = std::chrono::high_resolution_clock::now();
            bb_sort(test, result);
            const auto stop = std::chrono::high_resolution_clock::now();
            const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
            std::cout << "[" << "bb_sort" << "] " << ns.count() << " ns\n" << std::endl;
        }

        bool good = qsortTest.size() == result.size();

        for (int i = 0; i < qsortTest.size(); ++i) {

            auto eq = qsortTest[i] == result[i];

            if (!eq){
                std::cout << "Not eq" << i << " " << qsortTest[i] << "!=" << result[i] << std::endl;
            }

            good = eq && good;
        }

        if (!good)
        {
            allGood = false;
        }

        caseNumber += 1;
    }

    boost::ut::expect(allGood);
}

int main() {

    try {

        test_bucket_worst_1();

        test_bucket_worst_2();

        test_negative_ints();

        test_huge_gap();

        test_float_huge_gap();

        test_reports();

    }
    catch (const std::exception &e) {
        std::cout << e.what() << std::endl;
    }

    return 0;
}