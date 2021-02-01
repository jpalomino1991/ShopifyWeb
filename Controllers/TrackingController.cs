using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShopifyWeb.Data;
using ShopifyWeb.Models;
using System;
using System.Collections.Generic;
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
        [ValidateAntiForgeryToken]
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
                detail.Ship = _context.ShipAddress.Where(c => c.order_id == order.id).FirstOrDefault();
                detail.Status = generateList(order);

                return View(detail);
            }
            else
                return NotFound();
        }

        public List<OrderStatus> generateList(Orders order)
        {
            List<OrderStatus> ls = new List<OrderStatus>();
            string[] defaultStatus = { "Recibimos su pedido", "Pago confirmado", "Preparando pedido", "Pedido empacado", "Pedido entregado" };
            string[] cancelStatus = {"Recibimos su pedido", "Pago confirmado", "Cancelado"};

            if (order.status == "Cancelado")
                ls = createList(cancelStatus,order,33);
            else
                ls = createList(defaultStatus,order,20);            

            return ls;
        }

        public List<OrderStatus> createList(string[] lst, Orders order, int porcentage)
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

            i = 1;
            foreach (OrderStatus stat in stats)
            {
                OrderStatus orderStatus = ls.Find(s => s.Status == stat.Status);
                orderStatus.CreateDate = stat.CreateDate;
                orderStatus.Date = stat.CreateDate.ToShortDateString();
                orderStatus.Id = stat.Id;
                if (i == stats.Count)
                    orderStatus.Active = "active";
                i++;
            }

            return ls;
        }
    }
}