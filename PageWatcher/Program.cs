using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PageWatcher
{
    class Program
    {
        private static readonly string LAST_RESULT_FILE_PATH = "last_result.txt";
        private static readonly int TEST_INTERVAL_SECS = 30;

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    using (var client = new WebClient())
                    {

                        var anchor = "</strong> ( Ref : ";
                        client.Encoding = Encoding.UTF8;
                        var html = client.DownloadString("https://finfast.se/lediga-objekt");

                        var split = html.Split(new[] { anchor }, StringSplitOptions.None);

                        var names = split.Take(split.Length - 1).Select(s => s.Split('>').Last().Trim()).ToArray();
                        var refs = split.Skip(1).Select(s => s.Split(')').First().Trim()).ToArray();
                        var links = split.Take(split.Length - 1).Select(s => s.Split(new[] { "<a href=\"" }, StringSplitOptions.None).Last().Split(new[] { "\" title=\"Fakta\">" }, StringSplitOptions.None).First()).ToArray();

                        var lastRefs = new HashSet<string>();
                        if (File.Exists(LAST_RESULT_FILE_PATH)) lastRefs = new HashSet<string>(File.ReadAllLines(LAST_RESULT_FILE_PATH).Select(s => s.Trim()));

                        if (lastRefs.Count != refs.Count() || refs.Any(r => !lastRefs.Contains(r)))
                        {
                            var foundNew = false;
                            var body = "De senaste " + TEST_INTERVAL_SECS + " sekunderna har minst en ny lägenhet kommit:<br/>";
                            for (var i = 0; i < refs.Length; ++i)
                            {
                                if (lastRefs.Contains(refs[i])) continue;
                                body += "<a href=\"finfast.se/" + links[i] + "\" title=\"Fakta\"> <strong>" + names[i] + "</strong> ( Ref : " + refs[i] + ")</a><br/><br/>";
                                foundNew = true;
                            }
                            if (foundNew)
                            {
                                body += "Få hjälp med att fylla i intresseanmällan:<br/>\r\n" +
                                    "Förnamn(*)<br/>\r\n" +
                                    "Samuel<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Efternamn(*)<br/>\r\n" +
                                    "Blad<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Personnummer(*)<br/>\r\n" +
                                    "8810187438<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Gatuadress(*)<br/>\r\n" +
                                    "Visgatan 16c<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Postnummer(*)<br/>\r\n" +
                                    "70372<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Ort(*)<br/>\r\n" +
                                    "Örebro<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "E -post:(*)<br/>\r\n" +
                                    "benketriel@gmail.com<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Telefon dagtid: (*)<br/>\r\n" +
                                    "0765583316<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Arbetsgivare(*)<br/>\r\n" +
                                    "Sigma ITC<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Yrke /studier(*)<br/>\r\n" +
                                    "Data scientist och Software Engineer<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Netto inkomst(*)<br/>\r\n" +
                                    "336000<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Nuvarande hyresvärd<br/>\r\n" +
                                    "ÖBO<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Nuvarande värds telefonnummer<br/>\r\n" +
                                    "019 -19 42 00<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Antal barn<br/>\r\n" +
                                    "1<br/>\r\n" +
                                    "<br/>\r\n" +
                                    "Meddelande och medsökande<br/>\r\n" +
                                    "Medsökande: Noomi Blad 8407036667<br/>\r\n" +
                                    "Vi ansökande är ett gift par som har flyttat tillbaka till Sverige efter att ha bott utomlands (vi kom hit nov 2018).<br/>\r\n" +
                                    "Vi har en 2-årig dotter, och om allt gå bra, ett barn till i agusti.<br/>\r\n" +
                                    "Eftersom vi inte har någon boplats, bor vi hos mina svärföräldrar inneboende (address ovan) tills dess att vi hittar en plats att bo på.<br/>\r\n" +
                                    "";
                                var smtpClient = new SmtpClient
                                {
                                    Host = "smtp.gmail.com",
                                    Port = 587,
                                    UseDefaultCredentials = false,
                                    DeliveryMethod = SmtpDeliveryMethod.Network,
                                    EnableSsl = true,
                                    Credentials = new NetworkCredential("roborabak@gmail.com", "robotroll123"),
                                };
                                foreach (var dest in new[] {
                                    "benketriel@gmail.com",
                                    "n.andersson@live.se"
                                })
                                {
                                    var msg = new MailMessage("roborabak@gmail.com", dest, "Finfast ny lägenhet!", body);
                                    msg.IsBodyHtml = true;
                                    smtpClient.Send(msg);
                                }
                            }
                            File.WriteAllLines(LAST_RESULT_FILE_PATH, refs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Thread.Sleep(TEST_INTERVAL_SECS * 1000);
            }

        }
    }
}
