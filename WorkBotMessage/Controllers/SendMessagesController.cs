using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WorkBot.Storage;
using NLog;

namespace WorkBotMessage.Controllers
{
    public class SendMessagesController : Controller
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static TelegramBotClient client;

        public SendMessagesController(IConfiguration configuration)
        {
            var token = configuration["Token"];
            client = new TelegramBotClient(token);
        }

        // Главная страница с формой отправки и выводом сообщений
        public async Task<IActionResult> Index()
        {
            using var db = new DB();
            var messages = await db.MessageLogs
                .OrderByDescending(m => m.Date)
                .Take(50)
                .ToListAsync();

            ViewBag.MessageLogs = messages;

            return View();
        }

        // Поиск пользователя по ID
        [HttpPost]
        public async Task<IActionResult> FindUser(long userId)
        {
            using var db = new DB();

            var user = await db.Users.FirstOrDefaultAsync(u => u.ID == userId);

            var messages = await db.MessageLogs
                .OrderByDescending(m => m.Date)
                .Take(50)
                .ToListAsync();

            ViewBag.MessageLogs = messages;

            if (user == null)
            {
                var errMsg = $"Пользователь с ID {userId} не найден при поиске.";
                ViewBag.Error = errMsg;
                log.Warn(errMsg);
                return View("Index");
            }

            return View("Index", user);
        }

        // Отправка сообщения пользователю
        [HttpPost]
        public async Task<IActionResult> SendMessage(long userId, string message)
        {
            using var db = new DB();

            var user = await db.Users.FirstOrDefaultAsync(u => u.ID == userId);

            var messages = await db.MessageLogs
                .OrderByDescending(m => m.Date)
                .Take(50)
                .ToListAsync();

            ViewBag.MessageLogs = messages;

            if (user == null)
            {
                var errMsg = $"Пользователь с ID {userId} не найден при попытке отправить сообщение.";
                ModelState.AddModelError("", errMsg);
                log.Warn(errMsg);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                var errMsg = "Попытка отправить пустое сообщение.";
                ModelState.AddModelError("", errMsg);
                log.Warn(errMsg);
            }

            if (!ModelState.IsValid)
            {
                return View("Index", user);
            }

            try
            {
                await client.SendMessage(chatId: user.ID, text: message);
                ViewBag.Success = "Сообщение успешно отправлено!";
                log.Info($"Сообщение отправлено пользователю {user.ID} ({user.UserName ?? "без username"}). Текст: {message}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Ошибка при отправке сообщения пользователю {user.ID}: {ex.Message}";
                ModelState.AddModelError("", errorMessage);
                log.Error(ex, errorMessage);
            }

            return View("Index", user);
        }
    }
}

