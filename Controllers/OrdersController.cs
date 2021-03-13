using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using cloudscribe.Pagination.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using ShopifyWeb.Data;
using ShopifyWeb.Helpers;
using ShopifyWeb.Models;

namespace ShopifyWeb.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public OrdersController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Orders
        public IActionResult Index(string byOrderNumber, DateTime byDate, string byName, string byDni, string byPhone, string byEmail,int byPayment, int byPaymentState, int byOrderState, int pageNumber = 1, int pageSize = 10)
        {
            ViewBag.byName = byName;
            ViewBag.byOrderNumber = byOrderNumber;
            ViewBag.byEmail = byEmail;
            ViewBag.byDni = byDni;
            ViewBag.byPhone = byPhone;
            ViewBag.byPayment = byPayment;
            ViewBag.byPaymentState = byPaymentState;
            ViewBag.byOrderState = byOrderState;

            TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            if (byDate == DateTime.MinValue)
            {
                byDate = TimeZoneInfo.ConvertTime(DateTime.Now, tst);
            }
            ViewBag.byDate = byDate.ToString("yyyy-MM-dd");
            int ExcludeRecords = (pageSize * pageNumber) - pageSize;

            var orders = (from o in _context.Orders
                      join c in _context.Customer on o.customer_id equals c.id
                      join ca in _context.CustomerAddress on c.id equals ca.customer_id
                      join b in _context.BillAddress on o.id equals b.order_id
                      select new OrderList
                      {
                          id = o.id,
                          numero = o.order_number,
                          fecha = o.created_at,
                          nombre = c.first_name + " " + c.last_name,
                          total = o.total_price,
                          tipo = o.gateway,
                          estado = o.financial_status,
                          estadoOrden = o.status,
                          dni = ca.company,
                          correo = c.email,
                          telefono = b.phone
                      });

            if (!string.IsNullOrEmpty(byOrderNumber))
            {
                orders = orders.Where(o => o.numero.Contains(byOrderNumber));
            }
            if (!string.IsNullOrEmpty(byEmail))
            {
                orders = orders.Where(o => o.correo.Contains(byEmail));
            }
            if (byPayment > 0)
            {
                switch(byPayment)
                {
                    case 1:
                        orders = orders.Where(o => o.tipo.Equals("Bank Deposit"));
                        break;
                    case 2:
                        orders = orders.Where(o => o.tipo.Equals("mercado_pago"));
                        break;
                    case 3:
                        orders = orders.Where(o => o.tipo.Equals("Cash on Delivery (COD)"));                        
                        break;
                }
            }
            if (!string.IsNullOrEmpty(byName))
            {
                orders = orders.Where(o => o.nombre.Contains(byName));
            }
            if (byDate.Date != DateTime.Now.Date)
            {
                orders = orders.Where(o => o.fecha.Date.Equals(byDate.Date));
            }
            if (!string.IsNullOrEmpty(byDni))
            {
                orders = orders.Where(o => o.dni.Contains(byDni));
            }
            if (!string.IsNullOrEmpty(byPhone))
            {
                orders = orders.Where(o => o.telefono.Replace(" ","").Contains(byPhone));
            }
            if (byPaymentState > 0)
            {
                switch (byPaymentState)
                {
                    case 1:
                        orders = orders.Where(o => o.estado.Equals("pending"));
                        break;
                    case 2:
                        orders = orders.Where(o => o.estado.Equals("paid"));
                        break;
                }
            }
            if (byOrderState > 0)
            {
                switch (byOrderState)
                {
                    case 1:
                        orders = orders.Where(o => o.estadoOrden.Equals("Pedido recibido"));
                        break;
                    case 2:
                        orders = orders.Where(o => o.estadoOrden.Equals("Pago confirmado"));
                        break;
                    case 3:
                        orders = orders.Where(o => o.estadoOrden.Equals("Entregado al courier"));
                        break;
                    case 4:
                        orders = orders.Where(o => o.estadoOrden.Equals("Listo para recojo"));
                        break;
                    case 5:
                        orders = orders.Where(o => o.estadoOrden.Equals("Entregado al cliente"));
                        break;
                    case 6:
                        orders = orders.Where(o => o.estadoOrden.Equals("Cancelado"));
                        break;
                }
            }
            var filter = orders.OrderByDescending(p => p.fecha).Skip(ExcludeRecords).Take(pageSize);

            var result = new PagedResult<OrderList>
            {
                Data = filter.AsNoTracking().ToList(),
                TotalItems = orders.Count(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return View(result);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,email,created_at,updated_at,number,token,gateway,total_price,subtotal_price,total_tax,currency,financial_status,total_discounts,total_line_items_price,name,order_number")] Orders order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Edit/5
        public IActionResult Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            OrderDetail detail = new OrderDetail();
            detail.Order = _context.Orders.Find(id);

            IRestResponse response = CallShopify($"orders/{id}.json", Method.GET, null);

            if (response.StatusCode.ToString().Equals("OK"))
            {
                MasterOrder mo = JsonConvert.DeserializeObject<MasterOrder>(response.Content);
                if (mo.order.shipping_lines.Count > 0)
                {
                    detail.Order.shipping_price = mo.order.shipping_lines[0].price;
                }
            }
            else
                return NotFound();

            List<Item> items = _context.Item.Where(i => i.order_id.Equals(id)).ToList();
            detail.Bill = _context.BillAddress.Where(b => b.order_id.Equals(id)).FirstOrDefault();
            detail.Ship = _context.ShipAddress.Where(s => s.order_id.Equals(id)).FirstOrDefault();
            detail.Customer = _context.Customer.Where(c => c.id.Equals(detail.Order.customer_id)).FirstOrDefault();
            detail.Customer.default_address = _context.CustomerAddress.Where(d => d.customer_id.Equals(detail.Order.customer_id)).FirstOrDefault();
            detail.Items = items;
            detail.Combo = getStateValues(detail.Order.status,detail.Order.fechaEstimada.Contains("para recojo") ? true : false);

            detail.Customer.default_address.zip = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(detail.Customer.default_address.zip.ToLower());
            detail.Ship.zip = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(detail.Ship.zip.ToLower());
            detail.Bill.zip = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(detail.Bill.zip.ToLower());

            if (detail.Order.financial_status == "pending")
                detail.Order.financial_status = "Pendiente";
            else
                detail.Order.financial_status = "Pagado";

            switch(detail.Order.gateway)
            {
                case "Bank Deposit":
                    detail.Order.gateway = "Depósito Bancario";
                    break;
                case "Cash on Delivery (COD)":
                    detail.Order.gateway = "Contraentrega";
                    break;
                case "mercado_pago":
                    detail.Order.gateway = "Mercado Pago";
                    break;
            }

            foreach (Item item in detail.Items)
            {
                KellyChild product = _context.KellyChild.FromSqlInterpolated($"GetProductChildInfo @CodigoPadre = {item.sku}").ToList()[0];
                item.name = $"{item.title}{(product.Talla != "00" ? " - Talla " + product.Talla : "")}{(product.Taco != "00" ? " - Taco " + product.Taco : "")}";
                Product p = _context.Product.Find(item.product_id);
                if (p != null)
                {
                    item.handle = p.Handle;
                    ProductImage pi = _context.ProductImage.Where(i => i.product_id.Equals(p.Id)).FirstOrDefault();
                    if (pi != null)
                        item.imgURL = pi.src;
                }
            }

            if (detail == null)
            {
                return NotFound();
            }
            return View(detail);
        }

        public List<string> getStateValues(string state,bool store)
        {
            List<string> lst = new List<string>();
            switch(state)
            {
                case "Pedido recibido":
                    lst.Add(state);
                    break;
                case "Pago confirmado":
                    lst.Add(state);
                    if(store)
                        lst.Add("En camino a tienda");
                    else
                        lst.Add("Entregado al courier");
                    break;
                case "En camino a tienda":
                    lst.Add(state);
                    lst.Add("Listo para recojo");
                    break;
                case "Listo para recojo":
                    lst.Add(state);
                    lst.Add("Entregado al cliente");
                    break;
                case "Entregado al courier":
                    lst.Add(state);
                    lst.Add("Entregado al cliente");
                    break;
                case "Entregado al cliente":
                    lst.Add(state);
                    break;
                case "Cancelado":
                    lst.Add(state);
                    break;
            }
            return lst;
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id,string byState)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //string tz = TimeZoneConverter.TZConvert.WindowsToIana("SA Pacific Standard Time");
                    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                    Orders order = new Orders();
                    order = _context.Orders.Find(id);
                    order.status = byState;
                    order.updated_at = TimeZoneInfo.ConvertTime(DateTime.Now,tst);

                    OrderStatus status = new OrderStatus();
                    status.OrderId = id;
                    status.Status = byState;
                    status.CreateDate = TimeZoneInfo.ConvertTime(DateTime.Now, tst);

                    if(byState != "Pago confirmado" || byState != "Pedido recibido")//update with the fulfillment api
                    {
                        if(string.IsNullOrEmpty(order.fulfillment_id))
                        {
                            createFulfillment(order);
                        }
                        string stat = getStateForShopify(byState);
                        dynamic jOrder = new
                        {
                            @event = new
                            {
                                status = stat
                            }
                        };
                        IRestResponse response = CallShopify($"orders/{order.id}/fulfillments/{order.fulfillment_id}/events.json", Method.POST, jOrder);
                        if (response.StatusCode.ToString().Equals("Created"))
                            _logger.LogInformation("Status updated");
                        else
                        {
                            return NotFound();
                        }
                    }

                    _context.OrderStatus.Add(status);
                    _context.Orders.Update(order);

                    _context.SaveChanges();
                }
                catch (Exception e)
                {
                    return NotFound(e);
                    throw;
                }
                return Ok();//RedirectToAction(nameof(Index));
            }
            return View();
        }

        public string getStateName(string id)
        {
            switch(id)
            {
                case "1":
                    return "Recibimos su pedido";
                case "2":
                    return "Pedido confirmado";
                case "3":
                    return "Preparando pedido";
                case "4":
                    return "Pedido enviado";
                case "5":
                    return "Pedido entregado";
                case "6":
                    return "Cancelado";
            }
            return "";
        }

        public string getStateForShopify(string id)
        {
            switch (id)
            {
                case "En camino a tienda":
                    return "picked_up";
                case "Entregado al courier":
                    return "out_for_delivery";
                case "Entregado al cliente":
                    return "delivered";
                case "Listo para recojo":
                    return "ready_for_pickup";
            }
            return "";
        }

        public void createFulfillment(Orders order)
        {
            Fulfillment f = new Fulfillment();
            Web web = _context.Web.Find(1);
            f.location_id = web.LocationId;
            f.tracking_number = null;

            List<LineItem> lst = new List<LineItem>();
            List<Item> lstItem = _context.Item.Where(i => i.order_id == order.id).ToList();
            foreach(Item item in lstItem)
            {
                LineItem line = new LineItem();
                line.id = item.id;
                lst.Add(line);
            }
            f.line_items = lst;

            dynamic oFulfillment = new
            {
                fulfillment = f
            };

            IRestResponse response = CallShopify($"orders/{order.id}/fulfillments.json", Method.POST, oFulfillment);

            if (response.StatusCode.ToString().Equals("Created"))
            {
                MasterFulfillment mf = JsonConvert.DeserializeObject<MasterFulfillment>(response.Content);
                order.fulfillment_id = mf.fulfillment.id;
            }
            else
                _logger.LogError($"Error creating fulfillment");
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var order = await _context.Orders.FindAsync(id);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(string id)
        {
            return _context.Orders.Any(e => e.id == id);
        }

        public IRestResponse CallShopify(string resource, RestSharp.Method method, dynamic parameters)
        {
            try
            {
                Web web = _context.Web.Find(1);
                Uri url = new Uri(web.WebURL);
                RestClient rest = new RestClient(url);
                rest.Authenticator = new HttpBasicAuthenticator(web.WebAPI, web.WebPassword);

                RestRequest request = new RestRequest(resource, method);
                request.AddHeader("header", "Content-Type: application/json");

                if (parameters != null)
                {
                    dynamic JsonObj = JsonConvert.SerializeObject(parameters);
                    request.AddParameter("application/json", JsonObj, ParameterType.RequestBody);
                }

                IRestResponse response = rest.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || response.StatusCode.ToString().Equals("520"))
                {
                    System.Threading.Thread.Sleep(5000);
                    return CallShopify(resource, method, parameters);
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError("Error calling API", e);
                return null;
            }
        }
    }
}
