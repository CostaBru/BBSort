using System;
using System.Collections;
using System.Collections.Generic;

namespace Flexols.Data.Collections
{
    public class MinMaxHeap<T> : IEnumerable<T>
    {
        private static readonly int[] tab32 = new int[]
        {
            0,  9,  1, 10, 13, 21,  2, 29,
            11, 14, 16, 18, 22, 25,  3, 30,
            8, 12, 20, 28, 15, 17, 24,  7,
            19, 27, 23,  6, 26,  5,  4, 31
        };

        /**
         * @brief Returns the log base 2 of a number @b zvalue.
         **/
        static int log2(int value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return tab32[(uint)(value * 0x07C4ACDD) >> 27];
        }

        private readonly HybridList<T> heap_;
        private readonly IComparer<T> comparer_;

        public MinMaxHeap(HybridList<T> zcontainer, IComparer<T> zcompare = null)
        {
            heap_ = zcontainer;
            comparer_ = zcompare ?? Comparer<T>.Default;
        }

        /**
        * @brief Returns the number of elements in the heap.
        **/
        public int Count
        {
            get => heap_.Count;
        }


        /**
         * @brief Adds an element with the given value onto the heap.
         **/
        public void Add(T zvalue)
        {
            // Push the value onto the end of the heap
            heap_.Add(zvalue);

            // Reorder the heap so that the min-max heap property holds true
            trickleUp(heap_.Count - 1);
        }

        /**
         * @brief Returns the element with the greatest value in the heap.
         *
         * @exception std::underflow_error
         **/
        public ref T FindMax()
        {
            return ref heap_.ValueByRef(0);
        }

        /**
         * @brief Returns the element with the least value in the heap.
         *
         * @exception std::underflow_error
         **/
        public ref T FindMin()
        {
            // findMinIndex() will throgh an underflow_error if no min exists
            return ref heap_.ValueByRef(findMinIndex());
        }


        public (int, int, int) GetMaxMidMin()
        {
            if (comparer_.Compare(heap_[1], heap_[2]) == -1)
            {
                return (0, 2, 1);
            }
            return (0, 1, 2);
        }

