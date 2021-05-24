#ifndef BBSORT_SOLUTION_ARRAY_POOL_BUCKET_H
#define BBSORT_SOLUTION_ARRAY_POOL_BUCKET_H

#include "object_pool.h"

namespace pool {

    template<typename T>
    class array_pool_bucket {

        using array_value_type = T;
        using array_pointer = T *;

    private:

        object_pool<array_value_type> storage;

    public:

        int arrayLen;

        array_pool_bucket() {
        }

        ~array_pool_bucket() {

            clear();
        }

        array_pointer rentArray() {

            if (storage.empty()) {

                return static_cast<array_pointer>(::operator new(sizeof(array_value_type) * arrayLen));
            }

            return storage.pop();
        }

        void returnArray(array_pointer object) {

            storage.push(object);
        }

        void clear() {

            while (true) {

                array_pointer poolItem = storage.pop();

                if (poolItem == nullptr) {

                    break;
                }

                free(poolItem);
            }
        }
    };
}

#endif //BBSORT_SOLUTION_ARRAY_POOL_BUCKET_H
