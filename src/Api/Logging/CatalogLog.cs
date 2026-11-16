using Core.Events;

namespace Api.Logging;

// Повідомлення логу про зміну каталогу. Метод генерує компілятор (LoggerMessage): швидше і без помилок у шаблоні.
internal static partial class CatalogLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{Kind} {Id}: {Delta} -> залишок {QuantityAfter}")]
    public static partial void CatalogChanged(ILogger logger, CatalogChangeKind kind, string id, int delta, int quantityAfter);
}
