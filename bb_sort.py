import sys
from collections import defaultdict
from typing import List 
import random
import math

# Python 3 BB sort implementation.

# Copyright Dec 2020 Konstantin Briukhnov (kooltew at gmail.com) (CostaBru @KBriukhnov). San-Francisco Bay Area.

# Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

# The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

def bb_sort(array, O): 
    i = 0
    for item in bb_sort_core_to_iter(array, len(array),  O):
        array[i] = item

def bb_sort_to_stream(array, stream, O): 
    bb_sort_core_to_stream(array, len(array), stream, O)

def getBucketes(enumerable, count, count_map, O):

    def getLog(x):

        if x == 0:
            return 0

        if x == 1:
            return x

        if x == -1:
            return x

        if x < 0:
            return -math.log2(abs(x))

        return math.log2(x)

    def GetLinearTransformParams(x1, x2, y1, y2):

        dx = x1 - x2

        if dx == 0:
            return 0, 0

        a = (y1 - y2) / (dx)
        b = y1 - (a * x1)
        return a, b

    min_element, max_element, size = sys.maxsize, -sys.maxsize, count

    buckets = [None] * (size + 1)

    for item in enumerable:
        count_map[item] += 1
        min_element = min(min_element, item)
        max_element = max(max_element, item)

    O[0] += size

    a, b = GetLinearTransformParams(getLog(min_element), getLog(max_element), 0, size)
  
    for key in count_map.keys(): 
        # ApplyLinearTransform    
        index = int((a *  getLog(key)) + b) 
        bucket = buckets[index]

        if bucket:
            bucket.append(key)
        else:
            bucket = [key]
            buckets[index] = bucket

    O[0] += len(count_map)

    return buckets

def bb_sort_core_to_stream(enumerable, count, output, O): 

    def fillStream(val, output, count_map, O):
        valCount = count_map[val]

        O[0] += valCount

        for j in range(valCount):
            output.append(val)

    count_map = defaultdict(int)

    buckets = getBucketes(enumerable, count, count_map, O)

    for bucket in buckets:
        if bucket:
            bucketCount = len(bucket)
            if bucketCount == 1:
                fillStream(bucket[0], output, count_map, O)        
            elif bucketCount == 2:
                b1, b2 = bucket[0], bucket[1]
                if b1 > b2:
                    b1, b2 = b2, b1
                fillStream(b1, output, count_map, O)
                fillStream(b2, output, count_map, O)        
            else:
                bb_sort_core_to_stream(bucket, bucketCount, output, O)

def bb_sort_core_to_iter(enumerable, count, O): 

    def iterArr(val, count_map, O):
        valCount = count_map[val]

        O[0] += valCount

        for j in range(valCount):
            yield val

    count_map = defaultdict(int)

    buckets = getBucketes(enumerable, count, count_map, O)

    for bucket in buckets:
        if bucket:
            bucketCount = len(bucket)
            if bucketCount == 1:
                for item in iterArr(bucket[0], count_map, O):
                    yield item      
            elif bucketCount == 2:            
                b1, b2 = bucket[0], bucket[1]
                if b1 > b2:
                    b1, b2 = b2, b1               
                for item in iterArr(b1, count_map, O):
                    yield item 
                for item in iterArr(b2, count_map, O):
                    yield item     
            else:
                for item in bb_sort_core_to_iter(bucket, bucketCount, O):
                    yield item  
verbose = True
O = [0]
tests = []
arr = [-5, -10, 0, -3, 8, 5, -1, 10] 
tests.append(arr)
arr = [9,8,7,1, 100000000000] 
tests.append(arr)
arr = [ 0.9, 0.8, 0.7, 0.1, 100000000000] 
tests.append(arr)
arr = [0.0001, 0.0002, 0.0003, 1,2,3, 10,20,30, 100,200,300, 1000,2000,3000]
arr.reverse()
tests.append(arr)

bucket_worse_arr = []
arrt = [0.000000000001,0.000000000002,0.000000000003]
cluster = 10.0

for i in range(100):
    for a in arrt:
        bucket_worse_arr.append(a * cluster)
    cluster *= 10.0
bucket_worse_arr.reverse()

tests.append(bucket_worse_arr)

arr = list(range(300))
arr.reverse()
tests.append(arr)

arr = list(range(3000))
arr.reverse()
tests.append(arr)

arr = list(range(30000))
arr.reverse()
tests.append(arr)

arr = list(range(300000))
arr.reverse()
tests.append(arr)

arr = list(range(3000000))
arr.reverse()
tests.append(arr)

for i in range(10):

    random.seed(i)
    arr = random.sample(range(-1000000, 1000000), 10)
    tests.append(arr)

    arr = random.sample(range(-1000000, 1000000), 100)
    tests.append(arr)

    arr = random.sample(range(-1000000, 1000000), 1000)
    tests.append(arr)

    arr = random.sample(range(-1000000, 1000000), 10000)
    tests.append(arr)

    arr = random.sample(range(-1000000, 1000000), 100000)
    tests.append(arr)

    arr = random.sample(range(-1000000, 1000000), 1000000)
    tests.append(arr)

inplace = list(bucket_worse_arr)
inplace.sort()

bb_inplace = list(bucket_worse_arr)
bb_sort(bb_inplace, O)

assert bb_inplace == bucket_worse_arr

caseNumber = 1
allGood = True

if verbose:
    print("| case | good | iter |  N  |  3N  |  4N  | NLOGN |        N **2     | iter - NLOGN |") 
    print("|------|------|------|-----|------|------|-------|------------------|--------------|") 

for test in tests:

    O[0] = 0

    result = []

    bb_sort_to_stream(test, result, O)

    assert test != result

    test.sort()

    good = result == test

    if good is False:
        allGood = False

    if verbose:
        testLen = len(test)
        nlogN = testLen * math.log2(len(test))
        print("| " + str(caseNumber) + " | " + str(good) + " | " + str(O) + " | " + str(testLen) + " | "  + str(testLen * 3) + " | "  + str(testLen * 4) + " | " + str(round(nlogN)) + " | " + str(testLen ** 2) + " | " + str(round(nlogN - O[0])) + " |") 

    caseNumber += 1

if verbose:
    print("|------|------|------|-----|------|------|-------|------------------|--------------|") 
