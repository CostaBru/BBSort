#ifndef BBSORT_SOLUTION_OBJECT_POOL_H
#define BBSORT_SOLUTION_OBJECT_POOL_H

#include <cassert>
#include <cmath>
#include <type_traits>
#include <memory>
#include <algorithm>
#include <stdexcept>
#include <iterator>
#include "ptr_vector.h"

namespace pool
{
    template <typename T>
    class object_pool {

        using value_type        = T;
        using pointer           = T*;

    private:
        ptr_vector<pointer> items;

    public:

        object_pool() {
        }

        const bool empty(){

            return items.size() == 0;
        }

        pointer pop() {

            if (items.any()) {

                pointer *object = items.top();
                pointer ret = *object;
                items.pop();

                return ret;
            }

            return nullptr;
        }

        void push(pointer object) {

            items.push_back(object);
        }
    };
}

#endif //BBSORT_SOLUTION_OBJECT_POOL_H
