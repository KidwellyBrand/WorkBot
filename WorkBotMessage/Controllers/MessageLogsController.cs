using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkBot.Storage;

namespace WorkBotMessage.Controllers
{
    public class MessageLogsController : Controller
    {
        private readonly DB db;

        public MessageLogsController(DB context)
        {
            db = context;
        }

        // GET: BotMessageLogs
        public async Task<IActionResult> Index()
        {
            return View(await db.MessageLogs.ToListAsync());
        }

        // GET: BotMessageLogs/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var botUser = await db.MessageLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (botUser == null)
            {
                return NotFound();
            }

            return View(botUser);
        }

        // GET: BotMessageLogs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BotMessageLogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,UserName,FirstName,LastName,State,OrderType,IsRegistred,TimeStamp")] BotUser botUser)
        {
            if (ModelState.IsValid)
            {
                db.Add(botUser);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(botUser);
        }

        // GET: BotMessageLogs/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var botUser = await db.MessageLogs.FindAsync(id);
            if (botUser == null)
            {
                return NotFound();
            }
            return View(botUser);
        }

        // POST: BotMessageLogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        private bool BotUserExists(Guid id)
        {
            return db.MessageLogs.Any(e => e.Id == id);
        }
    }
}
