namespace BitirmeProject.IdentityService.Domain.Common;

public interface IEntity
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
