using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace EstateAgency.Services;

// Одна карточка каталога: всё, что нужно показать на экране для одного объекта.
// Разметка CatalogView.axaml берёт значения отсюда через {Binding ИмяСвойства}.
public class PropertyCard
{
    public int Id { get; set; }                      // property_id — чтобы открыть объект
    public string Code { get; set; } = "";           // код (для поиска)
    public string Title { get; set; } = "";          // название
    public string Address { get; set; } = "";        // адрес
    public string Type { get; set; } = "";           // тип объекта
    public string Status { get; set; } = "";         // статус объекта
    public bool IsOpen { get; set; }                 // открыт ли для заявок
    public string Params { get; set; } = "";         // площадь и комнаты
    public int Viewings { get; set; }                // сколько показов осталось
    public string ViewingsText { get; set; } = "";   // «Показов: много» / «Показов: мало»
    public decimal Price { get; set; }               // цена (для сортировки)
    public string PriceText { get; set; } = "";      // цена для показа
    public string CommissionText { get; set; } = ""; // комиссия агентства
    public IBrush? Background { get; set; }          // фон карточки (красный, если мало показов)
    public Bitmap? Image { get; set; }               // картинка или заглушка
}
