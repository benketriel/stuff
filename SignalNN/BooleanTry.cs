
using System.Drawing;

namespace SignalNN
{
    public class BooleanTry
    {

        public static void Go()
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
            var m = new SparseModel(2, 10, new[] { 1000, 1000, 1000, 1000 }, 1, r);
            //var m = new DenseModel(2, new[] { 1 }, 1, r);
            //var input = new float[] { 0.1f, 0.9f };
            var signals = 1000;
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
                Console.WriteLine("Loss: " + (lossSum / data.Count / signals) + " - Coverage: " + (covergeSum / data.Count) + " - Epoch: " + epoch);
                //signals += 1;
                if (epoch % 10 == 0)
                //if (epoch > 0)
                {
                    var testImg = new Bitmap(myBitmap.Width, myBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    foreach (var (i, o) in data.OrderBy(x => r.Next()))
                    {
                        m.SetInput(i, signals);
                        var coverage = m.Activate();
                        m.GetOutput(output);
                        m.Clear();
                        var pred = (int)Math.Round(255 * output[0]);
                        testImg.SetPixel(
                            (int)Math.Round(i[0] * (testImg.Width - 1)),
                            (int)Math.Round(i[1] * (testImg.Height - 1)),
                            Color.FromArgb(pred, pred, pred));
                    }
                    testImg.Save(@"C:\SB\temp\pred" + epoch + ".png", System.Drawing.Imaging.ImageFormat.Png);
                }
                epoch++;
            }

        }

        public class DenseModel
        {
            public Neuron[][] Layers;
            public Random Rand;
            public const int MIN_LINKS = 2;
            public Neuron OutputNeuron;
            public int SignalCount = 0;

            public DenseModel(int inputSize, int[] layerSizes, int outputSize, Random r)
            {
                Layers = new Neuron[layerSizes.Length + 2][];
                Rand = r;
                OutputNeuron = new Neuron("OUTPUT", outputSize * 2, 0, r);

                Layers[0] = new Neuron[] { new Neuron("" + 0 + ":" + 0, inputSize * 2, layerSizes[0] * MIN_LINKS, r) };
                for (var i = 1; i < Layers.Length - 1; ++i)
                {
                    var inLinks = i == 1 ? MIN_LINKS : layerSizes[i - 2] * MIN_LINKS;
                    var outLinks = i == Layers.Length - 2 ? MIN_LINKS : layerSizes[i] * MIN_LINKS;
                    Layers[i] = new Neuron[layerSizes[i - 1]];
                    for (var j = 0; j < layerSizes[i - 1]; ++j)
                    {
                        Layers[i][j] = new Neuron("" + i + ":" + j, inLinks, outLinks, Rand);
                    }
                }
                Layers[Layers.Length - 1] = new Neuron[] { new Neuron("" + (Layers.Length - 1) + ":" + 0, layerSizes.Last() * MIN_LINKS, outputSize * 2, r) };

                Layers[0][0].SetConnections(Enumerable.Range(0, layerSizes[0] * MIN_LINKS).Select(i => (dst: Layers[1][i / 2], pos: i % 2)).ToArray());
                for (var i = 1; i < Layers.Length - 1; ++i)
                {
                    var outLinks = i == Layers.Length - 2 ? MIN_LINKS : layerSizes[i] * MIN_LINKS;
                    for (var j = 0; j < layerSizes[i - 1]; ++j)
                    {
                        Layers[i][j].SetConnections(Enumerable.Range(0, outLinks).Select(k => (dst: Layers[i + 1][k / 2], pos: j * 2 + k % 2)).ToArray());
                    }
                }
                Layers.Last()[0].SetConnections(Enumerable.Range(0, outputSize * 2).Select(i => (dst: OutputNeuron, pos: i)).ToArray());
            }

            public void SetInput(float[] input, int signals)
            {
                if (input.Length * 2 != Layers.First()[0].InputSize) throw new Exception("Invalid dimensions");

                if (Layers[0][0].InputState.Any()) throw new Exception("Input layer hasn't been cleared");

                for (var i = 0; i < signals; ++i)
                {
                    var idx = Rand.Next(input.Length);
                    var val = Rand.NextDouble() < input[idx] ? 1 : 0;
                    Layers[0][0].InputState.Add((null, -1, idx * 2 + val));
                }

                SignalCount = signals;
            }

