using InvoiceApi.Data;
using InvoiceApi.Dtos;
using InvoiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApi.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateAsync(int userId, InvoiceCreateDto dto);
        Task<List<InvoiceDto>> GetAllAsync(int userId);
        Task<InvoiceDto> GetByIdAsync(int userId, int invoiceId);
        Task<List<InvoiceDto>> GetForPostmanAsync(int postmanId);
        Task<List<InvoiceDto>> GetForClientAsync(int clientId, bool pendingOnly);
        Task<InvoiceDto> AssignAsync(int userId, int invoiceId, int postmanId);
        Task<InvoiceDto> DeliverAsync(int userId, int invoiceId);
        Task<InvoiceDto> PayAsync(int userId, int invoiceId, decimal amount);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _db;

        public InvoiceService(AppDbContext db) => _db = db;

        private static readonly InvoiceStatus[] ClientVisible =
            { InvoiceStatus.Delivered, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid };

        private static readonly InvoiceStatus[] Pending =
            { InvoiceStatus.Delivered, InvoiceStatus.PartiallyPaid };


        private async Task<User> RequireUserAsync(int userId, UserRole role)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new NotFoundException($"User {userId} not found.");
            if (user.Role != role)
                throw new ForbiddenException($"This action requires the {role} role.");
            return user;
        }

        private async Task<Invoice> LoadInvoiceAsync(int invoiceId)
        {
            return await _db.Invoices
                .Include(i => i.Client)
                .Include(i => i.Postman)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId)
                ?? throw new NotFoundException($"Invoice {invoiceId} not found.");
        }

        private static InvoiceDto ToDto(Invoice i)
        {
            var paid = i.Payments.Sum(p => p.Amount);
            return new InvoiceDto(
                i.Id, i.InvoiceNumber, i.ClientId, i.Client.Name,
                i.PostmanId, i.Postman?.Name,
                i.Description, i.TotalAmount, paid, i.TotalAmount - paid,
                i.Status.ToString(), i.CreatedAt, i.DeliveredAt, i.Client.Address,
                i.Payments.OrderBy(p => p.PaidAt)
                          .Select(p => new PaymentDto(p.Id, p.Amount, p.PaidAt))
                          .ToList());
        }


        public async Task<InvoiceDto> CreateAsync(int userId, InvoiceCreateDto dto)
        {
            await RequireUserAsync(userId, UserRole.Finance);

            var client = await _db.Users.FindAsync(dto.ClientId);
            if (client is null || client.Role != UserRole.Client)
                throw new BusinessRuleException("ClientId must belong to a user with the Client role.");

            var count = await _db.Invoices.CountAsync();
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{count + 1:D5}",
                ClientId = client.Id,
                Description = dto.Description,
                TotalAmount = dto.TotalAmount
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            return ToDto(await LoadInvoiceAsync(invoice.Id));
        }

        public async Task<List<InvoiceDto>> GetAllAsync(int userId)
        {
            await RequireUserAsync(userId, UserRole.Finance);

            var invoices = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Client)
                .Include(i => i.Postman)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(ToDto).ToList();
        }

        public async Task<InvoiceDto> GetByIdAsync(int userId, int invoiceId)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new NotFoundException($"User {userId} not found.");
            var invoice = await LoadInvoiceAsync(invoiceId);

            bool allowed = user.Role switch
            {
                UserRole.Finance => true,
                UserRole.Postman => invoice.PostmanId == userId,
                UserRole.Client => invoice.ClientId == userId && ClientVisible.Contains(invoice.Status),
                _ => false
            };
            if (!allowed)
                throw new ForbiddenException("You are not allowed to view this invoice.");

            return ToDto(invoice);
        }

        public async Task<List<InvoiceDto>> GetForPostmanAsync(int postmanId)
        {
            await RequireUserAsync(postmanId, UserRole.Postman);

            var invoices = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Client)
                .Include(i => i.Postman)
                .Include(i => i.Payments)
                .Where(i => i.PostmanId == postmanId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(ToDto).ToList();
        }

        public async Task<List<InvoiceDto>> GetForClientAsync(int clientId, bool pendingOnly)
        {
            await RequireUserAsync(clientId, UserRole.Client);

            var allowedStatuses = pendingOnly ? Pending : ClientVisible;

            var invoices = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Client)
                .Include(i => i.Postman)
                .Include(i => i.Payments)
                .Where(i => i.ClientId == clientId && allowedStatuses.Contains(i.Status))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(ToDto).ToList();
        }


        public async Task<InvoiceDto> AssignAsync(int userId, int invoiceId, int postmanId)
        {
            await RequireUserAsync(userId, UserRole.Finance);

            var invoice = await LoadInvoiceAsync(invoiceId);
            if (invoice.Status != InvoiceStatus.Created)
            {
                throw new BusinessRuleException($"Invoice {invoiceId} is not in Created status and cannot be assigned.");
            }

            var postman = await _db.Users.FindAsync(postmanId);
            if (postman is null || postman.Role != UserRole.Postman)
            {
                throw new BusinessRuleException($"PostmanId {postmanId} must belong to a user with the Postman role.");
            }
            
            invoice.PostmanId = postman.Id;
            invoice.Status = InvoiceStatus.Assigned;

            await _db.SaveChangesAsync();

            return ToDto(await LoadInvoiceAsync(invoice.Id));
        }
        
        public async Task<InvoiceDto> DeliverAsync(int userId, int invoiceId)
        {
            await RequireUserAsync(userId, UserRole.Postman);

            var invoice = await LoadInvoiceAsync(invoiceId);

            if (invoice.PostmanId != userId)
            {
                throw new ForbiddenException(
                    $"Invoice {invoiceId} is not assigned to you.");
            }

            if (invoice.Status != InvoiceStatus.Assigned)
            {
                throw new BusinessRuleException(
                    "Only assigned invoices can be delivered.");
            }

            invoice.Status = InvoiceStatus.Delivered;
            invoice.DeliveredAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToDto(invoice);
        }

        public async Task<InvoiceDto> PayAsync(int userId, int invoiceId, decimal amount)
        {
            await RequireUserAsync(userId, UserRole.Client);

            var invoice = await LoadInvoiceAsync(invoiceId);

            if(invoice.ClientId != userId)
            {
                throw new ForbiddenException($"Invoice {invoiceId} is not assigned to you.");
            }

            if(invoice.Status != InvoiceStatus.Delivered && invoice.Status != InvoiceStatus.PartiallyPaid)
            {
                throw new BusinessRuleException($"Invoice {invoiceId} is not in a state that can be paid.");
            }

            var paidAmount = invoice.Payments.Sum(p => p.Amount);
            var remainingAmount = invoice.TotalAmount - paidAmount;

            if(amount <= 0)
            {
                throw new BusinessRuleException($"Payment amount must be greater than zero.");
            }
            if(amount > remainingAmount)
            {
                throw new BusinessRuleException($"Payment amount exceeds the remaining amount of {remainingAmount}.");
            }

            invoice.Payments.Add(new Payment
            {
                Amount = amount,
                PaidAt = DateTime.UtcNow
            });

            var remainingAfterPayment = remainingAmount - amount;

            invoice.Status = remainingAfterPayment == 0
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;

            if(remainingAfterPayment == 0)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }
            
            await _db.SaveChangesAsync();
            return ToDto(invoice);
        }
    }
}
