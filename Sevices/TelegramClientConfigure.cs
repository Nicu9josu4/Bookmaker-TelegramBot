using BookmakerTelegramBot.Controllers;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Keyboards = BookmakerTelegramBot.Models.MainKeyboards;
using TotalPages = BookmakerTelegramBot.Models.TotalPages;
using Users = BookmakerTelegramBot.Models.Users;

namespace BookmakerTelegramBot.Sevices
{
    public class TelegramClientConfigure
    {
        public DbMethodsController Controller = new DbMethodsController();
        public TelegramLogginingClientConfigure LoggClient = new TelegramLogginingClientConfigure();
        public NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public Users users = new Users();
        public TotalPages pages = new TotalPages();
        public Keyboards keyboards = new Keyboards();
        public string connectionString;
        public string Token;
        public TelegramBotClient bot;
        //public CultureInfo CultInfo = new CultureInfo("ro-RO");
        //public ResourceManager ResManager = new ResourceManager("BookmakerTelegramBot.Resources.Resources", Assembly.GetExecutingAssembly());

        public async Task StartClient()
        {
            bot = new TelegramBotClient(Token);
            users.SetValuesFromDb(connectionString);

            Controller.users = users;
            Controller.pages = pages;
            //Controller.keyboards = keyboards;

            bot.StartReceiving(
                    HandleUpdatesAsync,
                    HandleErrorAsync,
                    new ReceiverOptions { AllowedUpdates = { } },
                    cancellationToken: new CancellationTokenSource().Token);

            var me = await bot.GetMeAsync();
            Console.WriteLine($"Bot started: @{me.Username}");
            Console.ReadLine();
            new CancellationTokenSource().Cancel();
        }

