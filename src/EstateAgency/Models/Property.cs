using System;
using System.Collections.Generic;

namespace EstateAgency.Models;

public partial class Property
{
    public int PropertyId { get; set; }

    public string PropertyCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int TypeId { get; set; }

    public int StatusId { get; set; }

    public int ManagerId { get; set; }

    public decimal Price { get; set; }

    public decimal Area { get; set; }

    public int Rooms { get; set; }

    public int FreeViewings { get; set; }

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Manager { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual PropertyStatus Status { get; set; } = null!;

    public virtual PropertyType Type { get; set; } = null!;
}
