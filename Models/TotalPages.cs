using Telegram.Bot.Types.ReplyMarkups;

namespace BookmakerTelegramBot.Models
{
    public class TotalPages
    {
        public int? totalMatchPages { get; set; }
        public int? totalTeamPages { get; set; }
        public int? totalPlayersPages { get; set; }
        public int? totalTopVotersPages { get; set; }
        public int? totalHistoryPages { get; set; }
    }
}
