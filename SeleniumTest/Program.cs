using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

        class DateComparer : IComparer<DateTime>
        {
            public int Compare(DateTime x, DateTime y)
            {
                if (x <= y) return 1;
                return -1;
            }
        }

        static void OPGG()
        {
            var visitedMatches = new HashSet<string>();
            var matches = new SortedList<DateTime, string>(new DateComparer());
            var summoners = new List<string>() { "uroskg" };
            while (true)
            {
                if (!summoners.Any())
                {
                    if (!matches.Any()) break;
                    var currMatchId = matches.First();
                    matches.RemoveAt(0);
                    var currMatch = Db.LoadMatch(currMatchId.Value);
                    summoners = currMatch.AllSummoners().ToList();
                }
                var summInfos = Selenium.LoadSummoner(summoners);
                summoners.Clear();
                foreach (var si in summInfos)
                {
                    foreach (var match in si.Matches)
                    {
                        if (visitedMatches.Contains(match.Id)) continue;
                        matches.Add(match.Time, match.Id);
                        visitedMatches.Add(match.Id);
                    }
                }
                Console.WriteLine(matches.Count);
            }

            //Match has to include each summoner and their opscore, id is required
            //expand match after first checking the id and cache/db
            //


        }

    }
}
