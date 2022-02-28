using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderApi.Models;
using RabbitMQ.Client;

namespace OrderApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IConfiguration env;

        public OrdersController(OrderDbContext context, IConfiguration env)
        {
            _context = context;
            this.env = env;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.OrderID)
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
                if (!OrderExists(id))
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

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
                var factory = new ConnectionFactory()
                {
                    HostName = env.GetSection("RABBITMQHOST").Value,
                    Port = Convert.ToInt32(env.GetSection("RABBITMQPORT").Value),
                    UserName = env.GetSection("RABBITUSER").Value,
                    Password = env.GetSection("RABBITPASSWORD").Value
                };
                using (var connection = factory.CreateConnection())
                if (order != null )
                {
                    order.OrderStatus = "SUCCESS";
                   
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: "order-processed", durable: false, exclusive: false, autoDelete: false, arguments: null);

                        string message = "Type: ORDER_STATUS | Order ID:" + order.OrderID  + "|Toal:" + order.Total + "|Cart ID:" + order.CartID + "|OrderStatus:" + order.OrderStatus;
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "", routingKey: "order-processed", basicProperties: null, body: body);
                        Console.WriteLine(" [x] Sent {0}", message);
                    }
                }
                else
                {
                    order.OrderStatus = "FAILED";
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: "order-processed", durable: false, exclusive: false, autoDelete: false, arguments: null);

                        string message = "Type: ORDER_STATUS | Order ID:" + order.OrderID + "|Toal:" + order.Total + "|Cart ID:" + order.CartID + "|OrderStatus:" + order.OrderStatus;
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "", routingKey: "order-processed", basicProperties: null, body: body);
                        Console.WriteLine(" [x] Sent {0}", message);
                    }

                }
                

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("OrderStatus", new { id = order.OrderID }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
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

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }
    }
}
