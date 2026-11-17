using System;
using System.Collections.Generic;

namespace EstateAgency.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Viewing> Viewings { get; set; } = new List<Viewing>();
}
