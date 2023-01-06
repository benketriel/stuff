using SignalNN;

namespace Bootstrap
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //BooleanTry.Go();



            PGBased.GoMNIST();
            //ParallelPGBased.GoMNIST();

            Console.WriteLine("Finished - Press enter to exit");
            Console.ReadLine();
        }
    }
}