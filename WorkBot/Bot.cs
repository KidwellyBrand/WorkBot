using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Timer = System.Timers.Timer;
using NLog;
using WorkBot.Storage;
using WorkBot.Settings;

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
        { "start", StartCommand },
        { "register", RegisterCommand },
        { "profile", ProfileCommand },
        { "forget", ForgetCommand }
    };

    /// <summary>
    /// Конструктор бота
    /// </summary>
    /// <param name="token">Токен доступа</param>
    public Bot(string token)
    {
        timer = new Timer(1000); // интервал срабатывания - 1 секунда
        timer.Elapsed += Timer_Elapsed;
        HttpClient httpClient = new();
        client = new TelegramBotClient(token, httpClient);
        client.OnMessage += Client_OnMessage;
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
                             (x.State == Enums.State.Download || x.State == Enums.State.Register))
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
    /// <param name="type"></param>
    /// <returns></returns>
    private Task Client_OnMessage(Message message, UpdateType type)
    {
        try
        {
            switch (type)
            {
                case UpdateType.Message:
                    ProcessMessage(message);
                    break;

                default:
                    SendText(message.Chat.Id, $"Не поддерживается сообщение типа {type}", LogLevel.Warn);
                    break;
            }
        }
        catch (Exception ex)
        {
            SendText(message.Chat.Id, $"Внутренняя ошибка: {ex.Message}", LogLevel.Warn);
        }
        return Task.CompletedTask;
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
        user ??= new BotUser()
            {
                ID = message.Chat.Id
            };
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
            case Enums.State.Register:
                RegisterText(message, db, user);
                break;
            case Enums.State.Download:
                break;
            default:
                SendText(message.Chat.Id, $"Вы прислали мне {message.Text}");
            break;
  
        }

    }

    private void RegisterText(Message message, DB db, BotUser user)
    {
        switch (message.Text)
        {
            case Text.Register:
                user.UserName = message.Chat.Username;
                user.FirstName = message.Chat.FirstName;
                user.LastName = message.Chat.LastName;
                user.IsRegistred = true;
                user.State = Enums.State.Basic;
                db.SaveChanges();
                SendText(message.Chat.Id, $"Вы зарегистрированы");
                break;
            default:
                SendText(message.Chat.Id, $"Вы зачем-то прислали мне это: {message.Text}");
                break;
        }
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

    private static void StartCommand(Message message)
    {
        SendText(message.Chat.Id, $"Здравствуйте, {message.Chat.Username}");
    }
    private static void RegisterCommand(Message message)
    {
        using var db= new DB();
        BotUser? user = db.Users.Find(message.Chat.Id);
        if (user == null)
        {
            user = new BotUser()
            {
                ID = message.Chat.Id,
                State = Enums.State.Register
            };
            db.Users.Add(user);
        }
        else
        {
            user.State = Enums.State.Register;
        }
        db.SaveChanges();
        // Клавиатура из 3-х кнопок
        var keyboard = new KeyboardButton[]
        {
            new(Text.Register),
            new (Text.Cancel)
        };
        //Клавиатурная разметка
        var markup = new ReplyKeyboardMarkup(keyboard)
        {
            ResizeKeyboard = true
        };
        client.SendMessage(message.Chat.Id, $"Прошу вас зарегистрироваться", replyMarkup: markup);

    }
    private static void ProfileCommand(Message message)
    {
        using var db = new DB(true);
        db.EnableLogging = true;
        
        BotUser? user = db.Users.Find(message.Chat.Id);
        if (user == null || !user.IsRegistred)
        {
            SendText(message.Chat.Id, "Вы не зарегистрированы");
            return;
        }

        string s;
        s = $"@{user.UserName} {user.FirstName} {user.LastName}, вы зарегистрированы";
        SendText(message.Chat.Id, s);
    }
    private static void ForgetCommand(Message message)
    {
        using var db = new DB();
        BotUser? user = db.Users.Find(message.Chat.Id);
        if (user == null || !user.IsRegistred)
        {
            SendText(message.Chat.Id, "Вы не зарегистрированы");
            return;
        }
        db.Users.Remove(user);
        db.SaveChanges();
        SendText(message.Chat.Id, "Регистрация удалена");
    }
    

}