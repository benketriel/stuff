using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;

namespace EnchantedCaveBot //http://armorgames.com/play/17682/the-enchanted-cave-2?tag-referral=rpg
{
    class Program
    {
        const int WIDTH = 32;
        const int HEIGTH = 32;
        const int RES = 6;
        private static Point TopLeft = new Point();
        private static Point BottomRight = new Point();
        private static Tile[,] Map = new Tile[WIDTH, HEIGTH];
        private static int Px = 0;
        private static int Py = 0;
        private static List<Tuple<int, int>> DoubleCheck = new List<Tuple<int, int>>();

        [STAThread]
        static void Main(string[] args)
        {
            Point cursor = new Point();
            GetCursorPos(ref cursor);

            //while (true)
            //{
            //    Thread.Sleep(5);
            //    var newCursor = new Point();
            //    GetCursorPos(ref newCursor);
            //    if (newCursor != cursor)
            //    {
            //        cursor = newCursor;
            //        var c = GetColorAt(cursor);
            //        Console.WriteLine("X:" + cursor.X + " Y:" + cursor.Y + " A:" + c.A + " R: " + c.R + " G:" + c.G + " B:" + c.B);
            //    }
            //}

            Wait();
            GetCursorPos(ref TopLeft);

            Wait();
            FixTopLeft();

            BottomRight.X = TopLeft.X + WIDTH * RES;
            BottomRight.Y = TopLeft.Y + HEIGTH * RES;


            int tryi = 0;
            while (true)
            {
                try
                {
                    ReadMap();

                    while (true)
                    {
                        var path = FindNextTargetPath();
                        MovePath(path);
                    }

                }
                catch (Exception ex)
                {
                    if (++tryi >= 10)
                    {
                        Application.SetSuspendState(PowerState.Suspend, true, true);
                        Wait();
                    }
                    Console.WriteLine(ex.Message);

                }
            }


            while (!Keyboard.IsKeyDown(Key.Z))
            {
                //Method 1
                var fg = GetForegroundWindow();
                Console.WriteLine("PID: " + fg);
                PostMessage(fg, WM_KEYDOWN, 0x57, 0);
                Thread.Sleep(100);
                PostMessage(fg, WM_KEYUP, 0x57, 0);




                //DirectX
                //Send_Key(0x11, KEYEVENTF_KEYUP);


                //Method 2 - send msg is synchronuous unlike post msg, so here you can query stuff too
                //var fg = GetForegroundWindow();
                //Console.WriteLine("PID: " + fg);
                //if (fg != IntPtr.Zero)
                //{
                //    var res = SendMessage(fg, WM_KEYDOWN, (IntPtr)'W', IntPtr.Zero);
                //    int error_code = Marshal.GetLastWin32Error();
                //    Console.WriteLine(res + " - " + error_code);

                //    Thread.Sleep(100);

                //    res = SendMessage(fg, WM_KEYUP, (IntPtr)'W', IntPtr.Zero);
                //    error_code = Marshal.GetLastWin32Error();
                //    Console.WriteLine(res + " - " + error_code);
                //}


                //Method 3
                //try
                //{
                //    //SendKeys.SendWait("{W}");
                //    SendKeys.Send("{W}");
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}





                Console.Write('.');
                Thread.Sleep(2000);
            }
            Console.WriteLine("hi");



        }

        private static void FixTopLeft()
        {
            TopLeft.X += RES * 2;
            TopLeft.Y += RES * 2;

            while (true)
            {
                var c = GetColorAt(TopLeft);
                if (c.R == 0 && c.G == 0 && c.B == 0) break;
                --TopLeft.X;
            }
            ++TopLeft.X;

            while (true)
            {
                var c = GetColorAt(TopLeft);
                if (c.R == 0 && c.G == 0 && c.B == 0) break;
                --TopLeft.Y;
            }
            ++TopLeft.Y;
        }

