using cloudscribe.Pagination.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopifyWeb.Data;
using ShopifyWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Controllers
{
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ApplicationDbContext context, ILogger<LogsController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult Index(DateTime byDate, int pageNumber = 1, int pageSize = 10)
        {
            if(byDate == DateTime.MinValue)
            {
                byDate = DateTime.Now;
            }
            ViewBag.byDate = byDate.ToString("yyyy-MM-dd");
            var logs = _context.Logs.Where(l => l.DateStart.Date.Equals(byDate.Date));

            int ExcludeRecords = (pageSize * pageNumber) - pageSize;

            var filter = logs.OrderByDescending(p => p.DateStart).Skip(ExcludeRecords).Take(pageSize);

            var result = new PagedResult<Logs>
            {
                Data = filter.AsNoTracking().ToList(),
                TotalItems = logs.Count(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return View(result);
        }
    }
}
