using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace TWTools
{
    public class World
    {

        public String Name { get; set; }

        public List<Village> ListOfVillages = new List<Village>();

        public int Incomings { get; set; }
        public int Supports { get; set; }

        public String csrf;
        public TimeSpan TimeDifference { get; set; }

        private dynamic GameData;

        public World(String name)
        {
            Name = name;
        }
        public async Task Login(Browser browser)
        {
            Program.Log("Rozpoczęto logowanie do świata " + Name);

            // Pobiera link do logowania
            dynamic answer = JObject.Parse(await browser.Load(new Uri("https://www." + Program.GamePage.Domain + "/page/play/" + Name)));

            // Logowanie
            var data = await browser.Load(new Uri(Convert.ToString(answer["uri"])));


            // Pobiera csrf
            int index = data.IndexOf("TribalWars.updateGameData(");
            if (index >= 0)
            {
                data = data.Substring(index + 26);
                data = data.Substring(0, data.IndexOf(");"));
                dynamic GameData = JObject.Parse(data);

                csrf = GameData["csrf"];
                this.GameData = GameData;
            }

        }


        public async Task<bool> IsSessionExpired(Browser browser)
        {

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(await browser.Load(new Uri("https://" + Name + "." + Program.GamePage.Domain + "/game.php?screen=overview_villages&intro")));


            if (doc.GetElementbyId("mobileHeader") == null)
            {
                return true;
            }

            return false;
        }


        public async Task GetListOfVillages(Browser browser)
        {
            HtmlDocument doc = new HtmlDocument();

            DateTime now = DateTime.Now;


            string data = await browser.Load(new Uri("https://" + Name + "." + Program.GamePage.Domain + "/game.php?screen=info_player"));
            doc.LoadHtml(data);

            // Aktualny czas na serwerze
            DateTime ServerTime = new DateTime();

            {
                int index = data.IndexOf("Timing.init");
                if (index >= 0)
                {
                    var timestamp = data.Substring(index + 12);
                    timestamp = timestamp.Substring(0, timestamp.IndexOf(");"));

                    // Czas początkowy unix
                    ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    // Wczytanie ze stringa
                    ServerTime = ServerTime.AddSeconds(double.Parse(timestamp, CultureInfo.InvariantCulture)).ToLocalTime();
                }
            }

            // Różnica czasu pomiędzy serwerem, a klientem
            TimeDifference = ServerTime - now;
            Program.Log("Różnica czasu: " + TimeDifference, ConsoleColor.Yellow);

            // Tabelka z listą wiosek
            var list = doc.GetElementbyId("villages_list");


            // Reszta wiosek (> 25)
            HtmlDocument doc_more = new HtmlDocument();
            doc_more.LoadHtml(await browser.Load(new Uri("https://" + Name + "." + Program.GamePage.Domain + "/game.php?screen=info_player&ajax=fetch_villages&player_id=" + (string)GameData["player"]["id"])));

            dynamic more = JObject.Parse(doc_more.Text);

            // Dodaje dodatkowe wioski, nie widoczne od razu w profilu na wersji mobilnej
            list.InnerHtml += more["villages"];


            // Czyszczenie listy
            ListOfVillages.Clear();

            foreach (var item in list.SelectNodes("./div"))
            {
                //if(item.GetAttributeValue("id", "") == "village_list_more")
                if (item.GetAttributeValue("id", "") != "")
                {
                    // Pomija tego diva
                    continue;
                }



                // Pobranie id wioski z wartości id obrazka
                String id = item.SelectSingleNode(".//img").GetAttributeValue("id", "");
                id = id.Replace("reservation_", "");

                // Pobranie nazwy wioski
                String name = item.SelectSingleNode(".//a").InnerText;

                // Usunięcie wszystkich znaczników, aby pozostał sam tekst
                foreach (HtmlNode node in item.SelectNodes("*"))
                {
                    node.Remove();
                }

                // Odczytanie współrzędnych wioski
                string xy = item.InnerText;

                // Usuwanie nawiasów ()
                xy = xy.Replace("(", "");
                xy = xy.Replace(")", "");

                // Kasowanie białych znaków
                xy = Regex.Replace(xy, @"\s+", "");


                // Dodawanie wioski do listy
                ListOfVillages.Add(new Village()
                {
                    Id = id,
                    Name = name,
                    Cords = new Point(
                        Convert.ToInt32(xy.Substring(0, 3)),
                        Convert.ToInt32(xy.Substring(4, 3))
                    ),
                    World = this
                });
            }

            Program.Log("Pobrano listę wiosek (" + ListOfVillages.Count + ")");


        }

        public async Task RenewSession(Browser browser)
        {
            await Login(browser);
            Program.Log("Odnowiono sesję");
        }
    }
}
