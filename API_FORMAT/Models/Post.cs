using System;
using System.Collections.Generic;

namespace API_FORMAT.Models;

public partial class Post
{
    public int Id { get; set; }

    public string Content { get; set; } = null!;

    public string? MediaUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CommunityId { get; set; }

    public int? TopicId { get; set; }

    public string? ShareUrl { get; set; }

    public int? AuthorId { get; set; }

    public virtual User? Author { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Community? Community { get; set; }

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual Topic? Topic { get; set; }
}
