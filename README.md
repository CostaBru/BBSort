# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's Blue Boxers Case.

Python3 implementation of stable non comparsion sorting algorithm that works using O(N) time even for non uniformly distributed numbers.

Was developed on the same day when the magnificant Aleksey Navalny's Blue Boxers investigation was published. 

The BB sort is very simple and uses O(4N) in average. 

- Counting sort takes O(4N) and it is not effective on large numbers.
- Bucket is O(N ** 2) and has poor performance on non uniformly distributed numbers.

# The key idea

Let's consider that rescaling, math log and rounding operations take O(1). Having that the algorithm below needs O(4N) time to sort any number array. 

We will take the best from the counting and bucket sorting algorithms, use log scale to compress numbers, and keys normalization from 0 to array length for item bucket assignment.

# Algorithm

Count all dupicates and store it in map. Find min and max number in array. O(N)

Calculate parameters to normalize keys to output array size. O(1)

For each key in the map. O(M) where M number of unique items.

- Use math log to scale map keys much more closely to each other. O(1)

- Normalize the key using parameters we got earlier. O(1)

- Round that normalized value to integer and got a bucket index. O(1)

- Add the key to the bucket. O(1)

Once we got all numbers processed. We will have 4 cases: 

1. Empty bucket. Skip it.

2. Bucket with single item. Write key and duplicates to the output list. O(T), where T number of duplicates. T is equal to 1 in average.

3. Bucket with two items. Compare keys and write it and duplicates in order to the output list. O(2 * T), where T number of duplicates. T is equal to 1 in average.

4. Bucket with more than 3 items. Run the whole procedure for that bucket. O(C), where C is equal to 3 in average. 

Perform above checks and steps for each bucket. That will take O(N). Profit. 

The algorithm is easy and sweet. It can be ported to low level languages in minutes.

# Performance

| case | good | iter |  N  |  3N  |  4N  | NLOGN |        N **2     | iter - NLOGN |
|------|------|------|-----|------|------|-------|------------------|--------------|
| 1 | True | [24] | 8 | 24 | 32 | 24 | 64 | 0 |
| 2 | True | [23] | 5 | 15 | 20 | 12 | 25 | -11 |
| 3 | True | [23] | 5 | 15 | 20 | 12 | 25 | -11 |
| 4 | True | [69] | 15 | 45 | 60 | 59 | 225 | -10 |

### Below case is the worst for bucket sorting. The input is not uniformly distributed and has a lot of small clusters far from each other.

| case | good | iter |  N  |  3N  |  4N  | NLOGN |        N **2     | iter - NLOGN |
|------|------|------|-----|------|------|-------|------------------|--------------|
| 5 | True | [906] | 300 | 900 | 1200 | 2469 | 90000 | 1563 |

| case | good | iter |  N  |  3N  |  4N  | NLOGN |        N **2     | iter - NLOGN |
|------|------|------|-----|------|------|-------|------------------|--------------|
| 6 | True | [1238] | 300 | 900 | 1200 | 2469 | 90000 | 1231 |
| 7 | True | [13178] | 3000 | 9000 | 12000 | 34652 | 9000000 | 21474 |
| 8 | True | [135836] | 30000 | 90000 | 120000 | 446180 | 900000000 | 310344 |
| 9 | True | [1384258] | 300000 | 900000 | 1200000 | 5458381 | 90000000000 | 4074123 |
| 10 | True | [14021278] | 3000000 | 9000000 | 12000000 | 64549593 | 9000000000000 | 50528315 |

### Random array sorting tests 
| case | good | iter |  N  |  3N  |  4N  | NLOGN |        N **2     | iter - NLOGN |
|------|------|------|-----|------|------|-------|------------------|--------------|
| 11 | True | [46] | 10 | 30 | 40 | 33 | 100 | -13 |
| 12 | True | [492] | 100 | 300 | 400 | 664 | 10000 | 172 |
| 13 | True | [4868] | 1000 | 3000 | 4000 | 9966 | 1000000 | 5098 |
| 14 | True | [49210] | 10000 | 30000 | 40000 | 132877 | 100000000 | 83667 |
| 15 | True | [491212] | 100000 | 300000 | 400000 | 1660964 | 10000000000 | 1169752 |
| 16 | True | [4686420] | 1000000 | 3000000 | 4000000 | 19931569 | 1000000000000 | 15245149 |

# Advantages

BB sort can be used in lazy way. The output may be considered as a stream, iterator, or pipeline for next operation.

Task like "take M sorted items from M given unsorted set" is good for BB sorting. In that case first sorted item will be available in O(2N).

Hence we have copy of array in buckets and count map we can use source array as output as well.

# Disadvanteges

Because it has to do extra work before sorting, it performs worse that comparsion N logN sorting algorithms in case of small size arrays with item count less than 30.

# References

- https://en.wikipedia.org/wiki/Feature_scaling
- https://en.wikipedia.org/wiki/List_of_logarithmic_identities
- https://en.wikipedia.org/wiki/Bucket_sort
- https://en.wikipedia.org/wiki/Counting_sort
