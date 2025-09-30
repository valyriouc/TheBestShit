using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace iteration1.Models;

public sealed class Resource
{
    [Key]
    public uint Id { get; set; }

    public string Name { get; set; } = null!;

    [Url] public Uri Url { get; set; } = null!;
    
    public ulong UpVotes { get; set; }
    
    public ulong DownVotes { get; set; }
    
    public Category Category { get; set; } = null!;
    
    public TopFiveUser Owner { get; set; } = null!;
}

public sealed class Vote 
{
    [Key] public uint Id { get; set; }

    public TopFiveUser Owner { get; set; } = null!;
    
    public Resource Resource { get; set; } = null!;
    
    // true = upvote, false = downvote
    public bool Direction { get; set; }
}

public sealed class Category
{
    [Key] 
    public uint Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public TopFiveUser Owner { get; set; } = null!;
}

public sealed class TopFiveUser : IdentityUser<uint>
{
    public ulong Trust { get; set; } = 0;
}

public sealed class TopFiveRole : IdentityRole<uint>
{
    
}