using WorkBot.Enums;

namespace WorkBot.Storage
{
    /// <summary>
    /// Пользователь бота
    /// </summary>
    public class BotUser
    {
        /// <summary>
        /// Идентификатор пользователя в Telegram
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// Регистрационное имя пользователя
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Фамилия пользователя
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Состояние пользователя
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// Признак зарегистрированного пользователя
        /// </summary>
        public bool IsRegistred { get; set; }

        /// <summary>
        /// Время последнего взаимодействия с пользователем (UTC)
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}
