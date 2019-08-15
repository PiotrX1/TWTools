using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TWTools
{
    public interface IAction
    {
        Task<bool> Run(Browser browser);
        bool IsDone();
        string Description { get; }
        DateTime Date { get; }
        string Id { get; }
        void Display();
    }
    class SendArmy : IAction
    {
        private DateTime CommandDate { get; set; }
        private Village Village;
        private Army Army;
        private Point Cords;
        private readonly string TargetBuilding;
        bool Done = false;
        private readonly string HashId;

        public string Id
        {
            get
            {
                return HashId;
            }
        }

        public void Display()
        {
            Console.WriteLine("Wioska źródłowa: " + Village.Name + " (" + Village.Cords.ToString() + ")");
            Console.WriteLine("Cel: " + Cords.ToString() + "\n");

            Console.WriteLine("Wojska:");
            Army.Display();

            //if (Army.Catapult > 0)
            //    Console.WriteLine("Cel dla katapult: ");
        }



        // Atak lub wsparcie
        public enum CommandType
        {
            Attack,
            Support
        }
        readonly CommandType Command;

        public string Description
        {
            // Zwraca opis komendy w postaci stringu
            get
            {
                return ((Command == CommandType.Attack) ? "[Atak] " : "[Wsparcie] ") + Village.Name + " (" + Village.Cords.ToString() + ") => " + Cords.ToString() + " <" + Id + ">";
            }
        }

        public DateTime Date
        {
            get
            {
                return CommandDate;
            }
        }


        public SendArmy(DateTime _date, Village _village, Army _army, Point _cords, CommandType _command, string _TargetBuilding)
        {
            CommandDate = _date;
            Village = _village;
            Army = _army;
            Cords = _cords;
            Command = _command;
            TargetBuilding = _TargetBuilding;

            HashId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 8);
        }

        // Definiuje na którym etapie wykonania jest zadanie
        int Step = 1;

        // Zwraca aktualny czas po korekcie z serwera
        string Now()
        {
            DateTimeOffset d = DateTimeOffset.Now;// + Village.World.TimeDifference;
            return d.ToUnixTimeMilliseconds().ToString();
        }

        public async Task<bool> Run(Browser browser)
        {



            if ((DateTime.Now.AddSeconds(1) + Village.World.TimeDifference) >= CommandDate)
            {
                if (Step == 1)
                {

                    Step = 0; // Nie wykonuj nic dopóki Step1 nie ustali jak dalej postąpić

                    await Step1(browser);
                }

                if ((DateTime.Now + Village.World.TimeDifference) >= CommandDate && Step == 2)
                {
                    Done = true;
                    await Step2(browser);

                    return true;
                }
                return false;
            }


            return false;
        }

        private HttpContent Step2Content;
        private String Step2Url;

        // Krok 1
        private async Task Step1(Browser browser)
        {

            if (await Village.World.IsSessionExpired(browser))
            {
                await Village.World.RenewSession(browser);
            }

            Program.Log("Rozpoczynanie akcji...");

            string date = Now();

            // Pierwszy etap wysyłania ataku, odpowiada kliknięciu 'wyślij wojska' na mapie 

            string url = "https://" + Village.World.Name + "." + Program.GamePage.Domain + "/game.php?village=" + Village.Id + "&screen=place&ajax=command&target=&client_time=" + date;

            HttpContent c = new StringContent("village=" + Village.Id + "&screen=place&ajax=command&target=&client_time=" + date, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await browser.Client.PostAsync(url, c);


            if (response.IsSuccessStatusCode)
            {

                var data = await response.Content.ReadAsStringAsync();
                dynamic answer = JObject.Parse(data);


                var doc = new HtmlDocument();
                doc.LoadHtml((string)answer["dialog"]);

                var node = doc.DocumentNode.SelectSingleNode(".//input");

                string CodeName = node.GetAttributeValue("name", "");
                string Code = node.GetAttributeValue("value", "");



                // Wybieranie liczby wojsk



                string csrf = Village.World.csrf;

                string url2 = "https://" + Village.World.Name + "." + Program.GamePage.Domain + "/game.php?village=" + Village.Id + "&screen=place&ajax=confirm&h=" + csrf + "&client_time=" + Now();

                HttpContent c2 = new StringContent(CodeName + "=" + Code + "&template_id=&source_village=" + Village.Id + "&spear=" + Army.Spear + "&sword=" + Army.Sword + "&axe=" + Army.Axe + "&archer=" + Army.Archer + "&spy=" + Army.Spy + "&light=" + Army.Light + "&marcher=" + Army.Marcher + "&heavy=" + Army.Heavy + "&ram=" + Army.Ram + "&catapult=" + Army.Catapult + "&knight=" + Army.Knight + "&snob=" + Army.Snob + "&x=" + Cords.X + "&y=" + Cords.Y + "&input=&" + (Command == CommandType.Attack ? "attack" : "support") + "=1", Encoding.UTF8, "application/x-www-form-urlencoded");



                HttpResponseMessage response2 = await browser.Client.PostAsync(url2, c2);


                // przygotowanie do ostatniego etapu




                if (response2.IsSuccessStatusCode)
                {
                    var data2 = await response2.Content.ReadAsStringAsync();

                    dynamic answer2 = JObject.Parse(data2);

                    var doc2 = new HtmlDocument();

                    if (answer2["error"] == null)
                    {


                        doc2.LoadHtml((string)answer2["dialog"]);


                        string ch = doc2.DocumentNode.SelectSingleNode("//input[@name='ch']").GetAttributeValue("value", "");




                        Step2Url = "https://" + Village.World.Name + "." + Program.GamePage.Domain + "/game.php?village=" + Village.Id + "&screen=place&ajaxaction=popup_command&h=" + csrf + "&client_time=" + Now();




                        Step2Content = new StringContent((Command == CommandType.Attack ? "attack" : "support") + "=" + true + "&ch=" + ch + "&x=" + Cords.X + "&y=" + Cords.Y + "&source_village=" + Village.Id + "&spear=" + Army.Spear + "&sword=" + Army.Sword + "&axe=" + Army.Axe + "&archer=" + Army.Archer + "&spy=" + Army.Spy + "&light=" + Army.Light + "&marcher=" + Army.Marcher + "&heavy=" + Army.Heavy + "&ram=" + Army.Ram + "&catapult=" + Army.Catapult + "&knight=" + Army.Knight + "&snob=" + Army.Snob + "&building=" + TargetBuilding, Encoding.UTF8, "application/x-www-form-urlencoded");

                        Step = 2;

                    }
                    else
                    {
                        // Nie wysłano ataku
                        Program.Log("Nie udało się wysłać " + ((Command == CommandType.Attack) ? "ataku na " : "wsparcia do ") + Cords.ToString() + " z wioski " + Village.Name + " (" + Village.Cords.ToString() + ") - " + (string)answer2["error"] + " <" + Id + ">", ConsoleColor.Red);

                        Done = true;
                    }

                }
                else
                {

                    // Nie wysłano ataku
                    Program.Log("Nie udało się wysłać " + ((Command == CommandType.Attack) ? "ataku na " : "wsparcia do ") + Cords.ToString() + " z wioski " + Village.Name + " (" + Village.Cords.ToString() + ")" + " <" + Id + ">", ConsoleColor.Red);

                    Done = true;
                }

            }


        }

        // Krok 2
        private async Task Step2(Browser browser)
        {
            Done = true;

            HttpResponseMessage response = await browser.Client.PostAsync(Step2Url, Step2Content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();

                // Wysłano atak
                Program.Log("Wysłano " + ((Command == CommandType.Attack) ? "atak na " : "wsparcie do ") + Cords.ToString() + " z wioski " + Village.Name + " (" + Village.Cords.ToString() + ")" + " <" + Id + ">", ConsoleColor.Green);

            }
            else
            {
                // Nie wysłano ataku
                Program.Log("Nie udało się wysłać " + ((Command == CommandType.Attack) ? "ataku na " : "wsparcia do ") + Cords.ToString() + " z wioski " + Village.Name + " (" + Village.Cords.ToString() + ")" + " <" + Id + ">", ConsoleColor.Red);
            }
        }

        public bool IsDone()
        {
            return Done;
        }

    }
}
