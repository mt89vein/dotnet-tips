using DotNext;
using System.ComponentModel.DataAnnotations;

namespace ConcurrentUpdates;

public sealed class Product
{
    public int Id { get; private set; }

    public int Version { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public Product(int id, string name, string? description, decimal price)
    {
        Id = id;
        Version = 1;
        Name = name;
        Description = description;
        Price = price;
    }

    public Result<bool, ProductErrorCodes> ApplyChanges(ProductChanges changes)
    {
        var changed = false;

        if (changes.Price.IsNull)
        {
            return new Result<bool, ProductErrorCodes>(ProductErrorCodes.ProductPriceCannotBeEmpty);
        }

        if (changes.Name.IsNull)
        {
            return new Result<bool, ProductErrorCodes>(ProductErrorCodes.ProductNameCannotBeEmpty);
        }

        if (!changes.Name.IsUndefined && Name != changes.Name.Value)
        {
            Name = changes.Name.Value;
            changed = true;
        }

        if (!changes.Price.IsUndefined && Price != changes.Price)
        {
            Price = changes.Price.Value;
            changed = true;
        }

        if (!changes.Description.IsUndefined && Description != changes.Description)
        {
            Description = changes.Description.Value;
            changed = true;
        }

        if (changed)
        {
            Version++;
        }

        return changed;
    }
}

public enum ProductErrorCodes
{
    ProductNameCannotBeEmpty = 1,
    ProductPriceCannotBeEmpty = 2,
}

public readonly record struct ProductChanges
{
    public Optional<string> Name { get; }
    public Optional<string?> Description { get; }
    public Optional<decimal> Price { get; }

    public ProductChanges(Dictionary<string, string?> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        changes = new Dictionary<string, string?>(changes, StringComparer.OrdinalIgnoreCase);

        if (changes.TryGetValue(nameof(Name), out var productName))
        {
            Name = new Optional<string>(productName);
        }

        if (changes.TryGetValue(nameof(Description), out var productDescription))
        {
            Description = new Optional<string?>(productDescription);
        }

        if (changes.TryGetValue(nameof(Price), out var sProductPrice) &&
            decimal.TryParse(sProductPrice, out var productPrice))
        {
            Price = new Optional<decimal>(productPrice);
        }
    }
}
