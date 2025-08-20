public record OrderItem(string Sku, int Quantity, decimal Price);
public record Order(string Id, List<OrderItem> Items, decimal Shipping, decimal DiscountPercent);

public interface IOrderRepository
{
    Task<Order?> GetAsync(string id, CancellationToken ct = default);
    Task SaveAsync(Order order, CancellationToken ct = default);
}

public interface ITaxService
{
    decimal CalculateVat(decimal netAmount); // например, 20% НДС
}

public class OrderService
{
    private readonly IOrderRepository _repo;
    private readonly ITaxService _tax;

    public OrderService(IOrderRepository repo, ITaxService tax)
    {
        _repo = repo;
        _tax = tax;
    }

    // Итог = (сумма позиций + доставка – скидка) + НДС
    public async Task<decimal> CalculateTotalAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _repo.GetAsync(orderId, ct) 
                    ?? throw new InvalidOperationException("Order not found");

        var itemsSum = order.Items.Sum(i => i.Price * i.Quantity);
        var discount = (itemsSum + order.Shipping) * (order.DiscountPercent / 100m);
        var net = itemsSum + order.Shipping - discount;
        var vat = _tax.CalculateVat(net);

        return net + vat;
    }
}
