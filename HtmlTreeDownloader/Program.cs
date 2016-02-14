using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HtmlTreeDownloader
{
    class Program
    {
        static private int nDigits = 10;

        static void Main(string[] args)
        {
            var nums = new[] { 11, 12, 13, 14, 15, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54 };
            nums = nums.SelectMany(n =>
            {
                var l = new List<int>();
                for (int i = 0; i < 7; ++i)
                {
                    l.Add(n + i * 54);
                }
                return l;
            }).ToArray();
            var outfile = new List<string>();

            foreach (var line in File.ReadAllLines(@"C:\Users\Sam\Desktop\CA.tx"))
            {
                var o = line;
                var spl = line.Split(new[] { '\t', ' ' }).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                var parse = 0;
                if (spl.Any())
                {
                    int.TryParse(spl[0], out parse);
                }
                if (line.Trim().Length > 3 && nums.Any(n => parse == n))
                {
                    var indent = line.Substring(0, line.IndexOf(spl[0]));

                    spl[1] = Math.Max(0, (int.Parse(spl[2]) - (spl.Count() - 2))).ToString();
                    for (int i = 3; i < spl.Count(); ++i)
                    {
                        spl[i] = Math.Max(0, (int.Parse(spl[1]) + i - 2)).ToString();
                    }

                    o = indent + string.Join("\t\t", spl);
                }

                outfile.Add(o);
            }

            File.WriteAllLines(@"C:\Users\Sam\Desktop\CA2.tx", outfile);

            return;

            for (int i = 1; i <= 3; ++i)
            {
                Console.WriteLine("For " + i);
                var s = "";
                for (int j = 0; j < i; ++j) s += "0";
                Tuple<List<string>, HashSet<string>> done = new Tuple<List<string>, HashSet<string>>(new List<string>() { s }, new HashSet<string>() { s });
                int sum = Track(done);
                //Console.WriteLine("Total " + sum);
                Console.WriteLine(" -=-  -=-");
            }
            Console.Read();


            //var d = new Dictionary<string, string>();
            //var x = DateTime.Parse("9/12/2014 11:48:54 PM");

            //var l = File.ReadAllLines(@"E:\VideoNoomi\new  4.txt").ToList();


            ////l.ForEach(x =>
            ////{
            ////    var y = DateTime.Parse(x.Substring(1, x.IndexOf("]")));
            ////    d.Add((y.Month) + )
            ////});

            //File.WriteAllLines(@"E:\VideoNoomi\new  4.txt", null);





            //            var x = new Finder().Find("samuel", "henrikmill.com");
            //            //var x = new Finder().Find("alexander", "henrikmill.com");
            //            //var x = new Finder().Find("gällersta", "henrikmill.com");


        }

        private static int Track(Tuple<List<string>, HashSet<string>> done)
        {
            if (done.Item1.Count == Math.Pow(nDigits, done.Item1.First().Length))
            {
                Console.WriteLine(string.Join("", done.Item1.Select(x => x.Last())));
                //Console.Write(".");
                return 1;
            }

            int sum = 0;

            var last = done.Item1.Last();
            var next = last.Substring(1, last.Length - 1);
            for (int i = 0; i < nDigits; ++i)
            {
                var s = next + i;
                if (done.Item2.Contains(s)) continue;
                done.Item1.Add(s);
                done.Item2.Add(s);
                sum += Track(done);
                if (sum > 0) return sum;
                done.Item2.Remove(done.Item1.Last());
                done.Item1.RemoveAt(done.Item1.Count - 1);
            }

            return sum;
        }

    }

    class Finder
    {
        public List<string> Find(string str, string domain)
        {
            Domain = domain;
            ToFind = str;
            Running = true;
            DownloadMe.Add(domain);

            for (int i = 0; i < ConcurrentDownloaders; ++i)
            {
                new Thread(DownloaderThread).Start();
            }

            var res = FindFrom(domain, 0);
            Running = false;
            return res;
        }

        private void DownloaderThread()
        {
            while (Running)
            {
                string url = "";
                lock (DownloadMe)
                {
                    if (DownloadMe.Count > 0)
                    {
                        url = DownloadMe[0];
                        DownloadMe.RemoveAt(0);
                    }
                }
                if (url == "")
                {
                    Thread.Sleep(100);
                    continue;
                }

                string sha = GetSha(url);
                lock (DownloadMe)
                {
                    if (File.Exists(sha))
                    {
                        continue;
                    }
                }
                var Client = new WebClient();
                Client.Encoding = Encoding.UTF8;
                string html = "";
                try
                {
                    html = Client.DownloadString("http://" + url);
                }
                catch (Exception) { }
                lock (DownloadMe)
                {
                    File.WriteAllText(sha, html);
                }
            }
        }
        private List<string> FindFrom(string url, int depth)
        {
            if (Visited.Contains(url) || depth > MaxDepth) return new List<string>();
            Visited.Add(url);
            Report();

            SortedSet<string> links = new SortedSet<string>();
            List<string> res = new List<string>();
            try
            {
                var html = GetHtml(url);
                if (html.ToLower().Contains(ToFind))
                {
                    res.Add(url);
                }
                links = ExtractLinks(html);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            res.AddRange(links.SelectMany(l => FindFrom(l, depth + 1)));
            return res;
        }

        private string GetHtml(string url)
        {
            var sha = GetSha(url);
            while (true)
            {
                lock (DownloadMe)
                {
                    if (File.Exists(sha))
                    {
                        break;
                    }
                }
                Thread.Sleep(1000);
            }

            return File.ReadAllText(sha);
        }

        private System.String GetSha(string url)
        {
            return string.Join(",", SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("http://" + url)));
        }
        private void Report()
        {
            Console.WriteLine("" + Visited.Count + "/" + Touched.Count);
        }

        private SortedSet<string> ExtractLinks(string html)
        {
            var res = new SortedSet<string>();
            while (html.Contains(Domain))
            {
                html = html.Substring(html.IndexOf(Domain));
                if (EndOfLink.Any(end => html.Contains(end)))
                {
                    var index = EndOfLink.Select(end => html.IndexOf(end)).Where(i => i >= 0).Min();
                    var link = html.Substring(0, index);
                    if (!DontDownloadEnds.Any(x => link.ToLower().EndsWith(x)) && !Visited.Contains(link) && (link.StartsWith(Domain + "/") || link.StartsWith(Domain + "\\")))
                    {
                        res.Add(link);
                        Touched.Add(link);
                        lock (DownloadMe)
                        {
                            DownloadMe.Add(link);
                        }
                        Report();
                    }
                    html = html.Substring(index);
                }
                else
                {
                    html = html.Substring(Domain.Length);
                }
            }
            return res;
        }

        private List<string> DownloadMe = new List<string>();
        private string[] DontDownloadEnds = new string[] { ".png", ".gif", ".bmp", ".jpg", ".jpeg" };
        private string[] EndOfLink = new string[] { "\"", "<", ">", "'", "#" };
        private string Domain = "";
        private string ToFind = "";
        private SortedSet<string> Visited = new SortedSet<string>();
        private SortedSet<string> Touched = new SortedSet<string>();
        private int MaxDepth = 7;
        private bool Running = true;
        private int ConcurrentDownloaders = 10;
    }
}