            public float Activate()
            {
                var active = new HashSet<Neuron> { Layers[0][0] };
                var processed = 0;
                var total = Layers.Sum(x => x.Length) + 1;
                while (active.Any())
                {
                    var curr = active;
                    processed += curr.Count;
                    active = new HashSet<Neuron>();
                    foreach (var x in curr)
                    {
                        foreach (var y in x.Activate(SignalCount)) active.Add(y);
                    }
                }

                return (float)processed / total;
            }

            public void GetOutput(float[] output)
            {
                if (output.Length * 2 != Layers.Last()[0].OutputDistribution.Length) throw new Exception("Invalid dimensions");
                //for (var i = 0; i < output.Length; ++i)
                //{
                //    output[i] = Layers.Last()[0].OutputDistribution[i * 2];
                //}
                var outCount = new int[output.Length * 2];
                foreach (var (_, _, toIdx) in OutputNeuron.InputState)
                {
                    outCount[toIdx]++;
                }
                for (var i = 0; i < output.Length; ++i)
                {
                    var yes = outCount[i * 2] + 1;
                    var no = outCount[i * 2 + 1] + 1;
                    output[i] = (float)yes / (yes + no);
                }
            }

            public float Propagate(float[] targetOutput, float learningRate)
            {
                if (targetOutput.Length * 2 != Layers.Last()[0].OutputDistribution.Length) throw new Exception("Invalid dimensions");

                var outCount = new int[targetOutput.Length * 2];
                foreach (var (_, _, toIdx) in OutputNeuron.InputState)
                {
                    outCount[toIdx]++;
                }


                //var outAggr = outCount.Sum(x => (float)Math.Log(1 + x));
                var propagated = new HashSet<Neuron>();
                var active = new HashSet<Neuron>();
                var totalLoss = 0.0f;
                foreach (var (fromNeuron, fromIdx, i) in OutputNeuron.InputState)
                {
                    if (fromNeuron == null) continue;

                    var yes = outCount[i / 2 * 2] + 1;
                    var no = outCount[i / 2 * 2 + 1] + 1;
                    var tYes = targetOutput[i / 2];
                    var tNo = 1 - targetOutput[i / 2];

                    var loss = (i % 2 == 0 ? 1 : -1) * (float)(-tYes * Math.Log(yes) + tNo * Math.Log(no));

                    var grad = (i % 2 == 0 ? 1 : -1) * (float)(-tYes / yes + tNo / no);

                    totalLoss += loss;
                    fromNeuron.Gradients.Add((fromIdx, grad));
                    active.Add(fromNeuron);
                    propagated.Add(fromNeuron);
                }
                OutputNeuron.InputState.Clear();

                while (active.Any())
                {
                    var curr = active;
                    active = new HashSet<Neuron>();
                    foreach (var x in curr)
                    {
                        foreach (var y in x.Propagate())
                        {
                            active.Add(y);
                            propagated.Add(y);
                        }
                    }
                }

                foreach (var n in propagated)
                {
                    n.Commit(learningRate);
                }

                return totalLoss;
            }

            public void Clear()
            {
                foreach (var layer in Layers)
                {
                    foreach (var n in layer)
                    {
                        n.Clear();
                    }
                }
                OutputNeuron.Clear();
            }

        }

        public class SparseModel
        {
            public Neuron[][] Layers;
            public Random Rand;
            public const int MIN_LINKS = 2;
            public Neuron OutputNeuron;
            public int SignalCount = 0;