        private static List<BfsStep> FindNextTargetPath()
        {
            bool found = false;
            for (int y = 0; y < HEIGTH; ++y)
            {
                for (int x = 0; x < WIDTH; ++x)
                {
                    if (Map[x, y] == Tile.T_TREASURE1
                        || Map[x, y] == Tile.t_TREASURE2
                        || Map[x, y] == Tile.O_ENEMY)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            return PathBfs(!found);
        }

        class ListComp : IComparer<double>
        {
            public int Compare(double a, double b)
            {
                return a > b ? 1 : -1;
            }
        }

        private static List<BfsStep> PathBfs(bool goForGoal)
        {
            var steps = new SortedList<double, BfsStep>(new ListComp());
            var beenThere = new HashSet<string>();
            steps.Add(0, new BfsStep(null, Move.NOTHING, Px, Py, Tile._EMPTY));

            while (steps.Any())
            {
                var curr = steps.First();
                steps.RemoveAt(0);

                //Check if result
                if (goForGoal)
                {
                    if (curr.Value.Tile == Tile.X_GOAL)
                    {
                        return curr.Value.Path();
                    }
                }
                else
                {
                    if (curr.Value.Tile == Tile.O_ENEMY
                        || curr.Value.Tile == Tile.T_TREASURE1
                        || curr.Value.Tile == Tile.t_TREASURE2)
                    {
                        return curr.Value.Path();
                    }
                }

                //Ignore dummies
                if (curr.Value.Tile != Tile._EMPTY) continue;

                //Add sons
                if (!CheckBeenThere(beenThere, curr.Value.Px, curr.Value.Py - 1))
                {
                    steps.Add(Heuristic(curr.Value.Px, curr.Value.Py - 1),
                        new BfsStep(curr.Value, Move.UP, curr.Value.Px, curr.Value.Py - 1, Map[curr.Value.Px, curr.Value.Py - 1]));
                }
                if (!CheckBeenThere(beenThere, curr.Value.Px, curr.Value.Py + 1))
                {
                    steps.Add(Heuristic(curr.Value.Px, curr.Value.Py + 1),
                        new BfsStep(curr.Value, Move.DOWN, curr.Value.Px, curr.Value.Py + 1, Map[curr.Value.Px, curr.Value.Py + 1]));
                }
                if (!CheckBeenThere(beenThere, curr.Value.Px - 1, curr.Value.Py))
                {
                    steps.Add(Heuristic(curr.Value.Px - 1, curr.Value.Py),
                        new BfsStep(curr.Value, Move.LEFT, curr.Value.Px - 1, curr.Value.Py, Map[curr.Value.Px - 1, curr.Value.Py]));
                }
                if (!CheckBeenThere(beenThere, curr.Value.Px + 1, curr.Value.Py))
                {
                    steps.Add(Heuristic(curr.Value.Px + 1, curr.Value.Py),
                        new BfsStep(curr.Value, Move.RIGHT, curr.Value.Px + 1, curr.Value.Py, Map[curr.Value.Px + 1, curr.Value.Py]));
                }
            }

            throw new Exception("No destination found");
        }

        private static double Heuristic(int x, int y)
        {
            return Dist(x, y, (tx, ty) => tx == Px && ty == Py) - 0.9 * Dist(x, y, (tx, ty) => Map[tx, ty] == Tile.X_GOAL);
        }

        private static int Dist(int x, int y, Func<int, int, bool> goal)
        {
            var steps = new List<DistBfsStep>();
            var beenThere = new HashSet<string>();
            steps.Add(new DistBfsStep { Px = x, Py = y, Dist = 0 });
            var tt = Map[x, y];
            if (tt != Tile._EMPTY && tt != Tile.O_ENEMY && tt != Tile.t_TREASURE2) return -HEIGTH * WIDTH * 3;

            while (steps.Any())
            {
                var curr = steps.First();
                steps.RemoveAt(0);

                //Check if result
                if (goal(curr.Px, curr.Py))
                {
                    return curr.Dist;
                }

                //Ignore dummies
                var t = Map[curr.Px, curr.Py];
                if (t != Tile._EMPTY && t != Tile.O_ENEMY && t != Tile.t_TREASURE2) continue;

                //Add sons
                if (!CheckBeenThere(beenThere, curr.Px, curr.Py - 1))
                {
                    steps.Add(new DistBfsStep { Px = curr.Px, Py = curr.Py - 1, Dist = curr.Dist + 1 });
                }
                if (!CheckBeenThere(beenThere, curr.Px, curr.Py + 1))
                {
                    steps.Add(new DistBfsStep { Px = curr.Px, Py = curr.Py + 1, Dist = curr.Dist + 1 });
                }
                if (!CheckBeenThere(beenThere, curr.Px - 1, curr.Py))
                {
                    steps.Add(new DistBfsStep { Px = curr.Px - 1, Py = curr.Py, Dist = curr.Dist + 1 });
                }
                if (!CheckBeenThere(beenThere, curr.Px + 1, curr.Py))
                {
                    steps.Add(new DistBfsStep { Px = curr.Px + 1, Py = curr.Py, Dist = curr.Dist + 1 });
                }
            }

            throw new Exception("No path found for dist");
        }

        private static bool CheckBeenThere(HashSet<string> beenThere, int x, int y)
        {
            var key = "" + x + "," + y;
            if (beenThere.Contains(key))
            {
                return true;
            }
            beenThere.Add(key);
            return false;

        }

        class DistBfsStep
        {
            public int Px = 0;
            public int Py = 0;
            public int Dist = 0;
        }

        class BfsStep
        {
            public BfsStep Parent = null;
            public Move Move = Move.OK;
            public int Px = 0;
            public int Py = 0;
            public Tile Tile = Tile.W_WALL;

            public BfsStep(BfsStep parent, Move move, int px, int py, Tile tile)
            {
                Parent = parent;
                Move = move;
                Px = px;
                Py = py;
                Tile = tile;
            }

            public List<BfsStep> Path()
            {
                var res = new List<BfsStep>();
                var curr = this;
                do
                {
                    res.Add(curr);
                    curr = curr.Parent;
                } while (null != curr);

                res.Reverse();
                return res;
            }
        }

        private static void MovePath(List<BfsStep> path)
        {
            foreach (var p in path)
            {
                DoMove(p.Move);
                switch (p.Tile)
                {
                    case Tile._EMPTY:
                    case Tile.O_ENEMY:
                    case Tile.t_TREASURE2:
                        Thread.Sleep(50);
                        int i = 0;
                        while (!PlayerReached(p.Px, p.Py))
                        {
                            if (++i >= 50)
                            {
                                DoMove(Move.SHOP);
                                Thread.Sleep(500);
                                DoMove(Move.ESC);
                                Thread.Sleep(500);
                                Map[p.Px, p.Py] = Tile.W_WALL;
                                return;
                            }
                            DoMove(p.Move);
                            Thread.Sleep(10);
                        }
                        Px = p.Px;
                        Py = p.Py;
                        break;
                }
                switch (p.Tile)
                {
                    case Tile.O_ENEMY:
                    case Tile.t_TREASURE2:
                        Map[p.Px, p.Py] = Tile._EMPTY;
                        break;
                }
                switch (p.Tile)
                {
                    case Tile.T_TREASURE1:
                        DoubleCheck.Add(new Tuple<int, int>(p.Px, p.Py));
                        Map[p.Px, p.Py] = Tile.W_WALL;
                        Thread.Sleep(50);
                        DoMove(p.Move); //To make sure he takes it
                        break;
                }
                switch (p.Tile)
                {
                    case Tile.X_GOAL:
                        Thread.Sleep(1000);
                        DoMove(p.Move); //To make sure he takes it
                        Thread.Sleep(500);
                        DoMove(Move.OK);
                        Thread.Sleep(2000);
                        DoMove(Move.REVEAL);
                        Thread.Sleep(1200);
                        ReadMap();

                        break;
                }
                if (p.Tile == Tile._EMPTY && p.Move != Move.NOTHING)
                {
                    foreach (var dc in DoubleCheck)
                    {
                        if (ReadTile(dc.Item1, dc.Item2) == Tile.T_TREASURE1)
                        {
                            Map[dc.Item1, dc.Item2] = Tile.T_TREASURE1;
                            return;
                        }
                    }
                    DoubleCheck.Clear();
                }
                if (p.Tile == Tile.O_ENEMY)
                {
                    if (new Random().Next() % 2 == 0)
                    {
                        DoMove(Move.HEAL);
                    }
                }
            }
        }

        private static bool PlayerReached(int x, int y)
        {
            if (ReadTile(0, 0) == Tile.UNKNOWN) //Screen got black
            {
                Thread.Sleep(1000);
                DoMove(Move.ESC);
                Thread.Sleep(1000);
            }

            return ReadTile(x, y) == Tile.P_PLAYER;
        }

        private static void DoMove(Move move)
        {
            if (move == Move.NOTHING) return;

            int key = 0;

            switch (move)
            {
                case Move.DOWN: key = 0x53; break;
                case Move.LEFT: key = 0x41; break;
                case Move.OK: key = 0x51; break;
                case Move.REVEAL: key = 0x32; break;
                case Move.RIGHT: key = 0x44; break;
                case Move.UP: key = 0x57; break;
                case Move.ESC: key = 0x1B; break;
                case Move.SHOP: key = 0x45; break;
                case Move.HEAL: key = 0x31; break;
            }

            var fg = GetForegroundWindow();
            PostMessage(fg, WM_KEYDOWN, key, 0);
            Thread.Sleep(100);
            PostMessage(fg, WM_KEYUP, key, 0);
        }

        private static void ReadMap()
        {
            var map = SnapMinimap();
            for (int y = 0; y < HEIGTH; ++y)
            {
                for (int x = 0; x < WIDTH; ++x)
                {
                    var c = map.GetPixel(x * RES + RES / 2, y * RES + RES / 2);
                    Map[x, y] = ColorToTile(c);
                    if (Map[x, y] == Tile.P_PLAYER)
                    {
                        Map[x, y] = Tile._EMPTY;
                        Px = x;
                        Py = y;
                    }
                    Console.Write(Map[x, y].ToString()[0]);
                }
                Console.WriteLine();
            }
        }

        private static Tile ReadTile(int x, int y)
        {
            var p = new Point();
            p.X = TopLeft.X + x * RES + RES / 2;
            p.Y = TopLeft.Y + y * RES + RES / 2;
            var c = GetColorAt(p);

            return ColorToTile(c);
        }

        private static Tile ColorToTile(Color c)
        {
            if (c == Color.FromArgb(34, 34, 34)) return Tile.W_WALL;
            else if (c == Color.FromArgb(255, 114, 11)) return Tile.W_WALL; //Furnace
            else if (c == Color.FromArgb(0, 102, 153)) return Tile._EMPTY;
            else if (c == Color.FromArgb(85, 85, 85)) return Tile._EMPTY;
            else if (c == Color.FromArgb(0, 0, 0)) return Tile.X_GOAL;
            else if (c == Color.FromArgb(255, 255, 0)) return Tile.P_PLAYER;
            else if (c == Color.FromArgb(212, 0, 0)) return Tile.O_ENEMY;
            else if (c == Color.FromArgb(255, 253, 115)) return Tile.T_TREASURE1;
            else if (c == Color.FromArgb(239, 179, 85)) return Tile.t_TREASURE2;
            else if (c == Color.FromArgb(0, 255, 66)) return Tile.t_TREASURE2; //Powerup

            return Tile.UNKNOWN;
        }

        private static void Wait()
        {
            Thread.Sleep(300);
            while (!Keyboard.IsKeyDown(Key.Scroll)) Thread.Sleep(1);
        }

        static private Color GetColorAt(Point location)
        {
            Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        static private Bitmap SnapMinimap()
        {
            Bitmap map = new Bitmap(WIDTH * RES, HEIGTH * RES, PixelFormat.Format32bppArgb);

            using (Graphics gdest = Graphics.FromImage(map))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    int retval = BitBlt(gdest.GetHdc(), 0, 0, WIDTH * RES, HEIGTH * RES, gsrc.GetHdc(), TopLeft.X, TopLeft.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return map;
        }

        enum Tile
        {
            UNKNOWN,
            _EMPTY,
            O_ENEMY,
            X_GOAL,
            W_WALL,
            T_TREASURE1,
            t_TREASURE2,
            P_PLAYER,
        }

        enum Move
        {
            NOTHING,
            UP,
            DOWN,
            LEFT,
            RIGHT,
            REVEAL,
            OK,
            ESC,
            SHOP,
            HEAL,
        }







































        const int VK_F5 = 0x74;

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        const uint WM_KEYDOWN = 0x100;
        const uint WM_KEYUP = 0x101;
        const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        const int KEYEVENTF_KEYUP = 0x0002;
        const int KEYEVENTF_UNICODE = 0x0004;
        const int KEYEVENTF_SCANCODE = 0x0008;

        public static void Send_Key(short Keycode, int KeyUporDown)
        {
            INPUT[] InputData = new INPUT[1];

            InputData[0].type = 1;
            InputData[0].ki.wScan = Keycode;
            InputData[0].ki.dwFlags = KeyUporDown;
            InputData[0].ki.time = 0;
            InputData[0].ki.dwExtraInfo = IntPtr.Zero;

            SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT)));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public short wVk;      //Virtual KeyCode (not needed here)
            public short wScan;    //Directx Keycode 
            public int dwFlags;    //This tells you what is use (Keyup, Keydown..)
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
        }

        [DllImport("user32.dll")]
        static extern UInt32 SendInput(UInt32 nInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] INPUT[] pInputs, Int32 cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();




















        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll")]
        private static extern int SetForegroundWindow(IntPtr point);

        [DllImport("User32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

















        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);



    }
}


