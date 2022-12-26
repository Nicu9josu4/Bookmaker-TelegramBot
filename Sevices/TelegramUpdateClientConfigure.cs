using BookmakerTelegramBot.Controllers;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Keyboards = BookmakerTelegramBot.Models.MainKeyboards;
using TotalPages = BookmakerTelegramBot.Models.TotalPages;
using Users = BookmakerTelegramBot.Models.Users;

[assembly: NeutralResourcesLanguage("en-US")]

namespace BookmakerTelegramBot.Sevices
{
    public class TelegramUpdateClientConfigure
    {
        public DbMethodsController Controller = new DbMethodsController();
        public TelegramClientConfigure TelegramClient = new TelegramClientConfigure();
        public TelegramLogginingClientConfigure LoggClient = new TelegramLogginingClientConfigure();
        public NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public Users users = new Users();
        public TotalPages pages = new TotalPages();
        public Keyboards keyboards = new Keyboards();
        public string connectionString;
        public string Token;
        public int VotedTeam;

        //public ResourceManager ResManager = new ResourceManager("BookmakerTelegramBot.Resources.Resources", Assembly.GetExecutingAssembly());
        public async Task StartClient()
        {
            var bot = new TelegramBotClient(Token);
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

        private async Task HandleUpdatesAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                //Console.WriteLine("Message:       ID:" + update.Message.Chat.Id + ", Text:        " + update.Message.Text);
                await HandleMessage(bot, update.Message);
                _ = LoggClient.SendMessage($"{update.Message.Chat.Id} say: {update.Message.Text}");
                return;
            }
            if (update?.Type == UpdateType.CallbackQuery)
            {
                //Console.WriteLine("CallBackQuery: ID:" + update.CallbackQuery.From.Id + ", CallbackData:" + update.CallbackQuery.Data.ToString());
                await HandleCallbackQuery(bot, update.CallbackQuery);
                return;
            }

            return;
        }

