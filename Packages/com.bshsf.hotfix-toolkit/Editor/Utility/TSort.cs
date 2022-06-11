using System;
using System.Collections.Generic;
namespace zFramework.Hotfix.Toolkit
{
    public static class SortEx
    {
        //拓扑排序
        //https://stackoverflow.com/questions/4106862/how-to-sort-depended-objects-by-dependency/11027096#11027096
        public static IEnumerable<T> TSort<T, TKey>(this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, Func<T, TKey> comparerConditionSetter)
        {
            var sorted = new List<T>();
            var comparer = new GenericEqualityComparer<T, TKey>(comparerConditionSetter);
            var visited = new HashSet<T>(comparer);

            foreach (var item in source)
            {
                Visit(item, visited, sorted, dependencies);
            }
            return sorted;
        }

        private static void Visit<T>(T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies)
        {
            if (!visited.Contains(item))
            {
                visited.Add(item);
                foreach (var dep in dependencies(item))
                {
                    Visit(dep, visited, sorted, dependencies);
                }
                sorted.Add(item);
            }
        }
    }
    //https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
    /// <summary>
    /// 实例对比器
    /// </summary>
    /// <typeparam name="TItem">需要对比的对象</typeparam>
    /// <typeparam name="TKey">指定的判定条件</typeparam>
    public class GenericEqualityComparer<TItem, TKey> : IEqualityComparer<TItem>
    {
        private readonly Func<TItem, TKey> keySetter;
        private readonly EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
        public GenericEqualityComparer(Func<TItem, TKey> KeySetter) => this.keySetter = KeySetter;
        bool IEqualityComparer<TItem>.Equals(TItem x, TItem y) => (x == null && y == null) || (x != null && y != null && comparer.Equals(keySetter(x), keySetter(y)));
        int IEqualityComparer<TItem>.GetHashCode(TItem obj) => obj == null ? 0 : comparer.GetHashCode(keySetter(obj));
    }
}