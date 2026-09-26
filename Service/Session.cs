namespace TRPO.ElectronicsStore.Service;

public sealed class Session
{
    public bool IsManager { get; private set; }

    // Проверяет PIN 1234 и выдаёт права менеджера; при неверном PIN снимает эти права.
    public bool TryLogin(string pin) => IsManager = pin == "1234";

    // Переключает пользователя в режим посетителя без права изменять данные.
    public void EnterVisitor() => IsManager = false;

    // Проверяет права менеджера и выбрасывает ошибку, если посетитель пытается изменить данные.
    public void CheckManagerAccess()
    {
        if (!IsManager)
            throw new UnauthorizedAccessException("Изменять данные может только менеджер.");
    }
}
