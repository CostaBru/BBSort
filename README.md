# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's ``Blue Boxers`` Case.

Python3 implementation of stable hybrid of non comparsion counting and bucket sorting algorithm that works using ``O(N)`` time even for non uniformly distributed numbers.

Was developed on the same day when the magnificant Aleksey Navalny's Blue Boxers investigation was published. 

The ``BB sort`` is very simple and uses ``4N`` in average. 

- Counting sort takes ``4N`` and it is ``not effective`` on large numbers.
- Bucket is ``O(N ** 2)`` and has ``poor performance`` on non uniformly distributed numbers.

# Prerequisites

Let's consider that ``rescaling``, math ``log`` and ``rounding`` operations take ``O(1)``. Having that the algorithm below needs ``O(4N)`` time to sort any number array. 

We will take the best from the counting and bucket sorting algorithms, use log scale to compress numbers, and keys normalization from 0 to array length for item bucket assignment.

# Algorithm

Count all dupicates and store it in map. Find min and max number in array. ``O(N)``

Calculate parameters to normalize keys to output array size. ``O(1)``

For each key in the map. ``O(M)`` where ``M`` number of unique items.

- Use math log to scale map keys much more closely to each other. ``O(1)``

- Normalize the key using parameters we got earlier. ``O(1)``

- Round that normalized value to integer and got a bucket index. ``O(1)``

- Add the key to the bucket. ``O(1)``

<details>
		<summary> Getting buckets implementation </summary>
  
  ```python

      def Get_bucketes(items, count, count_map):

        def Get_log(x):
            if x == 0: return 0
            return math.log2(x) if x > 0 else -math.log2(abs(x))

        def Get_linear_transform_params(x1, x2, y1, y2):
            dx = x1 - x2
            if dx == 0: return 0, 0
            a = (y1 - y2) / dx
            b = y1 - (a * x1)
            return a, b

        # can be done in O(N)
        min_element, max_element, size =  min(items), max(items), count

        a, b     = Get_linear_transform_params(Get_log(min_element), Get_log(max_element), 0, size)
        buckets  = [None] * (size + 1)

        for item in items: count_map[item] += 1 

        for key in count_map.keys(): 
            # ApplyLinearTransform    
            index = int((a *  Get_log(key)) + b) 
            bucket = buckets[index]
            if bucket:  bucket.append(key)
            else:  buckets[index] =  [key]
        return buckets
   ```  
	
</details>

Once we got all numbers processed. We will have 4 cases: 

1. Empty bucket. Skip it.

2. Bucket with single item. Write key and duplicates to the output list. ``O(T)``, where ``T`` number of duplicates. ``T ``is equal to 1 in average.

3. Bucket with two items. Compare keys and write it and duplicates in order to the output list. ``O(2 * T)``, where ``T`` number of duplicates. ``T`` is equal to 1 in average.

4. Bucket with more than 3 items. Run the whole procedure for that bucket. ``O(C)``, where ``C`` is equal to 3 in average. 

Perform above checks and steps for each bucket. That will take ``O(N)``. Profit. 

<details>
		<summary> BB sort implementation </summary>
  
  ```python

      def BB_sort_core(enumerable, count, output): 

        def Fill_stream(val, output, count_map): 
            for j in range(count_map[val]): output.append(val)

        count_map = defaultdict(int)
        buckets   = Get_bucketes(enumerable, count, count_map)

        for bucket in buckets:
            if bucket:
                bucket_count = len(bucket)
                if bucket_count   == 1: Fill_stream(bucket[0], output, count_map)        
                elif bucket_count == 2:
                    b1, b2 = bucket[0], bucket[1]
                    if b1 > b2: b1, b2 = b2, b1
                    Fill_stream(b1, output, count_map)
                    Fill_stream(b2, output, count_map)        
                else:  BB_sort_core(bucket, bucket_count, output)
   ```  
	
</details>

The algorithm is easy and sweet. It can be ported to low level languages in minutes.

# Performance

### Below case is the worst for bucket sorting. The input is not uniformly distributed and has a lot of small clusters far from each other.

|   iter |  items(N)  |  3N  |  4N  | NLOGN |        N **2     | NLOGN - iter |
|--------|------------|------|------|-------|------------------|--------------|
| [906] | 300 | 900 | 1200 | 2469 | 90000 | 1563 |

### BB and quick sort comparison

The quick sort python implementation was taken from http://rosettacode.org/wiki/Compare_sorting_algorithms%27_performance

| case |   iter   |   q iter   |        N    |  NLOGN      |  BB time    |   Q time   |   iter - NLOGN    | Q time - BB time  |
|------|----------|------------|-------------|-------------|-------------|------------|-------------------|-------------------|
| 1 |  24  |    37  |  8   |   24.0    |  0.0   |   0.0  |   0  | 0.0 |
| 2 |  23  |    17  |  5   |   11.6096    |  0.0001   |   0.0  |   -11  | -0.0001 |
| 3 |  23  |    18  |  5   |   11.6096    |  0.0   |   0.0  |   -11  | -0.0 |
| 4 |  51  |    78  |  15   |   58.6034    |  0.0001   |   0.0001  |   8  | -0.0 |
| 5 |  1168  |    3220  |  300   |   2468.6456    |  0.0009   |   0.0008  |   1301  | -0.0001 |
| 6 |  13178  |    46330  |  3000   |   34652.2404    |  1.0045   |   0.0133  |   21474  | -0.9911 |
| 7 |  46  |    39  |  10   |   33.2193    |  0.0001   |   0.0  |   -13  | -0.0 |
| 8 |  4896  |    12184  |  1000   |   9965.7843    |  0.0039   |   0.0055  |   5070  | 0.0015 |
| 9 |  47348048  |    321673850  |  10000000   |   232534966.6421    |  67.2672   |   105.4689  |   185186919  | 38.2017 |
| 10 | 46  |    54  |  10   |   33.2193    |  0.0001   |   0.122  |   -13  | 0.1219 |
| 11 | 4954  |    12561  |  1000   |   9965.7843    |  0.0051   |   0.0042  |   5012  | -0.001 |
| 12 | 47350508  |    305759432  |  10000000   |   232534966.6421    |  77.2285   |   104.457  |   185184459  | 27.2285 |


The cases 9 and 12 where ``N=10 000 000`` show that ``BB sort`` performs much better than ``quick sort`` on huge lists.

# Advantages

``BB sort`` can be used in lazy way. The output may be considered as a stream, iterator, or pipeline for next operation.

Task like ``take M sorted items from N given unsorted items`` is good for ``BB sorting``. In that case first sorted item will be available in just ``O(2N)``.

Hence we have copy of array in buckets and count map we can use source array as output as well.

# Disadvanteges

Because it has to do extra work before sorting, it performs worse than comparsion ``N logN`` sorting algorithms in case of small size arrays with item count less than ``30``.

# References

- https://www.youtube.com/watch?v=ibqiet6Bg38
- https://en.wikipedia.org/wiki/Feature_scaling
- https://en.wikipedia.org/wiki/List_of_logarithmic_identities
- https://en.wikipedia.org/wiki/Bucket_sort
- https://en.wikipedia.org/wiki/Counting_sort
- http://rosettacode.org/wiki/Compare_sorting_algorithms%27_performance
