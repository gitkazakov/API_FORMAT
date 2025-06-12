using System;
using System.Collections.Generic;

namespace API_FORMAT.Models;

public partial class Community
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? PublicationCount { get; set; }

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
