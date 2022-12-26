using BookmakerTelegramBot.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Resources;
using Telegram.Bot;
using BookmakerTelegramBot.Controllers;
using System.Reflection;
using Keyboards = BookmakerTelegramBot.Models.MainKeyboards;
using TotalPages = BookmakerTelegramBot.Models.TotalPages;
using Users = BookmakerTelegramBot.Models.Users;
using Telegram.Bot.Extensions.Polling;
using System.Net;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using System.Linq;

namespace BookmakerTelegramBot.Sevices
{
    public class TelegramLogginingClientConfigure
    {
        public string Token;
        public TelegramBotClient bot;
        public async Task StartClient()
        {
            bot = new TelegramBotClient(Token);

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

        public async Task SendMessage(string Message)
        {
            await bot.SendTextMessageAsync(840323224, Message);
        }

        public async Task SendReportMessage(string Message)
        {
            try
            {
                await bot.SendTextMessageAsync(840323224, Message);
                await bot.SendTextMessageAsync(1132570191, Message);
            }
            catch(Exception ex)
            {
                _ = SendMessage(ex.Message);
            }
        }
        private async Task HandleUpdatesAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                //Console.WriteLine("Message:       ID:" + update.Message.Chat.Id + ", Text:        " + update.Message.Text);
                await bot.SendTextMessageAsync(update.Message.Chat.Id, update.Message.ToString());
                
                return;
            }
            


            return;
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
            return Task.CompletedTask;
        }
    }
}
