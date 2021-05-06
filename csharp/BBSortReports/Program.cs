using Flexols.Data.Collections;
using System;
using System.Collections.Generic;

namespace BBSortReports
{
    class Program
    {
        static int boundedRand(int start, int range)
        {
            var x = start;
            var m = x * range;
            var l = m;
            if (l < range)
            {
                var t = -range;
                if (t >= range)
                {
                    t -= range;
                    if (t >= range)
                        t %= range;
                }
                while (l < t)
                {
                    x = start;
                    m = x * range;
                    l = m;
                }
            }
            return m >> 32;
        }

        static HybridList<T> rangeN<T>(int start, int end, int n = int.MaxValue)
        {

            HybridList<T> t = new HybridList<T>(n);

            for (int i = start; i < end && i <= n; ++i)
            {
                var result = (T)Convert.ChangeType(i, typeof(T));

                t.Add(result);
            }

            return t;
        }


        static HybridList<T> sample<T>(HybridList<T> population, int count)
        {
            HybridList<T> result = new HybridList<T>(count);

            while (result.Count <= count)
            {
                result.Add(population[boundedRand(0, population.Count)]);
            }
            return result;
        }

        static void Shuffle<T>(Random rng, IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void Print(string message)
        {
            System.Console.WriteLine(message);
        }

        static void test_duplicate_reports<T>(Func<T, float> getLog) where T : IComparable
        {
            Print("test_duplicate_reports " + typeof(T).Name);

            HybridList<HybridList<T>> tests = new HybridList<HybridList<T>>();

            for (int i = 0; i < 1; ++i)
            {

                tests.Add(sample(rangeN<T>(-100000, 100000), 1000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 10000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 100000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 1000000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 10000000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 100000000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 2000000000));
            }

            var rand = new Random();

            var clock = new System.Diagnostics.Stopwatch();

            for (int i = 0; i < 2; ++i)
            {

                int caseNumber = 1;
                bool allGood = true;

                foreach (var test in tests)
                {

                    Print(caseNumber.ToString());

                    Shuffle(rand, test);

                    var bbsortTest = new T[test.Count];

                    test.CopyTo(bbsortTest, 0);


                    {
                        clock.Reset();

                        clock.Start();

                        var bbSort = new BBsort.BBSort<T>(getLog);

                        bbSort.Sort(bbsortTest);

                        clock.Stop();

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[bb_sort] {ms}ms size: {bbsortTest.Length}");
                    }

                    var qsortTest = new T[test.Count];

                    test.CopyTo(qsortTest, 0);

                    {
                        clock.Reset();

                        clock.Start();

                        var qs = new RosettaCode.QuickSort<T>();

                        qs.Sort(qsortTest);

                        clock.Stop();

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[qsort  ] {ms}ms size: {bbsortTest.Length}");
                    }


                    bool good = qsortTest.Length == bbsortTest.Length;

                    for (int j = 0; j < qsortTest.Length; ++j)
                    {
                        var eq = EqualityComparer<T>.Default.Equals(qsortTest[j], bbsortTest[j]);

                        if (!eq)
                        {
                            Print($"Not eq {j} {qsortTest[j]} != {bbsortTest[j]}");
                        }

                        good = eq && good;
                    }

                    if (!good)
                    {
                        allGood = false;
                    }

                    caseNumber += 1;
                }

                if (!allGood)
                {
                    throw new ApplicationException("Some test failed.");
                }
            }
        }

        public static void Main()
        {
            test_duplicate_reports<float>(BBsort.BBSort<float>.getLog);
        }
    }
}