        //public async Task SendNotification()
        //{
        //}
        private async Task HandleUpdatesAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
        {
            try
            {

                if (update.Type == UpdateType.Message && update?.Message?.Text != null)
                {
                    //Console.WriteLine("Message:       ID:" + update.Message.Chat.Id + ", Text:        " + update.Message.Text);
                    await HandleMessage(bot, update.Message);
                    _ = LoggClient.SendMessage($"{update.Message.Chat.Id} say: {update.Message.Text}");

                    return;
                    // an object reference is required for the non-static field
                }
                if (update?.Type == UpdateType.CallbackQuery)
                {
                    //Console.WriteLine("CallBackQuery: ID:" + update.CallbackQuery.From.Id + ", CallbackData:" + update.CallbackQuery.Data.ToString());
                    await HandleCallbackQuery(bot, update.CallbackQuery);
                    return;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = exception switch
                {
                    // catch an api error, Bigger exception is { <- 400 - the message text is not changed -> }
                    ApiRequestException apiRequestException
                        => $"Telegram api error: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };
                Console.WriteLine(exception);

                _ = LoggClient.SendMessage(exception.Message);
                logger.Error(errorMessage);
                 Controller.SetLoggs(null, null, null, exception.Message);
            }
            
            return;
        }

        public async Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            try
            {
                if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id))
                {
                    // Declare current User
                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == message.Chat.Id).First();
                    currentUser.TextMessage = message.Text;
                    // verify users.UsersList language and change UIculture
                    //if (currentUser.Language == "ro")
                    //{
                    //    //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ro-RO");
                    //    CultInfo = new CultureInfo("ro-RO");
                    //    //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ro-RO");
                    //}
                    //else if (currentUser.Language == "ru")
                    //{
                    //    //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
                    //    CultInfo = new CultureInfo("ru-RU");
                    //    //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
                    //}
                    //else
                    //{
                    //    //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
                    //    CultInfo = new CultureInfo("en-US");
                    //    //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
                    //}


                    int number;
                    // reporting a problem 
                    if (message.Text != null && currentUser.ReportAProblem == true)
                    {
                        currentUser.ReportAProblem = false;
                        _ = LoggClient.SendReportMessage(currentUser.TextMessage);
                        return;
                    }
                    // /report a probblem or a bug
                    if (message.Text == "/report")
                    {
                        currentUser.ReportAProblem = true;
                        if (currentUser.Language == "ro")
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Introduceți un mesaj cu problema găsită");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Введите сообщение с найденной проблемой");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Enter a message with the problem found");
                        }
                        return;
                    }
                    // /register Command
                    if (message.Text == "/register")
                    {
                        currentUser.IntroduceTotal = false;
                        currentUser.Registration = true;
                        currentUser.ReportAProblem = false;
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.FirstName != null && existentUser.FirstName != "") && (existentUser.LastName != null && existentUser.LastName != "")))
                        {
                            if (currentUser.Language == "ro")
                            {
                                var menuKeyboard = new InlineKeyboardMarkup(new[]
                            {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Da","register"),
                            InlineKeyboardButton.WithCallbackData("Nu","mainMenu")
                        }
                                });
                                await botClient.SendTextMessageAsync(message.Chat.Id, "V-ati inregistrat deja, doriti sa modificati datele?", replyMarkup: menuKeyboard);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                var menuKeyboard = new InlineKeyboardMarkup(new[]
                            {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Yes","register"),
                            InlineKeyboardButton.WithCallbackData("No","mainMenu")
                        }
                                });
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже зарегистрировались, желаете изменить данные?", replyMarkup: menuKeyboard);
                            }
                            else
                            {
                                var menuKeyboard = new InlineKeyboardMarkup(new[]
                            {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Yes","register"),
                            InlineKeyboardButton.WithCallbackData("No","mainMenu")
                        }
                                });
                                await botClient.SendTextMessageAsync(message.Chat.Id, "You have registered, do you want to change the data?", replyMarkup: menuKeyboard);
                            }
                        }
                        else
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Introduceți numele, prenumele și numarul de telefon conform modelului: Ivan - Ivanov - 79800000");
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Введите имя, фамилию и номер телефона по образцу: Ivan - Ivanov - 79800000");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter your name, surname and phone according to the model: Ivan - Ivanov - 79800000");
                            }
                        }

                        return;
                    }
                    // /menu or /start command
                    if (message.Text == "/start" || message.Text == "/menu")
                    {
                        currentUser.IntroduceTotal = false;
                        currentUser.Registration = false;
                        currentUser.ReportAProblem = false;

                        Controller.SetNewVoter(null, null, null, null, message.Chat.Id);
                        InlineKeyboardMarkup menuKeyboard;
                        InlineKeyboardMarkup menuKeyboardSelectLanguage;


                        if (currentUser.Language == "ro")
                        {
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Introduceti prognostic", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Topul participantilor", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("Istoria schimbarilor","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Descriere", "description"), InlineKeyboardButton.WithCallbackData("Reguli","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Setari", "settings")}});
                            menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                           {
                            InlineKeyboardButton.WithCallbackData("Romana","language-ro"),
                        InlineKeyboardButton.WithCallbackData("Rusa","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Engleza","language-en")
                        });
                        }
                        else if (currentUser.Language == "ru")
                        {
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Введите прогноз", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Список топ участников", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("История", "prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Описание", "description"), InlineKeyboardButton.WithCallbackData("Правила", "rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Настройки", "settings")}});
                            menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                           {
                            InlineKeyboardButton.WithCallbackData("Румынский","language-ro"),
                        InlineKeyboardButton.WithCallbackData("Русский","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Английский","language-en")
                        });
                        }
                        else
                        {
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Top Voters","topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("History","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Description", "description"), InlineKeyboardButton.WithCallbackData("Rules","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Settings", "settings")}});
                            menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                           {
                            InlineKeyboardButton.WithCallbackData("Romanian","language-ro"),
                        InlineKeyboardButton.WithCallbackData("Russian","language-ru"),
                            InlineKeyboardButton.WithCallbackData("English","language-en")
                        });
                        }

                        if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && existentUser.Language != null && existentUser.FirstName != null && existentUser.FirstName != "" && existentUser.LastName != null && existentUser.LastName != ""))
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Meniu principal", replyMarkup: menuKeyboard);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Главное меню", replyMarkup: menuKeyboard);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Main Menu", replyMarkup: menuKeyboard);
                            }
                        }
                        else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.Language == null || existentUser.Language == "")))
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Alege limba", replyMarkup: menuKeyboardSelectLanguage);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Настройки языка", replyMarkup: menuKeyboardSelectLanguage);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Select Language", replyMarkup: menuKeyboardSelectLanguage);
                            }
                        }
                        else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.FirstName == null || existentUser.FirstName == "") && (existentUser.LastName == null || existentUser.LastName == "")))
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Incearca comanda /register");
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Попробуйте команду /register");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Try command /register");
                            }
                        }
                        return;
                    }
                    // /description command
                    if (message.Text == "/description")
                    {
                        currentUser.IntroduceTotal = false;
                        currentUser.ReportAProblem = false;
                        currentUser.Registration = false;

                        if (currentUser.Language == "ro")
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Acest eveniment este creat în onoarea campionatului de fotbal din întreaga lume, sunteți binevenit\r\npoate participa oricine care face parte din echipa Moldcell.\r\nScopul este: Acumularea de puncte și ridicarea în vârf pentru a primi un premiu.\r\nCampionatul Mondial de Fotbal 2022 este organizat de 32 de echipe de fotbal în urma a 64 de meciuri între echipe\r\nEvenimentul dat nu este un joc de noroc sau un mediu în care va fi necesar să investești ceva.");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Это мероприятие создано в честь футбольного чемпионата со всего мира, добро пожаловать\r\nпринять участие может любой, кто является частью команды Moldcell.\r\nЦель: Накопить очки и подняться на вершину, чтобы получить приз.\r\nЧемпионат мира по футболу 2022 года организован 32 футбольными командами после 64 игр между командами.\r\nДанное событие не является азартной игрой или средой, в которую нужно будет что-то вкладывать.");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "This event is created in honor of the football championship from all over the world, You are welcome\r\nanyone who is part of the Moldcell team can participate.\r\nThe goal is: Accumulating points and rising to the top to receive a prize.\r\nFootball-World-Championship year 2022 is organized by 32 football teams following 64 games between teams\r\nThe given event is not a game of chance or an environment in which it will be necessary to invest something.");
                        }
                        return;
                    }
                    // /rules command
                    if (message.Text == "/rules")
                    {
                        currentUser.IntroduceTotal = false;
                        currentUser.Registration = false;
                        currentUser.ReportAProblem = false;

                        if (currentUser.Language == "ro")
                        {
                            await botClient.SendTextMessageAsync(currentUser.UserID, "Când și cum mă înscriu în concurs?\r\n\r\nPoți intra în goana cu premii oricând dorește inima ta!\r\n\r\nSingura condiție este ca acesta să fie între 20 noiembrie și 18 decembrie 2022 (durata Cupei Mondiale 2022).\r\n\r\nTot ce aveți nevoie este: numărul Moldcell și aplicația Telegram\r\n\r\nCe să fac?\r\n\r\nDeschide Telegram Chat Bot prin linkul: http://t.me/FWChampionshipBot\r\n\r\nÎnregistrați-vă cu numele, prenumele și numărul de telefon\r\n\r\nCreați predicții pentru următoarele evenimente:\r\n\r\n- echipa câștigătoare a meciului (prima, a doua sau la egalitate)\r\n- scorul exact al meciului\r\n- jucătorul echipei care va marca un gol în timpul meciului (puteți alege doar unul din una dintre echipe)\r\n- echipa câștigătoare a Cupei Mondiale 2022\r\n - In cazul in care după cele 90 de minute scorul este egal și se trece la cele 2 reprize de prelungiri, atunci la calcularea punctajului se va lua în considerare scorul de după finalizarea celor 2 reprize de prelungiri, adică după minutul 120.\r\n\r\nAcumulați puncte în cazul unei predicții corecte:\r\n\r\n• Dacă ghiciți echipa câștigătoare a meciului, acumulați 1 punct;\r\n• Dacă ghiciți jucătorul care va înscrie un gol în meci, acumulați 1 punct;\r\n• Dacă ghiciți scorul final al jocului, acumulați 3 puncte;\r\n• Dacă ghiciți câștigătorul final al Cupei Mondiale 2022 făcând un pronostic;\r\n* Până pe 20 noiembrie – 50 de puncte\r\n* Până pe 3 decembrie – 20 puncte\r\n* Până pe 9 decembrie – 10 puncte\r\n• Dacă nu ghiciți nici câștigătorul, nici scorul, nici jucătorul care va înscrie, acumulați 0 puncte;\r\n• Pentru meciurile nefinalizate, va fi considerat un răspuns lipsă, adică 0 puncte.\r\n• Dacă meciul a început, nu mai puteți face pronosticuri pentru acest meci.\r\n\r\nCe premii pot câștiga?\r\nOferim un mare premiu - consola Xbox Series X 1TB și două premii mici - Căștile fără fir HyperX Cloud Stinger Core 7.1 și căștile Monster Clarity 100 Airlinks Black!\r\n\r\nClasamentul final se va face în funcție de punctele acumulate de participanți, în ordine descrescătoare. Astfel, Marele Premiu va fi câștigat de cel cu cele mai multe puncte acumulate.\r\n\r\nÎn caz de egalitate, câștigă cel care a înregistrat mai devreme ultimul pronostic.");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.SendTextMessageAsync(currentUser.UserID, "Когда и как я могу принять участие в конкурсе?\r\n\r\nВы можете принять участие в розыгрыше призов, когда вашей душе угодно!\r\n\r\nЕдинственное условие — это период с 20 ноября по 18 декабря 2022 года (продолжительность чемпионата мира по футболу 2022 года).\r\n\r\nВсе, что вам нужно, это: номер Moldcell и приложение Telegram.\r\n\r\nЧто делать?\r\n\r\nОткройте Telegram Chat Bot по ссылке: http://t.me/FWChampionshipBot\r\n\r\nЗарегистрируйтесь под своим именем, фамилией и номером телефона\r\n\r\nСоздайте прогнозы для следующих событий:\r\n\r\n- команда-победитель матча (первая, вторая или ничья)\r\n- точный счет матча\r\n- игрок команды, которая забьет гол во время матча (можно выбрать только одного из одной из команд)\r\n- команда-победитель ЧМ-2022\r\n  - Если по истечении 90 минут счет равен и идет на 2 овертайма, то при подсчете очков будет учитываться счет после завершения 2 овертаймов, т.е. после 120-й минуты.\r\n\r\nНакапливайте баллы за правильный прогноз:\r\n\r\n• Если вы угадаете команду-победителя матча, получите 1 очко;\r\n• Если вы угадали игрока, который забьет гол в матче, получите 1 очко;\r\n• Если вы угадываете окончательный счет игры, вы получаете 3 очка;\r\n• Если вы угадали финального победителя ЧМ-2022, сделав прогноз;\r\n* До 20 ноября – 50 баллов\r\n* До 3 декабря – 20 баллов\r\n* До 9 декабря – 10 баллов\r\n• Если вы не угадываете ни победителя, ни счет, ни игрока, который забьет, вы получаете 0 очков;\r\n• За незавершенные совпадения будет считаться отсутствие ответа, т.е. 0 баллов.\r\n• Если матч начался, вы больше не можете делать прогнозы на этот матч.\r\n\r\nКакие призы я могу выиграть?\r\nМы разыгрываем один главный приз — консоль Xbox Series X емкостью 1 ТБ и два небольших приза — беспроводную гарнитуру HyperX Cloud Stinger Core 7.1 и гарнитуру Monster Clarity 100 Airlinks Black!\r\n\r\nОкончательный рейтинг будет основан на баллах, набранных участниками, в порядке убывания. Таким образом, главный приз получит тот, кто наберет наибольшее количество баллов.\r\n\r\nВ случае ничьей побеждает тот, кто записал последний прогноз ранее.");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(currentUser.UserID, "When and how do I enter the contest?\r\n\r\nYou can enter the prize rush whenever your heart desires!\r\n\r\nThe only condition is that it is between 20 November and 18 December 2022 (the duration of the 2022 World Cup).\r\n\r\nAll you need is: Moldcell number and Telegram app\r\n\r\nWhat to do?\r\n\r\nOpen Telegram Chat Bot via the link: http://t.me/FWChampionshipBot\r\n\r\nRegister with your name, surname and phone number\r\n\r\nCreate predictions for the following events:\r\n\r\n- the winning team of the match (first, second or tied)\r\n- the exact score of the match\r\n- the player of the team that will score a goal during the match (you can only choose one from one of the teams)\r\n- the winning team of the 2022 World Cup\r\n  - If after the 90 minutes the score is equal and it goes to the 2 overtime periods, then the score after the completion of the 2 overtime periods, i.e. after the 120th minute, will be taken into account when calculating the score.\r\n\r\nAccumulate points for a correct prediction:\r\n\r\n• If you guess the winning team of the match, collect 1 point;\r\n• If you guess the player who will score a goal in the match, collect 1 point;\r\n• If you guess the final score of the game, you collect 3 points;\r\n• If you guess the final winner of the World Cup 2022 by making a prediction;\r\n* Until November 20 – 50 points\r\n* Until December 3 – 20 points\r\n* Until December 9 – 10 points\r\n• If you guess neither the winner, nor the score, nor the player who will score, you accumulate 0 points;\r\n• For uncompleted matches, a missing answer will be considered, i.e. 0 points.\r\n• If the match has started, you can no longer make predictions for this match.\r\n\r\nWhat prizes can I win?\r\nWe're giving away one grand prize - the Xbox Series X 1TB console and two small prizes - the HyperX Cloud Stinger Core 7.1 Wireless Headset and the Monster Clarity 100 Airlinks Black Headset!\r\n\r\nThe final ranking will be based on the points accumulated by the participants, in descending order. Thus, the Grand Prize will be won by the one with the most accumulated points.\r\n\r\nIn case of a tie, the one who recorded the last prediction earlier wins.");
                        }
                        return;
                    }
                    // inserting total score of the match
                    string[] totalScore = message.Text.Split('/', ':');
                    if (currentUser.IntroduceTotal == true && totalScore.Length == 2)
                    {
                        InlineKeyboardMarkup menuKeyboard;
                        if (currentUser.Language == "ro")
                        {
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu")
                                }
                            });
                        }
                        else if (currentUser.Language == "ru")
                        {
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu")
                                }
                            });
                        }
                        else
                        {
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
                                }
                            });
                        }

                        if (totalScore[0] != null && totalScore[1] != null && totalScore[0] != "" && totalScore[1] != "")
                        {
                            if (int.TryParse(totalScore[0].Trim(), out number) && int.TryParse(totalScore[1].Trim(), out number) && Convert.ToInt32(totalScore[0].Trim()) >= 0 && Convert.ToInt32(totalScore[1].Trim()) >= 0)
                            {
                                if (currentUser.Language == "ro")
                                {
                                    var menuKeyboardVote = new InlineKeyboardMarkup(new[] {new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Inapoi", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu")
                                }
                            });
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    var menuKeyboardVote = new InlineKeyboardMarkup(new[] {new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Назад", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu")
                                }
                            });
                                }
                                else
                                {
                                    var menuKeyboardVote = new InlineKeyboardMarkup(new[] {new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Back", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
                                }
                            });
                                }

                                currentUser.IntroduceTotal = false;
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 4, Convert.ToInt32(totalScore[0]), Convert.ToInt32(totalScore[1]), null, null);
                                await botClient.SendTextMessageAsync(currentUser.UserID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                                await botClient.DeleteMessageAsync(currentUser.UserID, (int)currentUser.MessageID);
                            }
                            else if (Convert.ToInt32(totalScore[0].Trim()) < 0 || Convert.ToInt32(totalScore[1].Trim()) < 0)
                            {
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.SendTextMessageAsync(currentUser.UserID, "Scorul nu poate fi negativ");
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.SendTextMessageAsync(currentUser.UserID, "Счёт не может быть отрицательным");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(currentUser.UserID, "The score cannot be negative");
                                }
                            }
                            else
                            {
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.SendTextMessageAsync(currentUser.UserID, "Ati introdus un scor incorect, mai incercati");
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.SendTextMessageAsync(currentUser.UserID, "Вы ввели неверный счет, попробуйте еще раз");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(currentUser.UserID, "You have entered an incorrect score, try again");
                                }
                            }
                            return;
                        }
                        else
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(currentUser.UserID, "Ati introdus un scor incorect, mai incercati");
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(currentUser.UserID, "Вы ввели неверный счет, попробуйте еще раз");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(currentUser.UserID, "You have entered an incorrect score, try again");
                            }
                        }
                        return;
                    }
                    // Registration method
                    if (message.Text != null && currentUser.Registration == true)
                    {
                        var registrationData = message.Text.Trim();
                        string[] subs = registrationData.Split('-', '_', '.');

                        if (subs.Length == 3)
                        {
                            if (Regex.Match(subs[0].Trim(), @"^[a-zA-Z]").Success && Regex.Match(subs[1].Trim(), @"^[a-zA-Z]").Success && int.TryParse(subs[2], out number))
                            {
                                if (Convert.ToInt32(subs[2].Trim().Length) >= 8 && Convert.ToInt32(subs[2].Trim().Length) < 10)
                                {
                                    var resultNewVoter = Controller.SetNewVoter(subs[0].Trim(), subs[1].Trim(), subs[2].Trim(), currentUser.Language, Convert.ToInt64(message.Chat.Id));
                                    if (resultNewVoter)
                                    {
                                        //Console.WriteLine("Success");
                                        currentUser.Registration = false;
                                        // TODO
                                        InlineKeyboardMarkup menuKeyboard;
                                        if (currentUser.Language == "ro")
                                        {
                                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Introduceti prognostic", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Topul participantilor", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("Istoria schimbarilor","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Descriere", "description"), InlineKeyboardButton.WithCallbackData("Reguli","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Setari", "settings")}});
                                            await botClient.SendTextMessageAsync(message.Chat.Id, "V-ati inregistrat cu success", replyMarkup: menuKeyboard);
                                        }
                                        else if (currentUser.Language == "ru")
                                        {
                                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Введите прогноз", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Список топ участников", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("История", "prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Описание", "description"), InlineKeyboardButton.WithCallbackData("Правила", "rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Настройки", "settings")}});
                                            await botClient.SendTextMessageAsync(message.Chat.Id, "Вы успешно зарегистрировались", replyMarkup: menuKeyboard);
                                        }
                                        else
                                        {
                                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Top Voters","topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("History","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Description", "description"), InlineKeyboardButton.WithCallbackData("Rules","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Settings", "settings")}});
                                            await botClient.SendTextMessageAsync(message.Chat.Id, "You have registred successfully", replyMarkup: menuKeyboard);
                                        }

                                        //currentUser.registration = false;
                                    }
                                }
                                else if (Convert.ToInt32(subs[2].Trim().Length) < 8)
                                {
                                    if (currentUser.Language == "ro")
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat.Id, "Numărul dvs. nu conține suficiente cifre, vă rugăm să încercați din nou");
                                    }
                                    else if (currentUser.Language == "ru")
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat.Id, "Bаш номер не содержит достаточного количества цифр, попробуйте еще раз");
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat.Id, "Your number does not contain enough digits");
                                    }
                                }
                                else if (Convert.ToInt32(subs[2].Trim().Length) > 10)
                                {
                                    if (currentUser.Language == "ro")
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat.Id, "Numărul dvs. conține prea multe cifre, vă rugăm să încercați din nou");
                                    }
                                    else if (currentUser.Language == "ru")
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat.Id, "Ваш номер содержит слишком много цифр, попробуйте еще раз");
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat.Id, "Your number contains too many digits");
                                    }
                                }
                                return;
                            }
                            else if (!Regex.Match(subs[0].Trim(), @"^[a-zA-Z]").Success || !Regex.Match(subs[1].Trim(), @"^[a-zA-Z]").Success)
                            {
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ați introdus un nume incorect");
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вы ввели неверное имя");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, "You have entered an incorrect name");
                                }
                                return;
                            }
                            else
                            {
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ați introdus un număr de telefon incorect, vă rugăm să încercați din nou");
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вы ввели неверный номер телефона, попробуйте еще раз");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, "You have entered an incorrect phone number, please try again");
                                }
                                return;
                            }
                        }
                        else
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Nu ați introdus suficiente date, încercați din nou");
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы ввели недостаточно данных, попробуйте еще раз");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "You have not entered enough data, try again");
                            }
                            return;
                        }
                    }
                    
                }
                else // when users.UsersList is not exist is executing this block
                {
                    // /menu or /start command
                    if (message.Text == "/start" || message.Text == "/menu")
                    {
                        // including in the database a new users.UsersList
                        Controller.SetNewVoter(null, null, "en", null, message.Chat.Id);
                        // creating a 2 menu keyboards
                        // 1 - for menu with menu buttons
                        // 2 - for selecting language menu if is new users.UsersList

                        // Verify if users.UsersList is exists
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && existentUser.Language != null && existentUser.FirstName != null && existentUser.FirstName != "" && existentUser.LastName != null && existentUser.LastName != ""))
                        {
                            Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == message.Chat.Id).First();
                            InlineKeyboardMarkup menuKeyboard;
                            if (currentUser.Language == "ro")
                            {
                                menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Introduceti prognostic", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Topul participantilor", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("Istoria schimbarilor","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Descriere", "description"), InlineKeyboardButton.WithCallbackData("Reguli","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Setari", "settings")}});
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Meniu principal", replyMarkup: menuKeyboard);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Введите прогноз", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Список топ участников", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("История", "prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Описание", "description"), InlineKeyboardButton.WithCallbackData("Правила", "rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Настройки", "settings")}});
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Главное меню", replyMarkup: menuKeyboard);
                            }
                            else
                            {
                                menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Top Voters","topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("History","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Description", "description"), InlineKeyboardButton.WithCallbackData("Rules","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Settings", "settings")}});
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Main Menu", replyMarkup: menuKeyboard);
                            }
                        }
                        // verify if users.UsersList has a language
                        else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.Language == null || existentUser.Language == "")))
                        {
                            Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == message.Chat.Id).First();
                            InlineKeyboardMarkup menuKeyboardSelectLanguage;
                            if (currentUser.Language == "ro")
                            {
                                menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
    {
                            InlineKeyboardButton.WithCallbackData("Romana","language-ro"),
                            InlineKeyboardButton.WithCallbackData("Rusa","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Engleza","language-en")
                        });
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Alege limba", replyMarkup: menuKeyboardSelectLanguage);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                                {
                            InlineKeyboardButton.WithCallbackData("Румынский","language-ro"),
                            InlineKeyboardButton.WithCallbackData("Русский","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Английский","language-en")
                        });
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Настройки языка", replyMarkup: menuKeyboardSelectLanguage);
                            }
                            else
                            {
                                menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                                {
                            InlineKeyboardButton.WithCallbackData("Romanian","language-ro"),
                            InlineKeyboardButton.WithCallbackData("Russian","language-ru"),
                            InlineKeyboardButton.WithCallbackData("English","language-en")
                        });
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Select Language", replyMarkup: menuKeyboardSelectLanguage);
                            }
                        }
                        // verify if users.UsersList is not registred and display message try register
                        else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.FirstName == null || existentUser.FirstName == "") && (existentUser.LastName == null || existentUser.LastName == "")))
                        {
                            Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == message.Chat.Id).First();
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Încercați comanda /register");
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Попробуйте команду /register");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Try command /register");
                            }
                        }
                        return;
                    }
                    return;
                }
            }
            catch(Exception exception)
            {
                var errorMessage = exception switch
                {
                    // catch an api error, Bigger exception is { <- 400 - the message text is not changed -> }
                    ApiRequestException apiRequestException
                        => $"Telegram api error: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };
                Console.WriteLine(exception);

                _ = LoggClient.SendMessage(exception.Message);
                logger.Error(errorMessage);
                 Controller.SetLoggs(null, null, null, exception.Message);
            }
            return;
        }

        public async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery cq)
        {
            try
            {
                Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == cq.Message.Chat.Id).First();
                // verify if users.UsersList exists in database
                currentUser.CallBackData = cq.Data;
                currentUser.MessageID = cq.Message.MessageId;
                if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id && existentUser.FirstName != null && existentUser.FirstName != "" && existentUser.LastName != null && existentUser.LastName != ""))
                {
                    
                    

                    currentUser.Registration = false;
                    currentUser.IntroduceTotal = false;
                    currentUser.ReportAProblem = false;
                    // Get Main menu interface //
                    if (currentUser.CallBackData == "mainMenu")
                    {
                        if (currentUser.Language == null)
                        {
                            InlineKeyboardMarkup menuKeyboardSelectLanguage;
                            if (currentUser.Language == "ro")
                            {
                                menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                            {
                            InlineKeyboardButton.WithCallbackData("Romana","language-ro"),
                            InlineKeyboardButton.WithCallbackData("Rusa","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Engleza","language-en")
                        });
                                await botClient.EditMessageTextAsync(cq.Message.Chat.Id, (int)currentUser.MessageID, "Setarea limbii" /*"Select Language:"*/, replyMarkup: menuKeyboardSelectLanguage);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                                                        {
                            InlineKeyboardButton.WithCallbackData("Румынский","language-ro"),
                            InlineKeyboardButton.WithCallbackData("Русский","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Английский","language-en")
                        });
                                await botClient.EditMessageTextAsync(cq.Message.Chat.Id, (int)currentUser.MessageID, "Настройки языка" /*"Select Language:"*/, replyMarkup: menuKeyboardSelectLanguage);
                            }
                            else
                            {
                                menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                            {
                            InlineKeyboardButton.WithCallbackData("Romanian","language-ro"),
                            InlineKeyboardButton.WithCallbackData("Russian","language-ru"),
                            InlineKeyboardButton.WithCallbackData("English","language-en")
                        });
                                await botClient.EditMessageTextAsync(cq.Message.Chat.Id, (int)currentUser.MessageID, "Select Language" /*"Select Language:"*/, replyMarkup: menuKeyboardSelectLanguage);
                            }

                            return;
                        }
                        else
                        {
                            InlineKeyboardMarkup menuKeyboard;
                            if (currentUser.Language == "ro")
                            {
                                menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Introduceti prognostic", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Topul participantilor", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("Istoria schimbarilor","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Descriere", "description"), InlineKeyboardButton.WithCallbackData("Reguli","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Setari", "settings")}});
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Meniu principal.", replyMarkup: menuKeyboard);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Meniu principal", replyMarkup: menuKeyboard);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Введите прогноз", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Список топ участников", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("История", "prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Описание", "description"), InlineKeyboardButton.WithCallbackData("Правила", "rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Настройки", "settings")}});
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Главное меню.", replyMarkup: menuKeyboard);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Главное меню", replyMarkup: menuKeyboard);
                            }
                            else
                            {
                                menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Top Voters","topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("History","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Description", "description"), InlineKeyboardButton.WithCallbackData("Rules","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Settings", "settings")}});
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Main Menu.", replyMarkup: menuKeyboard);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Main Menu", replyMarkup: menuKeyboard);
                            }

                            return;
                        }
                    }
                    // Get Top voters list //
                    if (currentUser.CallBackData == "topVoters")
                    {
                        currentUser.CurentTopVotersPage = 0;
                        Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);

                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                        }

                        return;
                    }
                    // Register a new voter or update old //
                    if (currentUser.CallBackData == "register")
                    {
                        currentUser.Registration = true;
                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(cq.Message.Chat.Id, (int)currentUser.MessageID, $"Introduceți numele, prenumele și numarul de telefon conform modelului: Ivan - Ivanov - 79800000");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(cq.Message.Chat.Id, (int)currentUser.MessageID, $"Введите имя, фамилию и номер телефона по образцу: Ivan - Ivanov - 79800000");
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(cq.Message.Chat.Id, (int)currentUser.MessageID, $"Enter your name, surname and phone number according to the model: Ivan - Ivanov - 79800000");
                        }
                        return;
                    }
                    // Show rules page //
                    if (currentUser.CallBackData == "rules")
                    {
                        var menuKeyboardRules = new InlineKeyboardMarkup(new[]{
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                            }
                            });
                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Când și cum mă înscriu în concurs?\r\n\r\nPoți intra în goana cu premii oricând dorește inima ta!\r\n\r\nSingura condiție este ca acesta să fie între 20 noiembrie și 18 decembrie 2022 (durata Cupei Mondiale 2022).\r\n\r\nTot ce aveți nevoie este: numărul Moldcell și aplicația Telegram\r\n\r\nCe să fac?\r\n\r\nDeschide Telegram Chat Bot prin linkul: http://t.me/FWChampionshipBot\r\n\r\nÎnregistrați-vă cu numele, prenumele și numărul de telefon\r\n\r\nCreați predicții pentru următoarele evenimente:\r\n\r\n- echipa câștigătoare a meciului (prima, a doua sau la egalitate)\r\n- scorul exact al meciului\r\n- jucătorul echipei care va marca un gol în timpul meciului (puteți alege doar unul din una dintre echipe)\r\n- echipa câștigătoare a Cupei Mondiale 2022\r\n - In cazul in care după cele 90 de minute scorul este egal și se trece la cele 2 reprize de prelungiri, atunci la calcularea punctajului se va lua în considerare scorul de după finalizarea celor 2 reprize de prelungiri, adică după minutul 120.\r\n\r\nAcumulați puncte în cazul unei predicții corecte:\r\n\r\n• Dacă ghiciți echipa câștigătoare a meciului, acumulați 1 punct;\r\n• Dacă ghiciți jucătorul care va înscrie un gol în meci, acumulați 1 punct;\r\n• Dacă ghiciți scorul final al jocului, acumulați 3 puncte;\r\n• Dacă ghiciți câștigătorul final al Cupei Mondiale 2022 făcând un pronostic;\r\n* Până pe 20 noiembrie – 50 de puncte\r\n* Până pe 3 decembrie – 20 puncte\r\n* Până pe 9 decembrie – 10 puncte\r\n• Dacă nu ghiciți nici câștigătorul, nici scorul, nici jucătorul care va înscrie, acumulați 0 puncte;\r\n• Pentru meciurile nefinalizate, va fi considerat un răspuns lipsă, adică 0 puncte.\r\n• Dacă meciul a început, nu mai puteți face pronosticuri pentru acest meci.\r\n\r\nCe premii pot câștiga?\r\nOferim un mare premiu - consola Xbox Series X 1TB și două premii mici - Căștile fără fir HyperX Cloud Stinger Core 7.1 și căștile Monster Clarity 100 Airlinks Black!\r\n\r\nClasamentul final se va face în funcție de punctele acumulate de participanți, în ordine descrescătoare. Astfel, Marele Premiu va fi câștigat de cel cu cele mai multe puncte acumulate.\r\n\r\nÎn caz de egalitate, câștigă cel care a înregistrat mai devreme ultimul pronostic.", replyMarkup: menuKeyboardRules);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Когда и как я могу принять участие в конкурсе?\r\n\r\nВы можете принять участие в розыгрыше призов, когда вашей душе угодно!\r\n\r\nЕдинственное условие — это период с 20 ноября по 18 декабря 2022 года (продолжительность чемпионата мира по футболу 2022 года).\r\n\r\nВсе, что вам нужно, это: номер Moldcell и приложение Telegram.\r\n\r\nЧто делать?\r\n\r\nОткройте Telegram Chat Bot по ссылке: http://t.me/FWChampionshipBot\r\n\r\nЗарегистрируйтесь под своим именем, фамилией и номером телефона\r\n\r\nСоздайте прогнозы для следующих событий:\r\n\r\n- команда-победитель матча (первая, вторая или ничья)\r\n- точный счет матча\r\n- игрок команды, которая забьет гол во время матча (можно выбрать только одного из одной из команд)\r\n- команда-победитель ЧМ-2022\r\n  - Если по истечении 90 минут счет равен и идет на 2 овертайма, то при подсчете очков будет учитываться счет после завершения 2 овертаймов, т.е. после 120-й минуты.\r\n\r\nНакапливайте баллы за правильный прогноз:\r\n\r\n• Если вы угадаете команду-победителя матча, получите 1 очко;\r\n• Если вы угадали игрока, который забьет гол в матче, получите 1 очко;\r\n• Если вы угадываете окончательный счет игры, вы получаете 3 очка;\r\n• Если вы угадали финального победителя ЧМ-2022, сделав прогноз;\r\n* До 20 ноября – 50 баллов\r\n* До 3 декабря – 20 баллов\r\n* До 9 декабря – 10 баллов\r\n• Если вы не угадываете ни победителя, ни счет, ни игрока, который забьет, вы получаете 0 очков;\r\n• За незавершенные совпадения будет считаться отсутствие ответа, т.е. 0 баллов.\r\n• Если матч начался, вы больше не можете делать прогнозы на этот матч.\r\n\r\nКакие призы я могу выиграть?\r\nМы разыгрываем один главный приз — консоль Xbox Series X емкостью 1 ТБ и два небольших приза — беспроводную гарнитуру HyperX Cloud Stinger Core 7.1 и гарнитуру Monster Clarity 100 Airlinks Black!\r\n\r\nОкончательный рейтинг будет основан на баллах, набранных участниками, в порядке убывания. Таким образом, главный приз получит тот, кто наберет наибольшее количество баллов.\r\n\r\nВ случае ничьей побеждает тот, кто записал последний прогноз ранее.", replyMarkup: menuKeyboardRules);
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "When and how do I enter the contest?\r\n\r\nYou can enter the prize rush whenever your heart desires!\r\n\r\nThe only condition is that it is between 20 November and 18 December 2022 (the duration of the 2022 World Cup).\r\n\r\nAll you need is: Moldcell number and Telegram app\r\n\r\nWhat to do?\r\n\r\nOpen Telegram Chat Bot via the link: http://t.me/FWChampionshipBot\r\n\r\nRegister with your name, surname and phone number\r\n\r\nCreate predictions for the following events:\r\n\r\n- the winning team of the match (first, second or tied)\r\n- the exact score of the match\r\n- the player of the team that will score a goal during the match (you can only choose one from one of the teams)\r\n- the winning team of the 2022 World Cup\r\n  - If after the 90 minutes the score is equal and it goes to the 2 overtime periods, then the score after the completion of the 2 overtime periods, i.e. after the 120th minute, will be taken into account when calculating the score.\r\n\r\nAccumulate points for a correct prediction:\r\n\r\n• If you guess the winning team of the match, collect 1 point;\r\n• If you guess the player who will score a goal in the match, collect 1 point;\r\n• If you guess the final score of the game, you collect 3 points;\r\n• If you guess the final winner of the World Cup 2022 by making a prediction;\r\n* Until November 20 – 50 points\r\n* Until December 3 – 20 points\r\n* Until December 9 – 10 points\r\n• If you guess neither the winner, nor the score, nor the player who will score, you accumulate 0 points;\r\n• For uncompleted matches, a missing answer will be considered, i.e. 0 points.\r\n• If the match has started, you can no longer make predictions for this match.\r\n\r\nWhat prizes can I win?\r\nWe're giving away one grand prize - the Xbox Series X 1TB console and two small prizes - the HyperX Cloud Stinger Core 7.1 Wireless Headset and the Monster Clarity 100 Airlinks Black Headset!\r\n\r\nThe final ranking will be based on the points accumulated by the participants, in descending order. Thus, the Grand Prize will be won by the one with the most accumulated points.\r\n\r\nIn case of a tie, the one who recorded the last prediction earlier wins.", replyMarkup: menuKeyboardRules);
                        }

                        return;
                    }
                    // Show description page //
                    if (currentUser.CallBackData == "description")
                    {
                        if (currentUser.Language == "ro")
                        {
                            var menuKeyboardDescription = new InlineKeyboardMarkup(new[]{
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Acest eveniment este creat în onoarea campionatului de fotbal din întreaga lume, sunteți binevenit\r\npoate participa oricine care face parte din echipa Moldcell.\r\nScopul este: Acumularea de puncte și ridicarea în vârf pentru a primi un premiu.\r\nCampionatul Mondial de Fotbal 2022 este organizat de 32 de echipe de fotbal în urma a 64 de meciuri între echipe\r\nEvenimentul dat nu este un joc de noroc sau un mediu în care va fi necesar să investești ceva.", replyMarkup: menuKeyboardDescription);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            var menuKeyboardDescription = new InlineKeyboardMarkup(new[]{
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Это мероприятие создано в честь футбольного чемпионата со всего мира, добро пожаловать\r\nпринять участие может любой, кто является частью команды Moldcell.\r\nЦель: Накопить очки и подняться на вершину, чтобы получить приз.\r\nЧемпионат мира по футболу 2022 года организован 32 футбольными командами после 64 игр между командами.\r\nДанное событие не является азартной игрой или средой, в которую нужно будет что-то вкладывать.", replyMarkup: menuKeyboardDescription);
                        }
                        else
                        {
                            var menuKeyboardDescription = new InlineKeyboardMarkup(new[]{
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "This event is created in honor of the football championship from all over the world, You are welcome\r\nanyone who is part of the Moldcell team can participate.\r\nThe goal is: Accumulating points and rising to the top to receive a prize.\r\nFootball-World-Championship year 2022 is organized by 32 football teams following 64 games between teams\r\nThe given event is not a game of chance or an environment in which it will be necessary to invest something.", replyMarkup: menuKeyboardDescription);
                        }

                        return;
                    }
                    // Show prognosedHistory page //
                    if (currentUser.CallBackData == "prognoseHistory")
                    {
                        currentUser.CurentHistoryPage = 0;
                        await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, currentUser.CurentHistoryPage) + ".", replyMarkup: users.MenuKeyboardHistory);
                        await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                        return;
                    }
                    // Show Settings menu //
                    if (currentUser.CallBackData == "settings")
                    {
                        if (currentUser.Language == "ro")
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                                                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Setarea limbii","selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Setari", replyMarkup: menuKeyboardSetting);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                                                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Настройки языка","selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Настройки", replyMarkup: menuKeyboardSetting);
                        }
                        else
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Select Language","selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Settings", replyMarkup: menuKeyboardSetting);
                        }

                        return;
                    }
                    // Show available languages from settings menu //
                    if (currentUser.CallBackData == "selectLanguage")
                    {
                        InlineKeyboardMarkup menuKeyboardSelectLanguage;
                        if (currentUser.Language == "ro")
                        {
                            menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Romana","language-ro"),
                        InlineKeyboardButton.WithCallbackData("Rusa","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Engleza","language-en")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Inapoi","backToSettingsMenu"),
                                InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Setarea limbii", replyMarkup: menuKeyboardSelectLanguage);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Румынский","language-ro"),
                        InlineKeyboardButton.WithCallbackData("Русский","language-ru"),
                            InlineKeyboardButton.WithCallbackData("Английский","language-en")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Назад","backToSettingsMenu"),
                                InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Настройки языка", replyMarkup: menuKeyboardSelectLanguage);
                        }
                        else
                        {
                            menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Romanian","language-ro"),
                        InlineKeyboardButton.WithCallbackData("Russian","language-ru"),
                            InlineKeyboardButton.WithCallbackData("English","language-en")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Back","backToSettingsMenu"),
                                InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Select Language", replyMarkup: menuKeyboardSelectLanguage);
                        }

                        return;
                    }
                    // Start voting //
                    if (currentUser.CallBackData == "playGame")
                    {
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                        {
                            if (currentUser.Language == "ro")
                            {
                                var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Alegerea meciului","selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Alege echipa câștigătoarea la Campionat Mondial de Fotbal 2022","selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                    }
                        });
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Poti selecta meciul ca sa primesti mai multe puncte in prognozarea castigului sau sa alegi echipa care va deveni castigatoare in final", replyMarkup: menuKeyboardPlayGame);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Выберите матч","selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Выберите команду-победителя Чемпионата Мира по Футболу 2022","selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                    }
                        });
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Вы можете выбрать матч, чтобы получить больше очков в прогнозировании победы или выбрать команду, которая в итоге станет победителем", replyMarkup: menuKeyboardPlayGame);
                            }
                            else
                            {
                                var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Select Match","selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Select the Winner of the Football World Cup 2022","selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                    }
                        });
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "You can select match and get many points or you can try to select who was win in final", replyMarkup: menuKeyboardPlayGame);
                            }
                        }
                        else
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.SendTextMessageAsync(currentUser.UserID, $"Înregistrați-vă vă rog /register");

                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.SendTextMessageAsync(currentUser.UserID, $"Зарегистрируйтесь, пожалуйста /register");

                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(currentUser.UserID, $"Register please /register");

                            }
                        }
                        return;
                    }
                    // Select match menu, Display match List //
                    if (currentUser.CallBackData == "selectWinner")
                    {
                        currentUser.CurentMatchPage = (currentUser.CurentMatchPage == null || currentUser.CurentMatchPage == 0) ? 0 : currentUser.CurentMatchPage;
                        Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                        }

                        return;
                    }
                    // Display vote options in Tis match //
                    if (currentUser.CallBackData.Contains("MatchID"))
                    {
                        if (cq.Data.Substring(7) != "" && cq.Data.Substring(7) != null)
                        {
                            currentUser.MatchID = Convert.ToInt32(cq.Data.Substring(7));
                            Controller.ChangeSelectMatchPage(currentUser.UserID, (int)currentUser.MatchID);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                        }
                        else
                        {
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Ceva n-a mers bine", replyMarkup: users.MenuKeyboardSelectMatch);
                                await Task.Delay(500);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Что-то пошло не так", replyMarkup: users.MenuKeyboardSelectMatch);
                                await Task.Delay(500);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Something went wrong", replyMarkup: users.MenuKeyboardSelectMatch);
                                await Task.Delay(500);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                            }
                        }

                        return;
                    }
                    // Display list of Teams when anyone can select them just 1 //
                    if (currentUser.CallBackData == "selectWinnerTeam")
                    {
                        currentUser.CurentTeamPage = currentUser.CurentTeamPage = (currentUser.CurentTeamPage == null || currentUser.CurentTeamPage == 0) ? 0 : currentUser.CurentTeamPage;
                        Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                        Controller.GetVoters(currentUser.UserID);
                        // TODO: Change Language
                        if (currentUser.Language == "ro")
                        {
                            if (currentUser.VotedFinalTeam != null || currentUser.VotedFinalTeam != "") await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final:\nAti ales: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            else await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            if (currentUser.VotedFinalTeam != null || currentUser.VotedFinalTeam != "") await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022:\nВы выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            else await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                        }
                        else
                        {
                            if (currentUser.VotedFinalTeam != null || currentUser.VotedFinalTeam != "") await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team:\nYou have select: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            else await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                        }

                        return;
                    }
                    // Return to start voting page //
                    if (currentUser.CallBackData == "backToPlayMenu")
                    {
                        if (currentUser.Language == "ro")
                        {
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Alegerea meciului","selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Alege echipa câștigătoarea la Campionat Mondial de Fotbal 2022","selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                    }
                        });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Poti selecta meciul ca sa primesti mai multe puncte in prognozarea castigului sau sa alegi echipa care va deveni castigatoare in final", replyMarkup: menuKeyboardPlayGame);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Выберите матч","selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Выберите команду-победителя Чемпионата Мира по Футболу 2022","selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                    }
                        });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Вы можете выбрать матч, чтобы получить больше очков в прогнозировании победы или выбрать команду, которая в итоге станет победителем", replyMarkup: menuKeyboardPlayGame);
                        }
                        else
                        {
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Select Match","selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Select the Winner of the Football World Cup 2022","selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                    }
                        });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "You can select match and get many points or you can try to select who was win in final", replyMarkup: menuKeyboardPlayGame);
                        }

                        return;
                    }
                    // return to settings menu //
                    if (currentUser.CallBackData == "backToSettingsMenu")
                    {
                        if (currentUser.Language == "ro")
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                                                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Setarea limbii","selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Setari", replyMarkup: menuKeyboardSetting);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                                                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Настройки языка","selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Настройки", replyMarkup: menuKeyboardSetting);
                        }
                        else
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Select Language","selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Settings", replyMarkup: menuKeyboardSetting);
                        }

                        return;
                    }
                    // return to match list menu //
                    if (currentUser.CallBackData == "backToWinnerMenu")
                    {
                        currentUser.CurentMatchPage = (currentUser.CurentMatchPage == null || currentUser.CurentMatchPage == 0) ? 0 : currentUser.CurentMatchPage;
                        Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                        }
                        return;
                    }
                    // return to Vote menu //
                    if (currentUser.CallBackData == "backToSelectedMatchMenu")
                    {
                        currentUser.CurentMatchPage = (currentUser.CurentMatchPage == null || currentUser.CurentMatchPage == 0) ? 0 : currentUser.CurentMatchPage;
                        if (currentUser.MatchID != null)
                        {
                            Controller.ChangeSelectMatchPage(currentUser.UserID, (int)currentUser.MatchID);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                            return;
                        }
                        // TODO: Change Language
                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Ceva n-a mers bine", replyMarkup: users.MenuKeyboardSelectMatch);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Что-то пошло не так", replyMarkup: users.MenuKeyboardSelectMatch);
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Something went wrong", replyMarkup: users.MenuKeyboardSelectMatch);
                        }
                        return;
                    }
                    // Select pages from list who have pagination //
                    if (currentUser.CallBackData.Contains("FirstPage") || currentUser.CallBackData.Contains("PreventPage") || currentUser.CallBackData.Contains("NextPage") || currentUser.CallBackData.Contains("LastPage"))
                    {
                        //if (paginationType == 1) // Change page to selectWinnerTeam

                        if (cq.Data == "WinnerTeam FirstPage")
                        {
                            currentUser.CurentTeamPage = 0;
                            Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final.\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022.\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team.\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            }


                            return;
                        }
                        if (cq.Data == "WinnerTeam PreventPage")
                        {
                            if (currentUser.CurentTeamPage >= 1)
                            {
                                currentUser.CurentTeamPage -= 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final.\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022.\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team.\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }


                                return;
                            }
                            else
                            {
                                currentUser.CurentTeamPage = pages.totalTeamPages - 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final.\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022.\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team.\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }


                                return;
                            }
                        }
                        if (cq.Data == "WinnerTeam NextPage")
                        {
                            if (currentUser.CurentTeamPage < pages.totalTeamPages - 1)
                            {
                                currentUser.CurentTeamPage += 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final.\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022.\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team.\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }


                                return;
                            }
                            else
                            {
                                currentUser.CurentTeamPage = 0;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final.\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022.\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team.\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                }


                                return;
                            }
                        }
                        if (cq.Data == "WinnerTeam LastPage")
                        {
                            currentUser.CurentTeamPage = pages.totalTeamPages - 1;
                            Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final.\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Alegeti echipa care va iesi invingatoare in final\nAti ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022.\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Выберите команду, которая выиграет чемпионат мира по футболу 2022\nВы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team.\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register the final winner team\nYou have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                            }


                            return;
                        }

                        //if (paginationType == 2) // Change page to select Winner From Match

                        if (cq.Data == "Winner FirstPage")
                        {
                            currentUser.CurentMatchPage = 0;
                            Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                            }

                            return;
                        }
                        if (cq.Data == "Winner PreventPage")
                        {
                            if (currentUser.CurentMatchPage >= 1)
                            {
                                currentUser.CurentMatchPage -= 1;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                                }

                                return;
                            }
                            else
                            {
                                currentUser.CurentMatchPage = pages.totalMatchPages - 1;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                                }

                                return;
                            }
                        }
                        if (cq.Data == "Winner NextPage")
                        {
                            if (currentUser.CurentMatchPage < pages.totalMatchPages - 1)
                            {
                                currentUser.CurentMatchPage += 1;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                                }

                                return;
                            }
                            else
                            {
                                currentUser.CurentMatchPage = 0;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                                }

                                return;
                            }
                        }
                        if (cq.Data == "Winner LastPage")
                        {
                            currentUser.CurentMatchPage = pages.totalMatchPages - 1;
                            Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci.", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Inregistrati prognoza pentru acest meci", replyMarkup: users.MenuKeyboardSelectWinner);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч.", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Запишите прогноз на этот матч", replyMarkup: users.MenuKeyboardSelectWinner);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches.", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Register prognose for this matches", replyMarkup: users.MenuKeyboardSelectWinner);
                            }

                            return;
                        }

                        //if (paginationType == 3) // Change page to select players

                        if (cq.Data == "Players FirstPage")
                        {
                            currentUser.CurentPlayersPage = 0;
                            Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);

                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                            }

                            return;
                        }
                        if (cq.Data == "Players PreventPage")
                        {
                            if (currentUser.CurentPlayersPage >= 1)
                            {
                                currentUser.CurentPlayersPage -= 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);

                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }

                                return;
                            }
                            else
                            {
                                currentUser.CurentPlayersPage = pages.totalPlayersPages - 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }

                                return;
                            }
                        }
                        if (cq.Data == "Players NextPage")
                        {
                            if (currentUser.CurentPlayersPage < pages.totalPlayersPages - 1)
                            {
                                currentUser.CurentPlayersPage += 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }

                                return;
                            }
                            else
                            {
                                currentUser.CurentPlayersPage = 0;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                }

                                return;
                            }
                        }
                        if (cq.Data == "Players LastPage")
                        {
                            currentUser.CurentPlayersPage = pages.totalPlayersPages - 1;
                            Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                            }

                            return;
                        }

                        //if (paginationType == 4) // Change page to select topVoters

                        if (cq.Data == "TopVoters FirstPage")
                        {
                            currentUser.CurentTopVotersPage = 0;
                            Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            }

                            return;
                        }
                        if (cq.Data == "TopVoters PreventPage")
                        {
                            if (currentUser.CurentTopVotersPage >= 1)
                            {
                                currentUser.CurentTopVotersPage -= 1;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }

                                return;
                            }
                            else
                            {
                                currentUser.CurentTopVotersPage = pages.totalTopVotersPages - 1;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }

                                return;
                            }
                        }
                        if (cq.Data == "TopVoters NextPage")
                        {
                            if (currentUser.CurentTopVotersPage < pages.totalTopVotersPages - 1)
                            {
                                currentUser.CurentTopVotersPage += 1;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }

                                return;
                            }
                            else
                            {
                                currentUser.CurentTopVotersPage = 0;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                }

                                return;
                            }
                        }
                        if (cq.Data == "TopVoters LastPage")
                        {
                            currentUser.CurentTopVotersPage = pages.totalTopVotersPages - 1;
                            Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                            if (currentUser.Language == "ro")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Clasament participantilor \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Рейтинг участников \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results.\n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Participants results \n" + Controller.GetVoterPlace(currentUser.UserID), replyMarkup: users.MenuKeyboardSelectTopVoter);
                            }

                            return;
                        }

                        //if (paginationType == 5) // Change page to select history

                        if (cq.Data == "History FirstPage")
                        {
                            currentUser.CurentHistoryPage = 0;
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage) + ".", replyMarkup: users.MenuKeyboardHistory);
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                        }
                        if (cq.Data == "History PreventPage")
                        {
                            if (currentUser.CurentHistoryPage >= 1)
                            {
                                currentUser.CurentHistoryPage -= 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                                return;
                            }
                            else
                            {
                                if (currentUser.CurentHistoryPage == pages.totalHistoryPages - 1) await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage) + ".", replyMarkup: users.MenuKeyboardHistory);
                                currentUser.CurentHistoryPage = pages.totalHistoryPages - 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                                return;
                            }
                        }
                        if (cq.Data == "History NextPage")
                        {
                            if (currentUser.CurentHistoryPage < pages.totalHistoryPages - 1)
                            {
                                currentUser.CurentHistoryPage += 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                                return;
                            }
                            else
                            {
                                if (currentUser.CurentHistoryPage == 0) await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"{Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage)}.", replyMarkup: users.MenuKeyboardHistory);
                                currentUser.CurentHistoryPage = 0;
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                                return;
                            }
                        }
                        if (cq.Data == "History LastPage")
                        {
                            if (pages.totalHistoryPages != 0)
                            {
                                currentUser.CurentHistoryPage = pages.totalHistoryPages - 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage) + ".", replyMarkup: users.MenuKeyboardHistory);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: users.MenuKeyboardHistory);
                                return;
                            }
                            else
                            {
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Nu exista asa pagina.", replyMarkup: users.MenuKeyboardHistory);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Nu exista asa pagina", replyMarkup: users.MenuKeyboardHistory);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Страница не существует.", replyMarkup: users.MenuKeyboardHistory);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Страница не существует", replyMarkup: users.MenuKeyboardHistory);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "There is no such page.", replyMarkup: users.MenuKeyboardHistory);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "There is no such page", replyMarkup: users.MenuKeyboardHistory);
                                }
                                return;
                            }
                        }
                    }
                    // Select vote for final team //
                    if (currentUser.CallBackData.Contains("TeamID"))
                    {
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                        {
                            int teamID = Convert.ToInt32(cq.Data.Substring(6));
                            Controller.VoteFinalTeam(currentUser.UserID, teamID);
                            Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), 64, 6, null, null, null, teamID);
                            Controller.GetVoters(currentUser.UserID);
                            if (currentUser.Language == "ro")
                            {
                                var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{ new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Inapoi", "selectWinnerTeam"),
                                    InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                                }
                            });
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Ati ales cu success: " + currentUser.VotedFinalTeam, replyMarkup: menuKeyboardPlayGame);
                            }
                            else if (currentUser.Language == "ru")
                            {
                                var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{ new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Назад", "selectWinnerTeam"),
                                    InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                                }
                            });
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Вы успешно выбрали: " + currentUser.VotedFinalTeam, replyMarkup: menuKeyboardPlayGame);
                            }
                            else
                            {
                                var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{ new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Back", "selectWinnerTeam"),
                                    InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                                }
                            });
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "You have voted successfully: " + currentUser.VotedFinalTeam, replyMarkup: menuKeyboardPlayGame);
                            }

                            return;
                        }
                        if (currentUser.Language == "ro")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Înregistrați-vă vă rog /register");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Зарегистрируйтесь, пожалуйста /register");
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Please Register /register");
                        }
                        return;
                    }
                    // Menu with vote options //
                    if (currentUser.CallBackData.Contains("Vote"))
                    {
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                        {
                            InlineKeyboardMarkup menuKeyboardVote;
                            if (currentUser.Language == "ro")
                            {
                                menuKeyboardVote = new InlineKeyboardMarkup(new[] {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Inapoi", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Meniu principal","mainMenu")
                                }
                            }
                                );
                            }
                            else if (currentUser.Language == "ru")
                            {
                                menuKeyboardVote = new InlineKeyboardMarkup(new[] {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Назад", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Главное меню","mainMenu")
                                }
                            }
                                );
                            }
                            else
                            {
                                menuKeyboardVote = new InlineKeyboardMarkup(new[] {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Back", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                                }
                            }
                                                            );
                            }

                            if (cq.Data.Contains("VoteFirstTeamName"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(19));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 1, null, null, null, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);

                                return;
                            }
                            if (cq.Data.Contains("VoteSecondTeamName"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(20));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 2, null, null, null, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                                return;
                            }
                            if (cq.Data.Contains("VoteEqual"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(11));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 3, null, null, null, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                                return;
                            }
                            if (cq.Data.Contains("VoteTotal"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(11));
                                currentUser.IntroduceTotal = true;
                                if (currentUser.Language == "ro")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Pentru echipele {currentUser.VotedFirstTeam} si {currentUser.VotedSecondTeam} \nIntroduceti scorul final dupa modelul: 4/3", replyMarkup: menuKeyboardVote);
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Для команд {currentUser.VotedFirstTeam} и {currentUser.VotedSecondTeam} \nВведите финальный счёт игры согласно образца: 4/3", replyMarkup: menuKeyboardVote);
                                }
                                else
                                {
                                    await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"For teams {currentUser.VotedFirstTeam} and {currentUser.VotedSecondTeam} \nIntroduce final score by model: 4/3", replyMarkup: menuKeyboardVote);
                                }
                                return;
                            }
                            if (cq.Data.Contains("VotePlayers"))
                            {
                                currentUser.VotedPlayerTeam = currentUser.CallBackData.Split('+')[1];
                                try
                                {
                                    currentUser.CurentPlayersPage = 0;
                                    currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Split('+')[2]);
                                    Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, currentUser.CurentPlayersPage);
                                    if (currentUser.Language == "ro")
                                    {
                                        await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Selectați un jucător din echipă {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    }
                                    else if (currentUser.Language == "ru")
                                    {
                                        await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Выберите игрока из команды {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    }
                                    else
                                    {
                                        await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    }
                                }
                                catch (Exception exception)
                                {
                                    _ = LoggClient.SendMessage(exception.Message);
                                    logger.Error(exception);
                                     Controller.SetLoggs(null, null, null, exception.Message);
                                }

                                return;
                            }
                            if (cq.Data.Contains("VotePlayerID"))
                            {
                                int PlayerID = Convert.ToInt32(currentUser.CallBackData.Substring(12));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 5, null, null, PlayerID, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardSelectMatch);
                                return;
                            }
                            return;
                        }
                        await botClient.EditMessageTextAsync(currentUser.UserID, (int)currentUser.MessageID, "Please Register on /register");
                        return;
                    }
                    // Insert in Voter table his selected language if is changed //
                    if (currentUser.CallBackData.Contains("language"))
                    {
                        currentUser.MessageID = cq.Message.MessageId;
                        currentUser.Language = cq.Data.Substring(9);
                        InlineKeyboardMarkup menuKeyboard;
                        if (currentUser.Language == "ro")
                        {
                            Controller.SetNewVoter(null, null, null, "ro", currentUser.UserID);
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Introduceti prognostic", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Topul participantilor", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("Istoria schimbarilor","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Descriere", "description"), InlineKeyboardButton.WithCallbackData("Reguli","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Setari", "settings")}});
                            await botClient.SendTextMessageAsync(cq.Message.Chat.Id, "Meniu principal", replyMarkup: menuKeyboard);
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Controller.SetNewVoter(null, null, null, "ru", currentUser.UserID);
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Введите прогноз", "playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Список топ участников", "topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("История", "prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Описание", "description"), InlineKeyboardButton.WithCallbackData("Правила", "rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Настройки", "settings")}});
                            await botClient.SendTextMessageAsync(cq.Message.Chat.Id, "Главное меню", replyMarkup: menuKeyboard);
                        }
                        else
                        {
                            Controller.SetNewVoter(null, null, null, "en", currentUser.UserID);
                            menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Top Voters","topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData("History","prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData("Description", "description"), InlineKeyboardButton.WithCallbackData("Rules","rules")},
                            new[] { InlineKeyboardButton.WithCallbackData("Settings", "settings")}});
                            await botClient.SendTextMessageAsync(cq.Message.Chat.Id, "Main Menu", replyMarkup: menuKeyboard);
                        }
                        await botClient.DeleteMessageAsync(currentUser.UserID, (int)currentUser.MessageID);
                        return;
                    }
                    return;
                }
                // if users.UsersList is not in database with name and surname display message try register
                // {Optional block if is change something in database and participants have a menu and can press any buttons}
                else
                {
                    if (cq.Data.Contains("language"))
                    {
                        currentUser.MessageID = cq.Message.MessageId;
                        await botClient.DeleteMessageAsync(currentUser.UserID, (int)currentUser.MessageID);

                        currentUser.Language = cq.Data.Substring(9);
                        
                        if (currentUser.Language == "ro")
                        {
                            Controller.SetNewVoter(null, null, null, "ro", currentUser.UserID);
                            await botClient.SendTextMessageAsync(currentUser.UserID, $"Introduceți numele, prenumele și numarul de telefon conform modelului: Ivan - Ivanov - 79800000");

                            //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ro-RO");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Controller.SetNewVoter(null, null, null, "ru", currentUser.UserID);
                            await botClient.SendTextMessageAsync(currentUser.UserID, $"Введите имя, фамилию и номер телефона по образцу: Ivan - Ivanov - 79800000");

                            //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
                        }
                        else
                        {
                            Controller.SetNewVoter(null, null, null, "en", currentUser.UserID);
                            await botClient.SendTextMessageAsync(currentUser.UserID, $"Enter your name, surname and phone according to the model: Ivan - Ivanov - 79800000");

                            //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
                        }

                        currentUser.Registration = true;
                        return;
                    }
                    else // if is not registred but have a language next step is register
                    {
                        await botClient.EditMessageTextAsync(cq.Message.Chat.Id, cq.Message.MessageId, $"Try command /register");
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                _ = LoggClient.SendMessage(exception.Message);
                logger.Error(exception.Message);
                Controller.SetLoggs(null, null, null, exception.Message);

                return;
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cts)
        {
            var errorMessage = exception switch
            {
                // catch an api error, Bigger exception is { <- 400 - the message text is not changed -> }
                ApiRequestException apiRequestException
                    => $"Telegram api error: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(exception);

            _ = LoggClient.SendMessage(exception.Message);
            logger.Error(errorMessage);
             Controller.SetLoggs(null, null, null, exception.Message);
            return Task.CompletedTask;
        }

        public async void SendMessage(long ChatID, string Message)
        {
            try
            {
                await bot.SendTextMessageAsync(ChatID, Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}