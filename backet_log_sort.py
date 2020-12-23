import sys
from collections import defaultdict
from typing import List 
import random
import math 

# python 3 BBsort implemetation.

# Copyright Dec 2020 Konstantin Briukhnov (kooltew at gmail.com) (@CostaBru). San-Francisco Bay Area.

# Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

# The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

def backet_log_sort(arr, O): 
    rez = []

    backet_log_sort_core(arr, rez,  O)

    return rez

def backet_log_sort_core(arr, output_arr, O): 

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
    
    def fillArr(val, output_arr, count_map, O):
        valCount = count_map[val]

        O[0] += valCount

        for j in range(valCount):
            output_arr.append(val)

    count_map, min_element, max_element, size = defaultdict(int), sys.maxsize, -sys.maxsize, len(arr)

    buckets = [None] * (size + 1)

    for item in arr:
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

    for bucket in buckets:

        if bucket:
            lenBuckets = len(bucket)

            if lenBuckets == 1:
                fillArr(bucket[0], output_arr, count_map, O)        
            elif lenBuckets == 2:
                if bucket[0] > bucket[1]:
                    fillArr(bucket[1], output_arr, count_map, O)
                    fillArr(bucket[0], output_arr, count_map, O)
                else:
                    fillArr(bucket[0], output_arr, count_map, O)
                    fillArr(bucket[1], output_arr, count_map, O)        
            elif lenBuckets > 1:
                backet_log_sort_core(bucket, output_arr, O)

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

arr = []
arrt = [0.000000000001,0.000000000002,0.000000000003]
cluster = 10.0

for i in range(100):
    for a in arrt:
        arr.append(a * cluster)
    cluster *= 10.0
arr.reverse()

tests.append(arr)

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


caseNumber = 1
allGood = True

for t in tests:

    O[0] = 0

    result = list(backet_log_sort(t, O))
    t.sort()

    good = result == t

    if good is False:
        allGood = False

    if verbose:
        print("case:  " + str(caseNumber) + " good: " + str(good) + " iter = " + str(O) + " n = " + str(len(t)) + " 3n = "  + str(len(t) * 3) + " 4n = "  + str(len(t) * 4) + " , n*log n = " + str(len(t) * math.log2(len(t))) + " , n ** 2 = " + str(len(t) ** 2)) 

    caseNumber += 1

assert result == t    
