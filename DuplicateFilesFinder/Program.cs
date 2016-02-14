using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace DuplicateFilesFinder
{
    class Program
    {
        private static string[] formats = new string[] { "png", "bmp", "gif", "jpg", "jpeg", "tif", "tiff", };
        private static string OUT_FILE_NAME = "Filer.html";
        private static string INTRO = "<html><head><meta charset='UTF-8'><title>Files</title><style>.fold{background-size: 20px 20px;padding-left: 100px;background-repeat: no-repeat;background-image: url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAEEElEQVR4nO2WO28cVRSAv3Pver3epx0sGyMMIggCEiiRImihpCGFGySEUuQn0LikTgMlokkRKKhcgBA9HYgIoVRJJEQCeTiJ7Yx3ZnfndQ7FzMS7jhMFe2mAI13N686533z3zNWF/+O/HjJ+sXF+eRU49YT+l9bWN29NHeCnL999aWv7xkaej97odupqghZkNoEYBImkqR6fJkQNYCe4tdacszffPvW89zVBPIgzcCCuOBcH16+HXL784LON88tfHWawtfXNbw8ESNPRiZVnmt4yR27g1BAviAOcYV4QhRdW22zeGZ2JY32ffdN3QLj9N775dCU48/HtpUcADBtqBpoKTiFXcAriSxA18ADC6dOLDXEgMolgBmY2MaCI4MYwvv/u5tzG+eVX19Y3r04AYPSzzExTxBScCv3dmEGYFNMhlMdiWoqBy/pwxakBaaJEQUrF4QTE71EO7sYz0b3kwwtnW9eAr89djPLCgGmkOeSxIN64cnWLu/djWgvzGDWwshjsyeJrM3N0llYfXuu+56+cZBb4ZBT1o98u/fgBRGdqFVyemQ2HJt5D0E84+d7LLB1fAdcG6WH0gC5YG5O5x1M8XbSu//rziQtnW6s1gCzL+qowCBXxEO4m9BYbiA7AhiA74JpAF5MeYj2QDtAqDB0islEsQFi9nahi/VAxlFyh0ayBaqFcFNEMZBdkq7BCYUUOaSWJRwIEFcBQzeiHSpyO6Cy2IdOy+iirycAEkaNbGQY71OqN4KMv7umYAbOor4zimNUTbUgNETBPWXxWgJgc2Uoc7iLO3wOYMLA7yCDP6fWaWCKYB1GgXBVxNhUr0YOtDLgxDpCrmQX9nPmu0GzNoqkguRV/oC8Grxago1oZBjtZlsS/jwOEakowyJhvK+3mLJaCOYc4w3IQJ+WyfHQr4fbNOE+TCQNDxWR3kKK50mzUIS2WX5MiqfkCYhpWovt/xMDNcYDEzIjThO6x+XIJk72VTwS0GHgaVvo72+l+gCCzXPI8ZeFYe69crWxQfFE+HSuDYATw534DMuOUbucx//CUrMTDFBEZnLsYDSdrwFSWF2fp9hoHA0zJyiBIqNXd7SqdA/za+mYG0Go42q36kwH2W1GBjLIJpGApaOqwVNBEsMRhiaAZhFsxqlypUtSALjAriM06lXZn5ukBDmEl3EktejCYAOgBi5bV7ywsNp+7dnXr7wMcFA/3DVYUYbl5iUJN7/f11ni3ZeBZYPX1Ff/OWy/615xIjmCq5im2iK76wjJdtS8SG3v2MKkjBzDDq+EB8Y5kbka2655fPv8h/oryLxCK3V4HWAIWgBkgL5tRzHQ+dqzek7F74/2r85S9iamXeQGisg0mRJUdfJm0auOz+49GZeKRrfS/Pv4Czbp3PmsanwEAAAAASUVORK5CYII=);}</style></head><body><span><h1>Bilder som hittats mer än en gång!</h1>";
        private static string END = "</span></body></html>";
        private static string NO_RES = "<h2>Inga repeterade bilder hittades!</h2>";

        private static Dictionary<string, List<string>> hits = new Dictionary<string, List<string>>();

        static void Main(string[] args)
        {
            try
            {
                var root = Directory.GetCurrentDirectory();
                var outpath = Path.Combine(root, OUT_FILE_NAME);
                File.Delete(outpath);
                using (var f = File.Create(outpath)) { }
                Console.WriteLine("Resultat fil koll är OK, börjar leta i '" + root + "'");
                Thread.Sleep(2000);

                Find(root);
                Console.WriteLine("Klar med att leta! Skriver resultat fil '" + outpath + "'.");
                Thread.Sleep(2000);

                using (var f = File.AppendText(outpath))
                {
                    f.Write(INTRO);
                }

                bool found = false;
                foreach (var kvp in hits)
                {
                    if (kvp.Value.Count == 1) continue;
                    found = true;

                    using (var f = File.AppendText(outpath))
                    {
                        f.Write("<a href='" + kvp.Value[0] + "'><img src='" + kvp.Value[0] + "' style='width:25%'/></a><br/>");
                        f.Write("<h2>Hittades " + kvp.Value.Count + " gånger</h2>");
                        f.Write("<h3>");
                        foreach (var p in kvp.Value)
                        {
                            f.Write(p + " <a href='" + Path.GetDirectoryName(p) + "'> <span class='fold'/> </a><br/>");
                        }
                        f.Write("</h3>");
                    }
                }
                if (!found)
                {
                    using (var f = File.AppendText(outpath))
                    {
                        f.Write(NO_RES);
                    }
                }

                using (var f = File.AppendText(outpath))
                {
                    f.Write(END);
                }

                Console.WriteLine("Klar, tryck 'Enter' för att sluta. Öppna '" + outpath + "' för att se bilderna!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            Console.Read();

        }

        private static void Find(string dir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                {
                    try
                    {
                        foreach (string f in Directory.GetFiles(d))
                        {
                            try
                            {
                                if (formats.Any(x => f.ToLower().EndsWith(x)))
                                {
                                    HandleFile(f);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    Find(d);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void HandleFile(string f)
        {
            Console.WriteLine(f);
            var sha256 = GetChecksum(f);
            if (!hits.ContainsKey(sha256)) hits[sha256] = new List<string>();
            hits[sha256].Add(f);
        }

        private static string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        private static string GetChecksumBuffered(Stream stream)
        {
            using (var bufferedStream = new BufferedStream(stream, 1024 * 32))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}
