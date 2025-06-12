using System;
using System.Collections.Generic;

namespace API_FORMAT.Models;

public partial class Topic
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? IconUrl { get; set; }

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