            public SparseModel(int inputSize, int linksPerNeuron, int[] layerSizes, int outputSize, Random r)
            {
                Layers = new Neuron[layerSizes.Length + 2][];
                Rand = r;
                OutputNeuron = new Neuron("OUTPUT", outputSize * 2, 0, r);

                Layers[0] = new Neuron[] { new Neuron("" + 0 + ":" + 0, inputSize * 2, layerSizes[0] * MIN_LINKS, r) };
                for (var i = 1; i < Layers.Length - 1; ++i)
                {
                    var inLinks = i == 1 ? MIN_LINKS : Math.Min(linksPerNeuron, layerSizes[i - 2]) * MIN_LINKS;
                    var outLinks = i == Layers.Length - 2 ? MIN_LINKS : Math.Min(linksPerNeuron, layerSizes[i]) * MIN_LINKS;
                    Layers[i] = new Neuron[layerSizes[i - 1]];
                    for (var j = 0; j < layerSizes[i - 1]; ++j)
                    {
                        Layers[i][j] = new Neuron("" + i + ":" + j, inLinks, outLinks, Rand);
                    }
                }
                Layers[Layers.Length - 1] = new Neuron[] { new Neuron("" + (Layers.Length - 1) + ":" + 0, layerSizes.Last() * MIN_LINKS, outputSize * 2, r) };

                Layers[0][0].SetConnections(Enumerable.Range(0, layerSizes[0] * MIN_LINKS).Select(i => (dst: Layers[1][i / 2], pos: i % 2)).ToArray());
                for (var i = 1; i < Layers.Length - 1; ++i)
                {
                    var inLinks = i == 1 ? MIN_LINKS : Math.Min(linksPerNeuron, layerSizes[i - 2]) * MIN_LINKS;
                    var outLinks = i == Layers.Length - 2 ? MIN_LINKS : Math.Min(linksPerNeuron, layerSizes[i]) * MIN_LINKS;
                    for (var j = 0; j < layerSizes[i - 1]; ++j)
                    {
                        Layers[i][j].SetConnections(Enumerable.Range(0, outLinks).Select(k => (dst: Layers[i + 1][(j + k / MIN_LINKS) % Layers[i + 1].Length], pos: k)).ToArray());
                    }
                }
                Layers.Last()[0].SetConnections(Enumerable.Range(0, outputSize * 2).Select(i => (dst: OutputNeuron, pos: i)).ToArray());
            }

            public void SetInput(float[] input, int signals)
            {
                if (input.Length * 2 != Layers.First()[0].InputSize) throw new Exception("Invalid dimensions");

                if (Layers[0][0].InputState.Any()) throw new Exception("Input layer hasn't been cleared");

                for (var i = 0; i < signals; ++i)
                {
                    var idx = Rand.Next(input.Length);
                    var val = Rand.NextDouble() < input[idx] ? 1 : 0;
                    Layers[0][0].InputState.Add((null, -1, idx * 2 + val));
                }

                SignalCount = signals;
            }

            public float Activate()
            {
                var active = new HashSet<Neuron> { Layers[0][0] };
                var processed = 0;
                var total = Layers.Sum(x => x.Length) + 1;
                while (active.Any())
                {
                    var curr = active;
                    processed += curr.Count;
                    active = new HashSet<Neuron>();
                    foreach (var x in curr)
                    {
                        foreach (var y in x.Activate(SignalCount)) active.Add(y);
                    }
                }

                return (float)processed / total;
            }

            public void GetOutput(float[] output)
            {
                if (output.Length * 2 != Layers.Last()[0].OutputDistribution.Length) throw new Exception("Invalid dimensions");
                //for (var i = 0; i < output.Length; ++i)
                //{
                //    output[i] = Layers.Last()[0].OutputDistribution[i * 2];
                //}
                var outCount = new int[output.Length * 2];
                foreach (var (_, _, toIdx) in OutputNeuron.InputState)
                {
                    outCount[toIdx]++;
                }
                for (var i = 0; i < output.Length; ++i)
                {
                    var yes = outCount[i * 2] + 1;
                    var no = outCount[i * 2 + 1] + 1;
                    output[i] = (float)yes / (yes + no);
                }
            }

