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

    public ulong TotalVotes => UpVotes + DownVotes;
    
    public double Score
    {
        get
        {
            if (TotalVotes < 10)
            {
                return 0.0;
            }        
            
            return ConfidenceRankingAlgorithm.Confidence(UpVotes, DownVotes);
        }
    }

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
    
    public string Description { get; set; } = null!;
    
    public List<Resource> Resources { get; set; } = new();
    
    public TopFiveUser Owner { get; set; } = null!;
}

public sealed class TopFiveUser : IdentityUser
{
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