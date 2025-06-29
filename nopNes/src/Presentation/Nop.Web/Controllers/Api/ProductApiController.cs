using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Vendors;
using Nop.Services.Stores;
using Nop.Services.Media;

namespace Nop.Web.Controllers.Api
{
    [Route("api/products")]
    [ApiController]
    public class ProductApiController : Controller
    {
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IStoreContext _storeContext;
        private readonly IPictureService _pictureService;
        private readonly IProductAttributeService _productAttributeService;

        public ProductApiController(IProductService productService, IVendorService vendorService, IStoreContext storeContext, IPictureService pictureService, IProductAttributeService productAttributeService)
        {
            _productService = productService;
            _vendorService = vendorService;
            _storeContext = storeContext;
            _pictureService = pictureService;
            _productAttributeService = productAttributeService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Product name is required.");

            var product = new Product
            {
                Name = model.Name,
                ShortDescription = model.ShortDescription,
                FullDescription = model.FullDescription,
                Price = model.Price,
                Published = model.Published,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _productService.InsertProductAsync(product);

            return Ok(new
            {
                message = "Product created successfully",
                product.Id,
                product.Name,
                product.Price
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetAllOnHomepage()
        {
            var product = await _productService.GetAllProductsDisplayedOnHomepageAsync();

            var result = product.Select(c => new
            {
                c.Id,
                c.Name,
                c.ShortDescription,
                c.FullDescription,
                c.Price,
                c.Published,
                c.CreatedOnUtc,
                c.UpdatedOnUtc
            });

            return Ok(result);
        }

        [HttpGet("spec/{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            var vendor = await _vendorService.GetVendorByIdAsync(product.VendorId);
            var vendorName = vendor.Name;
            if (product == null)
                return NotFound($"No product found with ID {id}");

            var result = new
            {
                product.Id,
                product.Name,
                product.FullDescription,
                product.Price,
                product.Sku,
                vendorName
            };

            return Ok(result);
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchProductsByName([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Search term is required." });

            var products = await _productService.SearchProductsAsync(
                keywords: name,
                pageIndex: 0,
                pageSize: 20
            );

            var result = products.Select(p => new {
                p.Id,
                p.Name,
                p.ShortDescription,
                p.FullDescription,
                p.Price,
                p.Published
            });

            return Ok(result);
        }
        [HttpGet("category/{categoryId}/products")]
        public async Task<IActionResult> GetProductsByCategoryId(int categoryId)
        {
            var products = await _productService.SearchProductsAsync(
                categoryIds: new List<int> { categoryId },
                pageSize: int.MaxValue
            );

            var result = new List<object>();

            foreach (var product in products)
            {
                var picture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();

                result.Add(new
                {
                    product.Id,
                    product.Name,
                    product.Price,
                    product.ShortDescription,
                    product.FullDescription,
                    product.Published,
                    product.CreatedOnUtc,
                    product.UpdatedOnUtc,
                    pictureId = picture?.Id ?? 0,
                    seoFilename = picture?.SeoFilename ?? "",
                    mimeType = picture?.MimeType ?? ""
                });
            }

            return Ok(result);
        }
        [HttpGet("product/{productId}/rating")]
        public async Task<IActionResult> GetProductRating(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var currentStore = await _storeContext.GetCurrentStoreAsync();
            var reviews = await _productService.GetAllProductReviewsAsync(productId: productId, approved: true, storeId: currentStore.Id);
            if (reviews == null || !reviews.Any())
                return Ok(new { averageRating = 0, totalReviews = 0 });

            var averageRating = reviews.Average(r => r.Rating);
            return Ok(new { averageRating, totalReviews = reviews.Count });
        }


        // GET: api/products/{productId}/reviews
        //[HttpGet("{productId}/reviews")]
        //public async Task<IActionResult> GetProductReviews(int productId)
        // {
        // var product = await _productService.GetProductByIdAsync(productId);
        //     if (product == null)
        // return NotFound(new { message = "Product not found" });

        //var reviews = product.ProductReviews?.Where(r => r.IsApproved).Select(r => new
        //{
        //  r.Title,
        // r.ReviewText,
        // r.Rating,
        // r.CustomerId,
        //  r.CreatedOnUtc
        //}).ToList();

        // return Ok(reviews);
        // }
        //}
        [HttpGet("attributes/{productId}")]
        public async Task<IActionResult> GetProductAttributes(int productId)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || product.Deleted)
                return NotFound(new { message = "Product not found." });

            // Get product attribute mappings
            var mappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId);
            if (mappings == null || !mappings.Any())
                return Ok(new { productId, productName = product.Name, attributes = new List<object>() });

            var attributeList = new List<object>();

            foreach (var mapping in mappings)
            {
                var attribute = await _productAttributeService.GetProductAttributeByIdAsync(mapping.ProductAttributeId);
                if (attribute == null)
                    continue;

                var values = await _productAttributeService.GetProductAttributeValuesAsync(mapping.Id) ?? new List<ProductAttributeValue>();

                attributeList.Add(new
                {
                    AttributeId = attribute.Id,
                    AttributeName = attribute.Name,
                    AttributeDescription = attribute.Description,
                    IsRequired = mapping.IsRequired,
                    AttributeControlType = mapping.AttributeControlType.ToString(),
                    DisplayOrder = mapping.DisplayOrder,
                    Values = values.Select(v => new
                    {
                        v.Id,
                        v.Name,
                        v.ColorSquaresRgb,
                        v.PriceAdjustment,
                        v.WeightAdjustment,
                        v.Cost,
                        v.CustomerEntersQty,
                        v.Quantity,
                        v.IsPreSelected,
                        v.DisplayOrder,
                        v.PictureId
                    }).OrderBy(v => v.DisplayOrder).ToList()
                });
            }

            return Ok(new
            {
                productId,
                productName = product.Name,
                attributes = attributeList.OrderBy(a => ((dynamic)a).DisplayOrder).ToList()
            });
        }
        [HttpGet("filter-by-price")]
        public async Task<IActionResult> FilterProductsByPrice([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
        {
            if (minPrice < 0 || maxPrice < 0 || maxPrice < minPrice)
                return BadRequest(new { message = "Invalid price range." });
// Get all products within the price range
var products = await _productService.SearchProductsAsync(
    priceMin: minPrice,
    priceMax: maxPrice,
    pageSize: int.MaxValue
);

            var result = new List<object>();

            foreach (var product in products)
            {
                var picture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();
                var imageUrl = await _pictureService.GetPictureUrlAsync(
   pictureId: picture?.Id ?? 0,
   targetSize: 0,
   storeLocation: null,
   showDefaultPicture: true
   );
                result.Add(new
                {
                    product.Id,
                    product.Name,
                    product.ShortDescription,
                    product.FullDescription,
                    product.Price,
                    product.Published,
                    product.CreatedOnUtc,
                    ImageUrl = imageUrl
                });
            }

            return Ok(result);
        }
    }


    public class CreateProductRequest
    {
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string FullDescription { get; set; }
        public decimal Price { get; set; }
        public bool Published { get; set; }
    }
}