        public async Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            if (message.Chat.Id == 840323224 || message.Chat.Id == 1132570191)
            {
                // Declare current Users
                Users currentUser = users.UsersList.Where(x => x.UserID == message.Chat.Id).First();
                currentUser.TextMessage = message.Text;
                // verify users.UsersList Language and change UIculture
                
                int number;
                // /menu or /start command
                if (message.Text == "/start" || message.Text == "/menu")
                {
                    currentUser.IntroduceTotal = false;
                    Controller.SetNewVoter(null, null, null, null, message.Chat.Id);
                    var menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData( "Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData( "Settings", "settings")}});
                    var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                    {
                            InlineKeyboardButton.WithCallbackData( ("Romanian"),"Language-ro"),
                            InlineKeyboardButton.WithCallbackData( ("Russian"),"Language-ru"),
                            InlineKeyboardButton.WithCallbackData( ("English"),"Language-en")
                        });
                    if (users.UsersList.Exists(x => x.UserID == message.Chat.Id && x.Language != null && x.FirstName != null && x.FirstName != "" && x.LastName != null && x.LastName != ""))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ("Main Menu"), replyMarkup: menuKeyboard);
                    }
                    else if (users.UsersList.Exists(x => x.UserID == message.Chat.Id && (x.Language == null || x.Language == "")))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ("Select Language"), replyMarkup: menuKeyboardSelectLanguage);
                    }
                    else if (users.UsersList.Exists(x => x.UserID == message.Chat.Id && (x.FirstName == null || x.FirstName == "") && (x.LastName == null || x.LastName == "")))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"{("Try command")} /register");
                    }

                    return;
                }
                // TODO: inform
                if (message.Text.Contains("/notify"))
                {
                    try
                    {
                        //long[] IDs = { 840323224 };
                        
                        foreach (var Participant in users.UsersList)
                        {
                            try
                            {
                                string[] textToNotify = message.Text.Split('+');
                                //string url;
                                if (Participant.Language == "ro")
                                {
                                    //url = $"https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_Participant={Participant.UserParticipant}&text={textToNotify[1]}";
                                    TelegramClient.SendMessage(Participant.UserID, textToNotify[1]);
                                }
                                else if (Participant.Language == "ru")
                                {
                                    TelegramClient.SendMessage(Participant.UserID, textToNotify[2]);

                                    //url = $"https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id={Participant.UserParticipant}&text={textToNotify[2]}";
                                }
                                else
                                {
                                    TelegramClient.SendMessage(Participant.UserID, textToNotify[3]);

                                    //url = $"https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id={Participant.UserParticipant}&text={textToNotify[3]}";
                                }
                            }
                            catch(Exception exception)
                            {
                                Controller.SetLoggs(null, null, null, exception.Message);
                                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=UpdateFWC:Exception: {exception}");
                                _ = request.GetResponse();
                            }
                            //https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=840323224&text=Salut maine vom testa iarasi acest chat bot, Pentru maine vom avea 8 match-uri (Primele 8), erorile m-am straduit sa le repar si sper sa nu mai apara in timpul testarii, Multumim si ne revedem maine! Linkul pentru alaturare: https://t.me/+88_dbBFWEo85ZGEy

                            /*
                            Josu Ion - 840323224
                            https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=840323224&text=Meciul dintre Anglia și Iran a fost finalizat cu scorul 6 - 1. Următorul meci va începe astăzi la ora 18:00. Grăbiți-vă cu prognozarea. Mari succese!

                            Liviu pusca - 501652439
                            https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=501652439&text=Salut maine vom testa iarasi acest chat bot, Pentru maine vom avea 8 match-uri (Primele 8), erorile m-am straduit sa le repar si sper sa nu mai apara in timpul testarii, Multumim si ne revedem maine! Linkul pentru alaturare: https://t.me/+88_dbBFWEo85ZGEy

                            Elena  Pecerschih - 1132570191
                            https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=1132570191&text=Salut maine vom testa iarasi acest chat bot, Pentru maine vom avea 8 match-uri (Primele 8), erorile m-am straduit sa le repar si sper sa nu mai apara in timpul testarii, Multumim si ne revedem maine! Linkul pentru alaturare: https://t.me/+88_dbBFWEo85ZGEy

                            Mihail Cracea - 452429087
                            https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=452429087&text=Am corectat introducerea scorului cu 0, imi cer iertare de discomfort si multumim ca ati gasit greseala

                            Stanchevici Dumitru - 1141765793
                            https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=1141765793&text=Salut maine vom testa iarasi acest chat bot, Pentru maine vom avea 8 match-uri (Primele 8), erorile m-am straduit sa le repar si sper sa nu mai apara in timpul testarii, Multumim si ne revedem maine! Linkul pentru alaturare: https://t.me/+88_dbBFWEo85ZGEy
                            intrus : 554432744
                            Blocked
                            https://api.telegram.org/bot5790711361:AAHAOtoq0Jlee5RylQeiJp_I8h5aK3amw-o/sendMessage?chat_id=554432744&text=Who are you?
                            Catalin Nevoia 642352186

                            https://api.telegram.org/bot5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M/sendMessage?chat_id=642352186&text=Who are you?

                            */
                            // Using WebRequest
                            //WebRequest request = WebRequest.Create(url);
                            //_ = request.GetResponse();
                        }
                        //string result = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        // Using WebClient
                        //string result1 = new WebClient().DownloadString(url);
                    }
                    catch (Exception exception)
                    {
                        WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=UpdateFWC:Exception: {exception}");
                        _ = request.GetResponse();
                        logger.Error(exception.Message);
                    }
                }
                // Update Team Names
                if (message.Text.Contains("/update"))
                {
                    try
                    {
                        var str = message.Text.Split('+');
                        var UpdatedTeam = Controller.UpdateTeamNames(Convert.ToInt32(str[1]), str[2]);
                        if (UpdatedTeam) await botClient.SendTextMessageAsync(message.Chat.Id, "Updated Successfully");
                        else await botClient.SendTextMessageAsync(message.Chat.Id, "Something wrong");
                    }
                    catch (Exception exception)
                    {
                        WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=UpdateFWC:Exception: {exception.Message}");
                        _ = request.GetResponse();
                        logger.Error(exception.Message);
                    }
                }
                // inserting total score of the match
                string[] totalScore = message.Text.Split('/');
                if (currentUser.IntroduceTotal == true && totalScore.Length == 2)
                {
                    if (int.TryParse(totalScore[0], out number) && int.TryParse(totalScore[1], out number))
                    {
                        currentUser.IntroduceTotal = false;
                        Controller.UpdateMatches(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, null, null, Convert.ToInt32(totalScore[0]), Convert.ToInt32(totalScore[1]));
                        await botClient.SendTextMessageAsync(currentUser.UserID, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                        //await botClient.DeleteMessageAsync(currentUser.UserID, (int)currentUser.MessageID);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(currentUser.UserID, ("You entered an incorrect score, try again"));
                    }
                    return;
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Access prohibited, you are not authorized");
            }
            return;
        }

        public async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery cq)
        {
            try
            {
                if (cq.Message.Chat.Id == 840323224 || cq.Message.Chat.Id == 1132570191)
                {
                    // creating a users.UsersList object for work with them
                    Users currentUser = users.UsersList.Where(x => x.UserID == cq.Message.Chat.Id).First();
                    // change UI culture, and displaing Language for a other users.UsersList
                    if (currentUser.Language == "ro")
                    {
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                    }
                    else if (currentUser.Language == "ru")
                    {
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                    }
                    else
                    {
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                    }
                    currentUser.CallBackData = cq.Data;
                    currentUser.MessageID = cq.Message.MessageId;
                    // verify if users.UsersList exists in database

                    if (currentUser.CallBackData == "mainMenu")
                    {
                        try
                        {
                            if (currentUser.Language == null)
                            {
                                var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                            {
                            InlineKeyboardButton.WithCallbackData( ("Romanian"),"language-ro"),
                            InlineKeyboardButton.WithCallbackData( ("Russian"),"language-ru"),
                            InlineKeyboardButton.WithCallbackData( ("English"),"language-en")
                        });
                                await botClient.EditMessageTextAsync(cq.Message.Chat.Id, cq.Message.MessageId, ("Select Language") /*"Select Language:"*/, replyMarkup: menuKeyboardSelectLanguage);
                                return;
                            }
                            else
                            {
                                var menuKeyboard = new InlineKeyboardMarkup(new[]{
                                    new[] { InlineKeyboardButton.WithCallbackData( ("Start Prognose"),"playGame")},
                                    new[] { InlineKeyboardButton.WithCallbackData( ("Settings"), "settings")}});
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Main Menu") + ".", replyMarkup: menuKeyboard);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Main Menu"), replyMarkup: menuKeyboard);
                                return;
                            }
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Show Settings menu //
                    if (currentUser.CallBackData == "settings")
                    {
                        try
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData( ("Select Language"),"selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData( ("Main Menu"),"mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Settings"), replyMarkup: menuKeyboardSetting);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Show available languages from settings menu //
                    if (currentUser.CallBackData == "selectLanguage")
                    {
                        try
                        {
                            var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData( ("Romanian"),"language-ro"),
                                InlineKeyboardButton.WithCallbackData( ("Russian"),"language-ru"),
                            InlineKeyboardButton.WithCallbackData( ("English"),"language-en")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData( ("Back"),"backToSettingsMenu"),
                                InlineKeyboardButton.WithCallbackData( ("Main Menu"),"mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Select Language"), replyMarkup: menuKeyboardSelectLanguage);

                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Start voting //
                    if (currentUser.CallBackData == "playGame")
                    {
                        try
                        {
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData( ("Select Match"),"selectWinner")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData( ("Main Menu"),"mainMenu")
                                }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("You can select match and get many points or you can try to select who was win in final"), replyMarkup: menuKeyboardPlayGame);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Select match menu, Display match List //
                    if (currentUser.CallBackData == "selectWinner")
                    {
                        try
                        {
                            currentUser.CurentMatchPage = (currentUser.CurentMatchPage == null || currentUser.CurentMatchPage == 0) ? 0 : currentUser.CurentMatchPage; Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Display vote options in Tis match //
                    if (currentUser.CallBackData.Contains("MatchID"))
                    {
                        try
                        {
                            if (cq.Data.Substring(7) != "" && cq.Data.Substring(7) != null)
                            {
                                currentUser.MatchID = Convert.ToInt32(cq.Data.Substring(7));
                                Controller.UpdatedMatchPage(currentUser.UserID, (int)currentUser.MatchID);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Something went wrong"), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                                await Task.Delay(500);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                            }

                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Return to start voting page //
                    if (currentUser.CallBackData == "backToPlayMenu")
                    {
                        try
                        {
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData( ("Select Match"),"selectWinner")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData( ("Main Menu"),"mainMenu")
                                }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("You can select match and get many points or you can try to select who was win in final"), replyMarkup: menuKeyboardPlayGame);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // return to settings menu //
                    if (currentUser.CallBackData == "backToSettingsMenu")
                    {
                        try
                        {
                            var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData( ("Select Language"),"selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData( ("Main Menu"),"mainMenu")
                            }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Settings"), replyMarkup: menuKeyboardSetting);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // return to match list menu //
                    if (currentUser.CallBackData == "backToWinnerMenu")
                    {
                        try
                        {
                            currentUser.CurentMatchPage = 0;
                            Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // return to Vote menu //
                    if (currentUser.CallBackData == "backToSelectedMatchMenu")
                    {
                        try
                        {
                            currentUser.CurentMatchPage = 0;
                            Controller.UpdatedMatchPage(currentUser.UserID, (int)currentUser.MatchID);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Select pages from list who have pagination //
                    if (currentUser.CallBackData.Contains("FirstPage") || currentUser.CallBackData.Contains("PreventPage") || currentUser.CallBackData.Contains("NextPage") || currentUser.CallBackData.Contains("LastPage"))
                    {
                        try
                        {
                            //if (paginationType == 1) // Change page to selectWinnerTeam

                            if (cq.Data == "WinnerTeam FirstPage")
                            {
                                currentUser.CurentTeamPage = currentUser.CurentTeamPage == null ? 0 : currentUser.CurentTeamPage;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team") + ".", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team"), replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                return;
                            }
                            if (cq.Data == "WinnerTeam PreventPage")
                            {
                                if (currentUser.CurentTeamPage >= 1)
                                {
                                    currentUser.CurentTeamPage -= 1;
                                    Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team") + ".", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team"), replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    return;
                                }
                                else
                                {
                                    currentUser.CurentTeamPage = pages.totalTeamPages - 1;
                                    Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team") + ".", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team"), replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    return;
                                }
                            }
                            if (cq.Data == "WinnerTeam NextPage")
                            {
                                if (currentUser.CurentTeamPage < pages.totalTeamPages - 1)
                                {
                                    currentUser.CurentTeamPage += 1;
                                    Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team") + ".", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team"), replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    return;
                                }
                                else
                                {
                                    currentUser.CurentTeamPage = currentUser.CurentTeamPage == null ? 0 : currentUser.CurentTeamPage;
                                    Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team") + ".", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team"), replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                    return;
                                }
                            }
                            if (cq.Data == "WinnerTeam LastPage")
                            {
                                currentUser.CurentTeamPage = pages.totalTeamPages - 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team") + ".", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register the final winner team"), replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                                return;
                            }

                            //if (paginationType == 2) // Change page to select Winner From Match

                            if (cq.Data == "Winner FirstPage")
                            {
                                currentUser.CurentMatchPage = currentUser.CurentMatchPage == null ? 0 : currentUser.CurentMatchPage;
                                Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches") + ".", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                                return;
                            }
                            if (cq.Data == "Winner PreventPage")
                            {
                                if (currentUser.CurentMatchPage >= 1)
                                {
                                    currentUser.CurentMatchPage -= 1;
                                    Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches") + ".", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                                    return;
                                }
                                else
                                {
                                    currentUser.CurentMatchPage = pages.totalMatchPages - 1;
                                    Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches") + ".", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                                    return;
                                }
                            }
                            if (cq.Data == "Winner NextPage")
                            {
                                if (currentUser.CurentMatchPage < pages.totalMatchPages - 1)
                                {
                                    currentUser.CurentMatchPage += 1;
                                    Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches") + ".", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                                    return;
                                }
                                else
                                {
                                    currentUser.CurentMatchPage = currentUser.CurentMatchPage == null ? 0 : currentUser.CurentMatchPage;
                                    Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches") + ".", replyMarkup: users.MenuKeyboardSelectWinner);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                                    return;
                                }
                            }
                            if (cq.Data == "Winner LastPage")
                            {
                                currentUser.CurentMatchPage = pages.totalMatchPages - 1;
                                Controller.ChangeSelectUpdateWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches") + ".", replyMarkup: users.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ("Register prognose for this matches"), replyMarkup: users.MenuKeyboardSelectWinner);
                                return;
                            }

                            //if (paginationType == 3) // Change page to select players

                            if (cq.Data == "Players FirstPage")
                            {
                                currentUser.CurentPlayersPage = currentUser.CurentPlayersPage == null ? 0 : currentUser.CurentPlayersPage;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                return;
                            }
                            if (cq.Data == "Players PreventPage")
                            {
                                if (currentUser.CurentPlayersPage >= 1)
                                {
                                    currentUser.CurentPlayersPage -= 1;
                                    Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    return;
                                }
                                else
                                {
                                    currentUser.CurentPlayersPage = pages.totalPlayersPages - 1;
                                    Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    return;
                                }
                            }
                            if (cq.Data == "Players NextPage")
                            {
                                if (currentUser.CurentPlayersPage < pages.totalPlayersPages - 1)
                                {
                                    currentUser.CurentPlayersPage += 1;
                                    Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    return;
                                }
                                else
                                {
                                    currentUser.CurentPlayersPage = currentUser.CurentPlayersPage == null ? 0 : currentUser.CurentPlayersPage;
                                    Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    return;
                                }
                            }
                            if (cq.Data == "Players LastPage")
                            {
                                currentUser.CurentPlayersPage = pages.totalPlayersPages - 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: users.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                return;
                            }
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Select vote for final team //
                    if (currentUser.CallBackData.Contains("TeamID"))
                    {
                        try
                        {
                            if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                            {
                                int teamID = Convert.ToInt32(cq.Data.Substring(6));
                                if (VotedTeam == 1)
                                {
                                    Controller.UpdateMatches(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, teamID, null, null, null);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                                    VotedTeam = 0;
                                }
                                if (VotedTeam == 2)
                                {
                                    Controller.UpdateMatches(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, null, teamID, null, null);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                                    VotedTeam = 0;
                                }
                                Controller.GetVoters(currentUser.UserID);
                                //var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{ new[]
                                //    {
                                //        InlineKeyboardButton.WithCallbackData( ("Back"), "selectWinnerTeam"),
                                //        InlineKeyboardButton.WithCallbackData( ("Main Menu"),"mainMenu")
                                //    }
                                //});
                                //await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId,  ("You have voted successfully") + ": " + currentUser.VotedFinalTeam, replyMarkup: menuKeyboardPlayGame);
                                return;
                            }
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Please Register on /register");
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Menu with vote options //
                    if (currentUser.CallBackData.Contains("Vote"))
                    {
                        try
                        {
                            if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                            {
                                var menuKeyboardVote = new InlineKeyboardMarkup(new[] {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Back", "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData("Main Menu","mainMenu")
                                }
                            }
                                );
                                if (cq.Data.Contains("VoteTotal"))
                                {
                                    currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(11));
                                    currentUser.IntroduceTotal = true;
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"For teams {currentUser.VotedFirstTeam} {("and")} {currentUser.VotedSecondTeam} \n{("Introduce final score by model:")} 4/3", replyMarkup: menuKeyboardVote);
                                    return;
                                }
                                if (cq.Data.Contains("VotePlayers"))
                                {
                                    currentUser.VotedPlayerTeam = currentUser.CallBackData.Split('+')[1];
                                    try
                                    {
                                        currentUser.CurentPlayersPage = (currentUser.CurentPlayersPage == null || currentUser.CurentPlayersPage == 0) ? 0 : currentUser.CurentPlayersPage;
                                        currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Split('+')[2]);
                                        Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, currentUser.CurentPlayersPage);
                                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"Select player from team {currentUser.VotedPlayerTeam}", replyMarkup: users.MenuKeyboardSelectPlayer);
                                    }
                                    catch (Exception exception)
                                    {
                                        _ = LoggClient.SendMessage(exception.Message);
                                        logger.Error(exception);
                                    }

                                    return;
                                }
                                if (cq.Data.Contains("VotePlayerID"))
                                {
                                    int PlayerID = Convert.ToInt32(currentUser.CallBackData.Substring(12));
                                    Controller.VotePlayerGoals(currentUser.UserID, currentUser.MatchID, PlayerID);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.GetPrognoseInfoFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: users.MenuKeyboardUpdatedSelectMatch);
                                    return;
                                }
                                return;
                            }
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Please Register on /register");
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    // Insert in Voter table his selected language if is changed //
                    if (currentUser.CallBackData.Contains("language"))
                    {
                        try
                        {
                            currentUser.Language = cq.Data.Substring(9);
                            if (currentUser.Language == "en")
                            {
                                Controller.SetNewVoter(null, null, null, "en", currentUser.UserID);
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                            }
                            if (currentUser.Language == "ro")
                            {
                                Controller.SetNewVoter(null, null, null, "ro", currentUser.UserID);
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                            }
                            if (currentUser.Language == "ru")
                            {
                                Controller.SetNewVoter(null, null, null, "ru", currentUser.UserID);
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                            }
                            var menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData("Start Prognose","playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData("Settings", "settings")}});
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Main Menu", replyMarkup: menuKeyboard);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Main Menu.", replyMarkup: menuKeyboard);
                            return;
                        }
                        catch (Exception exception)
                        {
                            _ = LoggClient.SendMessage(exception.Message);
                            logger.Error(exception.Message);
                        }
                    }
                    //
                    if (cq.Data.Contains("SetFirstTeamName"))
                    {
                        currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(18));
                        VotedTeam = 1;
                        currentUser.CurentTeamPage = 0;
                        Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Register the final winner team", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);

                        return;
                    }
                    //
                    if (cq.Data.Contains("SetSecondTeamName"))
                    {
                        currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(19));
                        VotedTeam = 2;
                        currentUser.CurentTeamPage = 0;
                        Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Register the final winner team", replyMarkup: users.MenuKeyboardSelectFinalWinnerTeam);
                        return;
                    }
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(cq.Message.Chat.Id, "Access prohibited, you are not authorized");
                }
            }
            catch (ApiRequestException exception)
            {
                // catch an api error, Bigger exception is { <- 400 - the message text is not changed -> }
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                            => $"Telegram api error: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };
                logger.Error(errorMessage);
                //Console.WriteLine(errorMessage);
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
            return Task.CompletedTask;
        }
    }
}