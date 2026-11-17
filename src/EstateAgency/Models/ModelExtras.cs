namespace EstateAgency.Models;

public partial class User
{
    // ФИО одной строкой. Trim убирает пробел в конце, если отчества нет.
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    // ToString решает, как объект выглядит в ComboBox и ListBox.
    public override string ToString() => $"{FullName} ({Login})";
}

public partial class RequestType
{
    public override string ToString() => TypeName;
}

public partial class PropertyType
{
    public override string ToString() => TypeName;
}

public partial class PropertyStatus
{
    public override string ToString() => StatusName;
}

public partial class Property
{
    public override string ToString() =>
        $"{PropertyCode} · {Title} · {Status?.StatusName} · показов осталось: {FreeViewings}";
}

public partial class Request
{
    // Так заявка выглядит в списке заявок.
    public override string ToString() =>
        $"Заявка № {RequestId} · {CreatedAt:dd.MM.yyyy} · {Client?.FullName} · " +
        $"{Property?.PropertyCode} · {Type?.TypeName} · {Status?.StatusName}";
}

public partial class Viewing
{
    public bool IsDone => ActualStartAt != null;

    // Так показ выглядит в составе заявки.
    public override string ToString() => IsDone
        ? $"{ScheduledAt:dd.MM.yyyy HH:mm} · проведён · {Result}" +
          (string.IsNullOrWhiteSpace(Feedback) ? "" : $" · {Feedback}")
        : $"{ScheduledAt:dd.MM.yyyy HH:mm} · запланирован · менеджер {Manager?.FullName}";
}
