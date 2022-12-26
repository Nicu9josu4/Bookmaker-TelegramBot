using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Keyboards = BookmakerTelegramBot.Models.MainKeyboards;
using TotalPages = BookmakerTelegramBot.Models.TotalPages;
using Users = BookmakerTelegramBot.Models.Users;

namespace BookmakerTelegramBot.Controllers
{
    public class DbMethodsController
    {
        public NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public Users users = new Users();
        public TotalPages pages;
        //public Keyboards keyboards;
        //public ResourceManager ResManager = new ResourceManager("BookmakerTelegramBot.Resources.Resources", Assembly.GetExecutingAssembly());
        public string connectionString;

        ///Functions block
        ///
        public void GetVoters(long ChatID) // Get parameters for voters function
        {
            try
            {
                //users.SetValuesFromDb()
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_VotersFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var a = myClob.Value;
                    var d = JObject.Parse(a);
                    JArray array = (JArray)d["Result"];
                    var totalData = array.Count;
                    for (int i = 0; i < totalData; i++)
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == ChatID && existentUser.UserID == Convert.ToInt64(d["Result"][i]["ChatID"])))
                        {
                            Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
                            currentUser.Language = d["Result"][i]["Language"].ToString();
                            currentUser.FirstName = d["Result"][i]["FirstName"].ToString();
                            currentUser.LastName = d["Result"][i]["LastName"].ToString();
                            currentUser.VotedFinalTeam = d["Result"][i]["TeamName"].ToString();
                        }
                }
            }
            catch(Exception exception)
            {
            SetLoggs(null, null, null, exception.Message);

            }


        } // End get parameters for voters function

        public bool SetNewVoter(string Name, string Surname, string Phone, string Language, long ChatID) // Set new voter function
        {
            try
            {
                //users.SetValuesFromDb();
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Set_New_Voter";
                        cmd.Parameters.Add("Voter_Name", OracleDbType.Varchar2).Value = Name;
                        cmd.Parameters.Add("Voter_Surname", OracleDbType.Varchar2).Value = Surname;
                        cmd.Parameters.Add("Voter_Phone", OracleDbType.Varchar2).Value = Phone;
                        cmd.Parameters.Add("Voter_Language", OracleDbType.Varchar2).Value = Language;
                        cmd.Parameters.Add("ChatID", OracleDbType.Int64).Value = ChatID;
                        cmd.ExecuteScalar();
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == ChatID))
                        {
                            Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();

                            if (Name != null && Name != "")
                            {
                                currentUser.FirstName = Name;
                            }
                            if (Surname != null && Surname != "")
                            {
                                currentUser.LastName = Surname;
                            }
                            if (Language != null)
                            {
                                currentUser.Language = Language.ToString();
                            }
                        }
                        else
                        {
                            users.UsersList.Add(new Users() { UserID = Convert.ToInt64(ChatID) });
                        }
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Error(exception.Message);
                SetLoggs(null, null, null, exception.Message);

                return false;
            }
        } // End set new voter function

        public void ChangeSelectWinnerTeamPage(long ChatID, int? curPage) // Select winner final team page, start with 0 end with totalWinnerPages
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    if (curPage == null) curPage = 0;
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_TeamsFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    pages.totalTeamPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();

                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    for (var i = 0; i < totalData; i++)
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData(
                           $"{dataFromClob["Result"][i]["TeamName"]}",
                               $"TeamID{dataFromClob["Result"][i]["TeamID"]}"
                        ));
                    }
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "WinnerTeam FirstPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "WinnerTeam PreventPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalTeamPages}", "PageNow"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "WinnerTeam NextPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "WinnerTeam LastPage"));
                    if (currentUser.Language == "ro")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "backToPlayMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                    }
                    else if (currentUser.Language == "ru")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "backToPlayMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                    }
                    else
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "backToPlayMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                    }

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                            i += 4;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardSelectFinalWinnerTeam = new InlineKeyboardMarkup(menu.ToArray());
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

            }
        } // End select winner final team page, start with 0 end with totalWinnerPages

        public string ChangeSelectHistoryPage(long ChatID, int? curPage) // Get history form all prognoses
        {
            string historyDetails = "";
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_HistoryFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                    cmd.Parameters.Add("P_UserID", OracleDbType.Int32).Value = ChatID;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    pages.totalHistoryPages = Convert.ToInt32(dataFromClob["TotalRows"]) == 0 ? 1 : Convert.ToInt32(dataFromClob["TotalRows"]);

                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
                    

                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    //var keyboardInline = new InlineKeyboardButton[totalData][];

                    if (totalData != 0)
                    {
                        for (var i = 0; i < totalData; i++)
                        {

                            historyDetails += dataFromClob["Result"][i]["PrognosedDate"].ToString();

                            if (currentUser.Language == "ro")
                            {
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                {
                                    historyDetails += $" - Ai selectat {dataFromClob["Result"][i]["PrognosedTeam"]} din meciul dintre {dataFromClob["Result"][i]["MatchFTeam"]} si {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }                                                                                                                                                                             
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected                                                                                 
                                {                                                                                                                                                                             
                                    historyDetails += $" - Ai selectat {dataFromClob["Result"][i]["PrognosedTeam"]} din meciul dintre {dataFromClob["Result"][i]["MatchFTeam"]} si {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                {
                                    historyDetails += $" - Ai selectat egal din meciul dintre {dataFromClob["Result"][i]["MatchFTeam"]} si {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                {
                                    historyDetails += $" - Ati selectat scorul {dataFromClob["Result"][i]["FirstTeamScore"]}-{dataFromClob["Result"][i]["SecondTeamScore"]} din meciul dintre {dataFromClob["Result"][i]["MatchFTeam"]} si {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                {
                                    historyDetails += $" - Ati votat jucatori {dataFromClob["Result"][i]["PlayerName"]} din echipa {dataFromClob["Result"][i]["PlayerTeam"]} din meciul dintre {dataFromClob["Result"][i]["MatchFTeam"]} si {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 6) // Team selected
                                {
                                    historyDetails += $" - Ai ales echipa care va ieși in final {dataFromClob["Result"][i]["TeamName"]}\n";
                                }
                            }
                            else if (currentUser.Language == "ru")
                            {
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                {
                                    historyDetails += $" - Вы выбрали {dataFromClob["Result"][i]["PrognosedTeam"]} от матча между {dataFromClob["Result"][i]["MatchFTeam"]} и {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                {
                                    historyDetails += $" - Вы выбрали {dataFromClob["Result"][i]["PrognosedTeam"]} от матча между {dataFromClob["Result"][i]["MatchFTeam"]} и {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                {
                                    historyDetails += $" - Вы выбрали ничью в матче между {dataFromClob["Result"][i]["MatchFTeam"]} и {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                {
                                    historyDetails += $" - Вы выбрали счет {dataFromClob["Result"][i]["FirstTeamScore"]}-{dataFromClob["Result"][i]["SecondTeamScore"]} от матча между {dataFromClob["Result"][i]["MatchFTeam"]} и {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                {
                                    historyDetails += $" - Вы проголосовали за игрокa {dataFromClob["Result"][i]["PlayerName"]} из команды {dataFromClob["Result"][i]["PlayerTeam"]} от матча между {dataFromClob["Result"][i]["MatchFTeam"]} и {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 6) // Team selected
                                {
                                    historyDetails += $" - Вы выбрали команду, которая выйдет в финал {dataFromClob["Result"][i]["TeamName"]}\n";
                                }
                            }
                            else
                            {
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                {
                                    historyDetails += $" - You have select {dataFromClob["Result"][i]["PrognosedTeam"]} from match between {dataFromClob["Result"][i]["MatchFTeam"]} and {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                {
                                    historyDetails += $" - You have select {dataFromClob["Result"][i]["PrognosedTeam"]} from match between {dataFromClob["Result"][i]["MatchFTeam"]} and {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                {
                                    historyDetails += $" - You have select equal from match between {dataFromClob["Result"][i]["MatchFTeam"]} and {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                {
                                    historyDetails += $" - You have select score {dataFromClob["Result"][i]["FirstTeamScore"]}-{dataFromClob["Result"][i]["SecondTeamScore"]} from match between {dataFromClob["Result"][i]["MatchFTeam"]} and {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                {
                                    historyDetails += $" - You have select player {dataFromClob["Result"][i]["PlayerName"]} from team {dataFromClob["Result"][i]["PlayerTeam"]} from match between {dataFromClob["Result"][i]["MatchFTeam"]} and {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 6) // Team selected
                                {
                                    historyDetails += $" - You have chosen the team that will come out in the end {dataFromClob["Result"][i]["TeamName"]}\n";
                                }
                            }
                            
                        }
                    }
                    else
                    {
                        if (currentUser.Language == "ro")
                        {
                            historyDetails = "Nu ai luat nicio acțiune";
                        }
                        else if (currentUser.Language == "ru")
                        {
                            historyDetails = "Вы не предприняли никаких действий";
                        }
                        else
                        {
                            historyDetails = "You have taken no action";
                        }
                        
                    }
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "History FirstPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "History PreventPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalHistoryPages}", "prognoseHistory"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "History NextPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "History LastPage"));

                    if (currentUser.Language == "ro")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                    }
                    else if (currentUser.Language == "ru")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                    }
                    else
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                    }

                    

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                            i += 4;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardHistory = new InlineKeyboardMarkup(menu.ToArray());
                }
                return historyDetails;
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

                return "Something went wrong";
            }
        } // End get history form all prognoses

        public string SelectPrognoseFromMatch(long ChatID, int? matchID) // Get prognose for one of selected match
        {
            string prognoseDetails = "";
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_PrognoseFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_UserID", OracleDbType.Int32).Value = ChatID;
                    cmd.Parameters.Add("P_MatchID", OracleDbType.Int32).Value = matchID;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    JArray array1 = (JArray)dataFromClob["Players"];
                    var totalPlayersData = array1.Count;
                    //List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
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

                    string firstTypeSelected = "None";
                    string secondTypeSelected = "None";
                    string thirdTypeSelected = "None";
                    //var keyboardInline = new InlineKeyboardButton[totalData][];
                    if (totalData != 0)
                    {
                        if (Convert.ToDateTime(dataFromClob["MatchStart"]) < DateTime.Now)
                        {
                            for (var i = 0; i < totalData; i++)
                            {

                                if (currentUser.Language == "ro")
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }
                                        // ❌
                                        else
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }                         
                                        else                      
                                        {                         
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Ati votat egalitate ✅";
                                        }                        
                                        else                     
                                        {                        
                                            firstTypeSelected = $"Ati votat egalitate ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ✅";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"Ati votat jucatorul: {dataFromClob["Result"][i]["PlayerName"]} din echipa {dataFromClob["Result"][i]["PlayerTeam"]} ✅";
                                        }                         
                                        else                      
                                        {                         
                                            thirdTypeSelected = $"Ati votat jucatorul: {dataFromClob["Result"][i]["PlayerName"]} din echipa {dataFromClob["Result"][i]["PlayerTeam"]} ❌";
                                        }
                                    }
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }                         
                                        // ❌                     
                                        else                      
                                        {                         
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }                         
                                        else                      
                                        {                         
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Вы проголосовали за ничью ✅";
                                        }                         
                                        else                      
                                        {                         
                                            firstTypeSelected = $"Вы проголосовали за ничью ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ✅";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"Вы проголосовали за игрокa: {dataFromClob["Result"][i]["PlayerName"]} из команды {dataFromClob["Result"][i]["PlayerTeam"]} ✅";
                                        }                                                                                               
                                        else                                                                                            
                                        {                                                                                               
                                            thirdTypeSelected = $"Вы проголосовали за игрокa: {dataFromClob["Result"][i]["PlayerName"]} из команды {dataFromClob["Result"][i]["PlayerTeam"]} ❌";
                                        }
                                    }
                                }
                                else
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }                         
                                        // ❌                     
                                        else                      
                                        {                         
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }                         
                                        else                      
                                        {                         
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"You have select equal ✅";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"You have select equal ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ✅";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"You have select player: {dataFromClob["Result"][i]["PlayerName"]} from team {dataFromClob["Result"][i]["PlayerTeam"]} ✅";
                                        }
                                        else
                                        {
                                            thirdTypeSelected = $"You have select player: {dataFromClob["Result"][i]["PlayerName"]} from team {dataFromClob["Result"][i]["PlayerTeam"]} ❌";
                                        }
                                    }
                                }
                                
                            }

                            if (currentUser.Language == "ro")
                            {
                            prognoseDetails = $"În meciul dintre: {currentUser.VotedFirstTeam} si {currentUser.VotedSecondTeam}\n";

                            }
                            else if (currentUser.Language == "ru")
                            {
                            prognoseDetails = $"В матче между: {currentUser.VotedFirstTeam} и {currentUser.VotedSecondTeam}\n";

                            }
                            else
                            {
                            prognoseDetails = $"In the match between: {currentUser.VotedFirstTeam} and {currentUser.VotedSecondTeam}\n";
                            }
                            if (firstTypeSelected != "None")
                            {
                                prognoseDetails += $"\n- {firstTypeSelected}";
                            }
                            if (secondTypeSelected != "None")
                            {
                                prognoseDetails += $"\n- Total: {secondTypeSelected}";
                            }
                            if (thirdTypeSelected != "None")
                            {
                                prognoseDetails += $"\n- {thirdTypeSelected}";
                            }
                            if (firstTypeSelected == "None" && secondTypeSelected == "None" && thirdTypeSelected == "None")
                            {
                                prognoseDetails += "\n" + "None";
                            }

                            if (currentUser.Language == "ro")
                            {
                                prognoseDetails += $"\n\nScorul final\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                                                    $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\nJucători cu gol:\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} din echipa {dataFromClob["Players"][i]["TeamName"]}\n";
                            }
                            else if (currentUser.Language == "ru")
                            {
                                prognoseDetails += $"\n\nОкончательный счет\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                                                    $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\nИгроки, забившие:\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} из команды {dataFromClob["Players"][i]["TeamName"]}\n";
                            }
                            else
                            {
                                prognoseDetails += $"\n\nThe final score\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                                                    $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\nPlayers with goals:\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} from team {dataFromClob["Players"][i]["TeamName"]}\n";
                            }

                            
                            //prognoseDetails = $"{ResManager.GetString("You have vote:")}" +
                            //    $"\n- {ResManager.GetString("Teams or Equal")} - {firstTypeSelected}" +
                            //    $"\n- {ResManager.GetString("Total")} - {secondTypeSelected}" +
                            //    $"\n- {ResManager.GetString("Players")} - {thirdTypeSelected}";
                        }
                        else
                        {
                            for (var i = 0; i < totalData; i++)
                            {


                                if (currentUser.Language == "ro")
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                        // ❌
                                        else
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"Echipa învingătoare: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Ati votat egalitate ";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"Ati votat egalitate ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"Ati votat jucatorul: {dataFromClob["Result"][i]["PlayerName"]} din echipa {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                        }
                                        else
                                        {
                                            thirdTypeSelected = $"Ati votat jucatorul: {dataFromClob["Result"][i]["PlayerName"]} din echipa {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                        }
                                    }
                                }
                                else if (currentUser.Language == "ru")
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                        // ❌                     
                                        else
                                        {
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"Kоманда-победитель: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"Вы проголосовали за ничью ";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"Вы проголосовали за ничью ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"Вы проголосовали за игрокa: {dataFromClob["Result"][i]["PlayerName"]} из команды {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                        }
                                        else
                                        {
                                            thirdTypeSelected = $"Вы проголосовали за игрокa: {dataFromClob["Result"][i]["PlayerName"]} из команды {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                        }
                                    }
                                }
                                else
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                        // ❌                     
                                        else
                                        {
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"The winning team: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"You have select equal ";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"You have select equal ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"You have select player: {dataFromClob["Result"][i]["PlayerName"]} from team {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                        }
                                        else
                                        {
                                            thirdTypeSelected = $"You have select player: {dataFromClob["Result"][i]["PlayerName"]} from team {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                        }
                                    }
                                }
                            }
                            if (currentUser.Language == "ro")
                            {
                                prognoseDetails = $"În meciul dintre: {currentUser.VotedFirstTeam} si {currentUser.VotedSecondTeam}\n";

                            }
                            else if (currentUser.Language == "ru")
                            {
                                prognoseDetails = $"В матче между: {currentUser.VotedFirstTeam} и {currentUser.VotedSecondTeam}\n";

                            }
                            else
                            {
                                prognoseDetails = $"In the match between: {currentUser.VotedFirstTeam} and {currentUser.VotedSecondTeam}\n";
                            }
                            if (firstTypeSelected != "None")
                            {
                                prognoseDetails += $"\n- {firstTypeSelected}";
                            }
                            if (secondTypeSelected != "None")
                            {
                                prognoseDetails += $"\n- Total: {secondTypeSelected}";
                            }
                            if (thirdTypeSelected != "None")
                            {
                                prognoseDetails += $"\n- {thirdTypeSelected}";
                            }
                        }
                    }
                    else
                    {
                        //prognoseDetails += $"{ResManager.GetString("You have vote:")}\n- {ResManager.GetString("Teams or Equal")} - {ResManager.GetString("None")}\n- {ResManager.GetString("Total")} - {ResManager.GetString("None")}\n- {ResManager.GetString("Players")} - {ResManager.GetString("None")}";
                        if (Convert.ToDateTime(dataFromClob["MatchStart"]) > DateTime.Now)
                        {
                            if (currentUser.Language == "ro")
                            {
                                prognoseDetails += $"Votați vă rog:";
                            }
                            else if (currentUser.Language == "ru")
                            {
                                prognoseDetails += $"Проголосуйте пожалуйста:";
                            }
                            else
                            {
                                prognoseDetails += $"Vote please:";
                            }

                        }
                        else if (Convert.ToDateTime(dataFromClob["MatchStart"]) < DateTime.Now)
                        {
                            if (currentUser.Language == "ro")
                            {
                                prognoseDetails += $"Nu ati introdus datele";/*{ResManager.GetString("vote please")}*/
                                prognoseDetails += $"\n\nScorul final\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                                                    $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\nJucători cu gol:\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} din echipa {dataFromClob["Players"][i]["TeamName"]}\n";
                            }
                            else if (currentUser.Language == "ru")
                            {
                                prognoseDetails += $"Вы не ввели данные";/*{ResManager.GetString("vote please")}*/
                                prognoseDetails += $"\n\nОкончательный счет\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                                                    $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\nИгроки, забившие:\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} из команды {dataFromClob["Players"][i]["TeamName"]}\n";
                            }
                            else
                            {
                                prognoseDetails += $"You have not entered the data";/*{ResManager.GetString("vote please")}*/
                                prognoseDetails += $"\n\nThe final score\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                                                    $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\nPlayers with goals:\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} from team {dataFromClob["Players"][i]["TeamName"]}\n";
                            }
                        }
                    }
                }
                return prognoseDetails;
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

                return "Something went wrong";
            }
        } // End get prognose for one of selected match

        public string GetVoterPlace(long ChatID)
        {
            string VoterInfo = "";
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_VoterFromTop", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_UserID", OracleDbType.Int32).Value = ChatID;
                    cmd.ExecuteNonQuery();
                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;

                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();



                    //var keyboardInline = new InlineKeyboardButton[totalData][];
                    if (totalData != 0)
                    {
                        if (currentUser.Language == "ro")
                        {
                            if (Convert.ToInt32(dataFromClob["Result"][0]["Score"]) == 1) VoterInfo = $"Locul {dataFromClob["Result"][0]["Place"]}. {dataFromClob["Result"][0]["FirstName"]} {dataFromClob["Result"][0]["SecondName"]} - {dataFromClob["Result"][0]["Score"]} punct";
                            else VoterInfo = $"Locul {dataFromClob["Result"][0]["Place"]}. {dataFromClob["Result"][0]["FirstName"]} {dataFromClob["Result"][0]["SecondName"]} - {dataFromClob["Result"][0]["Score"]} puncte";
                        }
                        else if (currentUser.Language == "ru")
                        {
                            if (Convert.ToInt32(dataFromClob["Result"][0]["Score"]) == 1) VoterInfo = $"Место {dataFromClob["Result"][0]["Place"]}. {dataFromClob["Result"][0]["FirstName"]} {dataFromClob["Result"][0]["SecondName"]} - {dataFromClob["Result"][0]["Score"]} очко";
                            else VoterInfo = $"Место {dataFromClob["Result"][0]["Place"]}. {dataFromClob["Result"][0]["FirstName"]} {dataFromClob["Result"][0]["SecondName"]} - {dataFromClob["Result"][0]["Score"]} очков";
                        }
                        else
                        {
                            if (Convert.ToInt32(dataFromClob["Result"][0]["Score"]) == 1) VoterInfo = $"Place {dataFromClob["Result"][0]["Place"]}. {dataFromClob["Result"][0]["FirstName"]} {dataFromClob["Result"][0]["SecondName"]} - {dataFromClob["Result"][0]["Score"]} point";
                            else VoterInfo = $"Place {dataFromClob["Result"][0]["Place"]}. {dataFromClob["Result"][0]["FirstName"]} {dataFromClob["Result"][0]["SecondName"]} - {dataFromClob["Result"][0]["Score"]} points";
                        }
                    }
                    else
                    {
                        VoterInfo = $" ";
                    }
                }
                return VoterInfo;
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();

                // SetLoggs(null, null, null, exception.Message);

                return "Something went wrong";
            }
        }

        public void ChangeSelectWinnerPage(long ChatID, int? curPage) // Select winner page
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    if (curPage == null) curPage = 0;
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_MatchesFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Match_ID", OracleDbType.Int32).Value = curPage;
                    //OracleClob Clob = new OracleClob(conn);
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    pages.totalMatchPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
                    
                    for (var i = 0; i < totalData; i++)
                    {
                         
                        buttons.Add(InlineKeyboardButton.WithCallbackData(
                           $"{dataFromClob["Result"][i]["FirstTeam"]} VS {dataFromClob["Result"][i]["SecondTeam"]} \n\r ( {dataFromClob["Result"][i]["StartDate"]} )",
                               $"MatchID{dataFromClob["Result"][i]["MatchID"]}"
                        ));
                    }
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "Winner FirstPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "Winner PreventPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalMatchPages}", "selectWinner"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "Winner NextPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "Winner LastPage"));
                    if (currentUser.Language == "ro")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                    }
                    else if (currentUser.Language == "ru")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                    }
                    else
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                    }

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                            i += 4;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardSelectWinner = new InlineKeyboardMarkup(menu.ToArray());
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

            }
        } // End select winner page

        public void ChangeSelectTopVotersPage(long ChatID, int? curPage) // Select topVoters page
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    //if (curPage == null) curPage = 0;
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_TopVotersFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                    //OracleClob Clob = new OracleClob(conn);
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    pages.totalTopVotersPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
                    
                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    for (var i = 0; i < totalData; i++)
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData(
                           $"{dataFromClob["Result"][i]["FirstName"]} {dataFromClob["Result"][i]["SecondName"]} Score: {dataFromClob["Result"][i]["ScoreAfter"]} ( {dataFromClob["Result"][i]["ScoreBefore"]} + {dataFromClob["Result"][i]["FinalTeamPoints"]} )",
                               "topVoters"
                        ));
                    }
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "TopVoters FirstPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "TopVoters PreventPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalTopVotersPages}", "topVoters"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "TopVoters NextPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "TopVoters LastPage"));
                    if (currentUser.Language == "ro")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                    }
                    else if (currentUser.Language == "ru")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                    }
                    else
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                    }

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                            i += 4;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardSelectTopVoter = new InlineKeyboardMarkup(menu.ToArray());
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

            }
        } // End select topVoters page

        public void ChangeSelectMatchPage(long ChatID, int matchID) // Select match pages
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_MenuMatchesFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Match_ID", OracleDbType.Int32).Value = matchID;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);

                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();

                    currentUser.VotedFirstTeam = dataFromClob["Result"][0]["FirstTeamName"].ToString();
                    currentUser.VotedSecondTeam = dataFromClob["Result"][0]["SecondTeamName"].ToString();

                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    var menu = new List<InlineKeyboardButton[]>();
                    var date = DateTime.Now;
                    if (Convert.ToDateTime(dataFromClob["Result"][0]["StartTime"]) > date)
                    {
                        if (currentUser.Language == "ro")
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]}", $"VoteFirstTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]}", $"VoteSecondTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Egalitate", $"VoteEqual--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"👉🏻 Apasati aici pentru a alege scorul final 👈🏻", $"VoteTotal--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]} Jucatorii", $"VotePlayers+{dataFromClob["Result"][0]["FirstTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]} Jucatorii", $"VotePlayers+{dataFromClob["Result"][0]["SecondTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                        }
                        else if (currentUser.Language == "ru")
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]}", $"VoteFirstTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]}", $"VoteSecondTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Ничья", $"VoteEqual--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"👉🏻 Нажмите сюда чтобы выбрать окончательный счет 👈🏻", $"VoteTotal--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]}  Игроки", $"VotePlayers+{dataFromClob["Result"][0]["FirstTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]} Игроки", $"VotePlayers+{dataFromClob["Result"][0]["SecondTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                        }
                        else
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]}", $"VoteFirstTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]}", $"VoteSecondTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Equal", $"VoteEqual--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"👉🏻 Click here to select final score 👈🏻", $"VoteTotal--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]} Players", $"VotePlayers+{dataFromClob["Result"][0]["FirstTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]} Players", $"VotePlayers+{dataFromClob["Result"][0]["SecondTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                        }

                        

                        for (int i = 0; i < buttons.Count - 1; i++)
                        {
                            if (buttons.Count - 2 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            else if (buttons.Count - 4 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            else if (buttons.Count - 8 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2] });
                                i += 2;
                            }
                            else if (i != buttons.Count - 1)
                            {
                                menu.Add(new[] { buttons[i] });
                            }
                        }
                        users.MenuKeyboardSelectMatch = new InlineKeyboardMarkup(menu.ToArray());
                    }
                    else
                    {
                        if (currentUser.Language == "ro")
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                        }
                        else if (currentUser.Language == "ru")
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                        }
                        else
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                        }

                        menu.Add(new[] { buttons[0], buttons[1] });
                        users.MenuKeyboardSelectMatch = new InlineKeyboardMarkup(menu.ToArray());
                    }
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

            }
        } // End select match pages

        public void ChangeSelectPlayersPage(long ChatID, string teamName, int? curPage) // Select players page, voters can select who player has get a goal
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    if (curPage == null) curPage = 0;
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_PlayersFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("TeamName", OracleDbType.Varchar2).Value = teamName;
                    cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    pages.totalPlayersPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                        Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
                        currentUser.VotedPlayer = dataFromClob["Result"][0]["PlayerName"].ToString();

                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    for (var i = 0; i < totalData; i++)
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData(
                           $"{dataFromClob["Result"][i]["PlayerName"]}",
                               $"VotePlayerID{dataFromClob["Result"][i]["PlayerID"]}"
                        ));
                    }
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "Players FirstPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "Players PreventPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalPlayersPages}", "VotePlayers"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "Players NextPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "Players LastPage"));

                    if (currentUser.Language == "ro")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Inapoi", "backToSelectedMatchMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Meniu principal", "mainMenu"));
                    }
                    else if (currentUser.Language == "ru")
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", "backToSelectedMatchMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu"));
                    }
                    else
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "backToSelectedMatchMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));
                    }

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                            i += 4;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardSelectPlayer = new InlineKeyboardMarkup(menu.ToArray());
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

            }
        } // End select players page, voters can select who player has get a goal

        public void VoteFinalTeam(long ChatID, int teamID) // Vote final team function
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("PrognoseFinalTeam", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("VoterChatID", OracleDbType.Int32).Value = ChatID;
                    cmd.Parameters.Add("TeamID", OracleDbType.Int32).Value = teamID;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Warn(exception.Message);
                SetLoggs(null, null, null, exception.Message);

            }
        } // End vote final team function

        public void VoteFromMatch(long ChatID, int? matchID, int? voteType, int? teamScore_1, int? teamScore_2, int? votedPlayer, int? votedTeam) // Vote from match function
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("PrognoseVote", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("VoterChatID", OracleDbType.Int32).Value = ChatID;
                    cmd.Parameters.Add("MatchID", OracleDbType.Int32).Value = matchID;
                    cmd.Parameters.Add("Vote_Type", OracleDbType.Int32).Value = voteType;
                    cmd.Parameters.Add("Team_Score1", OracleDbType.Int32).Value = teamScore_1;
                    cmd.Parameters.Add("Team_Score2", OracleDbType.Int32).Value = teamScore_2;
                    cmd.Parameters.Add("Voted_Player", OracleDbType.Int32).Value = votedPlayer;
                    cmd.Parameters.Add("Voted_Team", OracleDbType.Int32).Value = votedTeam;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                logger.Warn(exception.Message);
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                SetLoggs(null, null, null, exception.Message);

            }
        } // End vote from match function

        public void UpdateMatches(long ChatID, int? matchID, int? fTeamID, int? sTeamID, int? teamScore_1, int? teamScore_2)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Update_Matches", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_MatchID", OracleDbType.Int32).Value = matchID;
                    cmd.Parameters.Add("P_FirstTeamID", OracleDbType.Int32).Value = fTeamID;
                    cmd.Parameters.Add("P_SecondTeamID", OracleDbType.Int32).Value = sTeamID;
                    cmd.Parameters.Add("P_FirstTeamScore", OracleDbType.Int32).Value = teamScore_1;
                    cmd.Parameters.Add("P_SecondTeamScore", OracleDbType.Int32).Value = teamScore_2;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                SetLoggs(null, null, null, exception.Message);

                logger.Warn(exception.Message);
            }
        }

        public void VotePlayerGoals(long ChatID, int? matchID, int? playerID)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Set_Player_Goals", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_MatchID", OracleDbType.Int32).Value = matchID;
                    cmd.Parameters.Add("P_PlayerID", OracleDbType.Int32).Value = playerID;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                SetLoggs(null, null, null, exception.Message);

                logger.Warn(exception.Message);
            }
        }
        
        public void UpdatedMatchPage(long ChatID, int matchID)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_MenuMatchesFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Match_ID", OracleDbType.Int32).Value = matchID;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    if (users.UsersList.Exists(existentUser => existentUser.UserID == ChatID))
                    {
                        Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == ChatID).First();
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
                        currentUser.VotedFirstTeam = dataFromClob["Result"][0]["FirstTeamName"].ToString();
                        currentUser.VotedSecondTeam = dataFromClob["Result"][0]["SecondTeamName"].ToString();
                    }
                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();



                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]}", $"VoteFirstTeamName--{matchID}"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]}", $"VoteSecondTeamName--{matchID}"));

                    buttons.Add(InlineKeyboardButton.WithCallbackData($"Set FirstTeam", $"SetFirstTeamName--{matchID}"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"Set SecondTeam", $"SetSecondTeamName--{matchID}"));

                    buttons.Add(InlineKeyboardButton.WithCallbackData($"👉🏻 Select final score of the match 👈🏻", $"VoteTotal--{matchID}"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]} Players", $"VotePlayers+{dataFromClob["Result"][0]["FirstTeamName"]}+{matchID}"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]} Players", $"VotePlayers+{dataFromClob["Result"][0]["SecondTeamName"]}+{matchID}"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "backToWinnerMenu"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        else if (buttons.Count - 4 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        else if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        else if (buttons.Count - 9 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardUpdatedSelectMatch = new InlineKeyboardMarkup(menu.ToArray());
                }
            }
            catch (Exception exception)
            {
                SetLoggs(null, null, null, exception.Message);

                logger.Warn(exception.Message);
            }
        }

        public string GetPrognoseInfoFromMatch(long ChatID, int? matchID) // Get prognose for one of selected match
        {
            string prognoseDetails = "";
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    if (matchID == null) matchID = 0;
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_PrognoseInfoFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_MatchID", OracleDbType.Int32).Value = matchID;
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    JArray array1 = (JArray)dataFromClob["Players"];
                    var totalPlayersData = array1.Count;
                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();


                    //var keyboardInline = new InlineKeyboardButton[totalData][];
                    prognoseDetails += $"{dataFromClob["Result"][0]["FirstTeamName"]} - {dataFromClob["Result"][0]["FirstTeamScore"]}\n" +
                                    $"{dataFromClob["Result"][0]["SecondTeamName"]} - {dataFromClob["Result"][0]["SecondTeamScore"]}\n\nPlayers with goals:\n";
                    if (totalPlayersData != 0)
                    {
                        for (int i = 0; i < totalPlayersData; i++)
                            prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} From {dataFromClob["Players"][i]["TeamName"]}\n";

                    }

                } 
                return prognoseDetails;
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                SetLoggs(null, null, null, exception.Message);

                logger.Warn(exception.Message);
                return "Something went wrong";
            }
        } // End get prognose for one of selected match

        public void ChangeSelectUpdateWinnerPage(long ChatID, int? curPage) // Select winner page
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    if (curPage == null) curPage = 0;
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_ADDMatchesFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    OracleParameter returnParameter = new OracleParameter();
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.Parameters.Add("P_Match_ID", OracleDbType.Int32).Value = curPage;
                    //OracleClob Clob = new OracleClob(conn);
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var valuesFromClob = myClob.Value;
                    var dataFromClob = JObject.Parse(valuesFromClob);
                    JArray array = (JArray)dataFromClob["Result"];
                    var totalData = array.Count;
                    pages.totalMatchPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
                    for (var i = 0; i < totalData; i++)
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData(
                           $"{dataFromClob["Result"][i]["FirstTeam"]} VS {dataFromClob["Result"][i]["SecondTeam"]} \n\r ( {dataFromClob["Result"][i]["StartDate"]} )",
                               $"MatchID{dataFromClob["Result"][i]["MatchID"]}"
                        ));
                    }
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "Winner FirstPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "Winner PreventPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalMatchPages}", "selectWinner"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "Winner NextPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "Winner LastPage"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("Back", "backToPlayMenu"));
                    buttons.Add(InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu"));

                    var menu = new List<InlineKeyboardButton[]>();
                    for (int i = 0; i < buttons.Count - 1; i++)
                    {
                        if (buttons.Count - 2 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                            i += 1;
                        }
                        if (buttons.Count - 7 == i)
                        {
                            menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                            i += 4;
                        }
                        else if (i != buttons.Count - 1)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                    }
                    users.MenuKeyboardSelectWinner = new InlineKeyboardMarkup(menu.ToArray());
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                SetLoggs(null, null, null, exception.Message);

                logger.Warn(exception.Message);
            }
        } // End select winner page

        public bool UpdateTeamNames(int TeamID, string TeamName)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Update_Teams";
                        cmd.Parameters.Add("P_ID", OracleDbType.Int32).Value = TeamID;
                        cmd.Parameters.Add("P_NAME", OracleDbType.Varchar2).Value = TeamName;
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                _ = request.GetResponse();
                SetLoggs(null, null, null, exception.Message);

                logger.Error(exception.Message);
                return false;
            }
        }

        public void SetLoggs(long? ChatID, string TextMessage, string CallbackData, string ExceptionMessage) // Set new voter function
        {
            try
            {
                //users.SetValuesFromDb();
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "SetLoggs";
                        cmd.Parameters.Add("P_UserID", OracleDbType.Varchar2).Value = ChatID;
                        cmd.Parameters.Add("P_UserMessage", OracleDbType.Varchar2).Value = TextMessage;
                        cmd.Parameters.Add("P_UserCallbackData", OracleDbType.Varchar2).Value = CallbackData;
                        cmd.Parameters.Add("P_ThrowedExceptions", OracleDbType.Varchar2).Value = ExceptionMessage;
                        cmd.ExecuteScalar();
                        
                    }
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                _ = request.GetResponse();
                SetLoggs(null, null, null, exception.Message);
                logger.Error(exception.Message);
            }
        } // End set new voter function

        ///End functions block
        ///
    }
}