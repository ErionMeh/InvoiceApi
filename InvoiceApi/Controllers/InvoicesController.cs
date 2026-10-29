using InvoiceApi.Dtos;
using InvoiceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _service;

        public InvoicesController(IInvoiceService service) => _service = service;


        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromQuery] int userId, InvoiceCreateDto dto)
        {
            var created = await _service.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpGet]
        public async Task<ActionResult<List<InvoiceDto>>> GetAll([FromQuery] int userId) =>
            Ok(await _service.GetAllAsync(userId));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InvoiceDto>> GetById(int id, [FromQuery] int userId) =>
            Ok(await _service.GetByIdAsync(userId, id));

        [HttpPut("{id:int}/assign")]
        public async Task<ActionResult<InvoiceDto>> Assign(int id, [FromQuery] int userId, AssignDto dto) =>
            Ok(await _service.AssignAsync(userId, id, dto.PostmanId));

        [HttpPut("{id:int}/deliver")]
        public async Task<ActionResult<InvoiceDto>> Deliver(int id, [FromQuery] int userId) =>
            Ok(await _service.DeliverAsync(userId, id));

        [HttpPost("{id:int}/payments")]
        public async Task<ActionResult<InvoiceDto>> Pay(int id, [FromQuery] int userId, PaymentCreateDto dto) =>
            Ok(await _service.PayAsync(userId, id, dto.Amount));

        [HttpGet("~/api/postmen/{postmanId:int}/invoices")]
        public async Task<ActionResult<List<InvoiceDto>>> ForPostman(int postmanId) =>
            Ok(await _service.GetForPostmanAsync(postmanId));

        [HttpGet("~/api/clients/{clientId:int}/invoices")]
        public async Task<ActionResult<List<InvoiceDto>>> ForClient(int clientId, [FromQuery] bool pending = false) =>
            Ok(await _service.GetForClientAsync(clientId, pending));
    }
}
