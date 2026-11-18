using SolidOps.UM.Domain;

namespace SolidOps.UM.Application;

public partial class TeamService
{
    private async partial Task<Error?> CheckMembersAsync(Team aggregate, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (aggregate.Members.Count > 0 && aggregate.Members.Count(m => m.IsOwner) != 1)
        {
            errors[nameof(Team.Members)] = ["A team with members has exactly one owner"];
        }
        foreach (var member in aggregate.Members)
        {
            if (await UserRepository.FindByEmailAsync(member.UserEmail, cancellationToken) is null)
            {
                errors[nameof(Team.Members)] = [$"No user has the email {member.UserEmail}"];
            }
        }
        return errors.Count > 0 ? Error.Invalid(errors) : null;
    }

    private partial Task<Error?> CheckNoMemberAsync(Team aggregate, CancellationToken cancellationToken) =>
        Task.FromResult(aggregate.Members.Count > 0 ? Error.Conflict("The team still has members") : null);
}
