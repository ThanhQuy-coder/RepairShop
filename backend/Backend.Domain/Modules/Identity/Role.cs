namespace Backend.Domain.Modules.Identity;

public class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private Role() { } // for EF Core

    public Role(int id, string name, string? description = null)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}