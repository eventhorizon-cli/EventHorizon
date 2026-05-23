namespace EventHorizon.Pricing;

public interface IModelPriceCatalogService
{
    Task<ModelPriceCatalog> GetCatalogAsync(CancellationToken cancellationToken);

    Task<int> RefreshAsync(CancellationToken cancellationToken);
}

