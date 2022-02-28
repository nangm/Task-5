using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CartApi.Models;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace CartApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly CartDbContext _context;
        private readonly IConfiguration env;

        public CartsController(CartDbContext context, IConfiguration env)
        {
            _context = context;
            this.env = env;
        }

        // GET: api/Carts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCarts()
        {
            return await _context.Carts.ToListAsync();
        }

        // GET: api/Carts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(int id)
        {
            var cart = await _context.Carts.FindAsync(id);

            if (cart == null)
            {
                return NotFound();
            }

            return cart;
        }

        // PUT: api/Carts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCart(int id, Cart cart)
        {
            if (id != cart.CartID)
            {
                return BadRequest();
            }

            _context.Entry(cart).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartExists(id))
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

        // POST: api/Carts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Cart>> PostCart(Cart cart)
        {
            cart.OrderStatus = "INITIATED";
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var factory = new ConnectionFactory()
            {
                HostName = env.GetSection("RABBITMQHOST").Value,
                Port = Convert.ToInt32(env.GetSection("RABBITMQPORT").Value),
                UserName = env.GetSection("RABBITUSER").Value,
                Password = env.GetSection("RABBITPASSWORD").Value
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "orders", durable: false, exclusive: false, autoDelete: false, arguments: null);

                string message = "Type: ORDER_CREATED | Order ID:" + cart.OrderID + "|Product ID:" + cart.ProductID + "|Product Price:" + cart.ProductPrice + "|Toal:" + cart.Total + "|Cart ID:" + cart.CartID;
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "", routingKey: "orders", basicProperties: null, body: body);
                Console.WriteLine(" [x] Sent {0}", message);
            }

            return CreatedAtAction("GetCart", new { id = cart.CartID }, cart);
        }

        // DELETE: api/Carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CartExists(int id)
        {
            return _context.Carts.Any(e => e.CartID == id);
        }
    }
}
