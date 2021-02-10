using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopifyWeb.Data;
using ShopifyWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Controllers
{
    public class TrackingController : Controller
    {
        private readonly ILogger<TrackingController> _logger;
        private readonly ApplicationDbContext _context;

        public TrackingController(ApplicationDbContext context, ILogger<TrackingController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string orderId)
        {
            if (_context.Orders.Any(o => o.order_number == orderId))
                return View();
            else
                return NotFound();
        }

        public IActionResult Tracking(string id)
        {
            Orders order = _context.Orders.Where(o => o.order_number == id).FirstOrDefault();
            if (order != null)
            {
                OrderDetail detail = new OrderDetail();

                detail.Order = order;
                detail.Items = _context.Item.Where(o => o.order_id == order.id).ToList();

                foreach(Item item in detail.Items)
                {
                    KellyChild product = _context.KellyChild.FromSqlInterpolated($"GetProductChildInfo @CodigoPadre = {item.sku}").ToList()[0];
                    item.name = $"{item.title}{(product.Talla != "00" ? " Talla - " + product.Talla : "")}{(product.Taco != "00" ? " Taco - " + product.Taco : "")}";
                }

                detail.Ship = _context.ShipAddress.Where(c => c.order_id == order.id).FirstOrDefault();
                detail.Status = generateList(order);
                detail.Customer = _context.Customer.Where(c => c.id == order.customer_id).FirstOrDefault();
                detail.Customer.first_name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(detail.Customer.first_name);
                detail.Customer.last_name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(detail.Customer.last_name);

                return View(detail);
            }
            else
                return NotFound();
        }

        public List<OrderStatus> generateList(Orders order)
        {
            List<OrderStatus> ls = new List<OrderStatus>();
            string[] pickupStatus = { "Pedido recibido", "Pago confirmado", "En camino a tienda", "Listo para recojo", "Entregado a cliente" };
            string[] defaultStatus = { "Pedido recibido", "Pago confirmado", "Entregado al courier", "Entregado al cliente" };

            if (order.status != "Cancelado")
            {
                if (order.fechaEstimada.Contains("para recojo"))
                    ls = createList(pickupStatus, order, order.status);
                else
                    ls = createList(defaultStatus, order, order.status);
            }

            return ls;
        }

        public List<OrderStatus> createList(string[] lst, Orders order, string statName)
        {
            List<OrderStatus> ls = new List<OrderStatus>();
            int i = 1;
            
            foreach(string item in lst)
            {
                OrderStatus status = new OrderStatus();
                status.Status = item;
                status.OrderId = order.id;

                ls.Add(status);
                i++;
            }

            List<OrderStatus> stats = _context.OrderStatus.Where(s => s.OrderId == order.id).ToList();

            foreach (OrderStatus stat in stats)
            {
                OrderStatus orderStatus = ls.Find(s => s.Status == stat.Status);
                orderStatus.CreateDate = stat.CreateDate;
                orderStatus.Date = stat.CreateDate.ToString("dd/MM/yyyy");
                orderStatus.Time = stat.CreateDate.ToString("HH:mm");
                orderStatus.Id = stat.Id;
                if (stat.Status == statName)
                    orderStatus.Active = "active";
            }

            return ls;
        }
    }
}