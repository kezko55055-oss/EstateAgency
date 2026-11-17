using System;
using System.Collections.Generic;

namespace EstateAgency.Models;

public partial class PropertyStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
