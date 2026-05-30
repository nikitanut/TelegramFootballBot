using Telegram.Bot.Types;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands;

public class RegisterCommand(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService) : Command
{
    public override string Name => "/reg";

    public override async Task ExecuteAsync(Message message)
    {
        var playerName = GetPlayerNameFrom(message);
        if (playerName == string.Empty)
        {
            await messageService.SendMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя");
            return;
        }

        var messageForUser = await RegisterPlayer(message);

        await Task.WhenAll(
        [
            messageService.SendMessageAsync(message.Chat.Id, messageForUser),
            sheetService.UpsertPlayerAsync(playerName),
            messageService.SendMessageToBotOwnerAsync($"{playerName} зарегистрировался")
        ]);
    }

    private string GetPlayerNameFrom(Message message)
    {
        return message.Text!.Length > Name.Length ? message.Text[Name.Length..].Trim() : string.Empty;
    }

    private async Task<string> RegisterPlayer(Message message)
    {
        var playerName = GetPlayerNameFrom(message);

        try
        {
            var existingPlayer = await playerRepository.GetAsync(message.From!.Id);
            var messageForUser = existingPlayer.Name == playerName ? "Вы уже зарегистрированы" : "Вы уже были зарегистрированы. Имя обновлено.";
            existingPlayer.Name = playerName;
            await playerRepository.UpdateAsync(existingPlayer);
            return messageForUser;
        }
        catch (UserNotFoundException)
        {
            var newPlayer = new Player(message.From!.Id, playerName, message.Chat.Id);
            await playerRepository.AddAsync(newPlayer);
            return "Регистрация прошла успешно";
        }
    }
}