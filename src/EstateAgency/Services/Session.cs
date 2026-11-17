using System.Text.RegularExpressions;
using EstateAgency.Data;
using EstateAgency.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Services;

// Session («сеанс») помнит, кто сейчас работает с программой.
// Класс static: он один на всю программу, пишем просто Session.CurrentUser.
public static class Session
{
    // Текущий пользователь. null означает «гость» (никто не вошёл).
    public static User? CurrentUser { get; private set; }

    public static string RoleName => CurrentUser?.Role.RoleName ?? "";

    // Уровни доступа. Названия ролей совпадают с таблицей roles.
    public static bool IsGuest => CurrentUser == null;
    public static bool IsAdmin => RoleName == "Администратор";
    public static bool IsStaff => RoleName == "Менеджер" || IsAdmin;

    // Подпись в шапке окна: «Иванов Иван Петрович · Клиент» или «Гость».
    public static string Caption =>
        CurrentUser == null ? "Гость" : $"{CurrentUser.FullName} · {RoleName}";

    // Вход: проверяем пароль; роль читается из БД.
    public static async Task LoginAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
            throw new AppException("Введите логин и пароль.");
        string name = login.Trim();
        using var db = Db.Create();
        User? user = await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Login == name);
        // Текст один и тот же, чтобы по ответу нельзя было узнать, существует ли логин.
        if (user == null || user.Password != password)
            throw new AppException("Неверный логин или пароль.");
        CurrentUser = user;
    }

    // Регистрация клиента. После успеха пользователь сразу входит в систему.
    public static async Task RegisterAsync(string lastName, string firstName, string? middleName,
        string phone, string email, string login, string password)
    {
        lastName = lastName.Trim();
        firstName = firstName.Trim();
        phone = phone.Trim();
        email = email.Trim();
        login = login.Trim();
        if (lastName.Length == 0 || firstName.Length == 0)
            throw new AppException("Укажите фамилию и имя.");
        if (!Regex.IsMatch(phone, @"^\+7\d{10}$"))
            throw new AppException("Телефон указывается в формате +7XXXXXXXXXX.");
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new AppException("Проверьте адрес электронной почты.");
        if (login.Length < 3)
            throw new AppException("Логин должен содержать не меньше 3 символов.");
        if (password.Length < 3)
            throw new AppException("Пароль должен содержать не меньше 3 символов.");

        using var db = Db.Create();
        // Контроль дублей: логин, телефон и почта не повторяются.
        if (await db.Users.AnyAsync(u => u.Login == login))
            throw new AppException("Такой логин уже занят.");
        if (await db.Users.AnyAsync(u => u.Phone == phone || u.Email == email))
            throw new AppException("Пользователь с таким телефоном или почтой уже есть.");

        int roleId = await db.Roles.Where(r => r.RoleName == "Клиент")
            .Select(r => r.RoleId).SingleAsync();
        db.Users.Add(new User
        {
            Login = login,
            Password = password,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            Phone = phone,
            Email = email,
            RoleId = roleId,
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
        await LoginAsync(login, password);
    }

    // Клиенты для выпадающего списка сотрудника.
    public static async Task<List<User>> GetClientsAsync()
    {
        RequireStaff();
        using var db = Db.Create();
        return await db.Users
            .Where(u => u.Role.RoleName == "Клиент")
            .OrderBy(u => u.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    // Выход: снова гость.
    public static void Logout()
    {
        CurrentUser = null;
    }

    // Проверки прав. Вызываются в сервисах ПЕРЕД изменением данных.
    public static void RequireStaff()
    {
        if (!IsStaff)
            throw new AppException("Недостаточно прав: нужна роль менеджера или администратора.");
    }

    public static void RequireAdmin()
    {
        if (!IsAdmin)
            throw new AppException("Недостаточно прав: нужна роль администратора.");
    }
}
