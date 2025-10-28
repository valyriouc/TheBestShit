using System.Linq.Expressions;
using System.Net;
using System.Text.Json.Serialization;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace iteration1.Controllers;

public sealed class VoteController(ApplicationDbContext dbContext) 
    : AppBaseController(dbContext)
{
    [HttpGet]
    public async Task<IActionResult> GetVoteAsync([FromQuery] uint resourceId)
    {
        TopFiveUser user = await GetCurrentUserAsync(); 

        Vote? vote = await _dbContext.Votes.FirstOrDefaultAsync(
            x => x.Resource.Id == resourceId && x.Owner.Id == user.Id);

        if (vote is null)
        {
            return NotFound("Vote not found for this user and resource.");
        }

        return Ok(vote);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateVoteAsync([FromBody] VoteRequest request)
    {
        TopFiveUser user = await GetCurrentUserAsync();
        
        using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            Resource? resource = await _dbContext.Resources.FindAsync(request.ResourceId);

            if (resource is null)
            {
                return NotFound($"The resource with id {request.ResourceId} was not found.");
            }

            Vote? existingVote = await _dbContext.Votes
                .FirstOrDefaultAsync(v =>
                    v.Resource.Id == request.ResourceId &&
                    v.Owner.Id == user.Id);

            if (existingVote is not null)
            {
                return Conflict("User has already voted on this resource.");
            }

            var vote = new Vote
            {
                Resource = resource,
                Owner = user,
                Direction = request.Direction
            };

            if (vote.Direction)
            {
                resource.UpVotes += 1;
            }
            else
            {
                resource.DownVotes += 1;
            }

            _dbContext.Votes.Add(vote);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new AppResponseInfo<VoteRequest>(
                HttpStatusCode.OK,
                "Vote recorded successfully.", request));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateVoteAsync([FromBody] VoteRequest request)
    {
        TopFiveUser user = await GetCurrentUserAsync();

        Vote? existingVote = await _dbContext.Votes
            .Include(v => v.Resource)
            .FirstOrDefaultAsync(v =>
                v.Resource.Id == request.ResourceId &&
                v.Owner.Id == user.Id);

        if (existingVote is null)
        {
            return NotFound($"Vote not found for resource with id {request.ResourceId}.");
        }

        // Only update if direction actually changes
        if (existingVote.Direction != request.Direction)
        {
            switch (existingVote.Direction)
            {
                case true when !request.Direction:
                {
                    // Changing from upvote to downvote
                    if (existingVote.Resource.UpVotes > 0)
                    {
                        existingVote.Resource.UpVotes -= 1;
                    }
                
                    existingVote.Resource.DownVotes += 1;
                    break;
                }
                case false when request.Direction:
                {
                    // Changing from downvote to upvote
                    if (existingVote.Resource.DownVotes > 0)
                    {
                        existingVote.Resource.DownVotes -= 1;
                    }
                
                    existingVote.Resource.UpVotes += 1;
                    break;
                }
            }

            existingVote.Direction = request.Direction;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new AppResponseInfo<VoteRequest>(
            HttpStatusCode.OK,
            "Vote updated successfully.", request));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteVoteAsync([FromQuery] uint resourceId)
    {
        TopFiveUser user = await GetCurrentUserAsync();
        Vote? existingVote = await _dbContext.Votes
            .Include(v => v.Resource)
            .FirstOrDefaultAsync(v =>
                v.Resource.Id == resourceId &&
                v.Owner.Id == user.Id);

        if (existingVote is null)
        {
            return NotFound("Vote not found for this resource.");
        }

        if (existingVote.Direction == true)
        {
            if (existingVote.Resource.UpVotes > 0)
                existingVote.Resource.UpVotes -= 1;
        }
        else
        {
            if (existingVote.Resource.DownVotes > 0)
                existingVote.Resource.DownVotes -= 1;
        }

        _dbContext.Votes.Remove(existingVote);
        await _dbContext.SaveChangesAsync();

        return Ok(new AppResponseInfo<string>(
            HttpStatusCode.OK,
            "Vote deleted successfully.", $"Vote for resource id {resourceId} deleted."));
    }
}

[method: JsonConstructor]
public readonly struct VoteRequest(
    uint resourceId,
    bool direction)
{
    public uint ResourceId { get; } = resourceId;

    public bool Direction { get; } = direction;
}