using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib
{
    public static class Extensions
    {
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            Random rnd = new Random();
            while (n > 1)
            {
                int k = (rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }

    }

    public class Helpers
    {

        public void DoParallelIf<T>(bool condition, IEnumerable<T> list, Action<T> toDo)
        {
            if (condition)
            {
                Parallel.ForEach(list, toDo);
            }
            else
            {
                foreach (var x in list)
                {
                    toDo(x);
                }
            }
        }

    }
}
