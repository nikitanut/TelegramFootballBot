using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Helpers;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Models.CallbackQueries;

namespace TelegramFootballBot.App.Services;

public class UpdateHandler(CommandFactory commandFactory, IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery),
            _ => UnknownUpdateHandlerAsync(update)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message)
    {
        var command = commandFactory.Create(message);
        if (command is null)
            return;

        try
        {
            await command.ExecuteAsync(message);
            var playerName = await GetPlayerNameAsync(message.From!.Id);
            logger.Information($"Command {message.Text} processed for user {playerName}");
        }
        catch (Exception ex)
        {
            var playerName = await GetPlayerNameAsync(message.From!.Id);
            logger.Error(ex, $"Error on processing {message.Text} command for user {playerName}");
            await messageService.SendMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
            await messageService.SendErrorMessageToUserAsync(message.Chat.Id, playerName);
        }
    }

    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        try
        {
            var callbackData = callbackQuery.Data;
            if (string.IsNullOrEmpty(callbackData))
                return;

            if (Callback.GetCallbackName(callbackData) == PlayerSetCallback.Name)
                await DetermineIfPlayerIsReadyToPlayAsync(callbackQuery);

            logger.Information($"Processed callback: {callbackQuery.Data}");
        }
        catch (Exception ex)
        {
            await ProcessCallbackError(callbackQuery, ex);
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.Information("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private async Task<string> GetPlayerNameAsync(long userId)
    {
        try
        {
            return (await playerRepository.GetAsync(userId)).Name;
        }
        catch (UserNotFoundException)
        {
            return string.Empty;
        }
    }

    private async Task DetermineIfPlayerIsReadyToPlayAsync(CallbackQuery callbackQuery)
    {
        var playerSetCallback = new PlayerSetCallback(callbackQuery.Data!);
        await ClearInlineKeyboardAsync(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId);

        try
        {
            await messageService.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error on deleting message");
        }

        if (IsButtonPressedAfterGame(playerSetCallback.GameDate))
        {
            logger.Information($"Button pressed after game: now - {DateTime.UtcNow}, game date - {playerSetCallback.GameDate.Date}");
            return;
        }

        var player = await playerRepository.GetAsync(callbackQuery.From.Id);
        await sheetService.SetApproveCellAsync(player.Name, GetApproveCellValue(playerSetCallback.UserAnswer));

        var approvedPlayersMessage = await sheetService.BuildApprovedPlayersMessageAsync();
        player.IsGoingToPlay = playerSetCallback.UserAnswer == Constants.YES_ANSWER;
        player.ApprovedPlayersMessageId = await SendApprovedPlayersMessageAsync(approvedPlayersMessage, callbackQuery.Message.Chat.Id, player);
        player.ApprovedPlayersMessage = approvedPlayersMessage;

        await playerRepository.UpdateAsync(player);
    }

    /// <summary>
    /// Sends an approved players message if it wasn't sent yet. Otherwise edits it.
    /// </summary>
    /// <param name="chatId">Player chat id</param>
    /// <param name="player">Player</param>
    /// <returns>Sent message id</returns>
    private async Task<int> SendApprovedPlayersMessageAsync(string message, ChatId chatId, Player player)
    {
        if (player.ApprovedPlayersMessageId != 0)
            await messageService.DeleteMessageAsync(chatId, player.ApprovedPlayersMessageId);

        var messageResponse = await messageService.SendMessageAsync(chatId, message);
        return messageResponse.MessageId;
    }

    private static string GetApproveCellValue(string userAnswer)
    {
        return userAnswer switch
        {
            Constants.YES_ANSWER => "1",
            Constants.NO_ANSWER => "0",
            Constants.MAYBE_ANSWER => "0.5",
            _ => throw new ArgumentOutOfRangeException($"userAnswer: {userAnswer}"),
        };
    }

    private static bool IsButtonPressedAfterGame(DateTime gameDate)
    {
        return gameDate.Date < DateTime.Now.Date;
    }

    private async Task ClearInlineKeyboardAsync(ChatId chatId, int messageId)
    {
        try
        {
            await messageService.ClearReplyMarkupAsync(chatId, messageId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error on clearing inline keyboard");
        }
    }

    private async Task ProcessCallbackError(CallbackQuery callbackQuery, Exception ex)
    {
        var messageForUser = string.Empty;
        var messageForBotOwner = string.Empty;
        var userId = callbackQuery.From.Id;
        var player = ex is UserNotFoundException ? null : await playerRepository.GetAsync(userId);

        switch (ex)
        {
            case UserNotFoundException:
                logger.Error($"User with id {userId} not found. Name: {callbackQuery.From.FirstName} {callbackQuery.From.LastName}");
                messageForUser = "Вы не зарегистрированы. Введите команду /reg Фамилия Имя.";
                messageForBotOwner = $"Пользователь {callbackQuery.From.FirstName} {callbackQuery.From.LastName} не найден";
                break;
            case TotalsRowNotFoundExeption:
                logger.Error("\"Всего\" row not found in excel-file");
                messageForUser = "Не найдена строка \"Всего\" в excel-файле.";
                messageForBotOwner = $"Не найдена строка \"Всего\" в excel-файле. Пользователь - {player!.Name}";
                break;
            case OperationCanceledException:
                logger.Error($"Operation {callbackQuery.Data} cancelled for user {player!.Name}.");
                messageForUser = "Не удалось обработать запрос.";
                messageForBotOwner = $"Операция обработки ответа отменена для пользователя {player.Name}";
                break;
            case ArgumentException exception:
                logger.Error($"Unexpected response for user {player!.Name}: {exception.ParamName}");
                messageForUser = "Непредвиденный вариант ответа.";
                messageForBotOwner = $"Непредвиденный вариант ответа для пользователя {player.Name}";
                break;
        }

        if (messageForUser == string.Empty)
        {
            logger.Error(ex, "Unexpected error");
            messageForUser = "Непредвиденная ошибка.";
            messageForBotOwner = $"Ошибка у пользователя {player!.Name}: {ex.Message}";
        }

        await NotifyAboutError(callbackQuery.Message!.Chat.Id, messageForUser, messageForBotOwner);
    }

    private async Task NotifyAboutError(ChatId chatId, string messageForUser, string messageForBotOwner)
    {
        await messageService.SendMessageAsync(chatId, messageForUser);

        if (AppSettings.NotifyOwner)
            await messageService.SendMessageToBotOwnerAsync(messageForBotOwner);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.Error("Polling failed with exception: {Exception}", exception);
        return Task.CompletedTask;
    }
}