using Microsoft.AspNetCore.Mvc;

namespace SafeWebCore.Examples.ApiService.Controllers;

/// <summary>
/// A typical secured API controller.
/// Security headers are added automatically — no per-controller configuration needed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private static readonly Product[] SampleProducts =
    [
        new(1, "Widget", 9.99m),
        new(2, "Gadget", 24.99m),
        new(3, "Doohickey", 4.99m),
    ];

    /// <summary>Returns all products. Response includes full security header set.</summary>
    [HttpGet]
    public IActionResult GetAll() => Ok(SampleProducts);

    /// <summary>Returns a single product by id.</summary>
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var product = Array.Find(SampleProducts, p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }
}

/// <param name="Id">Product identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Price">Unit price in USD.</param>
public sealed record Product(int Id, string Name, decimal Price);
