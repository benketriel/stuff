using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumTest
{
    public class Selenium
    {
        public static List<SummonerInfo> LoadSummoner(List<string> names, double maxAgeMinutes = Config.DEFAULT_MAX_AGE)
        {
            var result = new List<SummonerInfo>();
            ChromeDriver driver = null;
            var driverIsNew = true;
            try
            {
                foreach (var name in names)
                {
                    RetryButIgnoreAfterTimeout(() =>
                    {

                        var si = Db.LoadSummoner(name);
                        if (si != null && (DateTime.Now - si.LastUpdated).TotalMinutes < maxAgeMinutes)
                        {
                            result.Add(si);
                            return;
                        }
                        if (driver == null) driver = new ChromeDriver();

                        si = new SummonerInfo();
                        driver.Url = Config.HTTP_SUMMONER + name;

                        if (driverIsNew)
                        {
                            CloseGDPRWindow(driver);
                            driverIsNew = false;
                        }

                        si.LastUpdated = UpdateSummoner(driver, maxAgeMinutes);

                        LoadOnlyRankedGames(driver);

                        si.Name = GetSummonerName(driver, name);
                        si.ELO = GetElo(driver);
                        si.Matches = GetAllMatches(driver, si).ToArray();
                        result.Add(si);
                        Db.SaveSummoner(si);
                    });
                }
            }
            finally
            {
                if (driver != null) driver.Close();
            }
            return result;
        }


        private static bool IsStale(IWebElement element)
        {
            try
            {
                element.FindElements(By.Id("x"));
                return false;
            }
            catch (StaleElementReferenceException ex)
            {
                return true;
            }
        }

        private static void BusyWaitFor(Func<bool> condition, int sleepMillis = 100, int timeoutMillis = Config.DEFAULT_TIMEOUT_MILLIS)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < timeoutMillis && !condition()) Thread.Sleep(sleepMillis);
            if (stopwatch.ElapsedMilliseconds >= timeoutMillis) throw new TimeoutException();
        }

        private static bool CatchException(Action action)
        {
            try { action(); return false; }
            catch (Exception ex)
            {
                //Config.LogLine(ex);
                return true;
            }
        }

        private static void WaitAndRetry(Action action, Func<bool> condition, int timeoutMillis = Config.DEFAULT_TIMEOUT_MILLIS, int sleepMillis = 100)
        {
            if (CatchException(() => action()))
            {
                BusyWaitFor(condition, timeoutMillis, sleepMillis);
                action();
            }
        }

        private static void RetryButIgnoreAfterTimeout(Action action)
        {
            CatchException(() => { BusyWaitFor(() => !CatchException(action)); });
        }

        private static int ParseTitleToElo(string title)
        {
            title = " " + title.ToLower() + " ";
            var score = 0;
            var wDivision = true;
            title = title.Replace(" i ", " 1 ");
            title = title.Replace(" ii ", " 2 ");
            title = title.Replace(" iii ", " 3 ");
            title = title.Replace(" iv ", " 4 ");
            title = title.Replace(" v ", " 5 ");

            if (title.Contains("iron")) score += 0;
            else if (title.Contains("bronze")) score += 500;
            else if (title.Contains("silver")) score += 1000;
            else if (title.Contains("gold")) score += 1500;
            else if (title.Contains("platinum")) score += 2000;
            else if (title.Contains("diamond")) score += 2500;
            else if (title.Contains("grandmaster")) { score += 3000; wDivision = false; }
            else if (title.Contains("master")) { score += 3000; wDivision = false; }
            else if (title.Contains("challenger")) { score += 3000; wDivision = false; }
            else { score += 1500; wDivision = false; }

            var numbers = new string(title.Where(c => char.IsDigit(c) || c == ' ').ToArray()).Trim();
            var split = numbers.Split(' ');
            if (wDivision && split.Length > 0) if (int.TryParse(split[0], out int division)) score += 100 * (5 - division);
            if (split.Length > (wDivision ? 1 : 0)) if (int.TryParse(split[wDivision ? 1 : 0], out int lp)) score += lp;

            return score;
        }



        private static void CloseGDPRWindow(ChromeDriver driver)
        {
            RetryButIgnoreAfterTimeout(() =>
            {
                var gdprAccept = driver.FindElementsByTagName("button").Where(x => x.Text == "Continue Using Site" &&
                    x.GetAttribute("class").Contains("banner_save")).First();
                gdprAccept.Click();
                BusyWaitFor(() => CatchException(() => gdprAccept.Click()), 1000);
            });
        }

        private static DateTime UpdateSummoner(ChromeDriver driver, double maxAgeMinutes)
        {
            var lastUpdated = DateTime.Now;
            BusyWaitFor(() => !CatchException(() =>
            {
                var updatedTooltip = driver.FindElementByClassName("LastUpdate").FindElement(By.TagName("span"));
                WaitAndRetry(() => { lastUpdated = DateTime.Parse(updatedTooltip.GetAttribute("title")); }, () => updatedTooltip.GetAttribute("title") != null);

                if ((DateTime.Now - lastUpdated).TotalMinutes > maxAgeMinutes)
                {
                    Config.LogLine("Required update");
                    var updateButton = driver.FindElementById("SummonerRefreshButton");
                    updateButton.Click();
                    if (CatchException(() => driver.SwitchTo().Alert().Accept()))
                    {
                        BusyWaitFor(() => IsStale(updatedTooltip));
                    }
                    updatedTooltip = driver.FindElementByClassName("LastUpdate").FindElement(By.TagName("span"));
                }
                WaitAndRetry(() => { lastUpdated = DateTime.Parse(updatedTooltip.GetAttribute("title")); }, () => updatedTooltip.GetAttribute("title") != null);
                Config.LogLine("Summoner up to date");
            }));
            return lastUpdated;
        }

        private static void LoadOnlyRankedGames(ChromeDriver driver)
        {
            //Required - exceptions not ignored
            BusyWaitFor(() => !CatchException(() => { driver.FindElementByClassName("Navigation").FindElements(By.TagName("a")).Where(x => x.Text == "Ranked Solo").First().Click(); }));
            Config.LogLine("Ranked games loaded");
        }

        private static string GetSummonerName(ChromeDriver driver, string name)
        {
            BusyWaitFor(() => !CatchException(() =>
            {
                name = driver.FindElementByClassName("Name").Text;
                Config.LogLine("Detected summoner name: " + name);
            }));
            return name;
        }

        private static int GetElo(ChromeDriver driver)
        {
            int elo = 1500;
            BusyWaitFor(() => !CatchException(() =>
            {
                var tier = driver.FindElementByClassName("TierRank");
                elo = ParseTitleToElo(tier.Text);
                Config.LogLine("Detected elo: " + elo);
            }));
            return elo;
        }

        private static List<MatchInfo> GetAllMatches(ChromeDriver driver, SummonerInfo result)
        {
            var matches = new List<MatchInfo>();
            var gameItems = new IWebElement[0];
            BusyWaitFor(() =>
            {
                gameItems = driver.FindElementsByClassName("GameItem").ToArray();
                return gameItems.Length > 0;
            });
            foreach (var gameItem in gameItems)
            {
                Config.LogLine(" * Loading match nr " + (matches.Count + 1));
                var match = new MatchInfo();
                var invalid = false;
                CatchException(() =>
                {
                    BusyWaitFor(() => !CatchException(() =>
                    {
                        match.Id = gameItem.GetAttribute("data-game-id");
                        Config.LogLine("Detected match id " + match.Id);
                    }));
                    RetryButIgnoreAfterTimeout(() =>
                    {
                        result.Id = gameItem.GetAttribute("data-summoner-id");
                        Config.LogLine("Detected summoner id " + result.Id);
                    });
                    RetryButIgnoreAfterTimeout(() =>
                    {
                        var vd = gameItem.FindElement(By.ClassName("GameResult")).Text;
                        invalid = vd != "Victory" && vd != "Defeat";
                    });

                });
                if (invalid) continue;
                var loaded = Db.LoadMatch(match.Id);
                if (loaded != null)
                {
                    matches.Add(loaded);
                    continue;
                }
                BusyWaitFor(() => !CatchException(() =>
                {
                    var matchExpander = gameItem.FindElement(By.ClassName("StatsButton")).FindElement(By.ClassName("Off"));
                    matchExpander.Click();
                    BusyWaitFor(() => !CatchException(() => gameItem.FindElements(By.ClassName("GameDetailTableWrap")).First()));
                }));
                CatchException(() =>
                {
                    RetryButIgnoreAfterTimeout(() =>
                    {
                        match.Time = DateTime.Parse(gameItem.FindElement(By.ClassName("_timeago")).GetAttribute("title"));
                        Config.LogLine("Detected match time " + match.Time);
                    });

                    var gameTable = gameItem.FindElements(By.ClassName("GameDetailTableWrap")).First();
                    var names = gameTable.FindElements(By.ClassName("SummonerName")).SelectMany(x => x.FindElements(By.TagName("a"))).Select(x => x.Text).ToList();
                    var opscores = gameTable.FindElements(By.ClassName("OPScore")).Where(x => x.GetAttribute("class") == "OPScore Text").Select(x => x.Text).ToList();
                    if (names.Count != 10 || opscores.Count != 10) return;
                    for (var teamI = 1; teamI <= 2; ++teamI)
                    {
                        var matchTeam = teamI == 1 ? match.TeamA : match.TeamB;
                        Config.LogLine(" Loading team " + teamI);
                        for (var summI = 1; summI <= 5; ++summI)
                        {
                            BusyWaitFor(() => !CatchException(() =>
                            {
                                var summName = names[(teamI - 1) * 5 + summI - 1];
                                var opScoreStr = opscores[(teamI - 1) * 5 + summI - 1];
                                var opScore = double.Parse(opScoreStr, NumberStyles.Any, CultureInfo.InvariantCulture);
                                matchTeam.Add(new Tuple<string, double>(summName, opScore));
                                Config.LogLine("Detected summoner " + summName + " with opscore " + opScore);
                            }));
                        }
                    }
                    Db.SaveMatch(match);
                    matches.Add(match);
                });
            }
            Config.LogLine(" ** Loaded " + matches.Count + " matches");
            return matches;
        }

    }
}
