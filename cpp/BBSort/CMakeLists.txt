cmake_minimum_required(VERSION 3.19)
set(CMAKE_CXX_STANDARD 20)

project(BBSort CXX)
add_library(BBSort bb_sort.h fast_map.h minMaxHeap.h)
set_target_properties(BBSort PROPERTIES LINKER_LANGUAGE CXX)
set(CMAKE_EXE_LINKER_FLAGS "-static")