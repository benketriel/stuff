using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace FormConsole
{
    partial class IO : Form
    {
        public IO(ThreadStart f)
        {
            InitializeComponent();
            System.Windows.Forms.Timer ticker = new System.Windows.Forms.Timer();
            ticker.Interval = 5;
            ticker.Tick += Tick;
            ticker.Start();
            MainThread = new Thread(f);
            MainThread.Start();
        }

        public void OutputLine(string line)
        {
            OutputString(line + Environment.NewLine);
        }

        public void OutputString(string s)
        {
            lock (TextOut)
                TextOut += s;
        }

        public string GetInputLine()
        {
            while (true)
            {
                lock (LinesIn)
                {
                    if (LinesIn.Count > 0)
                    {
                        string s = LinesIn.First();
                        LinesIn.RemoveFirst();
                        return s;
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void InputLine(string line)
        {
            lock (LinesIn)
                LinesIn.AddLast(line);
        }

        private void InputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!e.Shift)
                {
                    InputLine(input.Text);
                    input.Clear();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void Tick(object sender, EventArgs e)
        {
            lock (TextOut)
            {
                if (TextOut.Length > 0)
                {
                    if (output.Text.Length + TextOut.Length >= output.MaxLength)
                    {
                        output.Text = output.Text.Substring(0, output.Text.Length / 2);
                    }
                    output.AppendText(TextOut);
                    output.ScrollToCaret();
                    TextOut = "";
                }
            }

            if (!MainThread.IsAlive)
            {
                Close();
            }
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            MainThread.Abort();
            Process.GetCurrentProcess().Close();
        }

        private Thread MainThread = null;
        private LinkedList<string> LinesIn = new LinkedList<string>();
        private string TextOut = "";

    }
}
