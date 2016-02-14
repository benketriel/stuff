using FormConsole;
using MinMax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _2048
{
    class Solver : FConsole
    {
        static void Main()
        {
            new Solver().Run();
        }

        private const int MAX_DEPTH = 6;
        override protected void MainThread()
        {
            Node2048 n = new Node2048().GetRandomChild(false);
            PrintLine(n.ToString());
            while (!n.IsTerminal(true))
            {
                IEnumerable<Node> moves = n.Children(true);
                Node2048 best = (Node2048)moves.First();
                int record = 0;
                foreach (var move in moves)
                {
                    int empty = (n.EmptyTiles() <= 1) ? 2 : n.EmptyTiles();
                    int depth = Math.Max(1, Math.Min(MAX_DEPTH, (int)(4 * Math.Log(5, empty))));
                    depth = 5;
                    int score = new AlphaBeta(true).Iterate(move, depth, int.MinValue, int.MaxValue, true);
                    if (score > record)
                    {
                        best = (Node2048)move;
                        record = score;
                    }
                }
                n = best;
                PrintLine(n.ToString());
                //ReadLine();
                n = n.GetRandomChild(false);
                PrintLine(n.ToString());
                //ReadLine();
            }

            PrintLine("Done");

            while (true) PrintLine(ReadLine());
        }
    }

    class Node2048 : Node
    {
        private const int BOARD_SIZE = 4;
        private int[,] board = new int[BOARD_SIZE, BOARD_SIZE];

        public int GetTotalScore(bool Player)
        {
            int record = 0;
            record += ChainScore(Player) * 100;
            record += MonotonyScore(Player) * 100;
            record += ContinuityScore(Player) * 1;
            record += SpaceScore(Player) * 100;
            record += PointsScore(Player) * 10000;
            return record;
        }

        private int PointsScore(bool Player)
        {
            int score = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    if (board[i, j] == 0) continue;
                    score += (int)Math.Pow(2, board[i, j]);
                }
            }
            return score;
        }

        private int SpaceScore(bool Player)
        {
            return EmptyTiles();
        }

        public int EmptyTiles()
        {
            int score = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    if (board[i, j] == 0) ++score;
                }
            }
            return score;
        }

        private int ContinuityScore(bool Player)
        {
            int score = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    foreach (var step in StepsFrom(new Tuple<int, int>(i, j)))
                    {
                        if (board[i, j] == board[step.Item1, step.Item2] + 1) ++score;
                    }
                }
            }
            return score;
        }

        private int MonotonyScore(bool Player)
        {
            int score = 0;
            for (int direction = 0; direction < 2; ++direction)
            {
                for (int i = 0; i < BOARD_SIZE; ++i)
                {
                    List<int> chain = new List<int>();
                    for (int j = 0; j < BOARD_SIZE; ++j)
                    {
                        Tuple<int, int> pos = BoardSelector(i, j, direction);
                        int val = board[pos.Item1, pos.Item2];
                        if (val != 0) chain.Add(val);
                    }
                    int min = chain.FirstOrDefault();
                    foreach (var x in chain)
                    {
                        if (min < x)
                        {
                            score -= 1;
                        }
                        min = Math.Min(min, x);
                    }
                }
            }
            return score;
        }

        private int ChainScore(bool Player)
        {
            List<Tuple<int, int>> list = new List<Tuple<int, int>>();
            int record = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    if (board[i, j] > record)
                    {
                        list.Clear();
                        record = board[i, j];
                    }
                    if (board[i, j] == record)
                    {
                        list.Add(new Tuple<int, int>(i, j));
                    }
                }
            }
            record = 0;
            foreach (var l in list)
            {
                record = Math.Max(record, FindLongestPath(l));
            }
            return record;
        }

        private int FindLongestPath(Tuple<int, int> pos)
        {
            int record = 0;
            foreach (var move in StepsFrom(pos))
            {
                if (board[move.Item1, move.Item2] == board[pos.Item1, pos.Item2] - 1)
                {
                    record = Math.Max(record, FindLongestPath(move) + 1);
                }
            }
            return record;
        }

        private IEnumerable<Tuple<int, int>> StepsFrom(Tuple<int, int> pos)
        {
            if (pos.Item1 > 0) yield return new Tuple<int, int>(pos.Item1 - 1, pos.Item2);
            if (pos.Item2 > 0) yield return new Tuple<int, int>(pos.Item1, pos.Item2 - 1);
            if (pos.Item1 < BOARD_SIZE - 1) yield return new Tuple<int, int>(pos.Item1 + 1, pos.Item2);
            if (pos.Item2 < BOARD_SIZE - 1) yield return new Tuple<int, int>(pos.Item1, pos.Item2 + 1);
        }

        public List<Node> Children(bool Player)
        {
            return EnumChildren(Player).Select(x => (Node)x).ToList();
        }

        private IEnumerable<Node2048> EnumChildren(bool Player)
        {
            if (Player)
            {
                for (int direction = 0; direction < 4; ++direction)
                {
                    bool success = false;
                    Node2048 clone = Clone();
                    for (int i = 0; i < BOARD_SIZE; ++i)
                    {
                        List<int> chain = new List<int>();
                        for (int j = 0; j < BOARD_SIZE; ++j)
                        {
                            Tuple<int, int> pos = BoardSelector(i, j, direction);
                            int val = clone.board[pos.Item1, pos.Item2];
                            if (val != 0)
                            {
                                chain.Add(val);
                                if (j + 1 > chain.Count) success = true;
                            }
                            clone.board[pos.Item1, pos.Item2] = 0;
                        }
                        List<int> mergedChain = new List<int>();
                        foreach (var x in chain)
                        {
                            if (!mergedChain.Any())
                            {
                                mergedChain.Add(x);
                            }
                            else if (mergedChain.Last() == x)
                            {
                                mergedChain.RemoveAt(mergedChain.Count - 1);
                                mergedChain.Add(x + 1);
                                success = true;
                            }
                            else
                            {
                                mergedChain.Add(x);
                            }
                        }
                        for (int j = 0; mergedChain.Any(); ++j)
                        {
                            Tuple<int, int> pos = BoardSelector(i, j, direction);
                            clone.board[pos.Item1, pos.Item2] = mergedChain.First();
                            mergedChain.RemoveAt(0);
                        }
                    }
                    if (success) yield return clone;
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        if (board[i, j] == 0)
                        {
                            Node2048 clone = Clone();
                            clone.board[i, j] = 1;
                            yield return clone;
                            clone = Clone();
                            clone.board[i, j] = 2;
                            yield return clone;
                        }
                    }
                }
            }
        }

        private Tuple<int, int> BoardSelector(int i, int j, int direction)
        {
            switch (direction)
            {
                case 0:
                    return new Tuple<int, int>(i, j);
                case 1:
                    return new Tuple<int, int>(j, i);
                case 2:
                    return new Tuple<int, int>(i, BOARD_SIZE - 1 - j);
                case 3:
                    return new Tuple<int, int>(BOARD_SIZE - 1 - j, i);

            }
            throw new Exception("Invalid call to Board Selector");
        }

        public bool IsTerminal(bool Player)
        {
            return !EnumChildren(Player).Any();
        }

        private Node2048 Clone()
        {
            Node2048 n = new Node2048();
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    n.board[i, j] = board[i, j];
                }
            }
            return n;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    double val = (board[i, j] == 0) ? 0 : Math.Pow(2, board[i, j]);
                    s += val + " ";
                }
                s += "," + Environment.NewLine;
            }
            return s;
        }

        public Node2048 GetRandomChild(bool Player)
        {
            List<Node2048> ch = EnumChildren(Player).ToList();
            for (int i = 0; i < ch.Count; ++i)
            {
                int c = ch.Count - i;
                int tot = (c / 2) * 9 + (c - c / 2);
                if (i % 2 == 0)
                {
                    if (new Random().Next() % tot < 9) return ch.ElementAt(i);
                }
                else
                {
                    if (new Random().Next() % tot == 0) return ch.ElementAt(i);
                }
            }
            throw new Exception("Algorithm Bug");
        }

    }

}

