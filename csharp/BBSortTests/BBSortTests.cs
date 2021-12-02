using Flexols.Data.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace BBSortTests
{
    public class Tests
    {       

        [SetUp]
        public void Setup()
        {
        }

        public static void Print(string message)
        {
            System.Console.WriteLine(message);
        }

        public void test_arrays<T>(T[] bb_rez, T[] goldenArr)
        {

            Assert.AreEqual(goldenArr.Length, bb_rez.Length, "sizes not equal");

            for (int i = 0; i < goldenArr.Length; ++i)
            {
                Assert.AreEqual(goldenArr[i], bb_rez[i], "Not equal at: " + i);
            }
        }

        public void sort_and_test(HybridList<float> arr)
        {
            var arrCopy = new HybridList<float>(arr);
            arrCopy.Reverse();

            var goldenArr = new float[arr.Count];
            arr.CopyTo(goldenArr, 0);

            Array.Sort<float>(goldenArr);

            var bbSort = new BBsort.DictLess.MinMaxList.BBSort<float>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog);

            var bbSortTest = new float[arrCopy.Count];
            
            arrCopy.CopyTo(bbSortTest, 0);

            bbSort.Sort(bbSortTest);

            test_arrays(bbSortTest, goldenArr);
        }

        [Test]
        public void test_bucket_worst_1()
        {
            Print("test_bucket_worst_1");

            HybridList<float> arr = new HybridList<float>() { 0.0001f, 0.0002f, 0.0003f, 1, 2, 3, 10, 20, 30, 100, 200, 300, 1000, 2000, 3000 };

            sort_and_test(arr);
        }

        [Test]
        public void test_bucket_worst_2()
        {

            Print("test_bucket_worst_2");

            HybridList<float> bucket_worse_arr = new HybridList<float>();
            HybridList<float> arrt = new HybridList<float>() { 0.0000000001f, 0.0000000002f, 0.0000000003f };
            float cluster = 10.0f;

            for (int i = 0; i < 10; ++i)
            {

                foreach (var val in arrt)
                {
                    bucket_worse_arr.Add(val * cluster);
                    cluster *= 10.0f;
                }
            }
            sort_and_test(bucket_worse_arr);
        }

        [Test]
        public void test_negative_ints()
        {

            Print("test_negative_ints");

            HybridList<float> arr = new HybridList<float>() { -5, -10, 0, -3, 8, 5, -1, 10 };
            sort_and_test(arr);
        }

        [Test]
        public void test_huge_gap()
        {

            Print("test_huge_gap");

            HybridList<float> arr = new HybridList<float>() { 9, 8, 7, 1, 1000000000 };
            sort_and_test(arr);
        }

        [Test]
        public void test_float_huge_gap()
        {

            Print("test_float_huge_gap");

            HybridList<float> arr = new HybridList<float>() { 0.9f, 0.8f, 0.7f, 0.1f, 1000000000 };
            sort_and_test(arr);
        }

        [Test]
        public void test_duplicates()
        {

            Print("test_duplicates");

            HybridList<float> arr = new HybridList<float>() { 10, 20, 40, 50, 60, 69, 70, 70, 70, 70, 70 };
            sort_and_test(arr);
        }
    }
}