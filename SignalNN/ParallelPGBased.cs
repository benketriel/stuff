using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalNN
{
    public class ParallelPGBased
    {
        private static bool CATEGORICAL = false;


        public static void GoMNIST()
        {
            Console.Write("Loading data...");
            var mnistTrain = new MNIST(true);
            var mnistTest = new MNIST(false);
            Console.WriteLine("Done");
            //DumpToFile(mnist.GetData().First().input, "test");

            var r = new Random(0);
            //var m = new SparseModel(inputSize: 28 * 28, linksPerNeuron: 1, hiddenLayers: 5, width: 1, navigationDataSize: 1, valueDataSize: 256, outputSize: 10, r); var signals = 1;
            //var m = new SparseModel(inputSize: 28 * 28, linksPerNeuron: 4, hiddenLayers: 5, width: 16, navigationDataSize: 16, valueDataSize: 64, outputSize: 10, r); var signals = 10;
            //var m = new SparseModel(inputSize: 28 * 28, linksPerNeuron: 1, hiddenLayers: 5, width: 1, navigationDataSize: 1, valueDataSize: 128, outputSize: 10, r); var signals = 1;
            var m = new SparseModel(inputSize: 28 * 28, linksPerNeuron: 4, hiddenLayers: 5, width: 16, navigationDataSize: 32, valueDataSize: 64, outputSize: 10, r); var signals = 100;

            var trainingGroupQueue = new List<int[]> { new[] { 0, 1, 2, 3 }, new[] { 4, 5 }, new[] { 6, 7 }, new[] { 8, 9 } };
            var finishedTrainingGroups = new List<int[]> { };
            var bestAccuracy = 0.0;
            var idleEpochs = 0;
            var finalPhase = false;

            var sw = new Stopwatch();
            var output = new float[10];
            var iter = 0;
            var epoch = 1;
            while (true)
            {
                sw.Restart();
                var lossSum = 0f;
                var covergeSum = 0f;
                var count = 0;
                foreach (var (i, o, w) in mnistTrain.GetData().OrderBy(x => r.Next()))
                {
                    if (!finalPhase && !trainingGroupQueue[0].Contains(Utils.HighestIndex(o))) continue;

                    m.SetInput(i, signals);
                    var coverage = m.Activate();
                    m.GetOutput(output);

                    var loss = m.Propagate(o, 1e-3f);

                    lossSum += loss;
                    covergeSum += coverage;

                    iter++;
                    count++;
                }

                //Test
                var hit = 0; var miss = 0;
                var oldHit = 0; var oldMiss = 0;
                foreach (var (i, o, w) in mnistTest.GetData())
                {
                    if (!finalPhase && !trainingGroupQueue[0].Contains(Utils.HighestIndex(o)) && (!finishedTrainingGroups.Any() || finishedTrainingGroups.All(g => !g.Contains(Utils.HighestIndex(o))))) continue;

                    m.SetInput(i, signals);
                    var coverage = m.Activate();
                    m.GetOutput(output);

                    if (finalPhase || trainingGroupQueue[0].Contains(Utils.HighestIndex(o)))
                    {
                        if (Utils.HighestIndex(output) == Utils.HighestIndex(o)) hit++; else miss++;
                    }
                    else
                    {
                        if (Utils.HighestIndex(output) == Utils.HighestIndex(o)) oldHit++; else oldMiss++;
                    }

                }
                var acc = (float)hit / Math.Max(1, hit + miss);
                var oldAcc = (float)oldHit / Math.Max(1, oldHit + oldMiss);
                if (bestAccuracy >= acc)
                {
                    idleEpochs++;
                }
                else
                {
                    idleEpochs = 0;
                    bestAccuracy = acc;
                }

                Console.WriteLine("" + (sw.ElapsedMilliseconds / 1000) + "s | Loss: " + (lossSum / count) + " - Accuracy: " + acc + " - Old Accuracy: " + oldAcc + " - Coverage: " + (covergeSum / count) + " - Epoch: " + epoch);

                if (idleEpochs > 10 && acc > 0.8)
                {
                    Console.WriteLine("Finished training group");
                    if (finalPhase) break;

                    finishedTrainingGroups.Add(trainingGroupQueue[0]);
                    trainingGroupQueue.RemoveAt(0);
                    if (!trainingGroupQueue.Any())
                    {
                        finalPhase = true;
                        Console.WriteLine("Now training all");
                    }
                    else
                    {
                        Console.WriteLine("Now training " + string.Join(", ", trainingGroupQueue[0]));
                    }
                    bestAccuracy = 0;
                    idleEpochs = 0;
                }

                epoch++;
            }
        }


        public static void GoTwitter()
        {
            var myBitmap = new Bitmap(@"C:\SB\temp\small.png");

            var data = new List<(float[] i, float[] o)>();
            for (int x = 0; x < myBitmap.Width; x++)
            {
                for (int y = 0; y < myBitmap.Height; y++)
                {
                    Color pixelColor = myBitmap.GetPixel(x, y);
                    // things we do with pixelColor
                    data.Add((
                        i: new float[] { (float)x / (myBitmap.Width - 1), (float)y / (myBitmap.Height - 1) },
                        o: new[] { pixelColor.GetBrightness() > 0.5 ? 1f : 0f }));
                    //o: new[] { x > y ? 1f : 0f }));
                }
            }

            var r = new Random(0);
            //var m = new DenseModel(2, new[] { 20, 20, 20, 20, 20, 20, 20 }, 1, r);
            //var m = new SparseModel(2, 10, new[] { 100, 100, 100, 100 }, new[] { 10, 10, 10, 10, 10 }, 1, r);
            //var m = new SparseModel(2, 2, new[] { 3, 4 }, new[] { 11, 12, 13 }, new[] { 97, 98, 99 }, 1, r);
            //var m = new SparseModel(2, 10, new[] { 100, 100, 100 }, new[] { 10, 10, 10, 10 }, new[] { 10, 10, 10, 10 }, 1, r);
            //var m = new SparseModel(2, 3, new[] { 10, 10, 10, 10, 10, 10, 10 }, new[] { 10, 10, 10, 10, 10, 10, 10, 10 }, new[] { 10, 10, 10, 10, 10, 10, 10, 10 }, 1, r); var signals = 10;
            //var m = new SparseModel(2, 10, new[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 }, new[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, new[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, 1, r); var signals = 1;
            //var m = new SparseModel(2, 1, new[] { 1, 1, 1, 1 }, new[] { 1, 1, 1, 1, 1 }, new[] { 100, 100, 100, 100, 100 }, 1, r); var signals = 1;
            var m = new SparseModel(inputSize: 2, linksPerNeuron: 4, hiddenLayers: 9, width: 64, navigationDataSize: 8, valueDataSize: 8, outputSize: 1, r); var signals = 16;
            //var m = new DenseModel(2, new[] { 1 }, 1, r);
            //var input = new float[] { 0.1f, 0.9f };
            //var signals = 10;
            var output = new float[1];
            //var targetOutput = new float[] { 0.8f };

            var iter = 0;
            var epoch = 1;
            while (true)
            {
                var lossSum = 0f;
                var covergeSum = 0f;
                foreach (var (i, o) in data.OrderBy(x => r.Next()))
                {
                    m.SetInput(i, signals);
                    var coverage = m.Activate();
                    m.GetOutput(output);
                    var loss = m.Propagate(o, 1e-3f);
                    //output[0] = i[0] > i[1] ? 1f : 0; var loss = m.Propagate(output);
                    lossSum += loss;
                    covergeSum += coverage;
                    //Console.WriteLine("Loss: " + loss + " - Coverage: " + coverage + " - Output: " + string.Join(",", output) + " - Iter: " + iter);
                    iter++;
                }
                Console.WriteLine("Loss: " + (lossSum / data.Count) + " - Coverage: " + (covergeSum / data.Count) + " - Epoch: " + epoch);
                //signals += 1;
                if (epoch % 100 == 0)
                //if (epoch > 0)
                {
                    var testImg = new Bitmap(myBitmap.Width, myBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    foreach (var (i, o) in data.OrderBy(x => r.Next()))
                    {
                        m.SetInput(i, signals);
                        var coverage = m.Activate();
                        m.GetOutput(output);
                        var pred = Math.Max(0, Math.Min(255, (int)Math.Round(255 * output[0])));
                        testImg.SetPixel(
                            (int)Math.Round(i[0] * (testImg.Width - 1)),
                            (int)Math.Round(i[1] * (testImg.Height - 1)),
                            Color.FromArgb(pred, pred, pred));
                    }
                    testImg.Save(@"C:\SB\temp\pred" + epoch + ".png", System.Drawing.Imaging.ImageFormat.Png);
                }
                epoch++;
            }





            //var n = new Neuron(new Random(0), 3, 2, 5, 7);
            //n.InputCardinality[0] = 0;
            //n.InputCardinality[1] = 1;
            //n.InputCardinality[2] = 4;
            //n.InputValues[0][0] = float.NaN;
            //n.InputValues[0][1] = float.NaN;
            //n.InputValues[0][2] = float.NaN;
            //n.InputValues[0][3] = float.NaN;
            //n.InputValues[0][4] = float.NaN;
            //n.InputValues[1][0] = 1;
            //n.InputValues[1][1] = 2;
            //n.InputValues[1][2] = 3;
            //n.InputValues[1][3] = 4;
            //n.InputValues[1][4] = 5;
            //n.InputValues[2][0] = 6;
            //n.InputValues[2][1] = 7;
            //n.InputValues[2][2] = 8;
            //n.InputValues[2][3] = 9;
            //n.InputValues[2][4] = 0;

            //n.Activate();




        }


        //
        private static float Temperature = 0.7f;
        private static float Linearity = 10f;
        private static float Leak = 0.1f;
        private static float Epsilon = 0.3f;

        private static void SoftMax(float[] x, float[] fx)
        {
            Utils.SoftMaxMultinomialProbabilities(x, Temperature, fx);
        }

        private static void DerivativeSoftMax(float[] x, float[] fx, float[] ddfx, float[] ddx)
        {
            var bias = 0f;
            for (var i = 0; i < x.Length; ++i)
            {
                bias += fx[i] * ddfx[i];
            }
            for (var i = 0; i < x.Length; ++i)
            {
                ddx[i] = fx[i] * (ddfx[i] - bias) / Temperature;
            }
        }

        private static void LeakySoftMax(float[] x, float[] fx)
        {
            Utils.SoftMaxMultinomialProbabilities(x, Temperature, fx);
            for (var i = 0; i < fx.Length; ++i)
            {
                fx[i] = fx[i] * (1 - Epsilon) + Epsilon / fx.Length;
            }
        }

        private static void DerivativeLeakySoftMax(float[] x, float[] fx, float[] ddfx, float[] ddx)
        {
            var bias = 0f;
            for (var i = 0; i < x.Length; ++i)
            {
                bias += (fx[i] - Epsilon / fx.Length) / (1 - Epsilon) * ddfx[i];
            }
            for (var i = 0; i < x.Length; ++i)
            {
                ddx[i] = (fx[i] - Epsilon / fx.Length) * (ddfx[i] - bias) / Temperature;
            }
        }

        private static void RELogU(float[] x, float[] fx)
        {
            for (var i = 0; i < x.Length; ++i)
            {
                float t = (float)(Linearity * Math.Log(1 + Math.Abs(x[i]) / Linearity));
                fx[i] = x[i] > 0.0 ? t : -t * Leak;
            }
        }

        private static void DerivativeRELogU(float[] x, float[] fx, float[] ddfx, float[] ddx)
        {
            for (var i = 0; i < x.Length; ++i)
            {
                float t = 1 / (1 + Math.Abs(x[i]) / Linearity);
                ddx[i] = (x[i] > 0.0 ? t : t * Leak) * ddfx[i];
            }
        }

        private static void Sigmoid(float[] x, float[] fx)
        {
            for (var i = 0; i < x.Length; ++i)
            {
                fx[i] = (float)(x[i] > 0.0 ? 1 / (1 + Math.Exp(-x[i])) : Math.Exp(x[i]) / (1 + Math.Exp(x[i])));
            }
        }

        private static void DerivativeSigmoid(float[] x, float[] fx, float[] ddfx, float[] ddx)
        {
            Sigmoid(x, ddx);
            for (var i = 0; i < x.Length; ++i)
            {
                ddx[i] = ddx[i] * (1 - ddx[i]);
            }
        }


        public class SparseModel
        {
            public Neuron[][] Layers;

            public Dictionary<Neuron, List<Link>> InLinks = new();
            public Dictionary<Neuron, List<Link>> OutLinks = new();

            public Random Rand;
            public int SignalCount = 0;

            public SparseModel(int inputSize, int linksPerNeuron, int hiddenLayers, int width, int navigationDataSize, int valueDataSize, int outputSize, Random r)
            {
                var layerNeuronCount = Enumerable.Range(0, hiddenLayers).Select(_ => width).ToArray();
                var layerNavigationDataSizes = Enumerable.Range(0, hiddenLayers + 1).Select(_ => navigationDataSize).ToArray();
                var layerValueDataSizes = Enumerable.Range(0, hiddenLayers + 1).Select(_ => valueDataSize).ToArray();

                if (layerNeuronCount.Length + 1 != layerNavigationDataSizes.Length) throw new Exception();
                if (layerNeuronCount.Length + 1 != layerValueDataSizes.Length) throw new Exception();
                if (linksPerNeuron > layerNeuronCount.Max()) throw new Exception();

                Rand = r;
                Layers = new Neuron[layerNeuronCount.Length + 2][];

                Layers[0] = new Neuron[] { new Neuron("INPUT", r, 1, layerNeuronCount[0], inputSize, layerNavigationDataSizes[0], inputSize, layerValueDataSizes[0], RELogU, DerivativeRELogU) };

                for (var layerI = 1; layerI < Layers.Length - 1; ++layerI)
                {
                    Layers[layerI] = new Neuron[layerNeuronCount[layerI - 1]];

                    var fanIn = layerI == 1 ? 1 : linksPerNeuron;
                    var fanOut = layerI == Layers.Length - 2 ? 1 : linksPerNeuron;
                    for (var neuronI = 0; neuronI < Layers[layerI].Length; ++neuronI)
                    {
                        Layers[layerI][neuronI] = new Neuron("" + layerI + ":" + neuronI, r, fanIn, fanOut, layerNavigationDataSizes[layerI - 1], layerNavigationDataSizes[layerI], layerValueDataSizes[layerI - 1], layerValueDataSizes[layerI], RELogU, DerivativeRELogU);
                    }
                }
                //MSE
                //Layers[^1] = new Neuron[] { new Neuron("OUTPUT", r, layerNeuronCount[^1], 1, layerNavigationDataSizes[^1], outputSize, layerValueDataSizes[^1], outputSize, RELogU, DerivativeRELogU) };
                //Layers[^1] = new Neuron[] { new Neuron("OUTPUT", r, layerNeuronCount[^1], 1, layerNavigationDataSizes[^1], outputSize, layerValueDataSizes[^1], outputSize, Sigmoid, DerivativeSigmoid) };

                //Categorical
                //Layers[^1] = new Neuron[] { new Neuron("OUTPUT", r, layerNeuronCount[^1], 1, layerNavigationDataSizes[^1], 1, layerValueDataSizes[^1], outputSize, SoftMax, DerivativeSoftMax) };
                Layers[^1] = new Neuron[] { new Neuron("OUTPUT", r, layerNeuronCount[^1], 1, layerNavigationDataSizes[^1], 1, layerValueDataSizes[^1], outputSize, SoftMax, DerivativeSoftMax) };

                for (var f = 0; f < layerNeuronCount[0]; ++f)
                {
                    AddLink(new Link { From = Layers[0][0], FromIdx = f, To = Layers[1][f], ToIdx = 0 });
                }
                for (var layerI = 1; layerI < Layers.Length - 2; ++layerI)
                {
                    for (var neuronFrom = 0; neuronFrom < Layers[layerI].Length; ++neuronFrom)
                    {
                        for (var f = 0; f < linksPerNeuron; ++f)
                        {
                            AddLink(new Link { From = Layers[layerI][neuronFrom], FromIdx = f, To = Layers[layerI + 1][(neuronFrom + f) % Layers[layerI + 1].Length], ToIdx = f });
                        }
                    }
                }
                for (var f = 0; f < layerNeuronCount[^1]; ++f)
                {
                    AddLink(new Link { From = Layers[^2][f], FromIdx = 0, To = Layers[^1][0], ToIdx = f });
                }

                //Test connections
                foreach (var l in Layers)
                {
                    foreach (var n in l)
                    {
                        if (n.Name != "INPUT" && n.FanIn > 0 && n.FanIn != InLinks[n].Count()) throw new Exception();
                        if (n.Name != "INPUT" && n.FanIn > 0 && n.FanIn != InLinks[n].Select(x => x.ToIdx).Distinct().Count()) throw new Exception();
                        if (n.Name != "OUTPUT" && n.FanOut > 0 && n.FanOut != OutLinks[n].Count()) throw new Exception();
                        if (n.Name != "OUTPUT" && n.FanOut > 0 && n.FanOut != OutLinks[n].Select(x => x.FromIdx).Distinct().Count()) throw new Exception();
                    }
                }

            }

            private void AddLink(Link l)
            {
                if (!InLinks.ContainsKey(l.To)) InLinks[l.To] = new();
                if (!OutLinks.ContainsKey(l.From)) OutLinks[l.From] = new();

                InLinks[l.To].Add(l);
                OutLinks[l.From].Add(l);
            }

            public void SetInput(float[] input, int signals)
            {
                var n = Layers[0][0];
                if (input.Length != n.InputNavigationData[0].Length) throw new Exception();
                if (input.Length != n.InputValueData[0].Length) throw new Exception();
                Array.Copy(input, n.InputNavigationData[0], input.Length);
                Array.Copy(input, n.InputValueData[0], input.Length);
                n.InputCardinality[0] = signals;
            }

            public float Activate()
            {
                foreach (var l in Layers.Skip(1))
                {
                    foreach (var n in l)
                    {
                        Array.Clear(n.InputCardinality);
                    }
                }

                var active = new HashSet<Neuron> { Layers[0][0] };
                var processed = 0;
                var total = Layers.Sum(x => x.Length);
                while (active.Any())
                {
                    var curr = active;
                    processed += curr.Count;
                    active = new HashSet<Neuron>();
                    foreach (var n in curr)
                    {
                        n.Activate();

                        foreach (var l in OutLinks.GetValueOrDefault(n, new()))
                        {
                            if (n.OutputCardinality[l.FromIdx] == 0) continue;

                            l.To.InputCardinality[l.ToIdx] += l.From.OutputCardinality[l.FromIdx];

                            if (l.From.OutputNavigationData[l.FromIdx].Length != l.To.InputNavigationData[l.ToIdx].Length) throw new Exception();
                            Array.Copy(l.From.OutputNavigationData[l.FromIdx], l.To.InputNavigationData[l.ToIdx], l.From.OutputNavigationData[l.FromIdx].Length);

                            if (l.From.OutputValueData[l.FromIdx].Length != l.To.InputValueData[l.ToIdx].Length) throw new Exception();
                            Array.Copy(l.From.OutputValueData[l.FromIdx], l.To.InputValueData[l.ToIdx], l.From.OutputValueData[l.FromIdx].Length);

                            lock (active) active.Add(l.To);
                        }
                    }
                }

                return (float)processed / total;
            }

            public void GetOutput(float[] output)
            {
                var n = Layers[^1][0];
                if (output.Length != n.OutputValueData[0].Length) throw new Exception();
                Array.Copy(n.OutputValueData[0], output, output.Length);
            }

            public float Propagate(float[] targetOutput, float learningRate)
            {
                var output = Layers[^1][0];
                if (targetOutput.Length != output.OutputValueData[0].Length) throw new Exception();

                //MSE
                //var totalLoss = 0f;
                //for (var i = 0; i < targetOutput.Length; ++i)
                //{
                //    totalLoss += (float)Math.Pow(targetOutput[i] - output.OutputValueData[0][i], 2);
                //    output.OutputValueDataGradients[0][i] = 2 * (targetOutput[i] - output.OutputValueData[0][i]); //It's the negative of the gradient of the loss; the gradient of the reward
                //}
                //output.Reward = -totalLoss;

                //Categorical
                var maxI = Utils.HighestIndex(targetOutput);
                var maxVal = Math.Max(Utils.EPSILON, output.OutputValueData[0][maxI]);
                var totalLoss = -(float)Math.Log(maxVal);
                for (var i = 0; i < targetOutput.Length; ++i)
                {
                    output.OutputValueDataGradient[0][i] = (float)(i == maxI ? 1 / maxVal : 0.0); //It's the negative of the gradient of the loss; the gradient of the reward
                }
                output.Reward = -totalLoss;

                Array.Clear(output.OutputNavigationDataGradient[0]);

                var propagated = new HashSet<Neuron>();
                var active = new HashSet<Neuron> { output };
                while (active.Any())
                {
                    var curr = active;
                    active = new HashSet<Neuron>();
                    foreach (var n in curr)
                    {
                        n.Propagate();
                        propagated.Add(n);

                        foreach (var l in InLinks.GetValueOrDefault(n, new()))
                        {
                            if (n.InputCardinality[l.ToIdx] == 0) continue;

                            if (l.To.InputNavigationDataGradient[l.ToIdx].Length != l.From.OutputNavigationDataGradient[l.FromIdx].Length) throw new Exception();
                            Array.Copy(l.To.InputNavigationDataGradient[l.ToIdx], l.From.OutputNavigationDataGradient[l.FromIdx], l.To.InputNavigationDataGradient[l.ToIdx].Length);

                            if (l.To.InputValueDataGradient[l.ToIdx].Length != l.From.OutputValueDataGradient[l.FromIdx].Length) throw new Exception();
                            Array.Copy(l.To.InputValueDataGradient[l.ToIdx], l.From.OutputValueDataGradient[l.FromIdx], l.To.InputValueDataGradient[l.ToIdx].Length);

                            l.From.Reward = l.To.Reward;

                            active.Add(l.From);
                        }
                    }
                }

                foreach (var n in propagated)
                {
                    n.Commit(learningRate);
                }

                return totalLoss;
            }

        }

        public struct Link
        {
            public Neuron From;
            public int FromIdx;

            public Neuron To;
            public int ToIdx;
        }

        public class Neuron
        {
            public readonly string Name;
            private readonly Random Rand;

            public readonly int FanIn;
            public readonly int FanOut;

            public readonly int NavigationDataSizeIn;
            public readonly int NavigationDataSizeOut;
            public readonly int ValueDataSizeIn;
            public readonly int ValueDataSizeOut;

            public readonly TrainableVariable[] WeightsDistribution; //separate 2-D matrix per input
            public readonly TrainableVariable BiasDistribution; //one vector for whole output distribution

            public readonly TrainableVariable[][] WeightsNavigationData; //separate 2-D matrix per input-output pair
            public readonly TrainableVariable[] BiasNavigationData; //one vector for each output data

            public readonly TrainableVariable[][] WeightsValueData; //separate 2-D matrix per input-output pair
            public readonly TrainableVariable[] BiasValueData; //one vector for each output data

            //Activation
            //Input
            public readonly int[] InputCardinality;
            public readonly float[][] InputNavigationData;
            public readonly float[][] InputValueData;

            //State
            public readonly float[] StateDistribution;
            public readonly float[][] StateNavigationData;
            public readonly float[][] StateValueData;

            //Output
            public readonly int[] OutputCardinality;
            public readonly float[] OutputDistribution;
            public readonly float[][] OutputNavigationData;
            public readonly float[][] OutputValueData;

            //Backpropagation
            //Input
            public readonly float[][] OutputNavigationDataGradient;
            public readonly float[][] OutputValueDataGradient;
            public float Reward;

            //State
            public readonly float[] StateDistributionGradient;
            public readonly float[][] StateNavigationDataGradient;
            public readonly float[][] StateValueDataGradient;

            //Output
            public readonly float[][] InputNavigationDataGradient;
            public readonly float[][] InputValueDataGradient;

            public readonly HashSet<TrainableVariable> UsedVariables = new();

            // All must be greater than 0 below
            private readonly float Temperature = 0.7f;

            private readonly Action<float[], float[]> Squash;
            private readonly Action<float[], float[], float[], float[]> DerivativeSquash;

            public Neuron(string name, Random rand, int fanIn, int fanOut, int navigationDataSizeIn, int navigationDataSizeOut, int valueDataSizeIn, int valueDataSizeOut, Action<float[], float[]> squash, Action<float[], float[], float[], float[]> derivativeSquash)
            {
                Name = name;
                Rand = new Random(rand.Next()); //Threadsafe random
                Squash = squash;
                DerivativeSquash = derivativeSquash;

                FanIn = fanIn;
                FanOut = fanOut;

                NavigationDataSizeIn = navigationDataSizeIn;
                NavigationDataSizeOut = navigationDataSizeOut;
                ValueDataSizeIn = valueDataSizeIn;
                ValueDataSizeOut = valueDataSizeOut;

                WeightsDistribution = Enumerable.Range(0, FanIn).Select(_ =>
                {
                    var v = new TrainableVariable(NavigationDataSizeIn * FanOut);
                    Utils.InitializeRandomVector(rand, v.Value);
                    return v;
                }).ToArray();
                BiasDistribution = new TrainableVariable(FanOut);

                WeightsNavigationData = Enumerable.Range(0, FanIn).Select(_ => Enumerable.Range(0, FanOut).Select(_ =>
                {
                    var v = new TrainableVariable(NavigationDataSizeIn * NavigationDataSizeOut);
                    Utils.InitializeRandomVector(rand, v.Value);
                    return v;
                }).ToArray()).ToArray();
                BiasNavigationData = Enumerable.Range(0, FanOut).Select(_ => new TrainableVariable(NavigationDataSizeOut)).ToArray();

                WeightsValueData = Enumerable.Range(0, FanIn).Select(_ => Enumerable.Range(0, FanOut).Select(_ =>
                {
                    var v = new TrainableVariable(ValueDataSizeIn * ValueDataSizeOut);
                    Utils.InitializeRandomVector(rand, v.Value);
                    return v;
                }).ToArray()).ToArray();
                BiasValueData = Enumerable.Range(0, FanOut).Select(_ => new TrainableVariable(ValueDataSizeOut)).ToArray();

                //Activation
                InputCardinality = new int[FanIn];
                InputNavigationData = Enumerable.Range(0, FanIn).Select(_ => new float[NavigationDataSizeIn]).ToArray();
                InputValueData = Enumerable.Range(0, FanIn).Select(_ => new float[ValueDataSizeIn]).ToArray();

                StateDistribution = new float[FanOut];
                StateNavigationData = Enumerable.Range(0, FanOut).Select(_ => new float[NavigationDataSizeOut]).ToArray();
                StateValueData = Enumerable.Range(0, FanOut).Select(_ => new float[ValueDataSizeOut]).ToArray();

                OutputCardinality = new int[FanOut];
                OutputDistribution = new float[FanOut];
                OutputNavigationData = Enumerable.Range(0, FanOut).Select(_ => new float[NavigationDataSizeOut]).ToArray();
                OutputValueData = Enumerable.Range(0, FanOut).Select(_ => new float[ValueDataSizeOut]).ToArray();

                //Backpropagation
                OutputNavigationDataGradient = Enumerable.Range(0, FanOut).Select(_ => new float[NavigationDataSizeOut]).ToArray();
                OutputValueDataGradient = Enumerable.Range(0, FanOut).Select(_ => new float[ValueDataSizeOut]).ToArray();

                StateDistributionGradient = new float[FanOut];
                StateNavigationDataGradient = Enumerable.Range(0, FanOut).Select(_ => new float[NavigationDataSizeOut]).ToArray();
                StateValueDataGradient = Enumerable.Range(0, FanOut).Select(_ => new float[ValueDataSizeOut]).ToArray();

                InputNavigationDataGradient = Enumerable.Range(0, FanIn).Select(_ => new float[NavigationDataSizeIn]).ToArray();
                InputValueDataGradient = Enumerable.Range(0, FanIn).Select(_ => new float[ValueDataSizeIn]).ToArray();
            }

            public override string ToString()
            {
                return "PGN[" + Name + "]";
            }

            public void Activate()
            {
                if (InputNavigationData.Length != FanIn) throw new Exception();
                if (InputValueData.Length != FanIn) throw new Exception();

                Array.Clear(OutputCardinality);
                var cSum = InputCardinality.Sum();

                ActivateNavigation(cSum);
                ActivateData(cSum);

            }

            private void ActivateNavigation(int cSum)
            {
                Array.Copy(BiasDistribution.Value, StateDistribution, FanOut);

                //Compute probabilities
                for (var fin = 0; fin < FanIn; ++fin)
                {
                    if (InputCardinality[fin] == 0) continue;

                    var w = WeightsDistribution[fin];

                    var data = InputNavigationData[fin];
                    if (data.Length != NavigationDataSizeIn) throw new Exception();
                    for (var sin = 0; sin < NavigationDataSizeIn; ++sin)
                    {
                        for (var fout = 0; fout < FanOut; ++fout)
                        {
                            StateDistribution[fout] += data[sin] * w.Value[sin * FanOut + fout] * InputCardinality[fin] / cSum;
                        }
                    }
                }

                //Sampling
                LeakySoftMax(StateDistribution, OutputDistribution);

                for (var i = 0; i < cSum; ++i)
                {
                    OutputCardinality[Utils.SampleIndexWithNormalizedProbs(OutputDistribution, Rand)]++;
                }
            }

            private void ActivateData(int cSum)
            {
                //Forward signals
                for (var fout = 0; fout < FanOut; ++fout)
                {
                    if (OutputCardinality[fout] == 0) continue;

                    var navigationState = StateNavigationData[fout];
                    var navigationBias = BiasNavigationData[fout];
                    Array.Copy(navigationBias.Value, navigationState, NavigationDataSizeOut);

                    var valueState = StateValueData[fout];
                    var valueBias = BiasValueData[fout];
                    Array.Copy(valueBias.Value, valueState, ValueDataSizeOut);

                    for (var fin = 0; fin < FanIn; ++fin)
                    {
                        if (InputCardinality[fin] == 0) continue;
                        var c = InputCardinality[fin] / cSum;

                        var navigationWeights = WeightsNavigationData[fin][fout];
                        var navigationData = InputNavigationData[fin];

                        for (var sin = 0; sin < NavigationDataSizeIn; ++sin)
                        {
                            var v = navigationWeights.Value;
                            for (var sout = 0; sout < NavigationDataSizeOut; ++sout)
                            {
                                navigationState[sout] += navigationData[sin] * v[sin * NavigationDataSizeOut + sout] * c;
                            }
                        }

                        var valueWeights = WeightsValueData[fin][fout];
                        var valueData = InputValueData[fin];

                        for (var sin = 0; sin < ValueDataSizeIn; ++sin)
                        {
                            var vdc = valueData[sin] * c;
                            var v = valueWeights.Value;
                            for (var sout = 0; sout < ValueDataSizeOut; ++sout)
                            {
                                valueState[sout] += vdc * v[sin * ValueDataSizeOut + sout];
                            }
                        }

                    }

                    Squash(navigationState, OutputNavigationData[fout]);
                    Squash(valueState, OutputValueData[fout]);
                }
            }

            public void Propagate()
            {
                for (var fin = 0; fin < FanIn; ++fin)
                {
                    Array.Clear(InputNavigationDataGradient[fin]);
                    Array.Clear(InputValueDataGradient[fin]);
                }

                var cSum = InputCardinality.Sum();

                //Compute OutCardinality * InError * grad log(DistStateProbabilities) and convert gradient through softmax to DistStateGradient
                {
                    for (var fout = 0; fout < FanOut; ++fout)
                    {
                        //StateDistributionGradient[fout] = Reward * (OutputCardinality[fout] - OutputDistribution[fout] * cSum) / Temperature; //If not leaky softmax could have used this optimization

                        //Temporary variable, optimization recycle the variable StateDistributionGradient
                        StateDistributionGradient[fout] = Reward * OutputCardinality[fout] / Math.Max(Utils.EPSILON, OutputDistribution[fout]);

                    }
                    DerivativeLeakySoftMax(StateDistribution, OutputDistribution, StateDistributionGradient, StateDistributionGradient);


                    //Propagate to BiasInDist
                    UsedVariables.Add(BiasDistribution);
                    BiasDistribution.GradientCount++;
                    for (var fout = 0; fout < FanOut; ++fout)
                    {
                        BiasDistribution.Gradient[fout] += StateDistributionGradient[fout];
                    }

                    //Propagate to WeightsInDist and input
                    for (var fin = 0; fin < FanIn; ++fin)
                    {
                        if (InputCardinality[fin] == 0) continue;
                        var c = InputCardinality[fin] / cSum;

                        var w = WeightsDistribution[fin];
                        UsedVariables.Add(w);
                        w.GradientCount++;

                        for (var sin = 0; sin < NavigationDataSizeIn; ++sin)
                        {
                            for (var fout = 0; fout < FanOut; ++fout)
                            {
                                InputNavigationDataGradient[fin][sin] += StateDistributionGradient[fout] * w.Value[sin * FanOut + fout] * c;
                                w.Gradient[sin * FanOut + fout] += StateDistributionGradient[fout] * InputNavigationData[fin][sin] * c;
                            }
                        }
                    }
                }

                //For each output, skipping those with 0 OutCardinality
                for (var fout = 0; fout < FanOut; ++fout)
                {
                    if (OutputCardinality[fout] == 0) continue;

                    //Convert the gradient through the squash and add to DataStateGradient
                    DerivativeSquash(StateNavigationData[fout], OutputNavigationData[fout], OutputNavigationDataGradient[fout], StateNavigationDataGradient[fout]);
                    DerivativeSquash(StateValueData[fout], OutputValueData[fout], OutputValueDataGradient[fout], StateValueDataGradient[fout]);

                    //Propagate to Bias
                    var navigationBias = BiasNavigationData[fout];
                    UsedVariables.Add(navigationBias);
                    navigationBias.GradientCount++;
                    for (var sout = 0; sout < NavigationDataSizeOut; ++sout)
                    {
                        navigationBias.Gradient[sout] += StateNavigationDataGradient[fout][sout];
                    }

                    var valueBias = BiasValueData[fout];
                    UsedVariables.Add(valueBias);
                    valueBias.GradientCount++;
                    for (var sout = 0; sout < ValueDataSizeOut; ++sout)
                    {
                        valueBias.Gradient[sout] += StateValueDataGradient[fout][sout];
                    }

                    //Propagate to Weights and input skipping those with 0 InCardinality
                    for (var fin = 0; fin < FanIn; ++fin)
                    {
                        if (InputCardinality[fin] == 0) continue;
                        var c = InputCardinality[fin] / cSum;

                        var navigationWeights = WeightsNavigationData[fin][fout];
                        UsedVariables.Add(navigationWeights);
                        navigationWeights.GradientCount++;
                        var nwg = navigationWeights.Gradient;
                        var nwv = navigationWeights.Value;

                        for (var sin = 0; sin < NavigationDataSizeIn; ++sin)
                        {
                            for (var sout = 0; sout < NavigationDataSizeOut; ++sout)
                            {
                                nwg[sin * NavigationDataSizeOut + sout] += StateNavigationDataGradient[fout][sout] * InputNavigationData[fin][sin] * c;
                                InputNavigationDataGradient[fin][sin] += StateNavigationDataGradient[fout][sout] * nwv[sin * NavigationDataSizeOut + sout] * c;
                            }
                        }

                        var valueWeights = WeightsValueData[fin][fout];
                        UsedVariables.Add(valueWeights);
                        valueWeights.GradientCount++;

                        var vwv = valueWeights.Value;
                        for (var sin = 0; sin < ValueDataSizeIn; ++sin)
                        {
                            var ivdc = InputValueData[fin][sin] * c;
                            var ig = 0f;
                            var svdg = StateValueDataGradient[fout];
                            for (var sout = 0; sout < ValueDataSizeOut; ++sout)
                            {
                                valueWeights.Gradient[sin * ValueDataSizeOut + sout] += svdg[sout] * ivdc;
                                ig += svdg[sout] * vwv[sin * ValueDataSizeOut + sout] * c;
                            }
                            InputValueDataGradient[fin][sin] += ig;
                        }

                    }
                }
            }

            public void Commit(float learningRate)
            {
                foreach (var v in UsedVariables)
                {
                    v.Commit(learningRate);
                }

                UsedVariables.Clear();
            }

        }

        public class TrainableVariable
        {
            public static int MIN_BATCH_SIZE = 128; // Default 1
            public readonly float[] Value;
            public readonly float[] Gradient;
            public int GradientCount = 0;
            public int Updates = 0;
            public readonly float[] AvgGradient;
            public readonly float[] AvgGradientSquared;
            private readonly float Beta;
            private readonly float Gamma;

            public TrainableVariable(int size, float beta = 0.9f, float gamma = 0.999f)
            {
                Beta = beta;
                Gamma = gamma;

                Value = new float[size];
                Gradient = new float[size];
                AvgGradient = new float[size];
                AvgGradientSquared = Enumerable.Range(0, size).Select(_ => 1.0f).ToArray();
            }
            public void Commit(float learningRate)
            {
                //if (GradientCount <= 0) return;
                if (GradientCount < MIN_BATCH_SIZE) return; //Stabler gradients

                var beta = Math.Min(Updates / (1f + Updates), Beta);
                var gamma = Math.Min(Updates / (1f + Updates), Gamma);
                for (var i = 0; i < Value.Length; ++i)
                {
                    var g = Gradient[i] / GradientCount;
                    //if (g == 0) continue;

                    AvgGradient[i] = AvgGradient[i] * beta + (1 - beta) * g;
                    AvgGradientSquared[i] = AvgGradientSquared[i] * gamma + (1 - gamma) * g * g;
                    Value[i] += (float)(learningRate * AvgGradient[i] / Math.Max(Utils.EPSILON, Math.Sqrt(AvgGradientSquared[i])));
                    Gradient[i] = 0;
                }
                Updates++;
                GradientCount = 0;
            }

        }

    }

}
