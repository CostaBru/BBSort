import math
import unittest
from random import  seed, sample
from time import perf_counter

from python3.bb_sort import bb_sort, bb_sort_to_stream
from python3.tests.qsort import qsortranpart


class BBSortTest(unittest.TestCase):

    def setUp(self):
        self.iterCounter = [0]
        self.iterCounterQ = [0]
        self.verbose = True

        if self.verbose:
            print("| case      | good |   iter   |   q iter   |        N    |  NLOGN      |  BB time    |   Q time   |   iter - NLOGN    | Q time - BB time  |")
            print("|-----------|------|----------|------------|-------------|-------------|-------------|------------|-------------------|-------------------|")

    def tearDown(self):
        if self.verbose:
            print("|-----------|------|----------|------------|-------------|-------------|-------------|------------|-------------------|-------------------|")

    def sort_and_test(self, arr):
        arr.reverse()

        inplace = list(arr)
        inplace.sort()

        bb_inplace = list(arr)
        bb_sort(bb_inplace, self.iterCounter)

        self.assertEqual(inplace, bb_inplace)

    def test_bucket_worst_1(self):
        arr = [0.0001, 0.0002, 0.0003, 1, 2, 3, 10, 20, 30, 100, 200, 300, 1000, 2000, 3000]
        self.sort_and_test(arr)

    def test_bucket_worst_2(self):

        self.iterCounter[0] = 0

        bucket_worse_arr = []
        arrt = [0.000000000001, 0.000000000002, 0.000000000003]
        cluster = 50.0

        for i in range(50):
            for a in arrt:
                bucket_worse_arr.append(a * cluster)
            cluster *= 10.0

        self.sort_and_test(bucket_worse_arr)

        if self.verbose:
            testLen = len(bucket_worse_arr)
            nlogN = round(testLen * math.log2(len(bucket_worse_arr)), 4)
            print(f"| bct wrst | True |   {self.iterCounter[0]}  |    0  |  {testLen}   |   {nlogN}    |  -1   |   -1  |   {round(nlogN - self.iterCounter[0])}  | 0 |")

    def test_negative_ints(self):
        arr = [-5, -10, 0, -3, 8, 5, -1, 10]
        self.sort_and_test(arr)

    def test_negative_ints(self):
        arr = [-5, -10, 0, -3, 8, 5, -1, 10]
        self.sort_and_test(arr)

    def test_huge_gap(self):
        arr = [9, 8, 7, 1, 100000000000]
        self.sort_and_test(arr)

    def test_float_huge_gap(self):
        arr = [0.9, 0.8, 0.7, 0.1, 100000000000]
        self.sort_and_test(arr)

    def test_reports(self):

        tests = []
        tests.append(list(range(3000)))

        for i in range(2):
            seed(i)
            arr = sample(range(-1000000, 1000000), 10)
            tests.append(arr)

            arr = sample(range(-1000000, 1000000), 1000)
            tests.append(arr)

            arr = sample(range(-10000000, 10000000), 10000000)
            tests.append(arr)

        caseNumber = 1
        allGood = True

        for test in tests:

            self.iterCounter[0] = 0

            result = []

            t1 = perf_counter()

            bb_sort_to_stream(test, result, self.iterCounter)

            bbTime = perf_counter() - t1

            t1 = perf_counter()

            self.iterCounterQ[0] = 0

            qsortTest = qsortranpart(test,  self.iterCounterQ)

            qTime = perf_counter() - t1

            good = qsortTest == result

            if good is False:
                allGood = False

            if self.verbose:
                testLen = len(test)
                nlogN = round(testLen * math.log2(len(test)), 4)
                print(f"| {caseNumber} | {good} |   {self.iterCounter[0]}  |    {self.iterCounterQ[0]}  |  {testLen}   |   {nlogN}    |  {round(bbTime, 4)}   |   {round(qTime, 4)}  |   {round(nlogN - self.iterCounter[0])}  | {round(qTime - bbTime, 4)} |")

            caseNumber += 1

        self.assertTrue(allGood)
