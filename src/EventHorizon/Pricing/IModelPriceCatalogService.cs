namespace EventHorizon.Pricing;

public interface IModelPriceCatalogService
{
    Task<Pricing.ModelPriceCatalog> GetCatalogAsync(CancellationToken cancellationToken);

    Task<int> RefreshAsync(CancellationToken cancellationToken);
}

