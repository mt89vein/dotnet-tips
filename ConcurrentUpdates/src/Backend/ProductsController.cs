using ConflictResolution.Centrifugo;
using DotNext;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ConcurrentUpdates;

[Route("api/[controller]")]
[ApiController]
public sealed class ProductsController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateProductModel product, [FromServices] ProductStore store)
    {
        var createdProduct = store.Save(product.Name, product.Description, product.Price);

        return Ok(createdProduct);
    }

    [HttpGet("{productId:int}")]
    public IActionResult Get([FromRoute] int productId, [FromServices] ProductStore store)
    {
        return Ok(store.GetById(productId));
    }

    [HttpPatch("{productId:int}")]
    public IActionResult PatchAsync(
        [FromRoute] int productId,
        [FromBody] ApplyChangesToProductModel model,
        [FromServices] ProductStore store,
        [FromServices] CentrifugoApi.CentrifugoApiClient centrifugo,
        [FromServices] IOptions<JsonOptions> options)
    {
        if (model is null)
        {
            return BadRequest("Cannot deserialize request body");
        }

        var product = store.GetById(productId);

        if (product is null)
        {
            return Conflict("There is no product with such id");
        }

        if (model.Version <= 0)
        {
            return BadRequest("Specify product version");
        }

        if (model.Version != product.Version)
        {
            return Conflict(product);
        }

        var changes = new ProductChanges(model.Changes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()));

        var applyChangesResult = product.ApplyChanges(changes);

        if (!applyChangesResult.IsSuccessful)
        {
            return applyChangesResult.Error switch
            {
                ProductErrorCodes.ProductNameCannotBeEmpty => BadRequest("Product name cannot be empty"),
                ProductErrorCodes.ProductPriceCannotBeEmpty => BadRequest("Product price cannot be empty"),
                _ => BadRequest("Unable to apply changes to product")
            };
        }

        if (applyChangesResult.Value)
        {
            // in real world, you may want to publish to centrifugo using outbox pattern
            // or at least asynchronously in fire & forget way
            centrifugo.Publish(new PublishRequest
            {
                Channel = $"products:{product.Id}",
                Data = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(product, options.Value.JsonSerializerOptions)),
            });

            return Ok(product);
        }

        return NoContent();
    }
}
