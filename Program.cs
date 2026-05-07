using Dz2._4;
using System;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Dz4
{
    internal class Program
    {
        static Player player;
        static Enemy enemy;
        static float tick;
        static CancellationTokenSource token;

        static async Task Main(string[] args)
        {
            string battleLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "battle.log");
            string gameSaveFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gameSaveFile.txt");






            if (!File.Exists(battleLog))
            {
                File.Create(battleLog).Dispose();
            }

            if (!File.Exists(gameSaveFile))
            {
                File.Create(gameSaveFile).Dispose();
            }

            Console.WriteLine("Выберите:");
            Console.WriteLine("1 - Начать игру заново");
            Console.WriteLine("2 - Продолжить");

            string? input = Console.ReadLine();
            while (true)
            {
                if (input != null)
                {
                    if (input == "1")
                    {
                        CreateNewGame(battleLog, gameSaveFile);
                        Console.WriteLine("Игра начата. Доступные команды: attack, heal, stats, log, cast, exit");
                        break;
                    }
                    if (input == "2")
                    {
                        LoadGame(gameSaveFile);
                        Console.WriteLine("Игра начата. Доступные команды: attack, heal, stats, log, cast, exit");


                        GameStatistic(battleLog);

                        break;
                    }
                    else
                    {
                        Console.WriteLine("Введено неверное значение");
                        input = Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("Ничего не введено");
                    input = Console.ReadLine();
                }
            }




            Task timeTask = RunGameClockAsync();

            Task eventTast = Event(battleLog, gameSaveFile);

            Task enemyTask = EnemyAutoAttackAsync(battleLog, gameSaveFile);




            while (true)
            {

                if (player.IsDead || enemy.IsDead)
                {
                    GameOver(battleLog, gameSaveFile);
                    break;
                }

                string? command = Console.ReadLine();

                if (command == "attack")
                {

                    if (player.IsDead)
                    {
                        Console.WriteLine("Вы мертвы и не можете атаковать");
                        GameOver(battleLog, gameSaveFile);
                        break;
                    }
                    if (!enemy.IsDead)
                    {
                        enemy.TakeDamage(20);
                        Console.WriteLine($"Вы нанесли 20 урона врагу. У врага {enemy.CurHp} HP");
                        SaveToLog(battleLog, $"ATTACK | dmg=20 | enemyHP={enemy.CurHp}");

                        if (enemy.IsDead)
                        {
                            Console.WriteLine("Враг побежден!");
                            SaveGame(gameSaveFile);
                            GameOver(battleLog, gameSaveFile);
                            break;
                        }


                    }
                    else
                    {
                        Console.WriteLine("Враг уже побежден!");

                        SaveGame(gameSaveFile);
                        GameOver(battleLog, gameSaveFile);
                        break;
                    }
                }
                else if (command == "heal")
                {

                    if (player.IsDead)
                    {
                        Console.WriteLine("Вы мертвы и не можете лечиться");
                        GameOver(battleLog, gameSaveFile);
                        break;
                    }
                    if (!player.IsDead)
                    {
                        player.Heal(30);
                        Console.WriteLine($"Вы вылечились на 30 HP. У вас {player.CurHp} HP");
                        SaveToLog(battleLog, $"HEAL | heal = 30 | playerHP={player.CurHp}");

                    }
                    else
                    {
                        Console.WriteLine("Труп не восскреснет");
                        SaveGame(gameSaveFile);
                        GameOver(battleLog, gameSaveFile);
                        break;
                    }
                }
                else if (command == "log")
                {
                    int k = 0;
                    foreach (string line in File.ReadAllLines(battleLog))
                    {
                        Console.WriteLine(line);
                        k++;
                        if (k == 10)
                        {
                            break;
                        }
                    }
                }


                else if (command == "stats")
                {
                    Console.WriteLine($"Игрок: HP = {player.CurHp}, Мертв = {player.IsDead}");
                    Console.WriteLine($"Враг: HP = {enemy.CurHp}, Мертв = {enemy.IsDead}");
                    Console.WriteLine($"Время боя: {tick} секунд");
                }


                else if (command == "cast")
                {
              
                    if (player.IsDead)
                    {
                        Console.WriteLine("Вы мертвы и не можете колдовать");
                        GameOver(battleLog, gameSaveFile);
                        break;
                    }
                    if (enemy.IsDead)
                    {
                        Console.WriteLine("Враг уже побежден, не на кого кастовать");
                        GameOver(battleLog, gameSaveFile);
                        break;
                    }
                    token = new CancellationTokenSource();
                    Task castTask = CastLoad(token.Token, battleLog, gameSaveFile);


                }



                else if (command == "exit")
                {
                    SaveGame(gameSaveFile);
                    Console.WriteLine("Игра сохранена. До свидания!");
                    break;
                }

                else
                {
                    Console.WriteLine("Неизвестная команда. Доступные команды: attack, heal, stats, log, cast exit");
                }





            }


        }

        static void CreateNewGame(string battleLog, string gameSaveFile)
        {
            File.WriteAllText(battleLog, string.Empty);
            File.WriteAllText(gameSaveFile, string.Empty);

            player = new Player(100, false);
            enemy = new Enemy(300, false);
            tick = 0;

            Console.WriteLine("История боя очищена. Новая игра начата.");

            SaveGame(gameSaveFile);
        }

        static void LoadGame(string gameSaveFile)
        {

            string[] lines = File.ReadAllLines(gameSaveFile);

            if (lines.Length >= 3)
            {
                int playerCurHp = int.Parse(lines[0]);
                int enemyCurHp = int.Parse(lines[1]);
                tick = int.Parse(lines[2]);

                player = new Player(playerCurHp, playerCurHp <= 0);
                enemy = new Enemy(enemyCurHp, enemyCurHp <= 0);

                Console.WriteLine($"Игра загружена. Игрок HP: {player.CurHp}, Враг HP: {enemy.CurHp}, Время: {tick} сек");

            }

        }

        static void SaveGame(string gameSaveFile)
        {
            if (player != null && enemy != null)
            {
                string[] saveData = {
                    player.CurHp.ToString(),
                    enemy.CurHp.ToString(),
                    tick.ToString()
                };
                File.WriteAllLines(gameSaveFile, saveData);
            }
        }

        static void SaveToLog(string battleLog, string message)
        {
            Object lockObject = new Object();
            lock (lockObject)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string fullLine = $"{time} | {message}\n";
                File.AppendAllText(battleLog, fullLine);
            }
        }

        static void GameStatistic(string battleLog)
        {
            int attackCount = 0;
            int healCount = 0;
            int eventCount = 0;
            int cancelledCount = 0;
            int successCount = 0;

            foreach (string line in File.ReadAllLines(battleLog))
            {
                if (line.Contains("| ATTACK |"))
                {
                    attackCount++;
                }
                else if (line.Contains("| HEAL |"))
                {
                    healCount++; 
                }
                else if (line.Contains("| EVENT |"))
                {
                    eventCount++;
                }
                else if (line.Contains("| CAST_CANCELLED |"))
                {
                    cancelledCount++; 
                }
                else if (line.Contains("| CAST_SUCCESS |"))
                {
                    successCount++;
                }



            }
            Console.WriteLine($"История: ATTACK={attackCount}, HEAL={healCount}, EVENT={eventCount}, CAST_CANCELLED={cancelledCount}, CAST_SUCCESS={successCount}");

        }

        static void GameOver(string battleLog, string gameSaveFile)
        {
            if (player.IsDead || enemy.IsDead)
            {
                if (player.IsDead)
                {
                    Console.WriteLine("Бой закончен. Победил: Противник");


                }
                else if (enemy.IsDead)
                {
                    Console.WriteLine("Бой закончен. Победил: Игрок");


                }

                GameStatistic(battleLog);
                File.WriteAllText(battleLog, string.Empty);
                File.WriteAllText(gameSaveFile, string.Empty);

            }

        }








        static async Task RunGameClockAsync()
        {


            while (true)
            {

                ++tick;

                await Task.Delay(1000);
            }


        }

        static async Task EnemyAutoAttackAsync(string battleLog, string gameSaveFile)
        {
            while (!player.IsDead && !enemy.IsDead)
            {
                await Task.Delay(2000);

                
                if (!player.IsDead && !enemy.IsDead)
                {
                    player.TakeDamage(5);
                    Console.WriteLine("Вы получили 5 урона");
                    SaveToLog(battleLog, $"ATTACK | dmg=5 | playerHP={player.CurHp}");
                    if (player.IsDead)
                    {
                        Console.WriteLine("Вы проиграли(");
                        SaveGame(gameSaveFile);
                        GameOver(battleLog, gameSaveFile);
                    }
                }
            }
        }






        static async Task Event(string battleLog, string gameSaveFile)
        {
            if (!player.IsDead || !enemy.IsDead) 
            {
                Random rnd = new Random();
                string[] events =
                {
                    "RAGE",
                    "BANDAGE",
                    "CALM"

                };

                while (true)
                {
                    
                    if (player.IsDead || enemy.IsDead)
                    {
                        break;
                    }

                    string evnt = (events[rnd.Next(0, events.Length)]);


                    if (evnt == "RAGE")
                    {
                        int hpDelta = rnd.Next(2, 6);
                        Console.WriteLine($"{evnt}: Враг атакует с дополнительной силой Игрок получает урон: -{hpDelta}хп");
                       
                        player.TakeDamage(hpDelta);
                        SaveToLog(battleLog, $"EVENT | kind = {evnt} | hpDelta = -{hpDelta} | playerHP = {player.CurHp}");

                        if (token != null && !token.IsCancellationRequested)
                        {
                            Console.WriteLine("Событие RAGE прервало ваше заклинание!");
                            token.Cancel();
                        }

                        
                        if (player.IsDead)
                        {
                            Console.WriteLine("Вы проиграли от события RAGE");
                            SaveGame(gameSaveFile);
                            GameOver(battleLog, gameSaveFile);
                            break;
                        }


                    }
                    else if (evnt == "BANDAGE")
                    {
                        int hpDelta = rnd.Next(1, 4);
                        Console.WriteLine($"{evnt}: Вы случайно находите бинты: +{hpDelta}хп");
                        player.Heal(hpDelta);
                        SaveToLog(battleLog, $"EVENT | kind = {evnt} | hpDelta = +{hpDelta} | playerHP = {player.CurHp}");

                    }
                    else if (evnt == "CALM")
                    {
                        Console.WriteLine("Ничего не произошло");
                        SaveToLog(battleLog, $"EVENT | kind = {evnt} | hpDelta = 0 |");
                    }
                    await Task.Delay(rnd.Next(2000, 5000));


                }
            }
            else
            {
                GameOver(battleLog, gameSaveFile);
            }


        }


        static async Task CastLoad(CancellationToken token, string battleLog, string gameSaveFile)
        {
            try
            {
                Console.WriteLine($"Начинается каст заклинания... (требуется 3 секунд)");

                for (int i = 0; i < 3; i++)
                {

                    token.ThrowIfCancellationRequested();
                    await Task.Delay(1000);

                }

                
                if (!player.IsDead && !enemy.IsDead)
                {
                    Console.WriteLine("ЗАКЛИНАНИЕ УСПЕШНО ПРИМЕНЕНО!");

                    if (!enemy.IsDead)
                    {
                        int spellDamage = 50;
                        enemy.TakeDamage(spellDamage);
                        Console.WriteLine($"Урон врагу: {spellDamage}. HP врага: {enemy.CurHp}");

                        SaveToLog(battleLog, $"CAST_SUCCESS | dmg={spellDamage} | enemyHP={enemy.CurHp}");

                        if (enemy.IsDead)
                        {
                            Console.WriteLine("Враг побежден заклинанием!");
                            SaveGame(gameSaveFile);
                            GameOver(battleLog, gameSaveFile);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Невозможно применить заклинание - кто-то уже мертв");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Каст ПРЕРВАН событием!");
                SaveToLog(battleLog, "CAST_CANCELLED | reason=EVENT_INTERRUPT");
            }
        }
    }
}