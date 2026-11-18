#nullable enable
using System;
using System.Collections.Generic;
namespace SolidOps.UM.Domain;
// Enum 
public enum InviteStatus
{
    Sent = 0,
Used = 1,
Declined = 2
}
// Object [AG]
public partial class LocalUser : AggregateRoot
{
}
public partial class User : AggregateRoot
{
}
public partial class Invite : AggregateRoot
{
}
public partial class Team : AggregateRoot
{
}
// Object [EN]
public partial class UserRight : Entity
{
}
public partial class Right : Entity
{
}
public partial class TeamMember : Entity
{
}
// Object [AG][EN]
public partial class LocalUser
{
    // Property [S][NO][NAR]
    public System.String Name { get; set; } = string.Empty;

    public System.String HashedPassword { get; set; } = string.Empty;

}
public partial class User
{
    // Property [S][NO][NAR]
    public System.String Email { get; set; } = string.Empty;

    public System.String Provider { get; set; } = string.Empty;

    public System.Boolean TechnicalUser { get; set; } = default(System.Boolean);

    // Property [M][NO][NAR][VO]
    public Preferences? Preferences { get; set; } = null!;

    // Property [M][LNA][AG][EN]
    public List<UserRight> UserRights { get; set; } = [];

}
public partial class UserRight
{

    // Property [M][NO][NAR][AG][EN]
    public Guid RightId { get; set; }
    public Right? Right { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

}
public partial class Right
{
    // Property [S][NO][NAR]
    public System.String Name { get; set; } = string.Empty;

}
public partial class Invite
{
    // Property [S][NO][NAR]
    public System.String Email { get; set; } = string.Empty;

    public System.String CreatorName { get; set; } = string.Empty;

    public System.String CreatorMessage { get; set; } = string.Empty;

    // Property [E][NO][NAR]
    public InviteStatus Status { get; set; }

    // Property [M][NO][NAR][AG][EN]
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }

    // Property [S][CA][NAR]
    public System.Boolean IsOpen => CalculateIsOpen();
    private partial System.Boolean CalculateIsOpen();

}
public partial class Team
{
    // Property [S][NO][NAR]
    public System.String Name { get; set; } = string.Empty;

    public String? Description { get; set; } = null;

    // Property [M][LNA][AG][EN]
    public List<TeamMember> Members { get; set; } = [];

}
public partial class TeamMember
{
    // Property [S][NO][NAR]
    public System.String UserEmail { get; set; } = string.Empty;

    public System.Boolean IsOwner { get; set; } = default(System.Boolean);

    // Property [M][NO][NAR][AG][EN]
    public Guid TeamId { get; set; }
    public Team? Team { get; set; }

}
// Object [VO]
public sealed partial record Preferences
{
    // Property [S][NO][NAR]
    public System.String Language { get; init; } = string.Empty;

    public System.String TimeZone { get; init; } = string.Empty;

}