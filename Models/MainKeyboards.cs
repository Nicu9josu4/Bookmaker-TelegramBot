using Telegram.Bot.Types.ReplyMarkups;

namespace BookmakerTelegramBot.Models
{
    public class MainKeyboards
    {
        public InlineKeyboardMarkup MenuKeyboardSelectFinalWinnerTeam { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectWinner { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectMatch { get; set; }
        public InlineKeyboardMarkup MenuKeyboardUpdatedSelectMatch { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectPlayer { get; set; }
        public InlineKeyboardMarkup MenuKeyboardSelectTopVoter { get; set; }
        public InlineKeyboardMarkup MenuKeyboardHistory { get; set; }

        //public InlineKeyboardMarkup menuKeyboardSelectFinalWinnerTeam = new InlineKeyboardMarkup(new[]{
        //    new[]
        //    {
        //        InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
        //    }
        //});
        //public InlineKeyboardMarkup menuKeyboardSelectWinner = new InlineKeyboardMarkup(new[]{
        //    new[] 
        //    { 
        //        InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu") 
        //    }
        //});
        //public InlineKeyboardMarkup menuKeyboardSelectMatch = new InlineKeyboardMarkup(new[]{
        //    new[]
        //    {
        //        InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
        //    }
        //});
        //public InlineKeyboardMarkup menuKeyboardSelectPlayer = new InlineKeyboardMarkup(new[]{
        //    new[]
        //    {
        //        InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
        //    }
        //});
        //public InlineKeyboardMarkup menuKeyboardSelectTopVoter = new InlineKeyboardMarkup(new[]{
        //    new[]
        //    {
        //        InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
        //    }
        //});
        //public InlineKeyboardMarkup menuKeyboardHistory = new InlineKeyboardMarkup(new[]{
        //    new[]
        //    {
        //        InlineKeyboardButton.WithCallbackData("Main Menu", "mainMenu")
        //    }
        //});
    }
}
