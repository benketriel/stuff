using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumTest
{
    class Program
    {

        static void Main(string[] args)
        {
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
    }
}
