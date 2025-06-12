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
    public class BotUsersController : Controller
    {
        private readonly DB db;

        public BotUsersController(DB context)
        {
            db = context;
        }

        // GET: BotUsers
        public async Task<IActionResult> Index()
        {
            return View(await db.Users.ToListAsync());
        }

        // GET: BotUsers/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var botUser = await db.Users
                .FirstOrDefaultAsync(m => m.ID == id);
            if (botUser == null)
            {
                return NotFound();
            }

            return View(botUser);
        }

        // GET: BotUsers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BotUsers/Create
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

        // GET: BotUsers/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var botUser = await db.Users.FindAsync(id);
            if (botUser == null)
            {
                return NotFound();
            }
            return View(botUser);
        }

        // POST: BotUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("ID,UserName,FirstName,LastName,State,OrderType,IsRegistred,TimeStamp")] BotUser botUser)
        {
            if (id != botUser.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Update(botUser);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BotUserExists(botUser.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(botUser);
        }

        // GET: BotUsers/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var botUser = await db.Users
                .FirstOrDefaultAsync(m => m.ID == id);
            if (botUser == null)
            {
                return NotFound();
            }

            return View(botUser);
        }

        // POST: BotUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var botUser = await db.Users.FindAsync(id);
            if (botUser != null)
            {
                db.Users.Remove(botUser);
            }

            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BotUserExists(long id)
        {
            return db.Users.Any(e => e.ID == id);
        }
    }
}
