using Core.Events;

namespace Core.Abstractions;

// Одержувач сповіщень, якого сервісу передають через конструктор (альтернатива події).
public interface INotificationSink
{
    void Notify(CatalogChangedEventArgs change);
}
