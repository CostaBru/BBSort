# BB sort (Blue Boxers sort)

In honor of Aleksey Navalny's Blue Boxers Case.

Python3 implementation of stable non comparsion sorting algorithm that works  using O(N) time even for non uniformly distributed numbers.

Was developed on the same day when the magnificant Aleksey Navalny's Blue Boxers investigation was published. 

# Main idea

Let's consider that rescaling, log operation and number rounding operations take O(1). Having that the algorith below needs O(3N) time to sort any number array.

# Algorithm

Count all dupicates and store it in map. Find min and max number in array.

Calculate parameters to normalize keys to output array size.

For each key in the map

- Use math log to scale map keys much more closely to each other. 

- Perfrom normalization the key using parameters we got earlier.

- Round that normalized log number to integer and got a bucket index.

- Store key in the bucket

Once we got all numbers processed. We will have 4 cases: 

1. Empty bucket. Skip it.

2. Bucket with sigle item. Write key and duplicates to the output list.

3. Bucket with two items. Compare keys and write it and duplicates in order to the output list.

4. Bucket with more than 3 items or more. Run the whole procedure for that bucket.

Perform above checks and steps for each bucket. Profit.

# References

- https://en.wikipedia.org/wiki/Feature_scaling
- https://en.wikipedia.org/wiki/List_of_logarithmic_identities
- https://en.wikipedia.org/wiki/Bucket_sort