        public ref T At(int index)
        {
            return ref heap_.ValueByRef(index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return heap_.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /**
         * @brief Returns the index of the parent of the node specified by
         *        @c zindex.
         **/
        static int parent(int zindex)
        {
            return (zindex - 1) / 2;
        }

        /**
         * @brief Returns the index of the left child of the node specified by
         *        @c zindex.
         **/
        static int leftChild(int zindex)
        {
            return 2 * zindex + 1;
        }

        /**
         * @brief Returns the index of the right child of the node specified by
         *        @c zindex.
         **/
        static int rightChild(int zindex)
        {
            return 2 * zindex + 2;
        }

        /**
         * @brief Returns @c true if the node specified by @c zindex is on a
         *        @e min-level.
         **/
        static bool isOnMinLevel(int zindex)
        {
            return log2(zindex + 1) % 2 == 1;
        }

        /**
         * @brief Returns @c true if the node specified by @c zindex is on a
         *        @e max-level.
         **/
        static bool isOnMaxLevel(int zindex)
        {
            return !isOnMinLevel(zindex);
        }

        /**
         * @brief Performs a sift-up, trickle-up, or bubble-up operation on the node
         *        specified by @c zindex without checking to see if the node is
         *        on the right @e track (min or max).
         *
         * @note This is a helper function for @c trickleUp(). For those not
         *       familiar with this convention, that is what the underscore after
         *       the name signifies.
         *
         * @tparam MaxLevel Set to @c true if and only if @c zindex is on a max
         *         level.
         **/

        static void Swap<T>(ref T val1, ref T val2) {
            var temp = val1;

            val1 = val2;

            val2 = temp;

        }

        void trickleUp_(int zindex, bool MaxLevel)
        {
            // Can't bring the root any farther up
            if (zindex == 0) return;

            // Find the parent of the passed node first
            int zindex_grandparent = parent(zindex);

            // If there is no grandparent, return
            if (zindex_grandparent == 0) return;

            // Find the grandparent
            zindex_grandparent = parent(zindex_grandparent);

            // Check to see if we should swap with the grandparent
            if ((comparer_.Compare(heap_[zindex], heap_[zindex_grandparent]) ^ (MaxLevel ? 1 : 0)) == -1)
            {
                Swap(ref heap_.ValueByRef(zindex_grandparent), ref heap_.ValueByRef(zindex));
                trickleUp_(zindex_grandparent, MaxLevel);
            }
        }

        /**
         * @brief Performs a sift-up, trickle-up, or bubble-up operation on the node
         *        specified by @c zindex.
         *
         * The operation is carried out similarily to the equivalant operation in a
         * standard heap.
         *
         * This function simply places the node on a min or max level as needed,
         * then calls @c trickleUp_() acoordingly.
         **/
        void trickleUp(int zindex)
        {
            // Can't bring the root any farther up
            if (zindex == 0) return;

            // Find the parent of the passed node
            var zindex_parent = parent(zindex);

            if (isOnMinLevel(zindex))
            {
                // Check to see if we should swap with the parent
                if (comparer_.Compare(heap_[zindex_parent], heap_[zindex]) == -1)
                {
                    Swap(ref heap_.ValueByRef(zindex_parent), ref heap_.ValueByRef(zindex));
                    trickleUp_(zindex_parent, true);
                }
                else
                {
                    trickleUp_(zindex, false);
                }
            }
            else
            {
                // Check to see if we should swap with the parent
                if (comparer_.Compare(heap_[zindex], heap_[zindex_parent]) == -1)
                {
                    Swap(ref heap_.ValueByRef(zindex_parent), ref heap_.ValueByRef(zindex));
                    trickleUp_(zindex_parent, false);
                }
                else
                {
                    trickleUp_(zindex, true);
                }
            }
        }

        /**
         * @brief Performs a sift-down, trickle-down, or bubble-down operation on
         *        the node specified by @c zindex without checking to see if the
         *        node is on the right @e track (min or max).
         *
         * @note This is a helper function for @c trickleDown(). For those not
         *       familiar with this convention, that is what the underscore after
         *       the name signifies.
         *
         * @tparam MaxLevel Set to @c true if and only if @c zindex is on a max
         *         level.
         *
         * @exception std::invalid_argument Thrown when no element as @c zindex exists.
         **/
        void trickleDown_(int zindex, bool MaxLevel)
        {
            /* In the following comments, substitute the word "less" with the word
             * "more" and the word "smallest" with the word "greatest" when MaxLevel
             * equals true. */

            // Ensure the element exists.
            if (zindex >= heap_.Count)
                throw new InvalidOperationException("Element specified by zindex does not exist");

            /* This will hold the index of the smallest node among the children,
             * grandchildren of the current node, and the current node itself. */
            int smallestNode = zindex;

            /* Get the left child, all other children and grandchildren can be found
             * from this value. */
            int left = leftChild(zindex);

            // Check the left and right child
            if (left < heap_.Count && (comparer_.Compare(heap_[left], heap_[smallestNode]) ^ (MaxLevel ? 1 : 0)) == -1)
                smallestNode = left;
            if (left + 1 < heap_.Count && (comparer_.Compare(heap_[left + 1], heap_[smallestNode]) ^ (MaxLevel ? 1 : 0)) == -1)
                smallestNode = left + 1;

            /* Check the grandchildren which are guarenteed to be in consecutive
             * positions in memory. */
            int leftGrandchild = leftChild(left);
            for (int i = 0; i < 4 && leftGrandchild + i < heap_.Count; ++i)
                if ((comparer_.Compare(heap_[leftGrandchild + i], heap_[smallestNode]) ^ (MaxLevel ? 1 : 0)) == -1)
                    smallestNode = leftGrandchild + i;

            // The current node was the smallest node, don't do anything.
            if (zindex == smallestNode) return;


            // Swap the current node with the smallest node
            Swap(ref heap_.ValueByRef(zindex), ref heap_.ValueByRef(smallestNode));

            // If the smallest node was a grandchild...
            if (smallestNode - left > 1)
            {
                // If the smallest node's parent is bigger than it, swap them
                if ((comparer_.Compare(heap_[parent(smallestNode)], heap_[smallestNode]) ^ (MaxLevel ? 1 : 0)) == -1)
                    Swap(ref heap_.ValueByRef(parent(smallestNode)), ref heap_.ValueByRef(smallestNode));

                trickleDown_(smallestNode, MaxLevel);
            }
        }

        /**
         * @brief Performs a sift-down, trickle-down, or bubble-down operation on
         *        the node specified by @c zindex.
         *
         * This operation is carried out similarily to the equivalent operation in a
         * standard heap.
         **/
        void trickleDown(int zindex)
        {
            if (isOnMinLevel(zindex))
                trickleDown_(zindex, false);
            else
                trickleDown_(zindex, true);
        }

        /**
         * @brief Finds the smallest element in the Min-Max Heap and return its
         *        index.
         *
         * @exception std::underflow_error
         **/
        int findMinIndex()
        {
            // There are four cases
            switch (heap_.Count)
            {
                case 0:
                    // The heap is empty so throw an error
                    throw new InvalidOperationException("No min element exists because " +
                                               "there are no elements in the " +
                                               "heap.");
                case 1:
                    // There is only one element in the heap, return that element
                    return 0;
                case 2:
                    // There are two elements in the heap, the child must be the min
                    return 1;
                default:
                    /* There are three or more elements in the heap, return the
                     * smallest child */
                    return (comparer_.Compare(heap_[1], heap_[2])) == -1 ? 1 : 2;
            }
        }       
    }
}
