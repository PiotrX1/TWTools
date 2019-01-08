using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TWTools
{
    class User
    {
        private string Username, Password;
        public List<World> ListOfWorlds = new List<World>();
        

        public async Task<bool> Login(Browser browser, String login, String password)
        {
            Username = login;
            Password = password;

            // Przesyła do API /page/auth dane logowania
            HttpContent c = new StringContent("username=" + login + "&password=" + password + "&remember=1", Encoding.UTF8, "application/x-www-form-urlencoded");


            HttpResponseMessage response = await browser.Client.PostAsync("https://" + Program.GamePage.Domain + Program.GamePage.Path + "/page/auth", c);


            if (response.IsSuccessStatusCode)
            {
                // Odczytywanie odpowiedzi ze strony serwera
                var data = await response.Content.ReadAsStringAsync();
                dynamic answer = JObject.Parse(data);


                if (answer["status"] == "success")
                {
                    Program.Log("Zalogowano na konto " + login);
                    return true;
                }
                else
                {
                    Program.Log("Błąd: " + answer["error"]);
                    Console.WriteLine((String)answer["error"]);
                    return false;
                }

            }

            return false;

        }
        public async Task<bool> Logout(Browser browser)
        {
            // Odwiedza stronę wylogowania
            HttpResponseMessage response = await browser.Client.GetAsync("https://" + Program.GamePage.Domain + Program.GamePage.Path + "/page/logout");

            if (response.IsSuccessStatusCode)
            {
                Program.Log("Wylogowano");
                return true;
            }

            return false;
        }


        public async Task GetListOfWorlds(Browser browser)
        {
            // Wczytuje listę światów na których aktywne jest konto
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(await browser.Load(new Uri("https://" + Program.GamePage.Domain)));


            var table = doc.DocumentNode.SelectSingleNode("//div[@class='worlds-container']");

            //if (table.SelectSingleNode("./h4").InnerHtml == "Aktualne światy")
            //{
            foreach (var x in table.SelectNodes("./a"))
            {

                if (x.SelectSingleNode("./span[@class='world_button_active']") != null)
                    ListOfWorlds.Add(new World(x.GetAttributeValue("href", "").Split('/').Last()));
            }
            //}

            Program.Log("Pobrano listę aktualnych światów (" + ListOfWorlds.Count + ")");
        }
    }
}
