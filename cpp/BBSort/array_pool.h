#ifndef BBSORT_SOLUTION_ARRAY_POOL_H
#define BBSORT_SOLUTION_ARRAY_POOL_H

#include <cassert>
#include <cmath>
#include <type_traits>
#include <memory>
#include <algorithm>
#include <stdexcept>
#include <iterator>
#include "ptr_vector.h"
#include "array_pool_bucket.h"

namespace pool {

    template<typename T>
    class array_pool {

        using array_value_type = T;
        using array_pointer = T *;
        using size_type = std::size_t;

    public:

        array_pool() {

            const int maximumArrayLength = 0x40000000;

            int maxBuckets = selectBucketIndex(maximumArrayLength);

            buckets = std::vector<array_pool_bucket<array_value_type>> (maxBuckets + 1);

            for (size_type i = 0; i < maxBuckets + 1; i++) {

                buckets[i].arrayLen = getMaxSizeForBucket(i);
            }
        }

        array_pointer rentArray(size_type &size) {

            int index = selectBucketIndex(size);

            if (index < buckets.size()) {

                size = buckets[index].arrayLen;

                return buckets[index].rentArray();
            }

            return static_cast<array_pointer>(::operator new(sizeof(array_value_type) * size));
        }

        void returnArray(array_pointer array, size_type &size) {

            if (destroying) {

                return;
            }

            int index = selectBucketIndex(size);

            if (index < buckets.size()) {

                buckets[index].returnArray(array);
            }
            else{

                free(array);
            }
        }

        ~array_pool() {

            destroying = true;

            buckets.clear();
        }

    private:

        bool destroying = false;

        std::vector <array_pool_bucket<array_value_type>> buckets;

        static int clz(uint32_t x) {

            static const char debruijn32[32] = {
                    0, 31, 9, 30, 3, 8, 13, 29, 2, 5, 7, 21, 12, 24, 28, 19,
                    1, 10, 4, 14, 6, 22, 25, 20, 11, 15, 23, 26, 16, 27, 17, 18
            };
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;
            return debruijn32[x * 0x076be629 >> 27];
        }

        static int selectBucketIndex(unsigned int bufferSize) {

            if (bufferSize <= 16) {

                return 0;
            }

            unsigned int bits = (bufferSize - 1) >> 4;
            return 32 - clz(bits);
        }

        static int getMaxSizeForBucket(int binIndex) {

            return 16 << binIndex;
        }
    };
}

#endif //BBSORT_SOLUTION_ARRAY_POOL_H
