
using Telegram.Bot.Types.ReplyMarkups;

namespace WorkBot.Models;

public class MyCallbackQuery
{
    public static InlineKeyboardMarkup CreateKeyboard(Dictionary<string, string> buttonData)
    {
        // Создаем список строк, каждая строка — один элемент списка кнопок
        var rows = new List<List<InlineKeyboardButton>>();

        foreach (var pair in buttonData)
        {
            // Каждая кнопка — отдельная строка с одним элементом
            var row = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(pair.Key, pair.Value)
            };
            rows.Add(row);
        }

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup CreateKeyboardUrl(Dictionary<string, string> buttonData)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        foreach (var pair in buttonData)
        {
            var row = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithUrl(pair.Key, pair.Value)
            };
            rows.Add(row);
        }

        return new InlineKeyboardMarkup(rows);
    }
}

