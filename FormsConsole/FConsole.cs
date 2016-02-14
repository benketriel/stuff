using System;
using System.Windows.Forms;

namespace FormConsole
{
    public abstract class FConsole
    {
        [STAThread]
        public void Run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Io = new IO(MainThread);
            Application.Run(Io);
        }

        protected void PrintLine(string line)
        {
            Io.OutputLine(line);
        }

        protected void Print(string s)
        {
            Io.OutputString(s);
        }

        protected string ReadLine()
        {
            return Io.GetInputLine();
        }

        protected abstract void MainThread();
        //Example:
        //{
        //    OutputLine("Echo!");
        //    while (true) OutputLine(GetInputLine());
        //}

        private IO Io = null;
    }

}
