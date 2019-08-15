using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace TWTools
{




    class Program
    {

        static BOT instance = new BOT();
        public static string sign = "$ ";

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("TWTools 1.0.0.1");
            Console.ResetColor();

            string login = "", password = "", world = "", file = "";


            // Obsługa parametrów programu
            for (int i=0; i < args.Length; i++)
            {
                if (args[i] == "-login")
                {
                    login = args[i + 1];
                }
                else if (args[i] == "-password")
                {
                    password = args[i + 1];
                }
                else if (args[i] == "-world")
                {
                    world = args[i + 1];
                }
                else if (args[i] == "-file")
                {
                    file = args[i + 1];
                }
            }

            // Główny zegar
            Timer MainTimer = new Timer(200);
            MainTimer.Elapsed += OnClockTick;
            MainTimer.AutoReset = true;
            MainTimer.Enabled = true;


            {
                bool promptDialog = false;
                do
                {
                    if (string.IsNullOrEmpty(login) || promptDialog)
                    {
                        Console.Write("Login: ");
                        login = Console.ReadLine();
                    }
                    if (string.IsNullOrEmpty(password) || promptDialog)
                    {
                        Console.Write("Hasło: ");
                        password = GetConsolePassword();
                    }
                    promptDialog = true;

                } while (!instance.TryLogin(login, password));

            }


            if(string.IsNullOrEmpty(world) || !instance.SelectWorld(world))
            {
                // Wybór świata
                do
                {
                    Console.WriteLine("Wybierz świat: ");
                    instance.PrintWorldList();
                    Console.Write("Wybór: ");
                }
                while (!instance.SelectWorld(int.Parse(Console.ReadLine())-1));
            }
            sign = instance.SelectedWorld + "$ ";
            instance.LoadWorld();
            //instance.PrintVillageList();

            if(!String.IsNullOrEmpty(file))
                instance.LoadTargetsFromFile(file);



            // Dopóki nie dostanie polecenia zakmnięcia
            bool Close = false;
            string command;
            while (!Close)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(sign);
                Console.ResetColor();
                command = Console.ReadLine().Trim();
                command = Regex.Replace(command, @"\s+", " ");

                if (command == "help" || command == "pomoc")
                {
                    Console.WriteLine("Lista dostępnych komend: ");
                    Console.WriteLine("[wczytaj nazwapliku.txt] \t - wczytuje komendy z pliku");
                    Console.WriteLine("[wioski] \t - wyświetla listę wiosek");
                    Console.WriteLine("[wioska nr] \t - wybiera wioskę");
                    Console.WriteLine("[zaplanowane] \t - wyświetla zaplanowane akcje");
                    Console.WriteLine("[akcja nr] \t - szczegółowa informacja o akcji");
                    Console.WriteLine("[planuj] \t - planuje atak/wsparcie");
                    Console.WriteLine("[usun nr] \t - usuwa zaplanowaną akcję");
                    Console.WriteLine("[cls] \t - czyści ekran");
                    Console.WriteLine("[exit] \t - zamyka aplikację");
                }
                else if (command == "wioski") 
                {
                    instance.PrintVillageList();
                }
                else if(command.StartsWith("wioska "))
                {
                    if(instance.SelectVillage(int.Parse(command.Substring(7))-1))
                    {
                        sign = instance.SelectedWorld + "/" + instance.SelectedVillage.Name + "$ ";
                    }
                }
                else if (command.StartsWith("wczytaj"))
                {
                    if(command.Length > 8)
                    {
                        instance.LoadTargetsFromFile(command.Substring(8));
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Nie podano ścieżki do pliku");
                        Console.ResetColor();
                    }
                }
                else if (command == "zaplanowane")
                {
                    if(ActionList.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Brak zaplanowanych akcji");
                        Console.ResetColor();
                    }
                    for (int i = 0; i < ActionList.Count; i++)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("[" + (i + 1) + "]\t");
                        Console.ResetColor();
                        Console.Write("[" + ActionList[i].Date.ToString("dd-MM-yyyy HH:mm:ss.fff") + "] "+ ActionList[i].Description + "\n");
                    }
                }
                else if (command == "planuj")
                {
                    if(instance.SelectedVillage != null)
                    {
                        instance.Plan();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Najpierw wybierz wioskę");
                        Console.ResetColor();
                    }
                }
                else if (command.StartsWith("akcja"))
                {
                    if (command.Length > 6)
                    {
                        {
                            int n = int.Parse(command.Substring(5));
                            
                            if(n > 0 && n < ActionList.Count)
                            {
                                ActionList[n - 1].Display();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Nie ma takiej akcji");
                                Console.ResetColor();
                            }
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Nie wpisano numeru akcji");
                        Console.ResetColor();
                    }


                }
                else if (command.StartsWith("usun"))
                {
                    if (command.Length > 6)
                    {
                        {
                            int n = int.Parse(command.Substring(5));


                            if (n > 0 && n < ActionList.Count)
                            {
                                RemoveAction(ActionList[n - 1]);
                            }


                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Nie wpisano numeru akcji");
                        Console.ResetColor();
                    }


                }
                else if (command == "cls" || command == "clear")
                {
                    Console.Clear();

                }
                else if (command == "exit")
                {
                    Close = true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Błędna komenda. Wpisz ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("pomoc");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" aby uzyskać listę dostępnych komend.\n");
                    Console.ResetColor();
                }


                //System.Threading.Thread.Sleep(500);


            }

        }

        public static void RegisterAction(IAction action)
        {
            ActionList.Add(action);
            ProgramAction += action.Run;

            ActionList.Sort((x, y) => x.Date.CompareTo(y.Date));
        }
        public static void RemoveAction(IAction action)
        {
            ProgramAction -= action.Run;
            ActionList.Remove(action);
        }

        private static List<IAction> ActionList = new List<IAction>();
        private delegate Task Action<in T>(T obj);
        private static Action<Browser> ProgramAction { get; set; }
        private static void OnClockTick(object sender, ElapsedEventArgs e)
        {
            foreach(var action in ActionList)
            {
                if (action.IsDone())
                    RemoveAction(action);    // Usuwa polecenie wykonywania akcji, jeśli została już zakończona
                                            
            }

            ProgramAction?.Invoke(instance.Browser);
        }

        // Określenie adresu gry
        public static GamePage GamePage = new GamePage { Domain = "plemiona.pl", Path = "" };

        // Funkcja służąca do wykonywania logów
        public static void Log(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            String message = "[" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "][" + GamePage.Domain + "] " + text;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            StreamWriter file = File.AppendText("log.txt");
            file.WriteLine(message);
            file.Close();

        }


        /* https://gist.github.com/huobazi/1039424 */
        private static string GetConsolePassword()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        Console.Write("\b\0\b");
                        sb.Length--;
                    }

                    continue;
                }

                Console.Write('*');
                sb.Append(cki.KeyChar);
            }
            return sb.ToString();
        }


    }



}
