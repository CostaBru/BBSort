cmake_minimum_required(VERSION 3.19)
set(CMAKE_CXX_STANDARD 20)

project(BBSort_solution CXX)

add_subdirectory(BBSort)
include_directories(BBSort)

add_subdirectory(BBSort_tests)