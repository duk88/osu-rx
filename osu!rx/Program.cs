using osu;
using osu.Enums;
using osu_rx.Configuration;
using osu_rx.Core.Relax;
using osu_rx.Core.Timewarp;
using SimpleDependencyInjection;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput.Native;

namespace osu_rx
{
    class Program
    {
        private static OsuManager osuManager;
        private static ConfigManager configManager;

        private static Relax relax;
        private static Timewarp timewarp;

        private static string defaultConsoleTitle;

        private static string[] links = new string[]
        {
            "https://ko-fi.com/mrflashstudio",
            "https://www.buymeacoffee.com/mrflashstudio",
            "https://www.paypal.me/mrflashstudio",
            "https://qiwi.com/n/mrflashstudio",
            "https://github.com/mrflashstudio/osu-rx",
            "https://www.mpgh.net/forum/showthread.php?t=1488076",
            "https://discord.gg/q3vS9yp",
        };

        static void Main(string[] args)
        {
            osuManager = new OsuManager();

            if (!osuManager.Initialize())
            {
                Console.Clear();
                Console.WriteLine("osu!rx failed to initialize:\n");
                Console.WriteLine("Memory scanning failed! Try restarting osu!, osu!rx or your computer to fix this issue.");
                Console.WriteLine("If that didn't helped, then report this on GitHub/MPGH.");
                Console.WriteLine("Please include as much info as possible (OS version, hack version, build source, debug info, etc.).");
                Console.WriteLine($"\n\nDebug Info:\n");
                Console.WriteLine(osuManager.DebugInfo);

                while (true)
                    Thread.Sleep(1000);
            }

            configManager = new ConfigManager();

            DependencyContainer.Cache(osuManager);
            DependencyContainer.Cache(configManager);

            relax = new Relax();
            timewarp = new Timewarp();

            defaultConsoleTitle = Console.Title;
            if (configManager.UseCustomWindowTitle)
                Console.Title = configManager.CustomWindowTitle;

            DrawMainMenu();
        }

        private static void DrawMainMenu()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = version.Remove(version.LastIndexOf(".0"));

