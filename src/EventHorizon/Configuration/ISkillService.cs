namespace EventHorizon.Configuration;

public interface ISkillService
{
    Task<SkillImportResponse> ImportAsync(ImportSkillRequest request, CancellationToken cancellationToken);
}

