using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TWTools
{
    public class Army
    {
        public int Spear { get; set; }
        public int Sword { get; set; }
        public int Axe { get; set; }
        public int Archer { get; set; }
        public int Spy { get; set; }
        public int Light { get; set; }
        public int Marcher { get; set; }
        public int Heavy { get; set; }
        public int Ram { get; set; }
        public int Catapult { get; set; }
        public int Knight { get; set; }
        public int Snob { get; set; }
        public int Militia { get; set; }

        public Army()
        {
            Spear = 0;
            Sword = 0;
            Axe = 0;
            Archer = 0;
            Spy = 0;
            Light = 0;
            Marcher = 0;
            Heavy = 0;
            Militia = 0;
            Knight = 0;
            Snob = 0;
            Catapult = 0;
            Ram = 0;
        }

        public void Display()
        {
            Console.WriteLine("Pinierzy: " + Spear);
            Console.WriteLine("Miecznicy: " + Sword);
            Console.WriteLine("Topornicy: " + Axe);
            Console.WriteLine("Łucznicy: " + Archer);
            Console.WriteLine("Zwiadowcy: " + Spy);
            Console.WriteLine("Lekka kawaleria: " + Light);
            Console.WriteLine("Łucznicy na koniu: " + Marcher);
            Console.WriteLine("Ciężka kawaleria: " + Heavy);
            Console.WriteLine("Rycerz: " + ((Knight > 0) ? "tak" : "nie"));
            Console.WriteLine("Szlachcic: " + Snob);
            Console.WriteLine("Katapulty: " + Catapult);
            Console.WriteLine("Tarany: " + Ram);
        }
    }

    public class VillageSummary
    {
        public int Wood { get; set; }
        public int Stone { get; set; }
        public int Iron { get; set; }
        public int Storage { get; set; }
        public int Population { get; set; }
        public int PopulationMax { get; set; }
        public List<Army> Army { get; set; }
        public int Points { get; set; }


        public VillageSummary()
        {
            Wood = 0;
            Stone = 0;
            Iron = 0;
            Storage = 0;
            Population = 0;
            PopulationMax = 0;
            Points = 0;
            Army = new List<Army>();
        }


    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
        public override string ToString()
        {
            return X + "|" + Y;
        }
        public static explicit operator string(Point p)
        {
            return p.ToString();
        }
        public Point(int _X, int _Y)
        {
            X = _X;
            Y = _Y;
        }
        public Point(string s)
        {
            X = int.Parse(s.Split("|")[0]);
            Y = int.Parse(s.Split("|")[1]);
        }
    }

    public class Village
    {
        public String Name { get; set; }
        public String Id { get; set; }
        public Point Cords { get; set; }
        public World World { get; set; }




        public class VillageHasAnotherOwnerException : System.Exception
        {
            public string Id;
        }


        public async Task<VillageSummary> GetSummary(Browser browser)
        {
            HtmlDocument doc = new HtmlDocument();

            // Wczytanie strony placu/wojska
            HttpResponseMessage response = await browser.LoadFullResponse(new Uri("https://" + World.Name + "." + Program.GamePage.Domain + "/game.php?village=" + Id + "&screen=place&mode=units"));


            doc.LoadHtml(await response.Content.ReadAsStringAsync());


            String data;
            int index = doc.Text.IndexOf("TribalWars.updateGameData(");


            // Sprawdza czy udało się pobrać dane świata, czy sesja jest aktualna
            if (index < 0)
            {
                // Odnowienie sesji
                await World.RenewSession(browser);
                return await GetSummary(browser);

            }
            else
            {


                // Sprawdza czy załadowano dobrą wioskę, czy nie nastąpiło przekierowanie
                if (response.Headers.GetValues("Set-Cookie").First().Substring(18) != Id)
                {
                    // Wykryto, że wioska należy do kogoś innego
                    // Przeładowuje świat
                    throw new VillageHasAnotherOwnerException() { Id = Id };
                }





                //if (index >= 0)
                // {


                // Wyodbrębnienie JSON ze strony
                data = doc.Text.Substring(index + 26);
                data = data.Substring(0, data.IndexOf(");"));

                // Parsuje JSON
                dynamic GameData = JObject.Parse(data);


                World.Incomings = Convert.ToInt32(GameData["player"]["incomings"]);
                World.Supports = Convert.ToInt32(GameData["player"]["supports"]);

                try
                {

                    return new VillageSummary()
                    {
                        Wood = Convert.ToInt32(GameData["village"]["wood"]),
                        Stone = Convert.ToInt32(GameData["village"]["stone"]),
                        Iron = Convert.ToInt32(GameData["village"]["iron"]),
                        Storage = Convert.ToInt32(GameData["village"]["storage_max"]),
                        Population = Convert.ToInt32(GameData["village"]["pop"]),
                        PopulationMax = Convert.ToInt32(GameData["village"]["pop_max"]),
                        Points = Convert.ToInt32(GameData["village"]["points"]),
                        Army = GetArmy(doc)
                    };


                }
                catch (ArgumentOutOfRangeException e)
                {

                    Program.Log(e.Message);
                    return new VillageSummary();
                }

                //}

            }
        }

        private Army CreateArmyFromNode(HtmlNode node, String marker = "td")
        {
            Army a = new Army()
            {
                Spear = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-spear')]").InnerText),
                Sword = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-sword')]").InnerText),
                Axe = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-axe')]").InnerText),

                Spy = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-spy')]").InnerText),
                Light = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-light')]").InnerText),

                Heavy = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-heavy')]").InnerText),

                Ram = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-ram')]").InnerText),
                Catapult = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-catapult')]").InnerText),

                Snob = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-snob')]").InnerText)

            };


            // Jeśli jednostki istnieją na tym świecie
            if (node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-archer')]") != null)
                a.Archer = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-archer')]").InnerText);
            if (node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-marcher')]") != null)
                a.Marcher = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-marcher')]").InnerText);
            if (node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-militia')]") != null)
                a.Militia = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-militia')]").InnerText);
            if (node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-knight')]") != null)
                a.Knight = Convert.ToInt32(node.SelectSingleNode(".//" + marker + "[contains(@class, 'unit-item-knight')]").InnerText);

            return a;
        }



        private List<Army> GetArmy(HtmlDocument doc)
        {
            var node = doc.GetElementbyId("units_home").SelectSingleNode(".//td[contains(@class, 'unit-item')]").ParentNode;
            var node_last = node.ParentNode.Descendants("tr").Last();

            List<Army> list = new List<Army>()
            {
                CreateArmyFromNode(node),
                CreateArmyFromNode(node_last, "th")
            };


            return list;

        }

    }
}
