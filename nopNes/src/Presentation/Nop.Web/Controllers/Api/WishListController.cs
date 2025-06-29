using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Media;
using Nop.Services.Orders;
namespace Nop.Web.Controllers.Api;
[Route("api/wishlist")]
[ApiController]
public class WishlistController : ControllerBase
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IPictureService _pictureService;

    public WishlistController(
     IShoppingCartService shoppingCartService,
     ICustomerService customerService,
     IProductService productService,
       IPictureService  pictureService)
    {
        _shoppingCartService = shoppingCartService;
        _customerService = customerService;
        _productService = productService;
          _pictureService = pictureService;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToWishlist([FromBody] WishlistItemDto model)
    {
        if (model == null)
            return BadRequest(new { message = "Invalid data." });

        var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        var product = await _productService.GetProductByIdAsync(model.ProductId);
        if (product == null)
            return NotFound(new { message = "Product not found." });

        await _shoppingCartService.AddToCartAsync(
            customer,
            product,
            ShoppingCartType.Wishlist,
            storeId: 1,
            quantity: model.Quantity
        );

        return Ok(new { message = "Product added to wishlist." });
    }
    [HttpGet("wishlist/{customerId}")]
    public async Task<IActionResult> GetWishlistItems(int customerId)
    {
        var customer = await _customerService.GetCustomerByIdAsync(customerId);
        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        var wishlistItems = await _shoppingCartService.GetShoppingCartAsync(
            customer,
            ShoppingCartType.Wishlist,
            storeId: 1
        );

        var result = new List<object>();

        foreach (var item in wishlistItems)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            if (product == null)
                continue;

            var imageUrl = await _pictureService.GetDefaultPictureUrlAsync(product.Id);

            result.Add(new
            {
                Id = item.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ShortDescription = product.ShortDescription,
                Quantity = item.Quantity,
                Price = product.Price,
                Published = product.Published,
                CreatedOnUtc = item.CreatedOnUtc,
                ImageUrl = imageUrl
            });
        }

        return Ok(result);
    }


    public class WishlistItemDto
    {
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
