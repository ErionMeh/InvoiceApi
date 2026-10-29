using InvoiceApi.Dtos;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;


namespace InvoiceApi.Services
{
    public static class InvoicePdfGenerator
    {
        public static byte[] Generate(InvoiceDto invoice)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text($"Invoice {invoice.InvoiceNumber}").FontSize(22).Bold();

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text($"Client: {invoice.ClientName}");
                        col.Item().Text($"Delivery address: {invoice.DeliveryAddress}");
                        col.Item().Text($"Date: {invoice.CreatedAt:dd.MM.yyyy}");
                        col.Item().Text($"Status: {invoice.Status}");
                        col.Item().PaddingTop(10).Text(invoice.Description);

                        col.Item().PaddingTop(10).Text($"Total: {invoice.TotalAmount:N2} EUR").Bold();
                        col.Item().Text($"Paid: {invoice.PaidAmount:N2} EUR");
                        col.Item().Text($"Remaining: {invoice.RemainingAmount:N2} EUR");
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                    });
                });
            }).GeneratePdf();
        }
    }
}
