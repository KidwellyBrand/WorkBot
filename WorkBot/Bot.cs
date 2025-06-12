using NLog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WorkBot.Models;
using WorkBot.Settings;
using WorkBot.Storage;
using static System.Net.WebRequestMethods;
using Timer = System.Timers.Timer;

namespace WorkBot;

public class Bot
{
    /// <summary>
    /// Протоколирование
    /// </summary>
    private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Клиент Telegram
    /// </summary>
    private static TelegramBotClient client;

    /// <summary>
    /// Таймер для проверки активности пользователей
    /// </summary>
    private readonly Timer timer;

    private delegate void CommandDelegate(Message message);
    private static readonly Dictionary<string, CommandDelegate> commands = new()
    {
        { "start", StartCommand},
        { "menu",MenuCommand}
    };
    /// <summary>
    /// Конструктор бота
    /// </summary>
    /// <param name="token">Токен доступа</param>
    public Bot(string token)
    {
        int delay = Config.Get<int>("Timeout", 60);
        DateTime limit = DateTime.UtcNow.AddSeconds(-delay);
        DateTime minValid = DateTime.UtcNow.AddHours(-1); // чтобы не реагировать на default

        timer = new Timer(1000); // интервал срабатывания - 1 секунда
        timer.Elapsed += Timer_Elapsed;
        HttpClient httpClient = new();
        client = new TelegramBotClient(token, httpClient);
        client.OnUpdate += Client_OnUpdate;
    }
    /// <summary>
    /// Тик таймера
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        int delay = Config.Get<int>("Timeout", 60);
        DateTime limit = DateTime.UtcNow.AddSeconds(-delay);
        DateTime minValid = DateTime.UtcNow.AddHours(-1); // чтобы не реагировать на default

