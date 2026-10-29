using System.ComponentModel.DataAnnotations;

namespace InvoiceApi.Dtos
{
    public class InvoiceCreateDto
    {
        public int ClientId { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 10_000_000)]
        public decimal TotalAmount { get; set; }
    }

    public class AssignDto
    {
        public int PostmanId { get; set; }
    }

    public class PaymentCreateDto
    {
        [Range(0.01, 10_000_000)]
        public decimal Amount { get; set; }
    }

    public record PaymentDto(
        int Id,
        decimal Amount,
        DateTime PaidAt);

    public record InvoiceDto(
        int Id,
        string InvoiceNumber,
        int ClientId,
        string ClientName,
        int? PostmanId,
        string? PostmanName,
        string Description,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal RemainingAmount,
        string Status,
        DateTime CreatedAt,
        DateTime? DeliveredAt,
        string DeliveryAddress,
        List<PaymentDto> Payments);
}
