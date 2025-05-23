using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkBot.Enums
{
    /// <summary>
    /// Состояние пользователя
    /// </summary>
    public enum State
    {
        /// <summary>
        /// Основное состояние бота
        /// </summary>
        Basic,
        /// <summary>
        /// Регистрация
        /// </summary>
        Register,
        /// <summary>
        /// Запрос прогноза погоды
        /// </summary>
        Download,
    }
}
