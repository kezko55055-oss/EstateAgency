using System;
using System.Collections.Generic;

namespace EstateAgency.Models;

public partial class RequestType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public decimal CommissionRate { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
