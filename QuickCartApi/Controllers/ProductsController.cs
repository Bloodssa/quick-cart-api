using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.DTOs.Product;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;
using QuickCartApi.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace QuickCartApi.Controllers
{
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [EnableRateLimiting(RateLimitingExtension.ReadPolicy)]
    public class ProductsController(IProductService productService) : ControllerBase
    {
        // GET: api/<ProductsController>
        [HttpGet]
        public async Task<ActionResult<ProductListResponse>> Get
        (
            [FromQuery] ProductQuery q
        )
        {
            return Ok(await productService.GetAllAsync(q));
        }

        // GET api/<ProductsController>/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var product = await productService.GetByIdAsync(id);
            return product is null ? NotFound(new { message = "Product not found."}) : Ok(product);
        }

        // POST api/<ProductsController>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<ActionResult<ProductResponse>> Create([FromForm] CreateProductRequest request)
        {
            var (product, error) = await productService.CreateAsync(request);
            if (error is not null) return BadRequest(new { message = error});

            return CreatedAtAction(nameof(GetById), new { id = product!.Id }, product);
        }

        // PUT api/<ProductsController>/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<ActionResult<ProductResponse>> Put(int id, [FromForm] UpdateProductRequest request)
        {
            var (success, error, response) = await productService.UpdateAsync(id, request);

            if (!success)
                return error == "Product not found." ?
                    NotFound(new { message = error }) : BadRequest(new { message = error });

            return Ok(response);
        }

        // DELETE api/<ProductsController>/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await productService.DeleteAsync(id);
            return result.Success ? NoContent() : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var (success, error) = await productService.DeleteImageAsync(id, imageId);
            return success ? NoContent() : NotFound(new { message = error });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/images/{imageId:int}/default")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<IActionResult> SetDefaultImage(int id, int imageId)
        {
            var (success, error) = await productService.SetDefaultImageAsync(id, imageId);
            return success ? NoContent() : NotFound(new { message = error });
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:int}/stock")]
        [EnableRateLimiting(RateLimitingExtension.WritePolicy)]
        public async Task<IActionResult> AdjustStock(int id, [FromBody] AdjustStockRequest request)
        {
            var (success, error, stock) = await productService.AdjustStockAsync(id, request.Delta);

            if (!success)
                return error == "Product not found." ? NotFound(new { message = error }) : BadRequest(new { message = error });

            return Ok(new { id, stockQuantity = stock });
        }

        [Authorize(Roles = "Admin")]    
        [HttpGet("stock-summary")]
        public async Task<ActionResult<StockSummaryResponse>> GetStockSummary()
            => Ok(await productService.GetStockSummaryAsync());
    }
}
