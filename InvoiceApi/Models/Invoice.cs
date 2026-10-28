namespace InvoiceApi.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public int ClientId { get; set; }
        public User Client { get; set; } = null!;

        public int? PostmanId { get; set; }
        public User? Postman { get; set; }

        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Created;

        public List<Payment> Payments { get; set; } = new();
    }

    public enum InvoiceStatus { Created, Assigned, Delivered, PartiallyPaid, Paid }
}
