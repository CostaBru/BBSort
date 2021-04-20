"""
Python 3 BB sort implementation.

Copyright Feb 2021 Konstantin Briukhnov (kooltew at gmail.com) (CostaBru @KBriukhnov). San-Francisco Bay Area.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
"""

import math
import sys
from collections import defaultdict

def bb_sort(array, iterCounter):
    """
     BB sort in place API.

     :param array: items to sort
     :type array: array

     :param iterCounter: iteration counter
     :type iterCounter: array
    """
    countMap = defaultdict(int)

    for item in array: countMap[item] += 1

    iterCounter[0] += len(countMap)

    i = 0
    for item in bb_sort_core_to_iter(countMap.keys(),  iterCounter, countMap):
        array[i] = item
        i += 1

def bb_sort_to_stream(array, stream, iterCounter):
    """
    BB sort writes result to stream.

    :param array: items to sort
    :type array: array

    :param stream: items to sort
    :type stream: array

    :param iterCounter: iteration counter
    :type iterCounter: array
    """

    countMap = defaultdict(int)

    for item in array: countMap[item] += 1

    iterCounter[0] += len(countMap)

    bb_sort_core_to_stream(countMap.keys(), stream, iterCounter, countMap)


def get_buckets(iterable, iterCounter):

    def get_log(x):
        ax = abs(x)
        if abs(x) < 2: return x
        return math.log2(x) if x > 0 else -math.log2(ax)

    def get_linear_transform_params(x1, x2, y1, y2):
        dx = x1 - x2
        if dx == 0: return 0, 0
        a = (y1 - y2) / dx
        b = y1 - (a * x1)
        return a, b

    minElement, maxElement, count = min(iterable), max(iterable), len(iterable)

    iterCounter[0] += count * 2

    size = (count // 2 + 1)

    buckets = [None] * size

    a, b = get_linear_transform_params(get_log(minElement), get_log(maxElement), 0, size)

    for item in iterable:
        # ApplyLinearTransform
        index = min(int((a *  get_log(item)) + b), size - 1)
        bucket = buckets[index]
        if bucket: bucket.append(item)
        else:      buckets[index] = [item]

    iterCounter[0] += len(iterable)

    return buckets


def bb_sort_core_to_stream(enumerable, output, iterCounter, countMap):

    def fill_stream(val, output, countMap, iterCounter):
        valCount = countMap[val]
        iterCounter[0] += valCount
        for j in range(valCount):
            output.append(val)

    buckets = get_buckets(enumerable, iterCounter)

    for bucket in buckets:
        if bucket:
            bucketCount = len(bucket)
            if bucketCount == 1:
                fill_stream(bucket[0], output, countMap, iterCounter)
            elif bucketCount == 2:
                b1, b2 = bucket[0], bucket[1]
                if b1 > b2: b1, b2 = b2, b1
                fill_stream(b1, output, countMap, iterCounter)
                fill_stream(b2, output, countMap, iterCounter)
            else:
                bb_sort_core_to_stream(bucket, output, iterCounter, countMap)

def bb_sort_core_to_iter(enumerable, iterCounter, countMap):

    def iter_arr(val, countMap, iterCounter):
        valCount = countMap[val]
        iterCounter[0] += valCount
        for j in range(valCount):
            yield val

    buckets = get_buckets(enumerable, iterCounter)

    for bucket in buckets:
        if bucket:
            bucketCount = len(bucket)
            if bucketCount == 1:
                for item in iter_arr(bucket[0], countMap, iterCounter):
                    yield item      
            elif bucketCount == 2:            
                b1, b2 = bucket[0], bucket[1]
                if b1 > b2:
                    b1, b2 = b2, b1               
                for item in iter_arr(b1, countMap, iterCounter):
                    yield item 
                for item in iter_arr(b2, countMap, iterCounter):
                    yield item     
            else:
                for item in bb_sort_core_to_iter(bucket, iterCounter, countMap):
                    yield item
