using Microsoft.Extensions.Configuration;

namespace WorkBot.Settings;
/// <summary>
/// Конфигурация приложения
/// </summary>
public static class Config
{
    /// <summary>
    /// Конфигурация приложения
    /// </summary>
    private static IConfigurationRoot config;

    /// <summary>
    /// Значение заданного строкового параметра конфигурации
    /// </summary>
    /// <param name="name">Имя параметра конфигурации</param>
    /// <returns></returns>
    public static string Get(string name)
    {
        return config.GetSection(name).Value ?? throw new Exception("Плохая конфигурация");
    }

    /// <summary>
    /// Значение параметра конфигурации заданного типа данных
    /// </summary>
    /// <typeparam name="T">Тип данных параметра конфигурации</typeparam>
    /// <param name="name">Имя параметра конфигурации</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static T Get<T>(string name)
    {
        return config.GetSection(name).Get<T>() ?? throw new Exception("Плохая конфигурация");
    }

    /// <summary>
    /// Значение параметра конфигурации заданного типа данных.
    /// Если конфигурация не задана, используется значение по умолчанию
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T Get<T>(string name, T defaultValue)
    {
        return config.GetSection(name).Get<T>() ?? defaultValue;
    }
}

