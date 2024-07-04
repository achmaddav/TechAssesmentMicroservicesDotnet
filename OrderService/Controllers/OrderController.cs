using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;
using System.Net.Http;
using static OrderService.Controllers.OrderController;

namespace OrderService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderController> _logger;

        public OrderController(OrderDbContext context, HttpClient httpClient, ILogger<OrderController> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> Get()
        {
            return await _context.Orders.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> Get(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return order;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] List<Order> orders) 
        {
            if (orders == null || !orders.Any())
            {
                return BadRequest("Invalid order request");
            }

            var jwtToken = await GetJwtToken();

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var createdOrders = new List<Order>();

            foreach (var order in orders)
            {
                var product = await _httpClient.GetFromJsonAsync<Product>($"http://localhost:5000/product/{order.ProductId}");
                if (product == null)
                {
                    return BadRequest($"Invalid product ID: {order.ProductId}");
                }

                if (product.Qty < order.Quantity)
                {
                    return BadRequest("Jumlah produk tidak mencukupi.");
                }

                product.Qty -= order.Quantity;

                await _httpClient.PutAsJsonAsync($"http://localhost:5000/product/{product.Id}", product);

                order.TotalPrice = product.Price * order.Quantity;
                _context.Orders.Add(order);
                createdOrders.Add(order);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Post), createdOrders);
        }

        private async Task<string> GetJwtToken()
        {
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6InRlc3QiLCJuYmYiOjE3MTkxMjA0NzIsImV4cCI6MTcxOTcyNTI3MiwiaWF0IjoxNzE5MTIwNDcyfQ.lsLzbPZ_zlzZQaIprzYELR9pWALWrAWLM5PbI7Mpgyo";
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Orders.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Qty { get; set; }
        }

        public class Payment
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public decimal Amount { get; set; }
            public string Status { get; set; }
        }
    }
}


