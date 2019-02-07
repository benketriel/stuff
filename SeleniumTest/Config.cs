using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTest
{
    public class Config
    {
        public const double DEFAULT_MAX_AGE = 60;
        public const int DEFAULT_TIMEOUT_MILLIS = 30 * 1000;
        public const int DEFAULT_SLEEP_MILLIS = 1000;
        public static string REGION = "eune";
        public static string HTTP_ROOT = "http://" + REGION + ".op.gg/";
        public static string HTTP_SUMMONER = HTTP_ROOT + "summoner/userName=";
        public static string DB_ROOT = "C:\\SB\\opgg\\";

        public static void LogLine(object line, int level = 1)
        {
            if (level <= 0) Console.WriteLine(line);
        }

    }
}
