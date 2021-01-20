using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cloudscribe.Pagination.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopifyWeb.Data;
using ShopifyWeb.Helpers;
using ShopifyWeb.Models;

namespace ShopifyWeb.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public IActionResult Index(string byOrderNumber,string byName,string byEmail,int byPayment, int pageNumber = 1, int pageSize = 10)
        {
            ViewBag.byName = byName;
            ViewBag.byOrderNumber = byOrderNumber;
            ViewBag.byEmail = byEmail;
            ViewBag.byPayment = byPayment;
            int ExcludeRecords = (pageSize * pageNumber) - pageSize;

            var orders = _context.Orders.Where(o => o.total_price > 0);

            if(!string.IsNullOrEmpty(byOrderNumber))
            {
                orders = orders.Where(o => o.order_number.Contains(byOrderNumber));
            }
            if (!string.IsNullOrEmpty(byEmail))
            {
                orders = orders.Where(o => o.email.Contains(byEmail));
            }
            if (byPayment > 0)
            {
                if(byPayment == 1)
                    orders = orders.Where(o => o.gateway.Equals("Bank Deposit"));
                else
                    orders = orders.Where(o => o.gateway.Equals("mercado_pago"));
            }
            if (!string.IsNullOrEmpty(byName))
            {
                List<Customer> customers = _context.Customer.Where(c => c.first_name.Contains(byName) || c.last_name.Contains(byName)).ToList();
            }
            var filter = orders.OrderByDescending(p => p.created_at).Skip(ExcludeRecords).Take(pageSize);

            var result = new PagedResult<Orders>
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
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("id,email,created_at,updated_at,number,token,gateway,total_price,subtotal_price,total_tax,currency,financial_status,total_discounts,total_line_items_price,name,order_number")] Orders order)
        {
            if (id != order.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
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
    }
}
