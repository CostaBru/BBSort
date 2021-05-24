#ifndef BBSORT_SOLUTION_GLOBAL_POOL_H
#define BBSORT_SOLUTION_GLOBAL_POOL_H

#include "array_pool.h"

namespace pool {

    template<class T>
    class global_array_pool {   public: static array_pool <T> GLOBAL_POOL;    };

    template<class T>
    array_pool <T> global_array_pool<T>::GLOBAL_POOL;
}
#endif //BBSORT_SOLUTION_GLOBAL_POOL_H
