using System.ComponentModel.DataAnnotations;
using iteration1.voting;
using Microsoft.AspNetCore.Identity;

namespace iteration1.Models;

public sealed class Resource
{
    [Key]
    public uint Id { get; set; }

    [Required]
    [MinLength(3)]
    public string Name { get; set; } = null!;

    [Url] public Uri Url { get; set; } = null!;
    
    public ulong UpVotes { get; set; }

    public ulong DownVotes { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public ulong TotalVotes => UpVotes + DownVotes;
    
    public double Score
    {
        get
        {
            if (TotalVotes < 20)
            {
                return 0.0;
            }        
            
            return ConfidenceRankingAlgorithm.Confidence(UpVotes, DownVotes);
        }
    }
    
    public Section Section { get; set; } = null!;
    
    public TopFiveUser Owner { get; set; } = null!;
}

public sealed class Vote 
{
    [Key] public uint Id { get; set; }

    public TopFiveUser Owner { get; set; } = null!;
    
    public Resource Resource { get; set; } = null!;
    
    public bool Direction { get; set; }
}

public sealed class Category
{
    [Key] 
    public uint Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Description { get; set; } = null!;

    public bool PublicEdit { get; set; } = false;
    
    public List<Section> Sections { get; set; } = new();
    
    public TopFiveUser Owner { get; set; } = null!;
}

// ask: what if another user wants to add a section or resource to a category or section of someone else 


public sealed class Section
{
    [Key] public uint Id { get; set; }

    public string Name { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public List<Resource> Resources { get; set; } = new();
    
    public bool PublicEdit { get; set; } = false;

    public Category Category { get; set; } = null!;

    public TopFiveUser Owner { get; set; } = null!;
}

public sealed class TopFiveUser : IdentityUser
{
    // todo: how to calculate the trust score?
    // based on experience, contributions, post upvotes received, 
    public ulong Trust { get; set; } = 0;
}

public sealed class TopFiveEmailSender : IEmailSender<TopFiveUser>
{
    public async Task SendConfirmationLinkAsync(TopFiveUser user, string email, string confirmationLink)
    {
    }

    public async Task SendPasswordResetLinkAsync(TopFiveUser user, string email, string resetLink)
    {
    }

    public async Task SendPasswordResetCodeAsync(TopFiveUser user, string email, string resetCode)
    {
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
    }
}