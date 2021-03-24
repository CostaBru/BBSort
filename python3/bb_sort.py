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

    i = 0
    for item in bb_sort_core_to_iter(array, len(array),  iterCounter):
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

    bb_sort_core_to_stream(array, len(array), stream, iterCounter)


def getBucketes(enumerable, count, countMap, iterCounter):

    def getLog(x):
        if x == 0: return 0
        return math.log2(x) if x > 0 else -math.log2(abs(x))

    def GetLinearTransformParams(x1, x2, y1, y2):
        dx = x1 - x2
        if dx == 0: return 0, 0
        a = (y1 - y2) / dx
        b = y1 - (a * x1)
        return a, b

    min_element, max_element, size = sys.maxsize, -sys.maxsize, count

    buckets = [None] * (size + 1)

    for item in enumerable: countMap[item] += 1; min_element = min(min_element, item); max_element = max(max_element, item)

    iterCounter[0] += size

    a, b = GetLinearTransformParams(getLog(min_element), getLog(max_element), 0, size)

    for key in countMap.keys():
        # ApplyLinearTransform
        index = int((a *  getLog(key)) + b)
        bucket = buckets[index]

        if bucket: bucket.append(key)
        else:      buckets[index] = [key]

    iterCounter[0] += len(countMap)

    return buckets


def bb_sort_core_to_stream(enumerable, count, output, iterCounter):

    def fillStream(val, output, countMap, iterCounter):
        valCount = countMap[val]
        iterCounter[0] += valCount
        for j in range(valCount):
            output.append(val)

    countMap = defaultdict(int)

    buckets = getBucketes(enumerable, count, countMap, iterCounter)

    for bucket in buckets:
        if bucket:
            bucketCount = len(bucket)
            if bucketCount == 1:
                fillStream(bucket[0], output, countMap, iterCounter)
            elif bucketCount == 2:
                b1, b2 = bucket[0], bucket[1]
                if b1 > b2: b1, b2 = b2, b1
                fillStream(b1, output, countMap, iterCounter)
                fillStream(b2, output, countMap, iterCounter)
            else:
                bb_sort_core_to_stream(bucket, bucketCount, output, iterCounter)


def bb_sort_core_to_iter(enumerable, count, iterCounter):

    def iterArr(val, countMap, iterCounter):
        valCount = countMap[val]
        iterCounter[0] += valCount
        for j in range(valCount):
            yield val

    countMap = defaultdict(int)

    buckets = getBucketes(enumerable, count, countMap, iterCounter)

    for bucket in buckets:
        if bucket:
            bucketCount = len(bucket)
            if bucketCount == 1:
                for item in iterArr(bucket[0], countMap, iterCounter):
                    yield item      
            elif bucketCount == 2:            
                b1, b2 = bucket[0], bucket[1]
                if b1 > b2:
                    b1, b2 = b2, b1               
                for item in iterArr(b1, countMap, iterCounter):
                    yield item 
                for item in iterArr(b2, countMap, iterCounter):
                    yield item     
            else:
                for item in bb_sort_core_to_iter(bucket, bucketCount, iterCounter):
                    yield item
