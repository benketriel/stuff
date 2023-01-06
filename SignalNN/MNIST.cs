using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalNN
{
    public class MNIST
    {
        //Load mnist

        public MNIST(bool train)
        {
            if (train)
            {
                DataPath = @"C:\repos\MNIST\train-images-idx3-ubyte.gz";
                LabelsPath = @"C:\repos\MNIST\train-labels-idx1-ubyte.gz";
            }
            else
            {
                DataPath = @"C:\repos\MNIST\t10k-images-idx3-ubyte.gz";
                LabelsPath = @"C:\repos\MNIST\t10k-labels-idx1-ubyte.gz";
            }

            var b = GZipFileToByteArray(DataPath);
            int n = ReadInt(b, 4);
            int r = ReadInt(b, 8);
            int c = ReadInt(b, 12);

            Data = Enumerable.Range(0, n).Select(i => Enumerable.Range(0, r * c).Select(j => b[16 + i * r * c + j] / 255.0f).ToArray()).ToList();

            b = GZipFileToByteArray(LabelsPath);
            n = ReadInt(b, 4);

            Labels = Enumerable.Range(0, n).Select(i => Utils.IndexToOneHot(b[8 + i], 10)).ToList();

            DataSize = r * c;
            LabelsSize = 10;
        }

        private readonly string DataPath;
        private readonly string LabelsPath;
        public int DataSize;
        public int LabelsSize;

        private readonly List<float[]> Data;
        private readonly List<float[]> Labels;

        private static int ReadInt(byte[] b, int offset)
        {
            uint n = 0;
            for (int i = 0; i < 4; ++i)
            {
                n <<= 8;
                n += b[offset + i];
            }
            return (int)n;
        }

        public static byte[] GZipFileToByteArray(string path)
        {
            using FileStream reader = File.OpenRead(path);
            using GZipStream zip = new GZipStream(reader, CompressionMode.Decompress, true);
            using MemoryStream ms = new MemoryStream();
            zip.CopyTo(ms);
            return ms.ToArray();
        }

        public IEnumerable<(float[] input, float[] output, float weight)> GetData()
        {
            for (var i = 0; i < Data.Count; ++i)
            {
                yield return (Data[i], Labels[i], 1.0f);
            }
        }

        public int GetInputSize() => DataSize;

        public int GetOutputSize() => LabelsSize;



    }
}
