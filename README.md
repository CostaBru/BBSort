# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's Blue Boxers Case.

Python3 implementation of stable non comparsion sorting algorithm that works  using O(N) time even for non uniformly distributed numbers.

Was developed on the same day when the magnificant Aleksey Navalny's Blue Boxers investigation was published. 

# The key idea

Let's consider that rescaling, math log and rounding operations take O(1). Having that the algorithm below needs O(3N) time to sort any number array.

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

2. Bucket with single item. Write key and duplicates to the output list. O(T), where T number of duplicates. T is equal to 1 in avarage.

3. Bucket with two items. Compare keys and write it and duplicates in order to the output list. O(2 * T), where T number of duplicates. T is equal to 1 in avarage.

4. Bucket with more than 3 items. Run the whole procedure for that bucket. O(C), where C is equal to 3 in avarage. 

Perform above checks and steps for each bucket. That will take O(N). Profit. 

The algorithm is easy and sweet. It can be ported to low level languages in minutes.

# Performance

- iter = [24], n = 8, 3n = 24, 4n = 32, iter - 4n = -8, n*log n = 24, n*log n - iter = 0 , n ** 2 = 64, n**2 - iter = 40
- iter = [23], n = 5, 3n = 15, 4n = 20, iter - 4n = 3, n*log n = 12, n*log n - iter = -11 , n ** 2 = 25, n**2 - iter = 2
- iter = [23], n = 5, 3n = 15, 4n = 20, iter - 4n = 3, n*log n = 12, n*log n - iter = -11 , n ** 2 = 25, n**2 - iter = 2
- iter = [69], n = 15, 3n = 45, 4n = 60, iter - 4n = 9, n*log n = 59, n*log n - iter = -10 , n ** 2 = 225, n**2 - iter = 156

### The worst case for bucket sorting. The input is not uniformly distributed and has a lot of small clusters far from each other.
- iter = [906], n = 300, 3n = 900, 4n = 1200, iter - 4n = -294, n*log n = 2469, n*log n - iter = 1563 , n ** 2 = 90000, n**2 - iter = 89094

- iter = [1238], n = 300, 3n = 900, 4n = 1200, iter - 4n = 38, n*log n = 2469, n*log n - iter = 1231 , n ** 2 = 90000, n**2 - iter = 88762
- iter = [13178], n = 3000, 3n = 9000, 4n = 12000, iter - 4n = 1178, n*log n = 34652, n*log n - iter = 21474 , n ** 2 = 9000000, n**2 - iter = 8986822
- iter = [135836], n = 30000, 3n = 90000, 4n = 120000, iter - 4n = 15836, n*log n = 446180, n*log n - iter = 310344 , n ** 2 = 900000000, n**2 - iter = 899864164
- iter = [1384258], n = 300000, 3n = 900000, 4n = 1200000, iter - 4n = 184258, n*log n = 5458381, n*log n - iter = 4074123 , n ** 2 = 90000000000, n**2 - iter = 89998615742

- iter = [14021278], n = 3000000, 3n = 9000000, 4n = 12000000, iter - 4n = 2021278, n*log n = 64549593, n*log n - iter = 50528315 , n ** 2 = 9000000000000, n**2 - iter = 8999985978722

### Random array sorting tests 
- iter = [46], n = 10, 3n = 30, 4n = 40, iter - 4n = 6, n*log n = 33, n*log n - iter = -13 , n ** 2 = 100, n**2 - iter = 54
- iter = [492], n = 100, 3n = 300, 4n = 400, iter - 4n = 92, n*log n = 664, n*log n - iter = 172 , n ** 2 = 10000, n**2 - iter = 9508
- iter = [4868], n = 1000, 3n = 3000, 4n = 4000, iter - 4n = 868, n*log n = 9966, n*log n - iter = 5098 , n ** 2 = 1000000, n**2 - iter = 995132
- iter = [49210], n = 10000, 3n = 30000, 4n = 40000, iter - 4n = 9210, n*log n = 132877, n*log n - iter = 83667 , n ** 2 = 100000000, n**2 - iter = 99950790
- iter = [491212], n = 100000, 3n = 300000, 4n = 400000, iter - 4n = 91212, n*log n = 1660964, n*log n - iter = 1169752 , n ** 2 = 10000000000, n**2 - iter = 9999508788
- iter = [4686420], n = 1000000, 3n = 3000000, 4n = 4000000, iter - 4n = 686420, n*log n = 19931569, n*log n - iter = 15245149 , n ** 2 = 1000000000000, n**2 - iter = 999995313580

# References

- https://en.wikipedia.org/wiki/Feature_scaling
- https://en.wikipedia.org/wiki/List_of_logarithmic_identities
- https://en.wikipedia.org/wiki/Bucket_sort
