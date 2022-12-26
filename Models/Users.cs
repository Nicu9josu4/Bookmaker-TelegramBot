using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;

namespace BookmakerTelegramBot.Models
{
    public class Users
    {
        public Int64 UserID { get; set; }
        public Int64? MessageID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Registration { get; set; } 
        public bool? IntroduceTotal { get; set; }
        public bool? ReportAProblem { get; set; }
        public string Language { get; set; } // en-US, ro-RO, ru-RU
        public int? CurentMatchPage { get; set; }
        public int? CurentTeamPage { get; set; }
        public int? CurentPlayersPage { get; set; }
        public int? CurentTopVotersPage { get; set; }
        public int? CurentHistoryPage { get; set; }
        public string TextMessage { get; set; }
        public string CallBackData { get; set; }
        public int? MatchID { get; set; }
        public string VotedFirstTeam { get; set; }
        public string VotedSecondTeam { get; set; }
        public string VotedPlayer { get; set; }
        public string VotedPlayerTeam { get; set; }
        public string VotedFinalTeam { get; set; }

        public InlineKeyboardMarkup MenuKeyboardSelectFinalWinnerTeam { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectWinner { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectMatch { get; set; }
        public InlineKeyboardMarkup MenuKeyboardUpdatedSelectMatch { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectPlayer { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectTopVoter { get; set; }
        public InlineKeyboardMarkup MenuKeyboardHistory { get; set; }

        public List<Users> UsersList = new List<Users>();
        public void SetValuesFromDb(string connectionString)
        {
            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                OracleCommand cmd = new OracleCommand("Get_VotersFunc", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                cmd.ExecuteNonQuery();

                OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                var DataFromClob = myClob.Value;
                var ValuesFromClob = JObject.Parse(DataFromClob);
                JArray array = (JArray)ValuesFromClob["Result"];
                var totalData = array.Count;
                for (int i = 0; i < totalData; i++)
                    UsersList.Add(new Users()
                    {
                        UserID = Convert.ToInt64(ValuesFromClob["Result"][i]["ChatID"]),
                        Language = ValuesFromClob["Result"][i]["Language"].ToString(),
                        FirstName = ValuesFromClob["Result"][i]["FirstName"].ToString(),
                        LastName = ValuesFromClob["Result"][i]["LastName"].ToString(),
                        VotedFinalTeam = ValuesFromClob["Result"][i]["TeamName"].ToString()
                    });
            }
        }
    }
}