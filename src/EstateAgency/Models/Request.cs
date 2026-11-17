using System;
using System.Collections.Generic;

namespace EstateAgency.Models;

public partial class Request
{
    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int PropertyId { get; set; }

    public int TypeId { get; set; }

    public int StatusId { get; set; }

    public decimal CommissionAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public virtual RequestStatus Status { get; set; } = null!;

    public virtual RequestType Type { get; set; } = null!;

    public virtual ICollection<Viewing> Viewings { get; set; } = new List<Viewing>();
}
