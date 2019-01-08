using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;

namespace TWTools
{
    public class Browser
    {
        public HttpClient Client { get; set; }

        public Browser()
        {
            Task.Run(() => Prepare()).Wait();
        }

        public CookieContainer Cookies { get; set; }

        private async Task Prepare()
        {
            Cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = Cookies
            };

            Client = new HttpClient(handler);

            // Przygotowuje niezbędne nagłówki do poprawnego działania 
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Add("AcceptEncoding", "gzip, deflate, br");
            Client.DefaultRequestHeaders.Add("AcceptLanguage", "pl,en-US;q=0.7;q=0.3");
            Client.DefaultRequestHeaders.Connection.Add("keep-alive");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Lenght", "50");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
            Client.DefaultRequestHeaders.Host = "www." + Program.GamePage.Domain;
            Client.DefaultRequestHeaders.Referrer = new Uri("https://" + Program.GamePage.Domain);
            Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");


            // User agent - desktop
            //Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0");


            // User agent - mobile
            Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G892A Build/NRD90M; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/60.0.3112.107 Mobile Safari/537.36");


            HtmlDocument document = new HtmlDocument();

            // Wczytanie strony głównej
            document.LoadHtml(await Load(new Uri("https://www." + Program.GamePage.Domain)));


            // Pobranie kodu csrf ze znacznika <meta> na stronie

            string csrf = document.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']").GetAttributeValue("content", "");

            // Dodanie csrf do nagłówków
            Client.DefaultRequestHeaders.Add("X-CSRF-Token", csrf);


        }
        public async Task<string> Load(Uri address)
        {
            // Nawiązywanie połączenia
            HttpResponseMessage response = await Client.GetAsync(address);

            // Jeśli udało się wczytać stronę
            if (response.IsSuccessStatusCode)
            {
                // Odczytywanie danych jako string
                return await response.Content.ReadAsStringAsync();

            }
            return null;
        }

        public async Task<HttpResponseMessage> LoadFullResponse(Uri address)
        {
            // Nawiązywanie połączenia
            HttpResponseMessage response = await Client.GetAsync(address);

            // Jeśli udało się wczytać stronę
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            return null;
        }


    }
}

public class GamePage
{
    public string Domain { get; set; }
    public string Path { get; set; }
}
