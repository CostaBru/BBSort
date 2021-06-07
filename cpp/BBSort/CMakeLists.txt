cmake_minimum_required(VERSION 3.19)
set(CMAKE_CXX_STANDARD 20)

project(BBSort CXX)
add_library(BBSort bb_sort.h fast_map.h min_max_heap.h bb_sort_get_top_n_lazy.h bb_sort_dictless.h object_pool.h poolable_vector.h ptr_vector.h array_pool_bucket.h array_pool.h global_array_pool.h fastmemcpy.h poolable_vector_lazy.h min_max_mid_vector.h bb_sort_dictless_min_max_vect.h)
set_target_properties(BBSort PROPERTIES LINKER_LANGUAGE CXX)
set(CMAKE_EXE_LINKER_FLAGS "-static")