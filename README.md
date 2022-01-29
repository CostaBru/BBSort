# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's ``Blue Boxers`` Case.

Python3\C++\C# implementation of stable hybrid of non comparison counting and bucket sorting algorithm that works using ``O(N)`` time even for non uniformly distributed numbers.

C++ and C# code taking advantage of using MIN\MAX data structure, and poolable list(vector) to lower memory allocation bottleneck effect.

The main idea and first lines of python code have come on the same day when the magnificent Aleksey Navalny's Blue Boxers investigation has been published. 

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

# Min\max data structures advantage

As the main requirement, we have to calculate min\max value of each input bucket to scale each value to its result bucket. We can use the min max heap as the bucket container data structure. It is known that it takes O ``N`` to build it.

Performance profiling result shows that counting duplicates and storing it in the map takes ~60% of execution time. We can take advantage of min\max heap here and use it to get rid of counting duplicates on the first step.  

To recognize that bucket is full of duplicates, we will compare the min and max values of the heap. 

Another good point of having min\max heap as bucket container: it allows us to handle bucket with 2 elements without comparing items. Bucket with 3 items requires only one comparison to get min\max and middle element.

<details>
		<summary> Duplicates handling </summary>
  
  ```csharp

     int case1(Stack<MinMaxHeap<T>> st,
                  MinMaxHeap<T> top,
                  List<T> output,
                  int index)
        {
            fillStream(ref top.At(0), output, index, top.Count);

            return top.Count;
        }

    int caseN(Stack<MinMaxHeap<T>> st,
                    MinMaxHeap<T> top,
                    List<T> output,
                    int index)
        {
            var allDuplicates = EqualityComparer<T>.Default.Equals(top.At(0), top.At(1));

            if (allDuplicates)
            {
                return case1(st, top, output, index);
            }

            var count = (top.Count / 2) + 1;

            var newBuckets = new PoolList<MinMaxHeap<T>>(count, count, count);

            getBuckets(ref top.FindMin(), ref top.FindMax(), top, newBuckets, count);

            for (int i = newBuckets.Count - 1; i >= 0; --i)
            {
                var minMaxHeap = newBuckets[i];
                
                if (minMaxHeap != null)
                {
                    st.Push(minMaxHeap);
                }
            }
            return 0;
        }

   ```  
	
</details>

Next steps in performance optimizations show that building that heap is very expensive. However, we need only min and max values, and do not have requirements to remove or modify the storage, so we can use custom vector that keeps track of min, max values instead of the min max heap.

Also, we can preserve the ```size equals 3``` case by adding ```mid``` field to our minMax vector and handle that special case there.

# Pragmatic version

As practice shown, we also require to cover a case when log values are only slightly different. For that case we cannot continue to bb-sorting. We can use bucket sorting or fall back to builtin sorting routine. Nevertheless, we are still doing up to 10 percent better.

# Performance 

C++20 implementation of BB sort was compared to QSort rand algorithm taken from the Rosettacode code base website.

Minor optimizations added to algorithm:
- MinMaxMid vector used as bucket storage.
- Instead of generation N buckets we create N/2 + 1.
- First level of buckets has 128 items only.

<details>
		<summary> Test data generation </summary>

  ```cpp
        tests.push_back(sample(range<T>(-100000, 100000), 1000));
        tests.push_back(sample(range<T>(-100000, 100000), 10000));
        tests.push_back(sample(range<T>(-100000, 100000), 100000));
        tests.push_back(sample(range<T>(-100000, 100000), 1000000));
        tests.push_back(sample(range<T>(-100000, 100000), 10000000));
        tests.push_back(sample(range<T>(-100000, 100000), 100000000));
        tests.push_back(sample(range<T>(-100000, 100000), 2000000000));
 
//generation methods

template <typename T>
std::vector<T> range(T start, T end){

    std::vector<T> t;
    for (int i = start; i < end; ++i) {
        t.push_back(i);
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
            result.push_back(i);
        }
    }
    return result;
}
  ```  


</details>

<details>
		<summary> C++ Full sort performance comparison tables </summary>

 ``OS``: Win10 Pro, ``CPU``: AMD Ryzen 7 4800H, ``RAM``: 64.0 GB, ``--O3``


``int:``

| case |    N         |    qsort (ns) |   bb sort (ns )  |
|------|--------------|---------------|------------------|
|   1  |     200 000  |     15 625 700 |    15 625 900   |
|   2  |   1 200 000  |     62 519 700 |    41 278 700   |
|   3  |  10 200 000  |    532 424 800 |   341 380 000   |
|   4  | 100 200 000  |  5 124 005 200 | 3 523 796 800   |
 
``double:``

| case |    N         |    qsort (ns) |      bb sort (ns )   |
|------|--------------|---------------|----------------------|
|   1  | 200 000      |     15 623 500 |      15 627 100     |
|   2  | 1 200 000    |     78 132 400 |      46 868 700     |
|   3  | 10 200 000   |    689 186 500 |     385 274 200     |
|   4  | 100 200 000  |  6 499 938 500 |   4 333 235 300     |

``float:``

| case |    N         |    qsort (ns)  |      bb sort (ns )   |
|------|--------------|----------------|----------------------|
|   1  | 200 000      |     15 629 900 |      15 633 500      |
|   2  | 1 200 000    |     93 750 000 |      25 944 600      |
|   3  | 10 200 000   |    703 174 200 |     337 268 600      |
|   4  | 100 200 000  |  6 523 601 000 |   3 546 221 800      |

</details>


<details>
		<summary> C# Full sort performance comparison for random numbers </summary>

 ``OS``: Win10 Pro, ``CPU``: AMD Ryzen 7 4800H, ``RAM``: 64.0 GB, ``--O3``


``int:``

| case |    N         |    qsort (ms)  |      bb sort (ms )   |  builtin (ms )   |
|------|--------------|----------------|----------------------|------------------|
|   1  |    100 001   |        27      |         4            |    6             | 
|   2  |  1 000 001   |       424      |        56            |    63            |
|   3  |  1 048 576   |       339      |        58            |    63            |


``float:``

| case |    N         |    qsort (ms)  |      bb sort (ms )   |  builtin (ms )   |
|------|--------------|----------------|----------------------|------------------|
|   1  |    100 001   |        39      |         6            |    7             | 
|   2  |  1 000 001   |       453      |        70            |    78            |
|   3  |  1 048 576   |        463     |        76            |    83            |

</details>

As shown above, c++ ``bb sort`` overcame quick sort run time performance up to 2 times.

C# ``BB sort`` poolable implementation without counting duplicates has up to 3 times full sort runtime performance than the quicksort\merge hybrid algorithm implementation taken from the Rosseta codebase and up to 10% against the builtin in .NET framework Array.Sort implementation.

# Advantages

``BB sort`` can be used in lazy way. We can consider the output as a stream, iterator, or pipeline for a next operation.

Task like ``take M sorted items from N given unsorted items`` is good for ``BB sorting``. In that case first sorted item will be available in just ``O(2N)``. On the other hand, the heap data structure or quick select algorithm can be used to solve this kind of task more effectively.

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
- http://www.cs.otago.ac.nz/staffpriv/mike/Papers/MinMaxHeaps/MinMaxHeaps.pdf
