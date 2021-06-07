#ifndef BBSORT_SOLUTION_MIN_MAX_MID_VECTOR_H
#define BBSORT_SOLUTION_MIN_MAX_MID_VECTOR_H

namespace minmax
{

#include <limits>

    template<class T, class Container>
    class min_max_mid_vector {

    public:

        Container Storage;

        T Max = -std::numeric_limits<T>::max();
        T Min =  std::numeric_limits<T>::max();

        ///Has valid and reliable value for case size() == 3 only.
        T Mid =  std::numeric_limits<T>::max();

        min_max_mid_vector() {
        }

        min_max_mid_vector(Container container)
                : Storage(container) {
        }

        min_max_mid_vector(min_max_mid_vector&& move) noexcept
        {
            move.Storage.swap(Storage);

            Max = move.Max;
            Min = move.Min;
            Mid = move.Mid;
        }

        bool empty() const {  return Storage.length == 0;  }

        unsigned int size() const {  return Storage.length; }

        void push(const T & value) {

            if (Storage.length == 2){

                if (value < Min){

                    T temp = Min;
                    Min = value;
                    Mid = temp;
                } else if (value > Max){

                    T temp = Max;
                    Max = value;
                    Mid = temp;
                }
                else{

                    Mid = value;
                }
            }
            else
            {
                Min = std::min(value, Min);
                Max = std::max(value, Max);
            }

            // Push the value onto the end of the heap
            Storage.push_back(value);
        }
    };
}

#endif //BBSORT_SOLUTION_MIN_MAX_MID_VECTOR_H
