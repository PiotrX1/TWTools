using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TWTools
{
    class BOT
    {
        readonly List<GamePage> Pages = new List<GamePage>
            {
                new GamePage() { Domain = "plemiona.pl", Path = "" },
                new GamePage() { Domain = "tribalwars.net", Path = "/en-dk" },
                new GamePage() { Domain = "die-staemme.de", Path = "" },
                new GamePage() { Domain = "vojnaplemen.si", Path = "" },
                new GamePage() { Domain = "voyna-plemyon.ru", Path = "" }
            };

        private User U;
        private Browser B;
        private World _SelectedWorld;
        private Village _SelectedVillage;

        public Village SelectedVillage
        {
            get
            {
                return _SelectedVillage;
            }
        }

        public string SelectedWorld
        {
            get
            {
                return _SelectedWorld.Name;
            }
        }

        public Browser Browser
        {
            get
            {
                return B;
            }
        }

        public bool TryLogin(string login, string password)
        {

            B = new Browser();
            U = new User();


            bool logged = false;

            Task.Run(async () =>
            {
                if (await U.Login(B, login, password)) // Próba zalogowania
                {
                    // Jeśli się udało to pobierz listę światów
                    await U.GetListOfWorlds(B);
                    logged = true;
                }
            }).Wait();

            // if (logged)
            //windowL.UpdateListSource(U.ReturnListOfWorlds());

            return logged;
        }
        public void PrintWorldList()
        {
            for (int i = 0; i < U.ListOfWorlds.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[" + (i + 1) + "]" + "\t");
                Console.ResetColor();
                Console.Write(U.ListOfWorlds[i].Name + "\n");

            }
            if (U.ListOfWorlds.Count == 0)
                Console.WriteLine("Brak aktualnych światów");
        }
        public void PrintVillageList()
        {
            for (int i = 0; i < _SelectedWorld.ListOfVillages.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[" + (i + 1) + "]" + "\t");
                Console.ResetColor();
                Console.Write("(" + _SelectedWorld.ListOfVillages[i].Cords + ") " + _SelectedWorld.ListOfVillages[i].Name + "\n");
            }
            if (_SelectedWorld.ListOfVillages.Count == 0)
                Console.WriteLine("Brak wiosek");
        }
        public void LoadWorld()
        {

            Task.Run(async () =>
            {
                // Logowanie do świata
                await _SelectedWorld.Login(B);
                await _SelectedWorld.GetListOfVillages(B);
            }).Wait();
            Program.Log("Świat " + _SelectedWorld.Name + " został pomyślnie wczytany");
        }

        //Wybiera świat
        public bool SelectWorld(int n)
        {
            try
            {
                _SelectedWorld = U.ListOfWorlds[n];
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        public bool SelectWorld(string name)
        {
            foreach(var w in U.ListOfWorlds)
            {
                if(w.Name == name)
                {
                    _SelectedWorld = w;
                    return true;
                }
            }
            return false;
        }


        // Wybiera wioskę
        public bool SelectVillage(int n)
        {
            try
            {
                _SelectedVillage = _SelectedWorld?.ListOfVillages[n];
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }


        // Wczytuje cele z pliku (plemsy.pl)
        public void LoadTargetsFromFile(string _filename)
        {
            string line;
            try
            {

                StreamReader file = new StreamReader(_filename);
                int count = 0;
                while((line = file.ReadLine()) != null)
                {
                    Regex regex_cords = new Regex("([0-9]{3}\\|[0-9]{3})");
                    if(regex_cords.IsMatch(line))
                    {
                        Match m = regex_cords.Match(line);
                        string village = m.ToString();
                        string target = m.NextMatch().ToString();

                        foreach(var v in _SelectedWorld.ListOfVillages)
                        {
                            if((string)v.Cords == village)
                            {

                                Regex regex_date = new Regex("([0-9]{2}\\.[0-9]{2}\\.[0-9]{4})");
                                Regex regex_time = new Regex("([0-9]{2}:[0-9]{2}:[0-9]{2})");

                                

                                if(regex_date.IsMatch(line) && regex_time.IsMatch(line))
                                {
                                    Match m2 = regex_date.Match(line);
                                    Match m3 = regex_time.Match(line);
                                    string[] date = m2.ToString().Split(".");
                                    string[] time = m3.ToString().Split(":");

                                    DateTime AttackTime = new DateTime(int.Parse(date[2]), int.Parse(date[1]), int.Parse(date[0]), int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]));

                                    // Ignoruje akcje, które mają przeszłą datę wysłania
                                    if (AttackTime < DateTime.Now)
                                        continue;

                                    Task.Run(async () =>
                                    {
                                        Army a = (await v.GetSummary(B)).Army[0];



                                        // Wybór najwolniejszej jednostki

                                        if (line.IndexOf("Taran") != -1 || line.IndexOf("Katapulta") != -1)
                                        {
                                            // Bez szlachcica
                                            a.Snob = 0;
                                        }
                                        else if(line.IndexOf("Zwiadowca") != -1)
                                        {
                                            Army temp = a;
                                            a = new Army { Spy = temp.Spy };   
                                        }
                                        else if (line.IndexOf("Lekki kawalerzysta") != -1 || line.IndexOf("Łucznik na koniu") != -1 || line.IndexOf("Rycerz") != -1)
                                        {
                                            Army temp = a;
                                            a = new Army { Spy = temp.Spy, Light=temp.Light, Marcher=temp.Marcher, Knight=temp.Knight };
                                        }
                                        else if (line.IndexOf("Ciężki kawalerzysta") != -1)
                                        {
                                            Army temp = a;
                                            a = new Army { Spy = temp.Spy, Light = temp.Light, Marcher = temp.Marcher, Knight = temp.Knight, Heavy=temp.Heavy };
                                        }
                                        else if (line.IndexOf("Pikinier") != -1 || line.IndexOf("Topornik") != -1 || line.IndexOf("Łucznik") != -1)
                                        {
                                            a.Snob = 0;
                                            a.Sword = 0;
                                            a.Ram = 0;
                                            a.Catapult = 0;
                                        }
                                        else if (line.IndexOf("Miecznik") != -1)
                                        {
                                            a.Snob = 0;
                                            a.Ram = 0;
                                            a.Catapult = 0;
                                        }

                                        /* -------------------------------------------------- */

                                        Program.RegisterAction(new SendArmy(AttackTime, v, a, new Point(target), SendArmy.CommandType.Attack, "wall"));
                                        count++;
                                    }).Wait();
                                }

                            }
                            
                            
                        }

                    }
                }
                Program.Log("Wczytano polecenia z pliku " + _filename + " ("+ count  + ")", ConsoleColor.Green);
            } 
            catch(FileNotFoundException e)
            {
                Program.Log("Nie odnaleziono pliku " + e.FileName, ConsoleColor.Red);
            }
            catch (ArgumentException)
            {
                Program.Log("Nie podano ścieżki do pliku", ConsoleColor.Red);
            }

        }
        public void Plan()
        {

            string wybor;
            SendArmy.CommandType type;
            do
            {
                Console.WriteLine("[1] Atak\n[2] Wsparcie");
                Console.Write("Wybór: ");
                wybor = Console.ReadLine();
            } while (wybor != "1" && wybor != "2");

            if (wybor == "1")
                type = SendArmy.CommandType.Attack;
            else
                type = SendArmy.CommandType.Support;

            Regex regex_cords = new Regex("([0-9]{3}\\|[0-9]{3})");
            do
            {
                Console.WriteLine("Podaj cel xxx|yyy");
                Console.Write("Wybór: ");
                wybor = Console.ReadLine();
            } while (!regex_cords.IsMatch(wybor));

            Point cords = new Point(int.Parse(wybor.Substring(0,3)), int.Parse(wybor.Substring(4,3)));



            Task.Run(async () =>
            {
                Army a = (await SelectedVillage.GetSummary(B)).Army[0];
                Army selectedArmy = new Army();

                string temp;
                int ilosc = a.Spear;

                do
                {
                    Console.Write("Pikinierzy [" + a.Spear + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Spear);

                selectedArmy.Spear = ilosc;

            
                ilosc = a.Sword;
                do
                {
                    Console.Write("Miecznicy [" + a.Sword + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Sword);

                selectedArmy.Sword = ilosc;

                ilosc = a.Axe;
                do
                {
                    Console.Write("Topornicy [" + a.Axe + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Axe);

                selectedArmy.Axe = ilosc;

                ilosc = a.Archer;
                do
                {
                    Console.Write("Łucznicy [" + a.Archer+ "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Archer);

                selectedArmy.Archer = ilosc;

                ilosc = a.Spy;
                do
                {
                    Console.Write("Zwiadowcy [" + a.Spy + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Spy);

                selectedArmy.Spy = ilosc;

                ilosc = a.Light;
                do
                {
                    Console.Write("Lekka kawaleria [" + a.Light + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Light);

                selectedArmy.Light = ilosc;

                ilosc = a.Marcher;
                do
                {
                    Console.Write("Łucznicy na koniu [" + a.Marcher + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Marcher);

                selectedArmy.Marcher = ilosc;


                ilosc = a.Heavy;
                do
                {
                    Console.Write("Ciężka kawaleria [" + a.Heavy + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Heavy);

                selectedArmy.Heavy = ilosc;


                ilosc = a.Ram;
                do
                {
                    Console.Write("Tarany [" + a.Ram + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Ram);

                selectedArmy.Ram = ilosc;

                ilosc = a.Catapult;
                do
                {
                    Console.Write("Katapulty [" + a.Catapult + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Catapult);

                selectedArmy.Catapult = ilosc;


                ilosc = a.Snob;
                do
                {
                    Console.Write("Szlachcice [" + a.Snob + "]: ");
                    temp = Console.ReadLine();
                    if(!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Snob);

                selectedArmy.Snob = ilosc;


                ilosc = a.Knight;
                do
                {
                    Console.Write("Rycerz [" + a.Knight + "]: ");
                    temp = Console.ReadLine();
                    if (!string.IsNullOrEmpty(temp))
                        int.TryParse(Console.ReadLine(), out ilosc);

                } while (ilosc < 0 || ilosc > a.Knight);

                selectedArmy.Knight = ilosc;

                if (selectedArmy.Catapult > 0)
                {
                    Console.WriteLine("Wybierz cel dla katapult:");

                    var targets = new List<string[]>
                    {
                        new[] { "ratusz", "main" },
                        new[] { "koszary", "barracks" },
                        new[] { "stajnia", "stable" },
                        new[] { "warsztat", "garage" },
                        new[] { "wieża", "watchtower" },
                        new[] { "pałac", "snob" },
                        new[] { "kuźnia", "smith" },
                        new[] { "plac", "place" },
                        new[] { "piedestał", "statue" },
                        new[] { "rynek", "market" },
                        new[] { "tartak", "wood" },
                        new[] { "cegielnia", "stone" },
                        new[] { "huta żelaza", "iron" },
                        new[] { "zagroda", "farm" },
                        new[] { "spichlerz", "storage" },
                        new[] { "mur", "wall" }
                    };

                    int wybor2;
                    do
                    {
                        for(int i = 0; i < targets.Count; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("[" + (i + 1) + "]" + "\t");
                            Console.ResetColor();
                            Console.Write(targets[i][0] + "\n");
                        }
                        Console.Write("Wybór: ");
                        int.TryParse(Console.ReadLine(), out wybor2);

                    } while (wybor2 < 0 || wybor2 > targets.Count);

                    DateTime date;
                    do
                    {
                        Console.Write("Podaj datę (dd-MM-yyyy HH:mm:ss.fff): ");
                        temp = Console.ReadLine();

                    } while (!DateTime.TryParseExact(temp, "dd-MM-yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date));



                    SendArmy s = new SendArmy(date, SelectedVillage, selectedArmy, cords, type, targets[wybor2][1]);
                    Program.RegisterAction(s);


                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Akcja zaplanowana na " + date.ToString("dd-MM-yyyy HH:mm:ss.fff"));
                    Console.ResetColor();


                }



            }).Wait();



        }
    }
}
