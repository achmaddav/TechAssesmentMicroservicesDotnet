using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;
using System.Net.Http;

namespace PaymentService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly HttpClient _httpClient;

        public PaymentController(PaymentDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
        {
            var dataPayment = await _context.Payments.FirstOrDefaultAsync(x => x.OrderId == payment.OrderId);
            if (dataPayment != null)
            {
                return BadRequest($"Sudah dibayar.");
            }

            var jwtToken = await GetJwtToken();

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var order = await _httpClient.GetFromJsonAsync<Order>($"http://localhost:5000/order/{payment.OrderId}");
            if (order == null)
            {
                return BadRequest($"Invalid order ID: {order.Id}");
            }

            if (payment.Amount < order.TotalPrice || payment.Amount > order.TotalPrice)
            {
                return BadRequest("Jumlah pembayaran tidak sesuai dengan jumlah yang harus dibayar.");
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            payment.Status = "Completed";
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
        }

        private async Task<string> GetJwtToken()
        {
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6InRlc3QiLCJuYmYiOjE3MTkxMjA0NzIsImV4cCI6MTcxOTcyNTI3MiwiaWF0IjoxNzE5MTIwNDcyfQ.lsLzbPZ_zlzZQaIprzYELR9pWALWrAWLM5PbI7Mpgyo";
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            return payment;
        }

        public class Order
        {
            public int Id { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice { get; set; }
        }
    }
}
