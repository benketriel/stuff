using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTest
{
    public class Db
    {
        private static Dictionary<string, MatchInfo> MatchCache = new Dictionary<string, MatchInfo>();

        public static MatchInfo LoadMatch(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            lock (MatchCache)
            {
                if (MatchCache.ContainsKey(id)) return MatchCache[id];
            }
            var path = MatchPath(id);
            try
            {
                using (Stream stream = File.Open(path, FileMode.Open))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    var result = (MatchInfo)binaryFormatter.Deserialize(stream);
                    lock (MatchCache)
                    {
                        MatchCache[id] = result;
                    }
                    return result;
                }
            }
            catch (Exception) { }
            return null;
        }

        public static void SaveMatch(MatchInfo match)
        {
            if (string.IsNullOrWhiteSpace(match.Id)) return;

            var path = MatchPath(match.Id);
            try
            {
                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    binaryFormatter.Serialize(stream, match);
                }
                lock (MatchCache)
                {
                    MatchCache[match.Id] = match;
                }
            }
            catch (Exception) { }
        }

        private static string MatchPath(string id)
        {
            return Path.Combine(Config.DB_ROOT, "m" + id);
        }


        private static Dictionary<string, SummonerInfo> SummonerCache = new Dictionary<string, SummonerInfo>();

        public static SummonerInfo LoadSummoner(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            lock (SummonerCache)
            {
                if (SummonerCache.ContainsKey(name)) return SummonerCache[name];
            }
            var path = SummonerPath(name);
            if (!File.Exists(path)) return null;
            try
            {
                using (Stream stream = File.Open(path, FileMode.Open))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    var result = (SummonerInfo)binaryFormatter.Deserialize(stream);
                    lock (SummonerCache)
                    {
                        SummonerCache[name] = result;
                    }
                    return result;
                }
            }
            catch (Exception) { }
            return null;
        }

        public static void SaveSummoner(SummonerInfo summoner)
        {
            if (string.IsNullOrWhiteSpace(summoner.Name)) return;

            var path = SummonerPath(summoner.Name);
            try
            {
                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    binaryFormatter.Serialize(stream, summoner);
                }
                lock (SummonerCache)
                {
                    SummonerCache[summoner.Name] = summoner;
                }
            }
            catch (Exception ex) { }
        }

        private static string SummonerPath(string name)
        {
            return Path.Combine(Config.DB_ROOT, "s_" + name);
        }





    }
}
