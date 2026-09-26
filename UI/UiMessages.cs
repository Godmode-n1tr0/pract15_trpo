using System.Windows;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TRPO.ElectronicsStore.UI;

public static class UiMessages
{

    // Показывает вопрос с кнопками Да и Нет и возвращает true только при согласии пользователя.
    public static bool ConfirmAction(string text) =>
        MessageBox.Show(text, "Подтверждение", MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    // Показывает понятное сообщение об ошибке и скрывает служебные подробности подключения к базе.
    public static void ShowError(Exception error)
    {
        // Исходная ошибка SQL может содержать имя сервера и подробности подключения.
        string message = error switch
        {
            SqlException or DbUpdateException => "Не удалось выполнить операцию с базой данных. Проверьте доступность SQL Server и настройки подключения, затем повторите попытку.",
            InvalidOperationException or UnauthorizedAccessException => error.Message,
            _ => "Не удалось выполнить операцию. Проверьте настройки подключения и введённые данные."
        };
        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
