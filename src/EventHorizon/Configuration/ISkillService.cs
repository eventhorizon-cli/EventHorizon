using EventHorizon.AGUI.DTOs;

namespace EventHorizon.Configuration;

public interface ISkillService
{
    Task<SkillImportResponseDTO> ImportAsync(ImportSkillRequestDTO request, CancellationToken cancellationToken);

    Task<SkillRemoveResponseDTO> RemoveGlobalAsync(string skillName, CancellationToken cancellationToken);

    Task<SkillRemoveResponseDTO> RemoveSessionAsync(string sessionId, string skillName, CancellationToken cancellationToken);
}
