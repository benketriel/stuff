using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalNN
{
    internal class Utils
    {
        public const float EPSILON = 1.0e-10f; //Want 1 - EPSILON to still be noticeable (around 1e-16 it won't anymore)

        public static float Max(float[] arr)
        {
            float best = arr[0];
            for (int i = 1; i < arr.Length; ++i)
            {
                if (best < arr[i])
                {
                    best = arr[i];
                }
            }
            return best;
        }


        public static IEnumerable<List<T>> Batchify<T>(IEnumerable<T> enumerable, int batchSize = 1024)
        {
            var debugCount = 0;
            var batch = new List<T>();
            foreach (var e in enumerable)
            {
                batch.Add(e);
                debugCount++;
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>();
                }
            }
            if (batch.Count > 0) yield return batch;
        }

        public static int[] NChooseM(int n, int m, Random r)
        {
            var res = new int[Math.Min(m, n)];
            var available = Enumerable.Range(0, n).ToList();
            for (int i = 0; i < m && available.Any(); ++i)
            {
                var idx = r.Next(available.Count());
                res[i] = available[idx];
                available.RemoveAt(idx);
            }
            return res;
        }

        public static int HighestIndex(IEnumerable<float> collection)
        {
            return HighestIndex(collection, (x, y) => x > y);
        }
        public static int HighestIndex(IEnumerable<int> collection)
        {
            return HighestIndex(collection, (x, y) => x > y);
        }
        public static int HighestIndex(IEnumerable<decimal> collection)
        {
            return HighestIndex(collection, (x, y) => x > y);
        }
        public static int HighestIndex<T>(IEnumerable<T> collection, Func<T, float> value)
        {
            return HighestIndex(collection, (x, y) => value(x) > value(y));
        }
        public static int HighestIndex<T>(IEnumerable<T> collection, Func<T, T, bool> isHigherThan)
        {
            var best = collection.First();
            var bestI = 0;
            var currI = -1;
            foreach (var curr in collection)
            {
                ++currI;
                if (isHigherThan(curr, best))
                {
                    best = curr;
                    bestI = currI;
                }
            }
            return bestI;
        }

        public static int MaxIndex(float[] arr, long offset, long length)
        {
            var best = arr[offset + 0];
            int bestI = 0;
            for (int i = 1; i < length; ++i)
            {
                if (best < arr[offset + i])
                {
                    best = arr[offset + i];
                    bestI = i;
                }
            }
            return bestI;
        }


        public static int SampleIndexWithNormalizedProbs(IEnumerable<float> distributions, Random r)
        {
            var rVal = r.NextDouble();
            var s = 0.0;
            var i = 0;
            foreach (var p in distributions)
            {
                s += p;
                if (rVal < s) return i;
                ++i;
            }
            return 0;
        }

        public static float[] Normalized(float[] arr)
        {
            var sum = arr.Sum();
            return arr.Select(x => x / sum).ToArray();
        }

        public static int SampleIndexWithProbs(float[] distributions, Random r)
        {
            return SampleIndexWithNormalizedProbs(Normalized(distributions), r);
        }

        public static float SampleGaussian(Random r, float mean, float stdDev)
        {
            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(r.NextDouble())) * Math.Sin(2.0 * Math.PI * r.NextDouble())); //random normal(0,1)
            float randNormal = mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;
        }

        public static int SamplePositiveGaussianInt(Random r, float stdDev)
        {
            //Not exact, could/should be improved, but here as a placeholder until improved
            var g = SampleGaussian(r, 0, stdDev);
            return (int)Math.Round(Math.Abs(g));
        }

        public static float SampleExponential(Random rand, float power)
        {
            return (float)(-Math.Log(1 - rand.NextDouble()) / power); //Mean is 1/power
        }

        public static int SamplePositiveExponentialInt(Random rand, float power)
        {
            return (int)Math.Round(SampleExponential(rand, power));
        }

        public static int SoftMaxMultinomial(float[] distributions, float τ, Random r)
        {
            var rVal = r.NextDouble();
            var s = 0.0;
            var max = distributions.Max();
            var sum = distributions.Sum(d => Math.Exp((d - max) / τ));
            for (var i = 0; i < distributions.Length; ++i)
            {
                s += Math.Exp((distributions[i] - max) / τ) / sum;
                if (rVal < s) return i;
            }
            return 0;
        }

        public static void SoftMaxMultinomialProbabilities(float[] distributions, float τ, float[] target)
        {
            var max = distributions.Max();
            var sum = distributions.Sum(a => Math.Exp((a - max) / τ));
            for (var i = 0; i < distributions.Length; ++i)
            {
                target[i] = (float)(Math.Exp((distributions[i] - max) / τ) / sum);
            }
        }

        public static T[] Scramble<T>(T[] vec, Random r)
        {
            return vec.ToList().OrderBy(x => r.Next()).ToArray();
        }

        public static float[] RandomNegatedOneHotVectors(float[] values, Random r, int[] partitions = null)
        {
            if (partitions == null) partitions = new int[] { 0 };
            var res = new float[values.Length];
            for (int i = 0; i < partitions.Length; ++i)
            {
                var from = partitions[i];
                var to = i + 1 < partitions.Length ? partitions[i + 1] : values.Length;

                var partOut = values.Skip(from).Take(to - from).ToList();
                var partI = partOut.IndexOf(partOut.Max());

                var randI = r.Next(to - from - 1);
                if (randI >= partI) ++randI;
                res[from + randI] = 1.0f;
            }
            return res;

            //return values.Select(x => x == 1 ? 0.0 : 1.0 / (values.Length - 1)).ToArray(); //This is the non-random version which I'm not sure works mathematically with softmax
        }

        public static bool ArraysEqual(int[] a1, int[] a2)
        {
            for (var i = 0; i < a1.Length; ++i)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }
        public static bool ArraysEqual(float[] a1, float[] a2)
        {
            for (var i = 0; i < a1.Length; ++i)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }
        public static bool ArraysEqual<T>(T[] a1, T[] a2) where T : class
        {
            for (var i = 0; i < a1.Length; ++i)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }

        public static float[] FindPerpendicularToInDirectionOf(float[] a, float[] b)
        {
            //Hyperplane perpendicular to a is a_0 * x_0 + ... + a_n * x_n = 0
            //Starting at point 0, we move to b - t * a to a point on the hyperplane
            //This point (vector from 0), is a vector perpendicular to a, but towards b, the solution

            var a_dot_b = Enumerable.Range(0, a.Length).Sum(i => a[i] * b[i]);
            var a_dot_a = Enumerable.Range(0, a.Length).Sum(i => a[i] * a[i]);

            //a_0 * (b_0 - t * a_0) + ... = 0, so
            var t = a_dot_b / a_dot_a;
            return Enumerable.Range(0, a.Length).Select(i => b[i] - t * a[i]).ToArray();
        }

        public static float[] IndexToOneHot(int i, int size)
        {
            var arr = new float[size];
            if (0 <= i && i < size) arr[i] = 1;
            else throw new Exception("Index out of bounds");
            return arr;
        }

        public static float[][] IndicesToOneHots(int[] indices, int[] sizes)
        {
            return Enumerable.Range(0, indices.Length).Select(i => IndexToOneHot(indices[i], sizes[i])).ToArray();
        }

        public static float[][] IndicesToOneHots(int[] indices, int size)
        {
            return Enumerable.Range(0, indices.Length).Select(i => IndexToOneHot(indices[i], size)).ToArray();
        }

        public static List<int[]> AllPossibleChoices(int[] ranges)
        {
            var res = new List<int[]>();
            var current = new int[ranges.Length];
            res.Add(current.ToArray());
            if (ranges.Length == 0 || ranges.Length == 1 && ranges[0] == 0) return res;
            while (true)
            {
                //Find next
                for (var i = ranges.Length - 1; i >= 0; --i)
                {
                    if (current[i] < ranges[i] - 1)
                    {
                        ++current[i];
                        break;
                    }
                    current[i] = 0;
                    if (i == 0) return res; //No more
                }

                //Add next
                res.Add(current.ToArray());
            }
        }

        public static List<float[][]> AllPossibleOneHotVectorCombinations(int[] sizes)
        {
            var acts = AllPossibleChoices(sizes);

            return acts.Select(act => Enumerable.Range(0, sizes.Length).Select(typeI =>
                IndexToOneHot(act[typeI], sizes[typeI])).ToArray()).ToList();
        }


        public static void InitializeRandomVector(Random r, float[] vec)
        {
            for (var i = 0; i < vec.Length; ++i)
            {
                vec[i] = (float)(r.NextDouble() * 0.2 - 0.1);
            }
        }

    }
}