            Console.Clear();
            Console.WriteLine($"osu!rx v{version} (GitHub release)");
            Console.WriteLine("\n---Main Menu---");
            Console.WriteLine("\n1. Start");
            Console.WriteLine("2. Settings\n");
            Console.WriteLine("3. I need help!");
            Console.WriteLine("4. Support development of osu!rx\n");
            Console.WriteLine("---------------\n");
            Console.WriteLine($"Join our discord server! https://discord.gg/q3vS9yp");
            Console.WriteLine("\n---------------\n");
            Console.WriteLine("Please note that every single feature is detected.");
            Console.WriteLine("Use only at your own risk.");
            Console.WriteLine("\n---------------");
            Console.WriteLine("\nSpecial thanks to:\n");
            Console.WriteLine("Azuki and HoLLy for being cuties and helping me with development ~ <3");
            Console.WriteLine("Capri, paprika, PerfectlyPlayer?!, de1uxe and LunaNASA for supporting me financially $$$");
            Console.WriteLine("\n---------------");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    DrawPlayer();
                    break;
                case ConsoleKey.D2:
                    DrawSettings();
                    break;
                case ConsoleKey.D3:
                    DrawHelpInfo();
                    break;
                case ConsoleKey.D4:
                    DrawSupportInfo();
                    break;
                default:
                    DrawMainMenu();
                    break;
            }
        }

        private static void DrawPlayer()
        {
            bool shouldExit = false;
            Task.Run(() =>
            {
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;

                shouldExit = true;
                relax.Stop();
                timewarp.Stop();
            });

            while (!shouldExit)
            {
                Console.Clear();
                Console.WriteLine("Idling");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                while (!osuManager.CanLoad && !shouldExit)
                    Thread.Sleep(5);

                if (shouldExit)
                    break;

                var beatmap = osuManager.Player.Beatmap;

                Console.Clear();
                Console.WriteLine($"Playing {beatmap.Artist} - {beatmap.Title} ({beatmap.Creator}) [{beatmap.Version}]");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                var relaxTask = Task.Factory.StartNew(() =>
                {
                    if (configManager.EnableRelax && osuManager.Player.CurrentRuleset == Ruleset.Standard)
                        relax.Start(beatmap);
                });

                var timewarpTask = Task.Factory.StartNew(() =>
                {
                    if (configManager.EnableTimewarp)
                        timewarp.Start();
                });

                Task.WaitAll(relaxTask, timewarpTask);
            }

            DrawMainMenu();
        }

        private static void DrawSettings()
        {
            Console.Clear();
            Console.WriteLine("---Settings---\n");
            Console.WriteLine("1. Relax settings");
            Console.WriteLine("2. Timewarp settings");
            Console.WriteLine("3. Other settings");

            Console.WriteLine("\nESC. Back to main menu");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D2:
                    DrawTimewarpSettings();
                    break;
                case ConsoleKey.D3:
                    DrawOtherSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawMainMenu();
                    break;
                default:
                    DrawSettings();
                    break;
            }
        }

        private static void DrawRelaxSettings()
        {
            Console.Clear();
            Console.WriteLine("---Relax Settings---\n");
            Console.WriteLine($"1. Relax                      | [{(configManager.EnableRelax ? "ENABLED" : "DISABLED")}]");
            Console.WriteLine($"2. Playstyle                  | [{configManager.PlayStyle}]");
            Console.WriteLine($"3. Primary key                | [{configManager.PrimaryKey}]");
            Console.WriteLine($"4. Secondary key              | [{configManager.SecondaryKey}]");
            Console.WriteLine($"5. Double delay key           | [{configManager.DoubleDelayKey}]");
            Console.WriteLine($"6. Max singletap BPM          | [{configManager.MaxSingletapBPM}BPM]");
            Console.WriteLine($"7. AlternateIfLessThan        | [{configManager.AlternateIfLessThan}ms]");
            Console.WriteLine($"8. Slider alternation binding | [{configManager.SliderAlternationBinding}]");
            Console.WriteLine($"9. Audio offset               | [{configManager.AudioOffset}ms]");
            Console.WriteLine($"0. HoldBeforeSpinner time     | [{configManager.HoldBeforeSpinnerTime}ms]");

            Console.WriteLine($"\nQ. HitTimings settings");
            Console.WriteLine($"W. Hitscan settings");

            Console.WriteLine("\nESC. Back to settings");

            OsuKeys[] osuKeys = (OsuKeys[])Enum.GetValues(typeof(OsuKeys));
            SliderAlternationBinding[] sliderBindings = (SliderAlternationBinding[])Enum.GetValues(typeof(SliderAlternationBinding));
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    configManager.EnableRelax = !configManager.EnableRelax;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D2:
                    Console.Clear();
                    Console.WriteLine("Select new playstyle:\n");
                    PlayStyles[] playstyles = (PlayStyles[])Enum.GetValues(typeof(PlayStyles));
                    for (int i = 0; i < playstyles.Length; i++)
                        Console.WriteLine($"{i + 1}. {playstyles[i]}");
                    if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int selected) && selected > 0 && selected < 4)
                        configManager.PlayStyle = (PlayStyles)selected - 1;
                    else
                        goto case ConsoleKey.D2;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D3:
                    Console.Clear();
                    Console.WriteLine("Enter new primary key:\n");
                    for (int i = 0; i < osuKeys.Length; i++)
                        Console.WriteLine($"{i + 1}. {osuKeys[i]}");
                    if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int primaryKey) && primaryKey > 0 && primaryKey < 5)
                        configManager.PrimaryKey = (OsuKeys)primaryKey - 1;
                    else
                        goto case ConsoleKey.D3;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D4:
                    Console.Clear();
                    Console.WriteLine("Enter new secondary key:\n");
                    for (int i = 0; i < osuKeys.Length; i++)
                        Console.WriteLine($"{i + 1}. {osuKeys[i]}");
                    if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int secondaryKey) && secondaryKey > 0 && secondaryKey < 5)
                        configManager.SecondaryKey = (OsuKeys)secondaryKey - 1;
                    else
                        goto case ConsoleKey.D4;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D5:
                    Console.Clear();
                    Console.Write("Enter new double delay key: ");
                    configManager.DoubleDelayKey = (VirtualKeyCode)Console.ReadKey(true).Key;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D6:
                    Console.Clear();
                    Console.Write("Enter new max singletap BPM: ");
                    if (int.TryParse(Console.ReadLine(), out int bpm))
                    {
                        configManager.MaxSingletapBPM = bpm;
                        configManager.AlternateIfLessThan = 60000 / bpm;
                    }
                    else
                        goto case ConsoleKey.D6;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D7:
                    Console.Clear();
                    Console.Write("Enter new AlternateIfLessThan: ");
                    if (int.TryParse(Console.ReadLine(), out int alternateIfLessThan))
                    {
                        configManager.AlternateIfLessThan = alternateIfLessThan;
                        configManager.MaxSingletapBPM = 60000 / alternateIfLessThan;
                    }
                    else
                        goto case ConsoleKey.D7;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D8:
                    Console.Clear();
                    Console.WriteLine("Select new slider alternation binding:\n");
                    for (int i = 0; i < sliderBindings.Length; i++)
                        Console.WriteLine($"{i + 1}. {sliderBindings[i]}");
                    if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int sliderBinding) && sliderBinding > 0 && sliderBinding < 3)
                        configManager.SliderAlternationBinding = (SliderAlternationBinding)sliderBinding - 1;
                    else
                        goto case ConsoleKey.D8;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D9:
                    Console.Clear();
                    Console.Write("Enter new audio offset: ");
                    if (int.TryParse(Console.ReadLine(), out int offset))
                        configManager.AudioOffset = offset;
                    else
                        goto case ConsoleKey.D9;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.D0:
                    Console.Clear();
                    Console.Write("Enter new HoldBeforeSpinner time: ");
                    if (int.TryParse(Console.ReadLine(), out int holdBeforeSpinnerTime))
                        configManager.HoldBeforeSpinnerTime = holdBeforeSpinnerTime;
                    else
                        goto case ConsoleKey.D0;
                    DrawRelaxSettings();
                    break;
                case ConsoleKey.Q:
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.W:
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawSettings();
                    break;
                default:
                    DrawRelaxSettings();
                    break;
            }
        }

        private static void DrawHitTimingsSettings()
        {
            Console.Clear();
            Console.WriteLine("---HitTimings Settings---\n");
            Console.WriteLine($"1. Minimum offset           | [{configManager.HitTimingsMinOffset}%]");
            Console.WriteLine($"2. Maximum offset           | [{configManager.HitTimingsMaxOffset}%]");
            Console.WriteLine($"3. Minimum alternate offset | [{configManager.HitTimingsAlternateMinOffset}%]");
            Console.WriteLine($"4. Maximum alternate offset | [{configManager.HitTimingsAlternateMaxOffset}%]");
            Console.WriteLine($"5. Minimum hold time        | [{configManager.HitTimingsMinHoldTime}ms]");
            Console.WriteLine($"6. Maximum hold time        | [{configManager.HitTimingsMaxHoldTime}ms]");
            Console.WriteLine($"7. Minimum slider hold time | [{configManager.HitTimingsMinSliderHoldTime}ms]");
            Console.WriteLine($"8. Maximum slider hold time | [{configManager.HitTimingsMaxSliderHoldTime}ms]");
            Console.WriteLine($"9. Double delay factor      | [{configManager.HitTimingsDoubleDelayFactor}x]\n");
            Console.WriteLine($"0. Fallback timing system   | [{(configManager.HitTimingsUseFallbackTimingSystem ? "ENABLED" : "DISABLED")}]");

            Console.WriteLine("\nESC. Back to relax settings");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    Console.Clear();
                    Console.Write("Enter new minimum offset: ");
                    if (int.TryParse(Console.ReadLine(), out int minOffset))
                        configManager.HitTimingsMinOffset = minOffset;
                    else
                        goto case ConsoleKey.D1;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D2:
                    Console.Clear();
                    Console.Write("Enter new maximum offset: ");
                    if (int.TryParse(Console.ReadLine(), out int maxOffset))
                        configManager.HitTimingsMaxOffset = maxOffset;
                    else
                        goto case ConsoleKey.D2;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D3:
                    Console.Clear();
                    Console.Write("Enter new minimum alternate offset: ");
                    if (int.TryParse(Console.ReadLine(), out int minAlternateOffset))
                        configManager.HitTimingsAlternateMinOffset = minAlternateOffset;
                    else
                        goto case ConsoleKey.D3;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D4:
                    Console.Clear();
                    Console.Write("Enter new maximum alternate offset: ");
                    if (int.TryParse(Console.ReadLine(), out int maxAlternateOffset))
                        configManager.HitTimingsAlternateMaxOffset = maxAlternateOffset;
                    else
                        goto case ConsoleKey.D4;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D5:
                    Console.Clear();
                    Console.Write("Enter new minimum hold time: ");
                    if (int.TryParse(Console.ReadLine(), out int minHold))
                        configManager.HitTimingsMinHoldTime = minHold;
                    else
                        goto case ConsoleKey.D5;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D6:
                    Console.Clear();
                    Console.Write("Enter new maximum hold time: ");
                    if (int.TryParse(Console.ReadLine(), out int maxHold))
                        configManager.HitTimingsMaxHoldTime = maxHold;
                    else
                        goto case ConsoleKey.D6;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D7:
                    Console.Clear();
                    Console.Write("Enter new minimum slider hold time: ");
                    if (int.TryParse(Console.ReadLine(), out int minSliderHold))
                        configManager.HitTimingsMinSliderHoldTime = minSliderHold;
                    else
                        goto case ConsoleKey.D7;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D8:
                    Console.Clear();
                    Console.Write("Enter new maximum slider hold time: ");
                    if (int.TryParse(Console.ReadLine(), out int maxSliderHold))
                        configManager.HitTimingsMaxSliderHoldTime = maxSliderHold;
                    else
                        goto case ConsoleKey.D8;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D9:
                    Console.Clear();
                    Console.Write("Enter double delay factor: ");
                    if (float.TryParse(Console.ReadLine(), out float doubleDelayFactor))
                        configManager.HitTimingsDoubleDelayFactor = doubleDelayFactor;
                    else
                        goto case ConsoleKey.D9;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.D0:
                    configManager.HitTimingsUseFallbackTimingSystem = !configManager.HitTimingsUseFallbackTimingSystem;
                    DrawHitTimingsSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawRelaxSettings();
                    break;
                default:
                    DrawHitTimingsSettings();
                    break;
            }
        }

        private static void DrawHitScanSettings()
        {
            Console.Clear();
            Console.WriteLine("---HitScan Settings---\n");
            Console.WriteLine($"1. HitScan                              | [{(configManager.EnableHitScan ? "ENABLED" : "DISABLED")}]");
            Console.WriteLine($"2. Prediction                           | [{(configManager.EnableHitScanPrediction ? "ENABLED" : "DISABLED")}]");
            Console.WriteLine($"3. Prediction direction angle tolerance | [{configManager.HitScanPredictionDirectionAngleTolerance}°]");
            Console.WriteLine($"4. Prediction radius scale              | [{configManager.HitScanPredictionRadiusScale}x]");
            Console.WriteLine($"5. Prediction max distance              | [{configManager.HitScanPredictionMaxDistance}px]");
            Console.WriteLine($"6. Miss radius                          | [{configManager.HitScanMissRadius}px]");
            Console.WriteLine($"7. Miss chance                          | [{configManager.HitScanMissChance}%]");
            Console.WriteLine($"8. Miss after HitWindow50               | [{(configManager.HitScanMissAfterHitWindow50 ? "ENABLED" : "DISABLED")}]");

            Console.WriteLine("\nESC. Back to relax settings");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    configManager.EnableHitScan = !configManager.EnableHitScan;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D2:
                    configManager.EnableHitScanPrediction = !configManager.EnableHitScanPrediction;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D3:
                    Console.Clear();
                    Console.Write("Enter new direction angle tolerance: ");
                    if (int.TryParse(Console.ReadLine(), out int angleTolerance))
                        configManager.HitScanPredictionDirectionAngleTolerance = angleTolerance;
                    else
                        goto case ConsoleKey.D3;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D4:
                    Console.Clear();
                    Console.Write("Enter new radius scale: ");
                    if (float.TryParse(Console.ReadLine(), out float scale))
                        configManager.HitScanPredictionRadiusScale = scale;
                    else
                        goto case ConsoleKey.D4;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D5:
                    Console.Clear();
                    Console.Write("Enter new max distance: ");
                    if (int.TryParse(Console.ReadLine(), out int maxDistance))
                        configManager.HitScanPredictionMaxDistance = maxDistance;
                    else
                        goto case ConsoleKey.D5;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D6:
                    Console.Clear();
                    Console.Write("Enter new miss radius: ");
                    if (int.TryParse(Console.ReadLine(), out int missRadius))
                        configManager.HitScanMissRadius = missRadius;
                    else
                        goto case ConsoleKey.D6;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D7:
                    Console.Clear();
                    Console.Write("Enter new miss chance: ");
                    if (int.TryParse(Console.ReadLine(), out int missChance))
                        configManager.HitScanMissChance = missChance;
                    else
                        goto case ConsoleKey.D7;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.D8:
                    configManager.HitScanMissAfterHitWindow50 = !configManager.HitScanMissAfterHitWindow50;
                    DrawHitScanSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawRelaxSettings();
                    break;
                default:
                    DrawHitScanSettings();
                    break;
            }
        }

        private static void DrawTimewarpSettings()
        {
            Console.Clear();
            Console.WriteLine("---Timewarp Settings---\n");
            Console.WriteLine($"1. Timewarp      | [{(configManager.EnableTimewarp ? "ENABLED" : "DISABLED")}]");
            Console.WriteLine($"2. Timewarp rate | [{configManager.TimewarpRate}x]\n");

            Console.WriteLine("\nESC. Back to settings");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    configManager.EnableTimewarp = !configManager.EnableTimewarp;
                    DrawTimewarpSettings();
                    break;
                case ConsoleKey.D2:
                    Console.Clear();
                    Console.Write("Enter new timewarp rate: ");
                    if (double.TryParse(Console.ReadLine(), out double rate))
                        configManager.TimewarpRate = rate;
                    else
                        goto case ConsoleKey.D2;
                    DrawTimewarpSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawSettings();
                    break;
                default:
                    DrawTimewarpSettings();
                    break;
            }
        }

        private static void DrawOtherSettings()
        {
            Console.Clear();
            Console.WriteLine("---Other Settings---\n");
            Console.WriteLine($"1. Custom window title | [{(configManager.UseCustomWindowTitle ? $"ON | {configManager.CustomWindowTitle}" : "OFF")}]");

            Console.WriteLine("\nESC. Back to settings");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    Console.Clear();
                    Console.WriteLine("Use custom window title?\n");
                    Console.WriteLine("1. Yes");
                    Console.WriteLine("2. No");
                    configManager.UseCustomWindowTitle = Console.ReadKey(true).Key == ConsoleKey.D1;
                    if (configManager.UseCustomWindowTitle)
                    {
                        Console.Clear();
                        Console.Write("Enter new custom window title: ");
                        configManager.CustomWindowTitle = Console.ReadLine();
                        Console.Title = configManager.CustomWindowTitle;
                    }
                    else
                        Console.Title = defaultConsoleTitle;
                    DrawOtherSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawSettings();
                    break;
                default:
                    DrawOtherSettings();
                    break;
            }
        }

        private static void DrawHelpInfo()
        {
            Console.Clear();
            Console.WriteLine("---I need assistance! What should i do?---\n");

            Console.WriteLine("You can ask for help on either MPGH or Discord.");
            Console.WriteLine("Select one of these below.\n");

            Console.WriteLine("1. Ask for help on MPGH");
            Console.WriteLine("2. Ask for help on Discord");

            Console.WriteLine("\nESC. Back to settings");

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.D1 || key == ConsoleKey.D2)
                Process.Start(links[key - ConsoleKey.D1 + 5]);
            else if (key == ConsoleKey.Escape)
                DrawMainMenu();

            DrawHelpInfo();
        }

        private static void DrawSupportInfo()
        {
            Console.Clear();
            Console.WriteLine("---What can i do to help osu!rx?---\n");

            Console.WriteLine("Glad you're interested!\n");

            Console.WriteLine("-----------------------------------\n");

            Console.WriteLine("If you like what i'm doing and are willing to support me financially - consider becoming a sponsor <3!");
            Console.WriteLine("Select any service below to proceed.\n");

            Console.WriteLine("1. Ko-fi");
            Console.WriteLine("2. Buy Me A Coffee");
            Console.WriteLine("3. PayPal");
            Console.WriteLine("4. Qiwi\n");

            Console.WriteLine("-----------------------------------\n");

            Console.WriteLine("If you can't or don't want to support me financially - that's totally fine!");
            Console.WriteLine("You can still help me by providing any feedback, reporting bugs, creating pull requests and requesting features!");
            Console.WriteLine("Any help is highly appreciated!\n");
            Console.WriteLine("5. Provide feedback via GitHub");
            Console.WriteLine("6. Provide feedback via MPGH");
            Console.WriteLine("7. Provide feedback via Discord\n");

            Console.WriteLine("-----------------------------------");

            Console.WriteLine("\nESC. Back to settings");

            var key = Console.ReadKey(true).Key;
            if (key >= ConsoleKey.D1 && key <= ConsoleKey.D7)
                Process.Start(links[key - ConsoleKey.D1]);
            else if (key == ConsoleKey.Escape)
                DrawMainMenu();

            DrawSupportInfo();
        }
    }
}
