#include "ut.h"
#include "qsortrand.h"
#include <bb_sort.h>
#include <bb_sort_get_top_n_lazy.h>
#include <bb_sort_dictless.h>
#include <min_max_heap.h>
#include <vector>
#include <random>
#include <chrono>

//https://github.com/boost-ext/ut#tutorial

void test_pool();

template <typename T>
void test_arrays(std::vector<T> bb_rez, std::vector<T> goldenArr){

    boost::ut::expect(goldenArr.size() == bb_rez.size()) << "sizes not equal";

    for (int i = 0; i < goldenArr.size(); ++i) {
        auto equal = goldenArr[i] == bb_rez[i];

        if (!equal){
            std::cout << "Not equal at: " << i << std::endl;
            boost::ut::expect(equal) << "Not equal at: " << i;
        }
    }
}

template <typename T>
void sort_and_test(std::vector<T> arr){

    std::vector<T> arrCopy(arr);
    std::reverse(arrCopy.begin(), arrCopy.end());

    std::vector<T> arrCopy2(arr);
    std::reverse(arrCopy2.begin(), arrCopy2.end());

    std::vector<T> goldenArr(arr);
    sort(goldenArr.begin(), goldenArr.end());

    bb_sort::sort(arrCopy);
    bb_sort_dictless::sort(arrCopy2);

    test_arrays<T>(arrCopy, goldenArr);
    test_arrays<T>(arrCopy2, goldenArr);
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
            bucket_worse_arr.emplace_back(val * cluster);
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

void test_duplicates(){

    std::cout << "test_duplicates" << std::endl;

    std::vector<float> arr = {10, 20, 40, 50, 60, 69, 70, 70 , 70 , 70 , 70};
    sort_and_test(arr);
}

template <typename T>
std::vector<T> range(T start, T end){

    std::vector<T> t;
    for (int i = start; i < end; ++i) {
        t.emplace_back(i);
    }

    return t;
}

template <typename T>
std::vector<T> rangeN(T start, T end, int n){

    std::vector<T> t;

    for (int i = start; i < end && t.size() <= n; ++i) {
        t.emplace_back(i);
    }

    return t;
}

template <typename T>
std::vector<T> sample(std::vector<T> population, long long count){

    std::vector<T> result;

    while (result.size() <= count) {

        std::vector<T> sampled;

        std::sample(population.begin(),
                    population.end(),
                    std::back_inserter(sampled),
                    count,
                    std::mt19937{std::random_device{}()});

        for(auto i: sampled){
            result.emplace_back(i);
        }
    }
    return result;
}

template <typename T>
void test_duplicate_reports(){

    std::cout << "test_duplicate_reports " << typeid(T).name() << std::endl;

    std::vector<std::vector<T>> tests;

    for(int i = 0; i < 1; ++i) {

        srand(i);

        tests.emplace_back(sample(range<T>(-100000, 100000), 100));
        tests.emplace_back(sample(range<T>(-100000, 100000), 10000));
        tests.emplace_back(sample(range<T>(-100000, 100000), 100000));
        tests.emplace_back(sample(range<T>(-100000, 100000), 1000000));
        tests.emplace_back(sample(range<T>(-100000, 100000), 10000000));
        tests.emplace_back(sample(range<T>(-100000, 100000), 100000000));
    }

    for(int i = 0; i < 2; ++i) {

        int caseNumber = 1;
        bool allGood = true;

        for (auto test : tests) {

            std::cout << caseNumber << std::endl;

            std::random_device rd;
            std::mt19937 g(rd());

            std::shuffle(test.begin(), test.end(), g);

            std::vector<T> bbsortTest(test);
            {
                const auto start = std::chrono::high_resolution_clock::now();
                bb_sort::sort(bbsortTest);
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "bb_sort     " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            int topN = 1;

            std::vector<T> getSortedTest(test);
            std::vector<T> topNSorted;
            {
                const auto start = std::chrono::high_resolution_clock::now();
                topNSorted = bb_sort_top_n_lazy::getTopSortedLazy(getSortedTest, topN );
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "get top    " << topN <<  "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            int topN2 = 100;

            std::vector<T> getSortedTest2(test);
            std::vector<T> topNSorted2;
            {
                const auto start = std::chrono::high_resolution_clock::now();
                topNSorted2 = bb_sort_top_n_lazy::getTopSortedLazy(getSortedTest2, topN2 );
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "get top  " << topN2 <<  "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            std::vector<T> qsortTest(test);
            {
                const auto start = std::chrono::high_resolution_clock::now();
                quicksortrand(qsortTest.begin(), qsortTest.end(), std::less<T>());
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "qsort       " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            std::vector<T> bbSortDictless(test);
            {
                const auto start = std::chrono::high_resolution_clock::now();
                bb_sort_dictless::sort(bbSortDictless );
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "bb_sort d   " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            {
                const auto start = std::chrono::high_resolution_clock::now();

                minmax::min_max_heap<T, pool::vector<T>> minMax;

                for (int j = 0; j < test.size(); ++j) {
                    minMax.push(test[i]);
                }

                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "minmax heap " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            bool good = qsortTest.size() == bbsortTest.size();

            for (int i = 0; i < qsortTest.size(); ++i) {

                auto eq = qsortTest[i] == bbsortTest[i];

                if (!eq) {
                    std::cout << "Not eq" << i << " " << qsortTest[i] << "!=" << bbsortTest[i] << std::endl;
                }

                good = eq && good;
            }

            for (int i = 0; i < qsortTest.size(); ++i) {

                auto eq = qsortTest[i] == bbSortDictless[i];

                if (!eq) {
                    std::cout << "Not eq D " << i << " " << qsortTest[i] << "!=" << bbSortDictless[i] << std::endl;
                }

                good = eq && good;
            }

            for (int i = 0; i < topN2; ++i) {

                auto eq = qsortTest[i] == topNSorted2[i];

                if (!eq) {
                    std::cout << "Top N: Not eq" << i << " " << qsortTest[i] << "!=" << topNSorted2[i] << std::endl;
                }

                good = eq && good;
            }

            if (!good) {
                allGood = false;
            }

            caseNumber += 1;
        }

        boost::ut::expect(allGood);
    }
}


template <typename T>
void test_unique_reports(){

    std::cout << "test_unique_reports " << typeid(T).name() << std::endl;

    std::vector<std::vector<T>> tests;

    for(int i = 0; i < 1; ++i) {

        tests.emplace_back(rangeN<T>(-10, 100000, 100));
        tests.emplace_back(rangeN<T>(-100000, 100000, 1000));
        tests.emplace_back(rangeN<T>(-100000, 100000, 10000));
        tests.emplace_back(rangeN<T>(-100000, 100000, 100000));
        tests.emplace_back(rangeN<T>(-1000000, 1000000, 1000000));
    }

    for(int i = 0; i < 2; ++i) {

        int caseNumber = 1;
        bool allGood = true;

        for (auto test : tests) {

            std::cout << caseNumber << std::endl;

            std::random_device rd;
            std::mt19937 g(rd());

            std::shuffle(test.begin(), test.end(), g);

            std::vector<T> bbsortTest(test);
            {
                const auto start = std::chrono::high_resolution_clock::now();
                bb_sort::sort(bbsortTest);
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "bb_sort     " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            int topN = 1;

            std::vector<T> getSortedTest(test);
            std::vector<T> topNSorted;
            {
                const auto start = std::chrono::high_resolution_clock::now();
                topNSorted = bb_sort_top_n_lazy::getTopSortedLazy(getSortedTest, topN );
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "get top    " << topN <<  "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            int topN2 = 100;

            std::vector<T> getSortedTest2(test);
            std::vector<T> topNSorted2;
            {
                const auto start = std::chrono::high_resolution_clock::now();
                topNSorted2 = bb_sort_top_n_lazy::getTopSortedLazy(getSortedTest2, topN2 );
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "get top  " << topN2 <<  "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            std::vector<T> qsortTest(test);
            {
                const auto start = std::chrono::high_resolution_clock::now();
                quicksortrand(qsortTest.begin(), qsortTest.end(), std::less<T>());
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "qsort       " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            std::vector<T> bbSortDictless(test);
            {
                const auto start = std::chrono::high_resolution_clock::now();
                bb_sort_dictless::sort(bbSortDictless );
                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "bb_sort d   " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            {
                const auto start = std::chrono::high_resolution_clock::now();

                minmax::min_max_heap<T, pool::vector<T>> minMax;

                for (int j = 0; j < test.size(); ++j) {
                    minMax.push(test[i]);
                }

                const auto stop = std::chrono::high_resolution_clock::now();
                const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(stop - start);
                std::cout << "[" << "minmax heap " << "] " << ns.count() << " ns" << " size: " << test.size() << std::endl;
            }

            bool good = qsortTest.size() == bbsortTest.size();

            for (int i = 0; i < qsortTest.size(); ++i) {

                auto eq = qsortTest[i] == bbsortTest[i];

                if (!eq) {
                    std::cout << "Not eq" << i << " " << qsortTest[i] << "!=" << bbsortTest[i] << std::endl;
                }

                good = eq && good;
            }

            for (int i = 0; i < qsortTest.size(); ++i) {

                auto eq = qsortTest[i] == bbSortDictless[i];

                if (!eq) {
                    std::cout << "Not eq D" << i << " " << qsortTest[i] << "!=" << bbSortDictless[i] << std::endl;
                }

                good = eq && good;
            }

            for (int i = 0; i < topN2; ++i) {

                auto eq = qsortTest[i] == topNSorted2[i];

                if (!eq) {
                    std::cout << "Top N: Not eq" << i << " " << qsortTest[i] << "!=" << topNSorted2[i] << std::endl;
                }

                good = eq && good;
            }

            if (!good) {
                allGood = false;
            }

            caseNumber += 1;
        }

        boost::ut::expect(allGood);
    }
}

int main() {

    try {

        test_bucket_worst_1();

        test_bucket_worst_2();

        test_negative_ints();

        test_huge_gap();

        test_float_huge_gap();

        test_duplicates();

        test_unique_reports<int>();

        test_duplicate_reports<int>();

        test_duplicate_reports<long>();

        test_duplicate_reports<float>();

        test_duplicate_reports<double>();

        test_unique_reports<long>();
    }
    catch (const std::exception &e) {
        std::cout << e.what() << std::endl;
    }

    return 0;
}
