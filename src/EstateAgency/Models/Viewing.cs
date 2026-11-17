using System;
using System.Collections.Generic;

namespace EstateAgency.Models;

public partial class Viewing
{
    public int ViewingId { get; set; }

    public int RequestId { get; set; }

    public int ManagerId { get; set; }

    public DateTime ScheduledAt { get; set; }

    public DateTime? ActualStartAt { get; set; }

    public DateTime? ActualEndAt { get; set; }

    public string? Result { get; set; }

    public string? Feedback { get; set; }

    public virtual User Manager { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
