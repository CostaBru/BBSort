# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's ``Blue Boxers`` Case.

Python3\C++\C# implementation of stable hybrid of non comparison counting and bucket sorting algorithm that works using ``O(N)`` time even for non uniformly distributed numbers.

C++ and C# code taking advantage of using MIN\MAX heap. C# part uses a poolable list to lower memory allocation bottleneck effect.

The main idea and python code has come on the same day when the magnificent Aleksey Navalny's Blue Boxers investigation was published. 

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

# Min max heap advantage

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


# Performance 

C++20 implementation of BB sort was compared to QSort rand algorithm taken from the Rosettacode code base website.

Minor optimizations added to algorithm:
- MinMax heap used as bucket storage.
- Instead of generation N buckets we create N/2.

Having that, BB sort shows the better performance starting N > 10 million for testing dataset generated below. 

Please note that those data sets generated have duplicates.

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

``N``: 100 000, ``OS``: Win10 Pro, ``CPU``: AMD Ryzen 7 4800H, ``RAM``: 64.0 GB, ``--O3``

duplicates in set, bb sort counting impl,

``int:``

| case |    N  |    qsort (ns) |   bb sort (ns )|
|------|-------|---------------|------------------|
|   1  | 1     |     10 943 200 |    21 121 400    |
|   2  | 10    |     69 736 500 |    58 756 600     |
|   3  | 100   |    547 019 000 |   337 920 000     |
|   4  | 1000  |   5 207 205 100 | 3 072 248 300     |
 
``double:``

| case |    N  |    qsort (ns) |   bb sort (ns )|
|------|-------|---------------|------------------|
|   1  | 1     |    15 002 300 |     37 014 600     |
|   2  | 10    |    83 578 600 |     162 684 300     |
|   3  | 100   |   680 668 800 |     942 057 100     |
|   4  | 1000  |  6 609 933 900 |  9 268 619 400     |

</details>

<details>
		<summary> C++ Get top N against full sort </summary>

``N``: 100 000, ``OS``: Win10 Pro, ``CPU``: AMD Ryzen 7 4800H, ``RAM``: 64.0 GB, ``--O3``

duplicates in set, bb sort dictless impl,

``int:``

| case |    N  |    qsort (ns)   |   get top 1 (ns ) |  get top 100 (ns )   |
|------|-------|-----------------|-------------------|----------------------|
|   1  | 1     |      10 943 200 |     10 774 800    |      10 396 400       |
|   2  | 10    |      69 736 500 |     53 510 400    |      52 891 200       |
|   3  | 100   |     547 019 000 |     462 951 700    |    447 414 400       |
|   4  | 1000  |   5 207 205 100 |    4 460 983 800    | 4 401 571 700       |

``double:``

| case |    N  |    qsort (ns)    |   get top 1 (ns ) |  get top 100 (ns )   |
|------|-------|------------------|-------------------|----------------------|
|   1  | 1     |       10 451 100 |     10 690 400    |     10 836 300       |
|   2  | 10    |       85 839 300 |     63 968 200    |     143 667 000       |
|   3  | 100   |      687 395 000 |    531 726 000    |    532 256 200       |
|   4  | 1000  |    6 733 593 800 |  5 100 721 600    |  5 092 541 900      |


</details>

From observation of result, I supposed to conclude that the main bottleneck of new algorithm is space requirements. Unfortunately, c++ poolable version of data storage overcame quick sort run time performance only in debug mode. To make it more practical, I hardcoded max bucket size (128) to gather 10-20% better run time for non counting case.

C# ``BB sort`` poolable implementation without counting duplicates has up to ``20% better`` full sort runtime performance than the quicksort\merge hybrid algorithm implementation taken from the Rosseta codebase. The usage of array pool reduces GC calls which lead to up to 3 times better performance on large arrays.

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
- http://www.cs.otago.ac.nz/staffpriv/mike/Papers/MinMaxHeaps/MinMaxHeaps.pdf