            public float Propagate(float[] targetOutput, float learningRate)
            {
                if (targetOutput.Length * 2 != Layers.Last()[0].OutputDistribution.Length) throw new Exception("Invalid dimensions");

                var outCount = new int[targetOutput.Length * 2];
                foreach (var (_, _, toIdx) in OutputNeuron.InputState)
                {
                    outCount[toIdx]++;
                }


                //var outAggr = outCount.Sum(x => (float)Math.Log(1 + x));
                var propagated = new HashSet<Neuron>();
                var active = new HashSet<Neuron>();
                var totalLoss = 0.0f;
                foreach (var (fromNeuron, fromIdx, i) in OutputNeuron.InputState)
                {
                    if (fromNeuron == null) continue;

                    var yes = outCount[i / 2 * 2] + 1;
                    var no = outCount[i / 2 * 2 + 1] + 1;
                    var tYes = targetOutput[i / 2];
                    var tNo = 1 - targetOutput[i / 2];

                    var loss = (i % 2 == 0 ? 1 : -1) * (float)(-tYes * Math.Log(yes) + tNo * Math.Log(no));

                    var grad = (i % 2 == 0 ? 1 : -1) * (float)(-tYes / yes + tNo / no);

                    totalLoss += loss;
                    fromNeuron.Gradients.Add((fromIdx, grad));
                    active.Add(fromNeuron);
                    propagated.Add(fromNeuron);
                }
                OutputNeuron.InputState.Clear();

                while (active.Any())
                {
                    var curr = active;
                    active = new HashSet<Neuron>();
                    foreach (var x in curr)
                    {
                        foreach (var y in x.Propagate())
                        {
                            active.Add(y);
                            propagated.Add(y);
                        }
                    }
                }

                foreach (var n in propagated)
                {
                    n.Commit(learningRate);
                }

                return totalLoss;
            }

            public void Clear()
            {
                foreach (var layer in Layers)
                {
                    foreach (var n in layer)
                    {
                        n.Clear();
                    }
                }
                OutputNeuron.Clear();
            }

        }

        public class Neuron
        {
            public string Name;
            public int InputSize;
            public List<(Neuron? fromNeuron, int fromIdx, int toIdx)> InputState;
            public Dictionary<int, int> AggregatedInputs;

            public float[] Weights;
            public float[] AvgGradients;
            public float[] AvgGradient2s;

            public float[] MatMul;
            public float[] OutputDistribution;
            public List<(int idx, float gradient)> Gradients;

            public Random Rand;
            public (Neuron dst, int pos)[] Connections = Array.Empty<(Neuron dst, int pos)>();

            public float Temperature = 0.7f;

            public Neuron(string name, int inputs, int outputs, Random r)
            {
                Name = name;
                InputSize = inputs;
                InputState = new List<(Neuron? fromNeuron, int fromIdx, int toIdx)>();
                AggregatedInputs = new Dictionary<int, int>();
                Weights = new float[inputs * outputs];
                AvgGradients = new float[inputs * outputs];
                AvgGradient2s = new float[inputs * outputs];
                MatMul = new float[outputs];
                OutputDistribution = new float[outputs];
                Gradients = new List<(int idx, float gradient)>();

                Rand = r;

                //Init weights
                for (var i = 0; i < inputs; ++i)
                {
                    for (var j = 0; j < outputs; ++j)
                    {
                        Weights[i * outputs + j] = (float)(r.NextDouble() * 0.2 - 0.1);
                        //AvgGradients[i * outputs + j] = 0.0f;
                        AvgGradient2s[i * outputs + j] = 1.0f;
                    }
                }
            }

            public override string ToString()
            {
                return "Neuron[" + Name + "]";
            }

            public void SetConnections((Neuron dst, int pos)[] connections)
            {
                Connections = connections;
            }

