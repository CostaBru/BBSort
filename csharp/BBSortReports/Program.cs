using Flexols.Data.Collections;
using System;
using System.Collections.Generic;

namespace BBSortReports
{
    class Program
    {
        static int boundedRand(int range, Func<int> rnd)
        {
            var t = (-range) % range;

            long l = 0;
            long m = 0;
            
            do {
                var x = rnd();
                m = (long)(x) * (long)(range);
                l = (long)(m);
            } while (l < t);
            return (int)(m >> 32);
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
        
        static HybridList<T> rangeRandomN<T>(int n = int.MaxValue)
        {
            var random = new Random();

            HybridList<T> t = new HybridList<T>(n);

            var isInt = typeof(T) == typeof(int); 
            
            for (int i = 0; i <= n; ++i)
            {
                var result = isInt ? (T) (object) random.Next() : (T)Convert.ChangeType(random.NextDouble() * random.Next(), typeof(T));

                t.Add(result);
            }

            return t;
        }


        static HybridList<T> sample<T>(HybridList<T> population, int count)
        {
            HybridList<T> result = new HybridList<T>(count);

            var random = new Random();
            
            Func<int> func = () => random.Next();

            while (result.Count <= count)
            {
                result.Add(population[boundedRand(population.Count, func)]);
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

        static void test_duplicate_reports<T>(Func<T, float> getLog, Func<T, double> getFloat)  where T : struct, IComparable,  IComparable<T>
        {
            Print("test_duplicate_reports " + typeof(T).Name);

            HybridList<HybridList<T>> tests = new HybridList<HybridList<T>>();

            for (int i = 0; i < 1; ++i)
            {

                tests.Add(sample(rangeN<T>(-100000, 100000), 1000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 10000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 100000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 1000000));
                tests.Add(sample(rangeN<T>(-100000, 100000), 1024 * 1024 - 1));
            }

            TestThis(getLog, getFloat, tests);
        }
        
        static void test_rand_reports<T>(Func<T, float> getLog, Func<T, double> getFloat)  where T : struct, IComparable,  IComparable<T>
        {
            Print("test_rand_reports " + typeof(T).Name);

            HybridList<HybridList<T>> tests = new HybridList<HybridList<T>>();

            for (int i = 0; i < 1; ++i)
            {
                tests.Add(rangeRandomN<T>(100));
                tests.Add(rangeRandomN<T>(10000));
                tests.Add(rangeRandomN<T>(100000));
                tests.Add(rangeRandomN<T>(1000000));
                tests.Add(rangeRandomN<T>((1024 * 1024) - 1));
            }

            TestThis(getLog, getFloat, tests, false);
        }

        private static void TestThis<T>(Func<T, float> getLog, Func<T, double> getFloat, HybridList<HybridList<T>> tests, bool shuffle = true) where T : struct, IComparable, IComparable<T>
        {
            var rand = new Random();

            var clock = new System.Diagnostics.Stopwatch();

            for (int i = 0; i < 3; ++i)
            {
                int caseNumber = 1;
                bool allGood = true;

                foreach (var test in tests)
                {
                    Print(caseNumber.ToString());

                    if (shuffle)
                    {
                        Shuffle(rand, test);
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


                        Print($"[qsort              ] {ms}ms size: {qsortTest.Length}");
                    }
                    
                    var buildInSortTest = new T[test.Count];
                    test.CopyTo(buildInSortTest, 0);
                    {
                        clock.Reset();

                        clock.Start();

                        Array.Sort(buildInSortTest);

                        clock.Stop();

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[build-in sort       ] {ms}ms size: {qsortTest.Length}");
                    }
                  

                    var bbsortDictlessMMTest = new T[test.Count];
                    test.CopyTo(bbsortDictlessMMTest, 0);
                    {
                        clock.Reset();

                        clock.Start();

                        var bbSort = new BBsort.DictLess.MinMaxList.BBSort<T>(getLog, getFloat);

                        
                        bbSort.Sort(bbsortDictlessMMTest);

                        clock.Stop();

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[bb_sort_dictless mm] {ms}ms size: {bbsortDictlessMMTest.Length}");
                    }

                    bool good = qsortTest.Length == bbsortDictlessMMTest.Length;


                    for (int j = 0; j < qsortTest.Length; ++j)
                    {
                        var eq = EqualityComparer<T>.Default.Equals(qsortTest[j], bbsortDictlessMMTest[j]);

                        if (!eq)
                        {
                            Print($"bbsortDictlessMM: Not eq MM {j} {qsortTest[j]} != {bbsortDictlessMMTest[j]}");
                            
                            break;
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

        static void test_unique_reports<T>(Func<T, float> getLog, Func<T, double> getFloat) where T : struct, IComparable,  IComparable<T>
        {
            Print("test_unique_reports " + typeof(T).Name);

            HybridList<HybridList<T>> tests = new HybridList<HybridList<T>>();

            for (int i = 0; i < 1; ++i)
            {
                tests.Add(rangeN<T>(-1000000, 1000000, 1000));
                tests.Add(rangeN<T>(-1000000, 1000000, 10000));
                tests.Add(rangeN<T>(-1000000, 1000000, 100000));
                tests.Add(rangeN<T>(-1000000, 1000000, 1000000));
                tests.Add(rangeN<T>(-1000000, 1000000, 1024 * 1024 - 1));
            }

            var rand = new Random();

            var clock = new System.Diagnostics.Stopwatch();

            for (int i = 0; i < 3; ++i)
            {
                int caseNumber = 1;
                bool allGood = true;

                foreach (var test in tests)
                {
                    Print(caseNumber.ToString());

                    Shuffle(rand, test);
                    
                    var bbsortDictlessMMTest = new T[test.Count];
                    test.CopyTo(bbsortDictlessMMTest, 0);
                    {

                        clock.Reset();

                        clock.Start();

                        var bbSort = new BBsort.DictLess.MinMaxList.BBSort<T>(getLog, getFloat);

                        bbSort.Sort(bbsortDictlessMMTest);

                        clock.Stop();

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[bb_sort_dictless mm] {ms}ms size: {bbsortDictlessMMTest.Length}");
                    }
                    
                    var buildInSortTest = new T[test.Count];
                    test.CopyTo(buildInSortTest, 0);
                    {
                        clock.Reset();

                        clock.Start();

                        Array.Sort(buildInSortTest);

                        clock.Stop();

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[build-in sort       ] {ms}ms size: {buildInSortTest.Length}");
                    }

                    var qsortTest = new T[test.Count];
                    test.CopyTo(qsortTest, 0);

                    {

                        clock.Reset();

                        clock.Start();

                        var qs = new RosettaCode.QuickSort<T>(0);

                        qs.Sort(qsortTest);

                        clock.Stop();
                        

                        var ms = clock.ElapsedMilliseconds;

                        Print($"[qsort              ] {ms}ms size: {bbsortDictlessMMTest.Length}");
                    }

                    var good = true;
                    
                    for (int j = 0; j < qsortTest.Length; ++j)
                    {
                        var eq = EqualityComparer<T>.Default.Equals(qsortTest[j], bbsortDictlessMMTest[j]);

                        if (!eq)
                        {
                            Print($"Not eq  mm {j} {qsortTest[j]} != {bbsortDictlessMMTest[j]}");
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
            test_rand_reports<float>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog, f => f);
            test_rand_reports<int>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog, f => f);
            
            test_duplicate_reports<float>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog, f => f);
            test_unique_reports<float>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog, f => f);
            
            test_duplicate_reports<int>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog, f => f);
            test_unique_reports<int>(BBsort.DictLess.MinMaxList.BBSort<float>.getLog, f => f);
        }
    }
}
