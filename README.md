# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's ``Blue Boxers`` Case.

Python3 implementation of stable hybrid of non comparison counting and bucket sorting algorithm that works using ``O(N)`` time even for non uniformly distributed numbers.

Was developed on the same day when the magnificent Aleksey Navalny's Blue Boxers investigation was published. 

The ``BB sort`` is very simple and uses ``5N`` operations in average. 

- Counting sort takes ``4N`` and it is ``not effective`` on large numbers.
- Bucket is ``O(N ** 2)`` and has ``poor performance`` on non uniformly distributed numbers.

# Prerequisites

Let's consider that ``rescaling``, math ``log``, and ``rounding`` operations take constant time. Having that the algorithm below needs ``O(4N)`` time to sort any number array. 

We will take the best from the counting and bucket sorting algorithms, use log scale to compress numbers, and keys normalization from 0 to array length for item bucket assignment.

# Algorithm

Count all duplicates and store it in the map. Find min and max number in the array. ``O(3N)``

Calculate parameters to normalize keys to output array size. ``O(1)``

For each key in the map. ``O(M)`` where ``M`` number of unique items.

- Use math log to scale map keys much more closely to each other. ``O(1)``

- Normalize the key using parameters we got earlier. ``O(1)``

- Round that normalized value to integer and got a bucket index. ``O(1)``

- Add the key to the bucket. ``O(1)``

<details>
		<summary> Getting buckets implementation </summary>
  
  ```python

      def Get_buckets(items, count, count_map):

        def Get_log(x):            
            if abs(x) < 2: return x
            return math.log2(x) if x > 0 else -math.log2(abs(x))

        def Get_linear_transform_params(x1, x2, y1, y2):
            dx = x1 - x2
            if dx == 0: return 0, 0
            a = (y1 - y2) / dx
            b = y1 - (a * x1)
            return a, b

        min_element, max_element, size =  min(items), max(items), count

        a, b     = Get_linear_transform_params(Get_log(min_element), Get_log(max_element), 0, size - 1)
        buckets  = [None] * size

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

Once we got all numbers processed, we will have 4 cases: 

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
        buckets   = Get_buckets(enumerable, count, count_map)

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

# Performance 

C++20 implementation of BB sort was compared to QSort rand algorithm taken from the Rosettacode code base website.

Minor optimizations added to algorithm:
- MinMax heap used as bucket storage.
- Instead of generation N buckets we create N/2.

Having that, BB sort shows the better performance starting N > 10 million.

<details>
		<summary> Performance comparison tables </summary>

``N``: 100 000, ``CPU``: AMD Ryzen 7 4800H, ``RAM``: 64.0 GB, ``--O3``

``uint8:``

| case |    N  |    qsort (ns) |   bb sort (ns )   |
|------|-------|---------------|------------------|
|   1  | 1     |      2 000 100 |     1 005 000     |
|   2  | 10    |    10  198 300 |   10  007 800     |
|   3  | 100   |   170  365 800 |  133  798 500     |
|   4  | 1000  |  1 727  540 500 | 1 331  475 000     |
|   5  | 20000  | 32 975 382 500 | 26 625 005 800    |

``long:``

| case |    N  |    qsort (ns) |   bb sort (ns )|
|------|-------|---------------|------------------|
|   1  | 1     |     11 002 500 |    59 367600     |
|   2  | 10    |     66 012 300 |    99 028900     |
|   3  | 100   |    524 117 900 |   385 087200     |
|   4  | 1000  |   5 092 121 800 |  3 051 038 300     |
|   5  | 20000  | 100 116 390 900 | 60 191 307 100    |

``double:``

| case |    N  |    qsort (ns) |   bb sort (ns )|
|------|-------|---------------|------------------|
|   1  | 1     |    14 016 300 |      77 567 800     |
|   2  | 10    |    84 019 200 |     255 961 100     |
|   3  | 100   |   6 78 025 400 |    10 1873 800     |
|   4  | 1000  |  65 38 606 100 |    8 978 723 200     |
|   5  | 20000  | 131 266 887 200 | 184 922 020 300    |

</details>

From observation of result, I can conclude that the main bottleneck of new algorithm is memory allocator. According to previous statement, we can consider using poolable vector data structure.

# Advantages

``BB sort`` can be used in lazy way. The output may be considered as a stream, iterator, or pipeline for next operation.

Task like ``take M sorted items from N given unsorted items`` is good for ``BB sorting``. In that case first sorted item will be available in just ``O(2N)``.

Hence, we have copy of array in buckets and count map, we can use source array as output as well.

# Disadvantages

Because it has to do extra work before sorting, it performs worse than comparison ``N logN`` sorting algorithms in case of small size arrays.

# References

- https://www.youtube.com/watch?v=ibqiet6Bg38
- https://en.wikipedia.org/wiki/Feature_scaling
- https://en.wikipedia.org/wiki/List_of_logarithmic_identities
- https://en.wikipedia.org/wiki/Bucket_sort
- https://en.wikipedia.org/wiki/Counting_sort
- http://rosettacode.org/wiki/Compare_sorting_algorithms%27_performance
