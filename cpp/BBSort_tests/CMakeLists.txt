cmake_minimum_required(VERSION 3.19)
project(BBSort_tests CXX)

set(CMAKE_CXX_STANDARD 20)

set(SOURCE_FILES
        test_bb_sort.cpp
        )

set(HEADER_FILES
        ut.h
        )

add_executable(BBSort_tests ${SOURCE_FILES} ${HEADER_FILES})

target_link_libraries(BBSort_tests BBSort)

set(CMAKE_EXE_LINKER_FLAGS "-static")