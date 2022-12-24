using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using HtmlAgilityPack;

namespace scaner_sudov {
    class Program {
        Dictionary<byte, string> sp = new Dictionary<byte, string>()
        {
            {151, "Экономический суд Брестской области" },
            {152, "Экономический суд Витебской области" },
            {153, "Экономический суд Гомельской области" },
            {154, "Экономический суд Гродненской области" },
            {156, "Экономический суд Минской области" },
            {157, "Экономический суд Могилевской области" },
            {155, "Экономический суд г. Минска" }
        };

        //WEB загрузка таблиц
        static void oblast(byte ch, IWebDriver drv) {
            Program p = new Program();
            drv.FindElement(By.ClassName("selection")).Click();

            IWebElement el = drv.FindElement(By.XPath("//span/span/span[1]/input"));
            el.Clear();
            el.SendKeys(p.sp[ch]);
            el.SendKeys(Keys.Enter);

        }
        static void captcha(IWebDriver drv) {

            while (true) {
                try {
                    IWebElement elm = drv.FindElement(By.XPath("//*[@id=\"scheduleList\"]/h3"));
                    break;
                } catch {
                    Console.Write("\rКАПЧА");
                    System.Threading.Thread.Sleep(1000);
                }
            }
            Console.Write("\r");
        }
        static void tabl(IWebDriver drv, byte key) {
            Program p = new Program();
            System.Threading.Thread.Sleep(2000);
            captcha(drv);
            ReadOnlyCollection<IWebElement> els = drv.FindElements(By.ClassName("table-striped"));
            string[] sp_t = t_tabl(drv);


            for (byte ch = 0; ch < sp_t.Length; ch++) {
                if (sp_t[ch]!=null) 
                    File.WriteAllText($"{p.sp[key]}{sp_t[ch]}.html", "<table>" + els[ch].GetAttribute("innerHTML") + "<table>");
            }
        }
        static string[] t_tabl(IWebDriver drv) {
            String txt1 = "Список дел об административных правонарушениях, назначенных к слушанию в суде первой инстанции";
            String txt2 = "Список экономических дел (заявлений, жалоб, ходатайств, представлений), назначенных к слушанию по первой инстанции";

            ReadOnlyCollection<IWebElement> els = drv.FindElements(By.CssSelector("a.col-md-12"));
            string[] sp = new string[els.Count];

            for (byte i = 0; i < els.Count; i++) {

                if (els[i].Text == txt1)
                    sp[i] = "ADM";
                else if (els[i].Text == txt2)
                    sp[i] = "SUD";
            }
            return sp;

        }

        //переделать для базы данных
        static string str_to_date(string s) {
            string[] sp = s.Split(new char[] { '.' });
            string ss = sp[0];
            ss += sp[1];
            ss += "1";
            ss += sp[2].Substring(2);
            return ss;
        }
        static void SUD(StreamWriter file, HtmlNode html, string name) {
            foreach (HtmlNode raw in html.SelectNodes(".//tr")) {
                HtmlNodeCollection ss = raw.SelectNodes(".//td");
                string now = str_to_date(ss[1].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r", ""));
                string nomer = ss[4].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r", "");
                string o_chom = ss[5].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r", "");
                string istets = ss[6].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r\r", " ").Replace("\r", "");
                string otvetchik = ss[7].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r\r", " ").Replace("\r", "");

                string itog = $"++ ДД|++ НН|++ И$|05 {now}|01 {nomer}|04 {o_chom}|02 {istets}|03 {otvetchik}|06 {name}|++ КК|++ ЯЯ|\n";
                file.Write(itog);
            }
        }
        static void ADM(StreamWriter file, HtmlNode html, string name) {
            foreach (HtmlNode raw in html.SelectNodes(".//tr")) {
                HtmlNodeCollection ss = raw.SelectNodes(".//td");
                string now = str_to_date(ss[1].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r", ""));
                string nomer = ss[4].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r", "");
                string FIO = ss[5].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r\r", " ").Replace("\r", "");
                string stia = ss[6].InnerText.Replace("\n", "").Replace("  ", "").Replace("\r\r", " ").Replace("\r", "");

                string itog = $"++ ДД|++ НН|++ И$|05 {now}|01 {nomer}|03 {FIO}|04 {stia}|06 {name}|++ КК|++ ЯЯ|\n";
                file.Write(itog);
            }
        }

        //основной метод
        static void Main(string[] args) {
            Program p = new Program();

            var driver = new ChromeDriver("d:\\programs\\chromedriver.exe");
            driver.Manage().Window.Position = new System.Drawing.Point(256, 0);
            driver.Navigate().GoToUrl("https://service.court.gov.by/ru/public/schedule");

            foreach (var i in p.sp) {
                captcha(driver);
                oblast(i.Key, driver);
                String today = DateTime.Now.Day.ToString();
                System.Threading.Thread.Sleep(1000);

                ReadOnlyCollection<IWebElement> els = driver.FindElements(By.ClassName("day-schedule-active"));
                byte butns = (byte)els.Count;
                //Console.WriteLine(butns);
                for (byte z = 0; z < butns; z++) {
                    captcha(driver);

                    if (today == els[z].Text) {
                        els[z].Click();
                        tabl(driver, i.Key);
                        break;
                    } 
                }
            }

            driver.Quit();

            //System.Environment.Exit(1);

            string path = Directory.GetCurrentDirectory();
            string[] sf = Directory.GetFiles(path);
            StreamWriter SUD_F = new StreamWriter(path + "\\1_SUD.txt", false);
            StreamWriter ADM_F = new StreamWriter(path + "\\1_ADM.txt", false);

            foreach (var i in sf) {
                if (!i.Contains("Экономический суд ")) continue;
                Console.WriteLine(i);

                string html = File.ReadAllText(i);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var table = doc.DocumentNode.SelectSingleNode("//tbody");
                
                string[] nm = i.Split(new char[] { '\\' });
                string name = nm[nm.Length - 1];

                if (i.Contains("SUD")) 
                    SUD(SUD_F, table, name.Substring(0, name.Length - 8));
                else 
                    ADM(ADM_F, table, name.Substring(0, name.Length - 8));

                File.Delete(i);
            }

            SUD_F.Close();
            ADM_F.Close();
        }
    }
}
