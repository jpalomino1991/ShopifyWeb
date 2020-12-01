using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using cloudscribe.Pagination.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using ShopifyWeb.Data;
using ShopifyWeb.Models;

namespace ShopifyWeb.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ILogger<ProductsController> _logger;
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Products
        public IActionResult Index2()
        {
            ViewProduct vp = new ViewProduct();
            vp.Brands = _context.Brand.ToList();
            vp.ProductTypes = _context.ProductType.ToList();
            vp.Products = _context.Product.Where(p => p.ParentId == null).ToList();
            return View(vp);
        }

        public IActionResult Index(string byName,string BySKU, string byVendor,string byType,string byStock,int pageNumber = 1,int pageSize = 5)
        {
            int ExcludeRecords = (pageSize * pageNumber) - pageSize;

            var products = _context.Product.Where(p => p.ParentId == null);
            if (!string.IsNullOrEmpty(byName))
                products = products.Where(p => p.Title.Contains(byName));
            if (!string.IsNullOrEmpty(BySKU))
                products = products.Where(p => p.SKU.Contains(BySKU));
            if (!string.IsNullOrEmpty(byType))
                products = products.Where(p => p.ProductType.Contains(byType));
            if (!string.IsNullOrEmpty(byVendor))
                products = products.Where(p => p.Vendor.Contains(byVendor));
            if (byStock == "2")
                products = products.Where(p => p.Status == "active");
            if (byStock == "3")
                products = products.Where(p => p.Status == "draft");

            var filter = products.Skip(ExcludeRecords).Take(pageSize);

            List<ViewProduct> ls = new List<ViewProduct>();
            ViewProduct vp = new ViewProduct();
            vp.Brands = _context.Brand.AsNoTracking().ToList();
            vp.ProductTypes = _context.ProductType.AsNoTracking().ToList();
            vp.Products = filter.AsNoTracking().ToList();

            ls.Add(vp);

            var result = new PagedResult<ViewProduct>
            {
                Data = ls,
                TotalItems = products.Count(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return View(result);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ParentId,Title,Description,Tags,Vendor,ProductType,Handle,Barcode,InventoryItemId,Stock,Price,CompareAtPrice,SKU,Size")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public IActionResult Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            EditProduct ps = new EditProduct();
            ps.parent = _context.Product.Find(id);
            var childs = _context.Product.Where(p => p.ParentId == id).OrderBy(p => p.ParentId).ToList();
            ps.childs = childs;

            List<string> lstimg = getFTPImages(ps.parent.SKU);
            ps.imgtoShow = lstimg;
            if (childs == null)
            {
                return NotFound();
            }
            return View(ps);
        }

        public List<string> getFTPImages(string sku)
        {
            List<ProductImage> lstImage = _context.ProductImage.Where(i => i.name.Contains(sku)).ToList();

            List<string> result = new List<string>();

            if (lstImage.Count > 0)
            {
                int i = 1;
                foreach (ProductImage image in lstImage)
                {
                    Web web = new Web();
                    web = _context.Web.Find(1);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{web.SMTPURL}/{image.name}");
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.Credentials = new NetworkCredential(web.SMTPUser, web.SMTPPassword);
                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    Stream responseStream = response.GetResponseStream();
                    byte[] bytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        responseStream.CopyTo(memoryStream);
                        bytes = memoryStream.ToArray();
                    }

                    string img = Convert.ToBase64String(bytes);

                    //img = $"<img src='data:image/jpeg;base64,{img}' class='file-preview-image' alt='{sku}_{i}.jpg' title='{sku}_{i}.jpg'>";

                    result.Add(img);
                    i++;
                }
            }
            return result;
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EditProduct lstProduct)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ProductShopify ps = new ProductShopify();

                    ps.vendor = lstProduct.parent.Vendor;
                    ps.product_type = lstProduct.parent.ProductType;
                    ps.body_html = lstProduct.parent.Description;
                    ps.tags = lstProduct.parent.Tags;
                    ps.handle = lstProduct.parent.Handle;
                    ps.id = lstProduct.parent.Id;
                    ps.title = lstProduct.parent.Title;                    
                    ps.metafields_global_description_tag = lstProduct.parent.SEODescription;
                    ps.metafields_global_title_tag = lstProduct.parent.SEOTitle;
                    lstProduct.parent.UpdateDate = DateTime.Now;

                    List<Variant> lsVariant = new List<Variant>();
                    int stock = 0;

                    foreach (Product child in lstProduct.childs)
                    {
                        Variant variant = new Variant();

                        variant.id = child.Id;
                        variant.sku = child.SKU;
                        variant.price = child.Price;
                        variant.option1 = child.Size;
                        variant.inventory_quantity = child.Stock;
                        variant.inventory_management = "shopify";
                        variant.compare_at_price = child.CompareAtPrice;
                        stock += child.Stock;
                        lsVariant.Add(variant);

                        child.UpdateDate = DateTime.Now;
                    }

                    ps.variants = lsVariant;
                    string status = "";
                    if (stock <= 0)
                        status = "draft";
                    else
                        status = "active";

                    ps.status = status;
                    lstProduct.parent.Status = status;

                    List<ImageShopify> imageShopifies = new List<ImageShopify>();
                    if(lstProduct.imgtoShow != null)
                    {
                        int i = 1;
                        foreach (var file in lstProduct.imgtoShow)
                        {
                            ImageShopify imgS = new ImageShopify();
                            imgS.attachment = file;
                            imgS.filename = $"{ps.metafields_global_title_tag.ToUpper().Replace(" ", "_")}_{i}.jpg";

                            imageShopifies.Add(imgS);
                            i++;
                        }
                    }

                    ps.images = imageShopifies;

                    dynamic oJson = new
                    {
                        product = ps
                    };

                    IRestResponse response = CallShopify("products/" + ps.id + ".json", Method.PUT, oJson);
                    if (response.StatusCode.ToString().Equals("OK"))
                    {
                        _context.Product.Update(lstProduct.parent);
                        _context.Product.UpdateRange(lstProduct.childs);
                        _context.SaveChanges();
                        _logger.LogInformation("Product uploaded");
                    }
                    else
                        _logger.LogError("Error uploading product: " + response.ErrorMessage);                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    /*if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }*/
                }
                return Ok();
            }
            return View(lstProduct);
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
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError("Error calling API",e);
                return null;
            }
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var product = await _context.Product.FindAsync(id);
            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(string id)
        {
            return _context.Product.Any(e => e.Id == id);
        }
    }
}