            public HashSet<Neuron> Activate(int totalSignals)
            {
                if (!InputState.Any()) new Exception();
                var outputSize = OutputDistribution.Length;
                if (outputSize == 0) return new HashSet<Neuron>();

                AggregatedInputs = new Dictionary<int, int>();
                foreach (var (_, _, toIdx) in InputState)
                {
                    AggregatedInputs[toIdx] = AggregatedInputs.GetValueOrDefault(toIdx, 0) + 1;
                }

                for (var j = 0; j < outputSize; ++j)
                {
                    var sum = 0f;
                    foreach (var (i, iVal) in AggregatedInputs)
                    {
                        sum += Weights[i * outputSize + j] * iVal / totalSignals;
                    }
                    MatMul[j] = sum;
                }

                {
                    int maxI = Utils.MaxIndex(MatMul, 0, outputSize);

                    var max = MatMul[maxI];
                    var sum = 0.0f;
                    for (int j = 0; j < outputSize; ++j)
                    {
                        sum += OutputDistribution[j] = (float)Math.Exp((MatMul[j] - max) / Temperature);
                    }
                    for (int j = 0; j < outputSize; ++j)
                    {
                        OutputDistribution[j] /= sum;
                    }
                }

                var activeNeurons = new HashSet<Neuron>();
                if (Connections.Any())
                {
                    for (var j = 0; j < InputState.Count; ++j)
                    {
                        var target = Utils.SampleIndexWithNormalizedProbs(OutputDistribution, Rand);
                        var (dst, pos) = Connections[target];
                        dst.InputState.Add((this, target, pos));
                        activeNeurons.Add(dst);
                    }
                }
                return activeNeurons;
            }

            public HashSet<Neuron> Propagate()
            {
                if (!Gradients.Any()) new Exception();

                var outputSize = OutputDistribution.Length;

                //Find gradient of OutputDistribution
                var totalGrads = new Dictionary<int, float>();
                foreach (var (idx, gradient) in Gradients)
                {
                    totalGrads[idx] = totalGrads.GetValueOrDefault(idx, 0) + gradient;
                }

                //Find gradient of MatMul
                var grad = new float[outputSize];
                for (var i = 0; i < outputSize; ++i)
                {
                    var fg = 0.0f;
                    foreach (var j in totalGrads.Keys)
                    {
                        var toGj = totalGrads[j];
                        var toSj = OutputDistribution[j];
                        fg += toGj * ((j == i ? 1.0f : 0.0f) - OutputDistribution[i]) * toSj / Temperature;
                    }
                    grad[i] += fg;
                }

                //Apply gradient to W
                foreach (var (i, iVal) in AggregatedInputs)
                {
                    for (var j = 0; j < outputSize; ++j)
                    {
                        var g = iVal * grad[j];

                        AvgGradients[i * outputSize + j] = AvgGradients[i * outputSize + j] * 0.9f + 0.1f * g;
                        //AvgGradients[i * outputSize + j] = AvgGradients[i * outputSize + j] * 0.99f + 0.01f * g;

                        //AvgGradient2s[i * outputSize + j] = AvgGradient2s[i * outputSize + j] * 0.99f + 0.01f * g * g;
                        AvgGradient2s[i * outputSize + j] = AvgGradient2s[i * outputSize + j] * 0.999f + 0.001f * g * g;
                    }
                }
                //Apply gradient to inputs
                var activeNeurons = new HashSet<Neuron>();
                foreach (var (fromNeuron, fromIdx, i) in InputState)
                {
                    if (fromNeuron == null) continue;

                    var gradient = 0.0f;
                    for (int j = 0; j < outputSize; ++j)
                    {
                        gradient += Weights[i * outputSize + j] * grad[j];
                    }
                    gradient /= AggregatedInputs[i];
                    fromNeuron.Gradients.Add((fromIdx, gradient));
                    activeNeurons.Add(fromNeuron);
                }

                InputState.Clear();
                AggregatedInputs.Clear();
                Gradients.Clear();

                return activeNeurons;
            }

            public void Commit(float learningRate)
            {
                var outputs = OutputDistribution.Length;
                for (var i = 0; i < InputSize; ++i)
                {
                    for (var j = 0; j < outputs; ++j)
                    {
                        var idx = i * outputs + j;
                        var g = learningRate * AvgGradients[idx] / (float)Math.Max(Utils.EPSILON, Math.Sqrt(AvgGradient2s[idx]));
                        Weights[idx] += -g;
                    }
                }
            }

            public void Clear()
            {
                InputState.Clear();
                AggregatedInputs.Clear();
                Gradients.Clear();

            }
        }


    }


}