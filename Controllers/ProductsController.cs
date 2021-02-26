using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using cloudscribe.Pagination.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        public IActionResult Index(string byName,string BySKU, string byVendor,string byType,string byStock,int pageNumber = 1,int pageSize = 10)
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

            var filter = products.OrderBy(p => p.SKU).Skip(ExcludeRecords).Take(pageSize);

            ViewBag.Brand = _context.Brand.OrderBy(b => b.Name).AsNoTracking().ToList();
            ViewBag.ProductType = _context.ProductType.OrderBy(p => p.Name).AsNoTracking().ToList();

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

                        string body = "<table width='100%'><tbody><tr><td><strong>Color: </strong>{0}</td><td><strong>Marca: </strong>{1}</td><td><strong>Taco:&nbsp;</strong>{2}</td></tr>" +
                            "<tr><td><strong>Material:<span>&nbsp;</span></strong>{3}</td><td><strong>Material Interior:<span>&nbsp;</span></strong>{4}</td><td><strong>Material de Suela:<span>" +
                            "&nbsp;</span></strong>{5}</td></tr><tr><td><strong>Hecho en:<span>&nbsp;</span></strong>{6}</td><td><strong>Modelo:<span>&nbsp;</span></strong>{7}</td><td><br></td>" +
                            "</tr></tbody></table>";

                        string cp = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.CodigoProducto.ToLower());
                        string mat = parent.Material != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(' ', parent.Material.ToLower().Split('/').ToList())) : "";
                        string matI = parent.MaterialInterior != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(',', parent.MaterialInterior.ToLower().Split('/').ToList())) : "";
                        string matS = parent.MaterialSuela != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(',', parent.MaterialSuela.ToLower().Split('/').ToList())) : "";
                        string col = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Color.ToLower());
                        string mar = parent.Marca != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Marca.ToLower()) : "";
                        string oca = parent.Ocasion != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Ocasion.ToLower()) : "";
                        string ten = parent.Tendencia != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Tendencia.ToLower()) : "";
                        string imageName = "";

                        string sex = parent.SegmentoNivel2;
                        if (parent.SegmentoNivel2 == "Mujer" && parent.SegmentoNivel3 == "Niña")
                            sex = "Niñas";
                        if (parent.SegmentoNivel2 == "Hombre" && parent.SegmentoNivel3 == "Niño")
                            sex = "Niños";

                        ps.Vendor = mar;

                        if (parent.SegmentoNivel4 == "Zapatos")
                        {
                            if (parent.SegmentoNivel5 == "Stiletto")
                                ps.ProductType = "Stilettos";
                            if (parent.SegmentoNivel5 == "Fiesta" || parent.SegmentoNivel5 == "Vestir")
                                ps.ProductType = $"{parent.SegmentoNivel4} de {parent.SegmentoNivel5}";
                            else
                                ps.ProductType = $"{parent.SegmentoNivel4} {parent.SegmentoNivel5}";
                        }
                        if (parent.SegmentoNivel4 == "Accesorios")
                            ps.ProductType = parent.SegmentoNivel5;
                        else
                            ps.ProductType = parent.SegmentoNivel4;
                        ps.Description = String.Format(body, col, mar, parent.Taco, mat, matI, matS, parent.HechoEn, cp);
                        ps.Tags = $"{(parent.SegmentoNivel4 == "Accesorios" ? $"{parent.SegmentoNivel4},{parent.SegmentoNivel5}" : ps.ProductType)},{mat},{col},{cp},{mar},{parent.SegmentoNivel1},{(sex == "Unisex" ? "Hombre,Mujer" : (sex != parent.SegmentoNivel2 ? "Kids," + sex : sex))},{parent.SegmentoNivel4},{parent.CodigoPadre},{ten},{oca},{parent.Taco}";
                        ps.Handle = $"{cp}-{parent.SegmentoNivel4}-{sex}-{col}-{mar}";

                        if (parent.SegmentoNivel4 == "Pantuflas" || parent.SegmentoNivel4 == "Alpargatas")
                        {
                            ps.Title = $"{parent.SegmentoNivel4} {col} {cp}";
                            ps.SEODescription = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} {parent.SegmentoNivel4} {cp} {mat} {col} {mar}";
                            ps.SEOTitle = $"{parent.SegmentoNivel4} {cp} {mat} | {col} | {mar}";
                            imageName = $"{parent.SegmentoNivel4}_{cp}_{mat}_{col}_{mar}";
                        }
                        else
                        {
                            if (parent.SegmentoNivel5 == "Stiletto")
                            {
                                ps.Title = $"Stilettos {col} {cp}";
                                ps.SEODescription = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} Stilettos {cp} {mat} {col} {mar}";
                                ps.SEOTitle = $"Stilettos {cp} {mat} | {col} | {mar}";
                                imageName = $"{parent.SegmentoNivel4}_{parent.SegmentoNivel5}_{cp}_{mat}_{col}_{mar}";
                            }
                            if (parent.SegmentoNivel4 == "Accesorios")
                            {
                                ps.Title = $"{parent.SegmentoNivel5} {col} {cp}";
                                ps.SEODescription = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} {parent.SegmentoNivel5} {cp} {mat} {col} {mar}";
                                ps.SEOTitle = $"{parent.SegmentoNivel5} {cp} {mat} | {col} | {mar}";
                                imageName = $"{parent.SegmentoNivel5}_{cp}_{mat}_{col}_{mar}";
                            }
                            else
                            {
                                ps.Title = $"{parent.SegmentoNivel4} {parent.SegmentoNivel5} {col} {cp}";
                                ps.SEODescription = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} {parent.SegmentoNivel4} {parent.SegmentoNivel5} {cp} {mat} {col} {mar}";
                                ps.SEOTitle = $"{parent.SegmentoNivel4} {parent.SegmentoNivel5} {cp} {mat} | {col} | {mar}";
                                imageName = $"{parent.SegmentoNivel4}_{parent.SegmentoNivel5}_{cp}_{mat}_{col}_{mar}";
                            }
                        }

                        ps.SEODescription = ps.SEODescription.Trim();
                        ps.CreateDate = DateTime.Now;
                        ps.UpdateDate = DateTime.Now;
                        ps.SKU = SKU;

                        List<KellyChild> lsChild = new List<KellyChild>();
                        List<ProductTempImage> lstImage = new List<ProductTempImage>();

                        lsChild = _context.KellyChild.FromSqlInterpolated($"GetProductChildInfo {parent.CodigoPadre}").ToList();
                        lstImage = _context.ProductTempImage.Where(i => i.sku == parent.CodigoPadre).OrderBy(i => i.name).ToList();

                        string talla = String.Join(",", lsChild.Select(r => r.Talla).ToArray());
                        ps.Tags += "," + talla;

                        List<string> imageShopifies = new List<string>();

                        if (lstImage.Count > 0)
                        {
                            foreach (ProductTempImage image in lstImage)
                            {
                                Web web = _context.Web.Find(1);
                                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{web.SMTPURL}/{image.name}{image.extension}");
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
                            variant.Peso = child.Peso;
                            stock += child.StockTotal < 0 ? 0 : child.StockTotal;
                            lsVariant.Add(variant);
                        }

                        if (stock <= 0 || lstImage.Count == 0)
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

        public ImageShopify getImageFromFtp(ProductTempImage image, string imageName, int i)
        {
            try
            {
                Web web = _context.Web.Find(1);
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{web.SMTPURL}/{image.name}{image.extension}");
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

                ImageShopify imgS = new ImageShopify();
                imgS.attachment = img;
                imgS.filename = $"{imageName.ToUpper()}_{i}.jpg";
                imgS.alt = $"{imageName.ToUpper()}_{i}.jpg";

                return imgS;
            }
            catch (Exception e)
            {
                if (e.Message == "Unable to connect to the remote server")
                {
                    return getImageFromFtp(image, imageName, i);
                }
                _logger.LogError(e, "Error in ftp");
                return null;
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
        public IActionResult Create(EditProduct lstProduct)
        {
            if(ModelState.IsValid)
            {
                return Ok();
            }
            return NotFound();
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

            List<ProductImage> lstImg = _context.ProductImage.Where(p => p.product_id == id).ToList();
            List<string> lstStr = new List<string>();

            foreach(ProductImage pi in lstImg)
            {
                lstStr.Add(pi.src);
            }
            //List<string> lstimg = getFTPImages(ps.parent.SKU);
            ps.imgtoShow = lstStr;
            if (childs == null)
            {
                return NotFound();
            }
            return View(ps);
        }

        public List<string> getFTPImages(string sku)
        {
            List<ProductTempImage> lstImage = _context.ProductTempImage.Where(i => i.sku.Contains(sku)).ToList();

            List<string> result = new List<string>();

            if (lstImage.Count > 0)
            {
                int i = 1;
                foreach (ProductTempImage image in lstImage)
                {
                    Web web = new Web();
                    web = _context.Web.Find(1);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{web.SMTPURL}/{image.name}{image.extension}");
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

        [HttpPost]
        public FileResult Download(string byName, string BySKU, string byVendor, string byType, string byStock)
        {
            if (string.IsNullOrEmpty(byStock))
                byStock = "1";
            List<ProductDownload> products = _context.ProductDownload.FromSqlInterpolated($"GetProductForDownload @byName = {(string.IsNullOrEmpty(byName) ? 0 : 1)},@Name = {(string.IsNullOrEmpty(byName) ? "" : byName)},@bySKU = {(string.IsNullOrEmpty(BySKU) ? 0 : 1)},@SKU = {(string.IsNullOrEmpty(BySKU) ? "" : BySKU)},@byVendor = {(string.IsNullOrEmpty(byVendor) ? 0 : 1)},@Vendor = {(string.IsNullOrEmpty(byVendor) ? "" : byVendor)},@byType = {(string.IsNullOrEmpty(byType) ? 0 : 1)},@Type = {(string.IsNullOrEmpty(byType) ? "" : byType)},@byStock = {(byStock == "1" ? 0 : 1)},@Stock = {(byStock == "2" ? "active" : "draft")}").ToList();

            List<object> customers = new List<object>();
            customers.Insert(0, new string[30] { "SKU", "Handle","Titulo", "Tags", "Id de Shopify", "Tipo de Producto", "Proveedor", "Titulo SEO", "Metadescripcion", "URL", "Talla", "Stock", "Precio", "Precio Promocion", "Color Padre", "Material", "Taco", "Genero", "Ocasion", "Tendencia", "Departamento", "Estado", "Cantidad de Imagenes", "Fecha de Creacion", "Marca", "Material Interior", "Material Suela", "Hecho En", "Modelo", "Peso" });

            StringBuilder sb = new StringBuilder();

            object[] customer = (object[])customers[0];
            for (int j = 0; j < customer.Length; j++)
            {
                sb.Append(customer[j].ToString() + ';');
            }
            sb.Append("\r\n");

            foreach(ProductDownload p in products)
            {
                sb.Append(p.ToLine());
                sb.Append("\r\n");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Product.csv");
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
                    ps.published_scope = "global";
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
                        variant.grams = child.Peso;
                        variant.weight = child.Peso.ToString();
                        variant.weight_unit = "g";
                        stock += child.Stock;
                        lsVariant.Add(variant);

                        child.UpdateDate = DateTime.Now;
                    }

                    ps.variants = lsVariant;
                    string status = "";
                    if (stock <= 0 || lstProduct.imgtoShow == null)
                        status = "draft";
                    else
                        status = "active";

                    ps.status = status;
                    lstProduct.parent.Status = status;

                    List<ImageShopify> imageShopifies = new List<ImageShopify>();
                    ps.images = new List<ImageShopify>();
                    if(lstProduct.imgtoShow.Count > 0)
                    {
                        int i = 1;
                        foreach (string file in lstProduct.imgtoShow)
                        {                            
                            if (!Regex.IsMatch(file, @"^(http|https|ftp|)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?([a-zA-Z0-9\-\?\,\'\/\+&%\$#_]+)"))
                            {
                                ImageShopify imgS = new ImageShopify();
                                imgS.attachment = file;
                                imgS.filename = $"{ps.metafields_global_title_tag.ToUpper().Replace("| ", "").Replace(" ", "_")}_{i}.jpg";
                                imgS.alt = imgS.filename;
                                imageShopifies.Add(imgS);
                            }
                            i++;
                        }
                    }

                    List<Option> lsOpt = new List<Option>();
                    Option option = new Option();
                    option.name = "Talla";
                    option.position = 1;
                    lsOpt.Add(option);

                    ps.options = lsOpt;

                    if (ps.id == null)
                        ps.images = imageShopifies;                    

                    dynamic oJson = new
                    {
                        product = ps
                    };

                    if(ps.id != null)
                    {
                        List<ProductImage> lstPi = _context.ProductImage.Where(p => p.product_id == ps.id).ToList();
                        foreach(ProductImage pi in lstPi)
                        {
                            if(!lstProduct.imgtoShow.Contains(pi.src))
                            {
                                IRestResponse response1 = CallShopify($"products/{ps.id}/images/{pi.id}.json",Method.DELETE,null);
                                if(response1.StatusCode.ToString().Equals("OK"))
                                {
                                    _context.ProductImage.Remove(pi);
                                }
                                else
                                {
                                    _logger.LogError("Error uploading product: " + response1.ErrorMessage);
                                    return NotFound(response1.ErrorMessage);
                                }
                            }
                        }

                        IRestResponse response = CallShopify("products/" + ps.id + ".json", Method.PUT, oJson);
                        if (response.StatusCode.ToString().Equals("OK"))
                        {
                            if (imageShopifies.Count > 0)
                            {
                                foreach(ImageShopify imgS in imageShopifies)
                                {
                                    dynamic oJ = new
                                    {
                                        image = imgS
                                    };
                                    IRestResponse response1 = CallShopify($"products/{ps.id}/images.json", Method.POST, oJ);
                                    if(response1.StatusCode.ToString().Equals("OK"))
                                    {
                                        MasterImage mi = JsonConvert.DeserializeObject<MasterImage>(response1.Content);

                                        ProductImage pi = new ProductImage();
                                        pi.id = mi.image.id;
                                        pi.product_id = lstProduct.parent.Id;
                                        pi.src = mi.image.src;
                                        pi.alt = mi.image.alt;

                                        _context.ProductImage.Add(pi);
                                    }
                                    else
                                    {
                                        _logger.LogError("Error uploading product: " + response1.ErrorMessage);
                                        return NotFound(response1.ErrorMessage);
                                    }
                                }                                
                            }
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

                            _context.Product.Add(lstProduct.parent);
                            _context.Product.AddRange(lstProduct.childs);

                            foreach (ImageShopify img in mp.product.images)
                            {
                                ProductImage pi = new ProductImage();
                                pi.id = img.id;
                                pi.product_id = lstProduct.parent.Id;
                                pi.src = img.src;
                                pi.alt = img.alt;

                                _context.ProductImage.Add(pi);
                            }

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

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || response.StatusCode.ToString().Equals("520"))
                {
                    System.Threading.Thread.Sleep(5000);
                    return CallShopify(resource, method, parameters);
                }

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
        public IActionResult DeleteConfirmed(string id)
        {
            Product product = _context.Product.Find(id);
            List<Product> childs = _context.Product.Where(p => p.ParentId == id).ToList();
            List<ProductImage> images = _context.ProductImage.Where(p => p.product_id == id).ToList();

            IRestResponse response = CallShopify("products/" + product.Id + ".json", Method.DELETE, null);
            if (response.StatusCode.ToString().Equals("OK"))
            {
                _context.Product.Remove(product);
                _context.Product.RemoveRange(childs);
                _context.ProductImage.RemoveRange(images);
                _context.SaveChanges();
                _logger.LogInformation("Product deleted");
                return Ok();
            }
            else
            {
                _logger.LogError("Error deleting product: " + response.ErrorMessage);
                return NotFound(response.ErrorMessage);
            }
        }

        private bool ProductExists(string id)
        {
            return _context.Product.Any(e => e.Id == id);
        }
    }
}
