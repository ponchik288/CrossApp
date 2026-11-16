namespace Core.Domain;

// Правило, що охоплює ДВІ сутності: переміщати можна лише той самий товар (однаковий SKU).
// Жодна з сутностей сама не знає про іншу, тому таке правило живе окремо від них.
public static class StockTransfer
{
    public static void Move(Product from, Product to, int amount)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (from.Sku != to.Sku)
            throw new DomainException(
                $"Не можна перемістити {from.Sku} у запис товару {to.Sku}: артикули різні");

        // Спочатку видача: вона перевіряє і кількість, і залишок. Якщо вона не пройде, прихід не відбудеться.
        from.Issue(amount);
        to.RegisterArrival(amount);
    }
}