        using var db = new DB();
        foreach (var user in db.Users
                 .Where(x => x.TimeStamp < limit && x.TimeStamp > minValid &&
                             (x.State == Enums.State.Order))
                 .ToList())
        {
            SendText(user.ID, $"Вы про меня забыли");
            user.State = Enums.State.Basic;
            db.SaveChanges();
        }
    }

    /// <summary>
    /// Запуск бота
    /// </summary>
    public void Start()
    {
        // Установление соединения и определение текущего пользователя
        var user = client.GetMe().Result;

        timer.Start();

        log.Info($"Подключение выполнено: {user.Username}");
    }

    public void Stop()
    {
        timer.Stop();
    }
    /// <summary>
    /// Отправка сообщения пользователю с протоколированием
    /// </summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="message">Текст сообщения</param>
    /// <param name="level">Уровень сообщения для протоколирования</param>
    private static void SendText(long id, string message, LogLevel? level = null)
    {
        if (level == null)
        {
            level = LogLevel.Info;
        }
        log.Log(level, message);
        // отправка сообщения с закрытием клавиатуры, если она была открыта ранее
        client.SendMessage(id, message, replyMarkup: new ReplyKeyboardRemove());
    }
    /// <summary>
    /// Обработка входящих сообщений разных типов
    /// </summary>
    /// <param name="message"></param>
    /// <param name="update"></param>
    /// <returns></returns>
    private async Task Client_OnUpdate(Update update)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message != null)
                        ProcessMessage(update.Message);
                    break;

                case UpdateType.CallbackQuery:
                    if (update.CallbackQuery != null)
                        await ProcessCallbackQuery(update.CallbackQuery);
                    break;

                default:
                    if (update.Message != null)
                        SendText(update.Message.Chat.Id, $"Не поддерживается сообщение типа {update.Type}", LogLevel.Warn);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (update.Message != null)
                SendText(update.Message.Chat.Id, $"Внутренняя ошибка: {ex.Message}", LogLevel.Warn);
        }
    }

    private async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
    {
        await client.AnswerCallbackQuery(callbackQuery.Id);
        using var db = new DB();
        BotUser? user = db.Users.Find(callbackQuery.Message.Chat.Id);
        if (user != null)
        {
            // Обновляем время активности пользователя, чтобы таймер "забыл"
            user.TimeStamp = DateTime.UtcNow;
            db.SaveChanges();
        }
        else 
        {
            user = new BotUser()
            {
                ID = callbackQuery.Message.Chat.Id,
                UserName = callbackQuery.Message.Chat.Username,
                FirstName = callbackQuery.Message.Chat.FirstName,
                TimeStamp = DateTime.UtcNow,
            };
            db.Users.Add(user);
            db.SaveChanges();
        }
        switch (callbackQuery.Data)
        {
            case "order":
                user.State = Enums.State.Order;
                var buttonData = new Dictionary<string, string>
                {
                    { "🪧Описание для карточки товара", "description" },
                    { "📄Перевод текста с сохранением стиля", "translate" },
                    { "🛍Описание для продажи товара", "descorder" },
                    { "📚Поздравления и стихи на заказ","poems" },
                    {"📇Резюме","resume" }
                };

                var keyboard = MyCallbackQuery.CreateKeyboard(buttonData);

                await client.SendMessage(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "Выберите интересующий вас вариант:",
                    replyMarkup: keyboard
                );
                await RemoveButtonInline.RemoveButtonInAsync(client, callbackQuery, callbackQuery.Message.Chat.Id);

                break;
            case "description":
            case "translate":
            case "descorder":
            case "poems":
            case "resume":
                user.State = Enums.State.Order;
                user.OrderType = callbackQuery.Data; // Сохраняем подтип заказа
                db.SaveChanges();
                await client.SendMessage(callbackQuery.Message.Chat.Id, "Введите текст для обработки:");
                await RemoveButtonInline.RemoveButtonInAsync(client, callbackQuery, callbackQuery.Message.Chat.Id);
                break;
            case "faq":
                await client.SendMessage(callbackQuery.Message.Chat.Id, "Вот ответы на частые вопросы...");
                await RemoveButtonInline.RemoveButtonInAsync(client, callbackQuery, callbackQuery.Message.Chat.Id);
                break;
            case "reviews":
                var buttonUrl = new Dictionary<string, string>
                {
                    { "Перейти к отзывам", "https://t.me/+78nH4Y-sV3ZkN2Uy" },
                }
        ;
                var keyboardUrl = MyCallbackQuery.CreateKeyboardUrl(buttonUrl);
                await client.SendMessage(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "Нажмите кнопку ниже, чтобы перейти к отзывам:",
                    replyMarkup: keyboardUrl);
                await RemoveButtonInline.RemoveButtonInAsync(client, callbackQuery, callbackQuery.Message.Chat.Id);
                break;
            case "examples":
                 buttonUrl = new Dictionary<string, string>
                {
                    { "Перейти к примерам", "https://t.me/+Yav5LWHXJk4zNDYy" }
                };
                keyboardUrl = MyCallbackQuery.CreateKeyboardUrl(buttonUrl);
                await client.SendMessage(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "Нажмите кнопку ниже, чтобы перейти к примерам:",
                    replyMarkup: keyboardUrl);
                await RemoveButtonInline.RemoveButtonInAsync(client, callbackQuery, callbackQuery.Message.Chat.Id);
                break;

            default:
                await client.SendMessage(callbackQuery.Message.Chat.Id, "Неизвестная команда.");
                break;
        }
    }

    /// <summary>
    /// Обработка входящих сообщений
    /// </summary>
    /// <param name="message"></param>
    private void ProcessMessage(Message message)
    {
        // Обновление метки времени пользователя
        using (var db = new DB())
        {
            BotUser? user = db.Users.Find(message.Chat.Id);
            if (user != null)
            {
                user.TimeStamp = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        // Обработка сообщений осуществляется отдельными методами, которые именуются
        // следующий образом: Process<имя типа сообщения>
        // Метод должен принимать один параметр типа Message
        // См. метод ProcessSticker(message)
        string methodName = $"Process{message.Type}";

        var methods = GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(x => x.Name == methodName
                && x.GetParameters().Length == 1
                && x.GetParameters().First().ParameterType == typeof(Message));
        // Проверка на наличие метода
        if (!methods.Any())
        {
            SendText(message.Chat.Id, $"Не поддерживается сообщение типа {message.Type}", LogLevel.Warn);
            return;
        }
        if (methods.Count() > 1)
        {
            SendText(message.Chat.Id, $"Найдено больше одного метода для сообщений типа {message.Type}", LogLevel.Warn);
            return;
        }

        methods.First().Invoke(this, [message]);
    }
    /// <summary>
    /// Обработка входящего текста
    /// </summary>
    /// <param name="message"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Метод вызывается через рефлексию")]
    private void ProcessText(Message message)
    {
        if (message.Text!.StartsWith('/'))
        {
            ProcessCommand(message);
            return;
        }

        using var db = new DB();
        BotUser? user = db.Users.Find(message.Chat.Id);
        if (user == null)
        {
            user = new BotUser()
            {
                ID = message.Chat.Id,
                UserName = message.Chat.Username,
                FirstName = message.Chat.FirstName
            };
            db.Users.Add(user);  // Добавляем только для нового пользователя
        }
        db.SaveChanges();
        // Обработка текста "Отмена" как универсального действия
        // вне зависимости от состояния
        if (message.Text == Text.Cancel)
        {
            user.State = Enums.State.Basic;
            db.SaveChanges();
            SendText(message.Chat.Id, $"Команда отменена");
            return;
        }
        switch (user.State)
        {
            case Enums.State.Order:
                OrderText(message, db, user);
                break;
            default:
                SendText(message.Chat.Id, $"Вы прислали мне {message.Text}");
                break;
        }
    }

    private void OrderText(Message message, DB db, BotUser user)
    {
        switch (user.OrderType)
        {
            case "description":
            case "translate":
            case "descorder":
            case "poems":
            case "resume":
                MessageLog messageLog = new()
                {
                    Username = message.Chat.Username,
                    ChatId = message.Chat.Id,
                    Date = DateTime.Now,
                    Text = message.Text!,
                };
                db.MessageLogs.Add(messageLog);
                db.SaveChanges();
                SendText(message.Chat.Id, $"Ваш заказ принят");
                break;
            default:
                SendText(message.Chat.Id, "Неизвестный тип заказа. Пожалуйста, начните заново.", LogLevel.Warn);
                break;
        }

        user.State = Enums.State.Basic;
        user.OrderType = null;
        db.SaveChanges();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Метод вызывается через рефлексию")]
    private void ProcessStiker(Message message)
    {
        SendText(message.Chat.Id, $"Спасибо за стикер");
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Метод вызывается через рефлексию")]
    private void ProcessAnimation(Message message)
    {
        SendText(message.Chat.Id, $"Спасибо за гифку!");
    }
    /// <summary>
    /// Обработка входящих команд
    /// </summary>
    /// <param name="message"></param>
    private void ProcessCommand(Message message)
    {
        string cmd = message.Text![1..].ToLower();

        // ищем команду в словаре команд
        if (commands.TryGetValue(cmd, out var command))
        {
            command(message);
        }
        else
        {
            SendText(message.Chat.Id, $"Не поддерживается команда {cmd}", LogLevel.Warn);
        }
    }

    private static async void StartCommand(Message message)
    {
        Menu.SendMenu(client, message);
    }
    private static async void MenuCommand(Message message)
    {
        Menu.SendMenu(client, message);
    }

}