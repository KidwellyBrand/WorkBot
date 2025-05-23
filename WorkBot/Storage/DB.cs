using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkBot.Settings;
using Microsoft.EntityFrameworkCore;

namespace WorkBot.Storage
{
    public class DB : DbContext
    {
        /// <summary>
        /// Протоколирование
        /// </summary>
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly bool useLazyProxy;

        /// <summary>
        /// Управление протоколированием
        /// </summary>
        public bool EnableLogging { get; set; }

        public DB(bool useLazyProxy = false)
        {
            this.useLazyProxy = useLazyProxy;
        }

        /// <summary>
        /// Пользователи
        /// </summary>
        public virtual DbSet<BotUser> Users { get; set; }  

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Имя файла базы данных
            string file = Config.Get("File");
            // база данных SQLite
            optionsBuilder.UseSqlite($"Data Source={file};");

            if (useLazyProxy)
            {
                optionsBuilder.UseLazyLoadingProxies();
            }

            // трассировка SQL-запросов
            optionsBuilder.LogTo(message =>
            {
                if (EnableLogging)
                {
                    log.Trace(message);
                }
            }, Microsoft.Extensions.Logging.LogLevel.Information);
        }
    }

}
