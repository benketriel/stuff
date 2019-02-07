using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTest
{
    [Serializable]
    public class MatchInfo
    {
        public DateTime Time = DateTime.Now;
        public string Id = "";
        public List<Tuple<string, double>> TeamA = new List<Tuple<string, double>>();
        public List<Tuple<string, double>> TeamB = new List<Tuple<string, double>>();
        public bool Trainable = false;

        public double GetOpScore(string summoner)
        {
            return TeamA.Concat(TeamB).FirstOrDefault(x => x.Item1 == summoner)?.Item2 ?? 0.5;
        }

        public double GetTeamAverage(string summoner)
        {
            return GetTeam(summoner)?.Select(x => x.Item2).Average() ?? 0.5;
        }

        public double GetOpponentAverage(string summoner)
        {
            return GetOpponentTeam(summoner)?.Select(x => x.Item2).Average() ?? 0.5;
        }

        private List<Tuple<string, double>> GetTeam(string summoner)
        {
            if (TeamA.Any(x => x.Item1 == summoner)) return TeamA;
            if (TeamB.Any(x => x.Item1 == summoner)) return TeamB;
            return null;
        }

        private List<Tuple<string, double>> GetOpponentTeam(string summoner)
        {
            if (TeamA.Any(x => x.Item1 == summoner)) return TeamB;
            if (TeamB.Any(x => x.Item1 == summoner)) return TeamA;
            return null;
        }

        public IEnumerable<string> AllSummoners()
        {
            return TeamA.Concat(TeamB).Select(x => x.Item1);
        }

    }

    [Serializable]
    public class SummonerInfo
    {
        public string Name = "";
        public string Id = "";
        public DateTime LastUpdated = DateTime.Now;
        public double ELO = 1500;
        public MatchInfo[] Matches = new MatchInfo[0];

        public IEnumerable<string> AllSummonersDistinct()
        {
            var seen = new HashSet<string>();
            foreach (var match in Matches)
            {
                foreach (var summ in match.AllSummoners())
                {
                    if (seen.Contains(summ)) continue;
                    seen.Add(summ);
                    yield return summ;
                }
            }
        }

    }

}
