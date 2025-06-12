using Telegram.Bot;
using Telegram.Bot.Types;

namespace WorkBot.Models
{
    public static class RemoveButtonInline
    {
        public static async Task RemoveButtonInAsync(ITelegramBotClient botClient, CallbackQuery callback, long id)
        {
            await botClient.DeleteMessage(
            chatId: id,
            messageId: callback.Message.MessageId
        );
        }
    }
}
