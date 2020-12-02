using System;
using System.Collections.Generic;
using System.Globalization;
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
            ViewBag.byName = byName;
            ViewBag.bySKU = BySKU;
            ViewBag.byVendor = byVendor;
            ViewBag.byType = byType;
            ViewBag.byStock = byStock;
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

            ViewBag.Brand = _context.Brand.AsNoTracking().ToList();
            ViewBag.ProductType = _context.ProductType.AsNoTracking().ToList();

            var result = new PagedResult<Product>
            {
                Data = filter.AsNoTracking().ToList(),
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
        public IActionResult Create(string SKU)
        {
            try
            {
                Product product = _context.Product.Where(p => p.SKU == SKU).SingleOrDefault();

                if (product == null)
                {
                    EditProduct ep = new EditProduct();

                    var lst = _context.ProductKelly.FromSqlInterpolated($"GetProductInfoForShopify @CodigoSistema = {SKU}").ToList();

                    if (lst != null)
                    {
                        ProductKelly parent = lst[0];
                        Product ps = new Product();

                        string body = "<table width='100%'><tbody><tr><td><strong>Color: </strong>Camel</td><td><strong>Marca: </strong>{0}</td><td><strong>Taco:&nbsp;</strong>{1}</td></tr>" +
                            "<tr><td><strong>Material:<span>&nbsp;</span></strong>{2}</td><td><strong>Material Interior:<span>&nbsp;</span></strong>{3}</td><td><strong>Material de Suela:<span>" +
                            "&nbsp;</span></strong>{4}</td></tr><tr><td><strong>Hecho en:<span>&nbsp;</span></strong>{5}</td><td><strong>Modelo:<span>&nbsp;</span></strong>{6}</td><td><br></td>" +
                            "</tr></tbody></table>";

                        ps.Vendor = parent.Marca;
                        ps.ProductType = parent.SegmentoNivel4;
                        ps.Description = String.Format(body, parent.Marca, parent.Taco, parent.Material, parent.MaterialInterior, parent.MaterialSuela, parent.HechoEn, parent.CodigoProducto);
                        ps.Tags = $"{parent.SegmentoNivel2},{parent.Color},{parent.CodigoProducto},{parent.Material},{parent.Marca},{parent.SegmentoNivel1},{parent.SegmentoNivel4},{parent.SegmentoNivel5},{parent.CodigoPadre}";
                        ps.Handle = $"{parent.CodigoProducto}-{parent.SegmentoNivel4}-{parent.SegmentoNivel2}-{parent.Color}-{parent.Marca}";

                        string cp = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.CodigoProducto.ToLower());
                        string mat = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Material.ToLower());
                        string col = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Color.ToLower());
                        string mar = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Marca.ToLower());
                        ps.Title = $"{parent.SegmentoNivel1} {col} {cp}";
                        ps.SEODescription = $"{(parent.Campaña == null ? "" : parent.Campaña + " ")} {parent.SegmentoNivel2} {parent.SegmentoNivel5} {col} {mat} {col} {mar}";
                        ps.SEOTitle = $"{parent.SegmentoNivel5} {cp} {mat}|{col}|{mar}";
                        ps.CreateDate = DateTime.Now;
                        ps.UpdateDate = DateTime.Now;

                        List<KellyChild> lsChild = new List<KellyChild>();
                        List<ProductImage> lstImage = new List<ProductImage>();

                        lsChild = _context.KellyChild.FromSqlInterpolated($"GetProductChildInfo {parent.CodigoPadre}").ToList();
                        lstImage = _context.ProductImage.Where(i => i.name.Contains(parent.CodigoPadre)).ToList();

                        string talla = String.Join(",", lsChild.Select(r => r.Talla).ToArray());
                        ps.Tags += "," + talla;

                        List<string> imageShopifies = new List<string>();

                        if (lstImage.Count > 0)
                        {
                            foreach (ProductImage image in lstImage)
                            {
                                Web web = _context.Web.Find(1);
                                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(web.SMTPURL + "/" + image.name);
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

                                imageShopifies.Add(img);
                            }
                        }

                        ep.imgtoShow = imageShopifies;

                        List<Product> lsVariant = new List<Product>();
                        int stock = 0;

                        foreach (KellyChild child in lsChild)
                        {
                            Product variant = new Product();

                            string promoPrice = GetPromoPrice(child.InicioPromocion, child.FinPromocion, child.PermitePromocion, child.PrecioTV, child.Promocion);
                            variant.SKU = child.CodigoSistema;
                            variant.Price = promoPrice == "" ? child.PrecioTV : decimal.Parse(promoPrice);
                            variant.Size = child.Talla.ToString();
                            variant.Stock = child.StockTotal <= 0 ? 0 : child.StockTotal;
                            variant.CompareAtPrice = promoPrice == "" ? promoPrice : child.PrecioTV.ToString();
                            variant.CreateDate = DateTime.Now;
                            variant.UpdateDate = DateTime.Now;
                            stock += child.StockTotal;
                            lsVariant.Add(variant);
                        }

                        if (stock <= 0)
                            ps.Status = "draft";
                        else
                            ps.Status = "active";
                        ep.parent = ps;
                        ep.childs = lsVariant;

                        return View("Edit", ep);
                    }
                    else
                        return View("Index");
                }
                else
                    return RedirectToAction("Edit", new { id = product.Id });
            }
            catch(Exception e)
            {
                _logger.LogError("Error getting product information", e);
                return NotFound();
            }
        }

        public string GetPromoPrice(string beginDate, string endDate, string isPromo, decimal price, decimal discount)
        {
            string compare_price = "";
            if (string.IsNullOrEmpty(beginDate))
                return compare_price;
            DateTime fechaFin = DateTime.Parse(endDate).AddDays(1).AddTicks(-1);
            if (DateTime.Parse(beginDate) <= DateTime.Now && DateTime.Now <= fechaFin)
            {
                if (isPromo == "Si")
                {
                    decimal promocion = discount / 100;
                    if (promocion > 0)
                    {
                        compare_price = (price * (1 - promocion)).ToString();
                    }
                }
            }

            return compare_price;
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

                    List<Option> lsOpt = new List<Option>();
                    Option option = new Option();
                    option.name = "Talla";
                    option.position = 1;
                    lsOpt.Add(option);

                    ps.options = lsOpt;

                    ps.images = imageShopifies;

                    dynamic oJson = new
                    {
                        product = ps
                    };

                    if(ps.id != null)
                    {
                        IRestResponse response = CallShopify("products/" + ps.id + ".json", Method.PUT, oJson);
                        if (response.StatusCode.ToString().Equals("OK"))
                        {
                            _context.Product.Update(lstProduct.parent);
                            _context.Product.UpdateRange(lstProduct.childs);
                            _context.SaveChanges();
                            _logger.LogInformation("Product uploaded");
                        }
                        else
                        {
                            _logger.LogError("Error uploading product: " + response.ErrorMessage);
                            return NotFound(response.ErrorMessage);
                        }
                    }
                    else
                    {
                        IRestResponse response = CallShopify("products.json", Method.POST, oJson);
                        if (response.StatusCode.ToString().Equals("Created"))
                        {
                            MasterProduct mp = JsonConvert.DeserializeObject<MasterProduct>(response.Content);
                            if (mp.product != null)
                            {
                                lstProduct.parent.Id = mp.product.id;
                                foreach(Variant variant in mp.product.variants)
                                {
                                    Product child = lstProduct.childs.Where(c => c.SKU == variant.sku).FirstOrDefault();
                                    child.Id = variant.id;
                                    child.InventoryItemId = variant.inventory_item_id;
                                    child.ParentId = mp.product.id;
                                }
                            }

                            _context.Product.Update(lstProduct.parent);
                            _context.Product.UpdateRange(lstProduct.childs);
                            _context.SaveChanges();
                            _logger.LogInformation("Product created");
                        }
                        else
                        {
                            _logger.LogError("Error uploading product: " + response.ErrorMessage);
                            return NotFound(response.ErrorMessage);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error updating product", e);
                    return NotFound(e.Message); 
                }
                return Ok();
            }
            return Ok();
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
