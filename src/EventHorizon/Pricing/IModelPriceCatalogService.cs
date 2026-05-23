namespace EventHorizon.Pricing;

public interface IModelPriceCatalogService
{
    bool TryGetCatalog(out ModelPriceCatalog? catalog);
    Task RefreshIfNeededAsync(CancellationToken cancellationToken);
}

