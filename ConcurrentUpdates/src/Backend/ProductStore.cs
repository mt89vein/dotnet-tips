namespace ConcurrentUpdates;

public sealed class ProductStore
{
    private readonly Dictionary<int, Product> _products = new();
    private int _counter = 1;

    public ProductStore()
    {
        // seed
        _products[1] = new Product(1, "test", null, 23.42m);
    }

    public Product? GetById(int productId)
    {
        return _products.GetValueOrDefault(productId);
    }

    public Product Save(string name, string? description, decimal price)
    {
        var product = new Product(Interlocked.Increment(ref _counter), name, description, price);

        return _products[product.Id] = product;
    }
}
