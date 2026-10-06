namespace KBS.Models.Entities;

public class OrderDetail
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public Order? Order { get; set; }

    public Product? Product { get; set; }
}
