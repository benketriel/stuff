using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTest
{
    class Program
    {

        static void Main(string[] args)
        {
            OPGG();
            return;
            var driver = new ChromeDriver();
            driver.Url = "https://www.google.com/";
            var searchBar = driver.FindElementByXPath("//*[@id=\"tsf\"]/div[2]/div/div[1]/div/div[1]/input");
            searchBar.Click();
            searchBar.SendKeys("Selenium Test");
            //var searchButton = driver.FindElementByXPath("//*[@id=\"tsf\"]/div[2]/div/div[3]/center/input[1]");
            var searchButton = driver.FindElementByXPath("//*[@id=\"tsf\"]/div[2]/div/div[2]/div[2]/div/center/input[1]");
            searchButton.Click();

            var i = 0;
            Console.WriteLine("Ads:");
            while (true)
            {
                try
                {
                    ++i;
                    var res = driver.FindElementByXPath("//*[@id=\"vn1s0p" + i + "c0\"]/h3");
                    Console.WriteLine(res.Text);
                }
                catch (Exception) { break; }
            }
            i = 0;
            Console.WriteLine("Links:");

            while (true)
            {
                try
                {
                    ++i;
                    var res = driver.FindElementByXPath("//*[@id=\"rso\"]/div[1]/div/div[" + i + "]/div/div/div[1]/a[1]/h3");
                    //*[@id="rso"]/div[3]/div/div[1]/div/div/div[1]/a[1]/h3
                    Console.WriteLine(res.Text);
                }
                catch (Exception) { break; }
            }

            //firstResult.Click();

            driver.Close();

        }

        static void OPGG()
        {
            var visitedMatchesAndElo = new Dictionary<string, Tuple<MatchInfo, double>>();
            var matches = new List<string>();

            var initSummoner = Selenium.LoadSummoner(new List<string>() { "uroskg" }).First();
            matches = initSummoner.Matches.Select(m => m.Id).ToList();
            visitedMatchesAndElo = initSummoner.Matches.ToDictionary(m => m.Id, m => new Tuple<MatchInfo, double>(m, initSummoner.ELO));

            //var matches = new SortedList<string, MatchInfo>(new MatchInfoComparer(visitedMatchesAndElo));
            var iteration = 0;
            Parallel.ForEach(Enumerable.Range(0, 3), _ =>
            {
                var summoners = new List<string>();
                MatchInfo currMatch = null;
                while (true)
                {
                    lock (matches)
                    {
                        if (!summoners.Any())
                        {
                            if (!matches.Any()) break;
                            var currIndex = IndexOfMax(matches, id => MatchEloScore(visitedMatchesAndElo[id]));
                            var currMatchId = matches[currIndex];

                            //MatchEloScore(visitedMatchesAndElo[currMatchId]);
                            //MatchEloScore(visitedMatchesAndElo[matches[0]]);

                            matches.RemoveAt(currIndex);
                            currMatch = Db.LoadMatch(currMatchId);
                            summoners = currMatch.AllSummoners().ToList();
                        }
                    }
                    var summInfos = Selenium.LoadSummoner(summoners);
                    summoners.Clear();
                    if (currMatch != null)
                    {
                        currMatch.Trainable = true;
                        Db.SaveMatch(currMatch);
                    }
                    lock (matches)
                    {
                        foreach (var si in summInfos)
                        {
                            foreach (var match in si.Matches)
                            {
                                if (visitedMatchesAndElo.ContainsKey(match.Id)) continue;
                                visitedMatchesAndElo[match.Id] = new Tuple<MatchInfo, double>(match, si.ELO);
                                matches.Add(match.Id);
                            }
                        }
                        Console.WriteLine("Matches ready to train " + (++iteration - 1) + ", queue size: " + matches.Count);
                    }
                }
            });
            Console.WriteLine("Program actually finished!?");
        }

        private static int IndexOfMax<T>(IEnumerable<T> collection, Func<T, double> score)
        {
            if (!collection.Any()) return -1;
            double bestScoreSoFar = score(collection.First());
            int index = 0;
            int bestIndexSoFar = index;
            foreach (var item in collection.Skip(1))
            {
                ++index;
                var itemScore = score(item);
                if (itemScore > bestScoreSoFar)
                {
                    bestScoreSoFar = itemScore;
                    bestIndexSoFar = index;
                }
            }
            return bestIndexSoFar;
        }

        private static double MatchEloScore(Tuple<MatchInfo, double> matchElo)
        {
            var existingSumms = matchElo.Item1.AllSummoners().Select(name => Db.LoadSummoner(name)).Count(si => (si != null && (DateTime.Now - si.LastUpdated).TotalMinutes < Config.DEFAULT_MAX_AGE));
            var hoursAgo = (DateTime.Now - matchElo.Item1.Time).TotalHours;
            var score = existingSumms * 5 - hoursAgo + matchElo.Item2 * 25 / 1000.0;
            return score;
        }

    }
}
