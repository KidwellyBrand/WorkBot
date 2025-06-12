using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace WorkBot.Models
{
    public class Menu
    {
        public  static async void  SendMenu(ITelegramBotClient botClient, Message message)
        {
            var stream = File.OpenRead("./Resource/Меню.png");
            var buttonData = new Dictionary<string, string>
            {
                { "✅ Заказ", "order" },
                { "🌟 Отзывы", "reviews" },
                { "📸 Примеры", "examples" },
                { "❓ FAQ", "faq" }
            };

            var keyboard = MyCallbackQuery.CreateKeyboard(buttonData);

            await botClient.SendPhoto(chatId: message.Chat.Id,
                                   photo: InputFile.FromStream(stream),
                                   replyMarkup: keyboard);
        }
    }
}
