namespace Flexols.Data.Collections
{
    public static class _
    {
        public static void Swap<T>(ref T var1, ref T var2)
        {
            var temp = var1;
            var1 = var2;
            var2 = temp;
        }
        
        public static HybridList<T> List<T>(params T[] items)
        {
            return new HybridList<T>(items);
        }
        
        public static HybridDict<T, V> Map<T, V>(params (T,V)[] items)
        {
            var hybridDict = new HybridDict<T, V>();

            foreach (var tuple in items)
            {
                hybridDict[tuple.Item1] = tuple.Item2;
            }
            
            return hybridDict;
        }
        
        public static HybridDict<T, object> MapObj<T>(params (T,object)[] items)
        {
            var hybridDict = new HybridDict<T, object>();

            foreach (var tuple in items)
            {
                hybridDict[tuple.Item1] = tuple.Item2;
            }
            
            return hybridDict;
        }
    }
}