namespace Core;

// Порушення бізнес-правила предметної області (а не помилка аргументу чи диска).
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception inner) : base(message, inner) { }
}
