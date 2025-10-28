using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iteration1.Controllers;

public sealed class ResourceController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] string? section)
    {
        var resources = await _dbContext.Resources
            .Include(x => x.Section)
            .Include(x => x.Owner)
            .Where(x => x.Section.Name == section)
            .Select(x => new ResourceResponse(x))
            .ToListAsync();

        return Ok(resources);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAsync()
    {
        TopFiveUser user = await GetCurrentUserAsync();
        List<ResourceResponse> resources = await _dbContext.Resources
            .Include(x => x.Section)
            .Include(x => x.Owner)
            .Where(x => x.Owner == user)
            .Select(x => new ResourceResponse(x))
            .ToListAsync();
        return Ok(resources);
    }

    [AllowAnonymous]
    [HttpGet("five")]
    public async Task<IActionResult> GetTopFiveAsync(
        [FromQuery] string section)
    {
        // Fetch resources and materialize them first
        List<Resource> allResources = await _dbContext.Resources
            .Include(x => x.Section)
            .Include(x => x.Owner)
            .Where(x => x.Section.Name == section)
            .ToListAsync();

        // Order by Score in memory (since Score is a computed property)
        return Ok(allResources
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => new ResourceResponse(x)));
    }

    [HttpPost("create/{sectionId}")]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] uint sectionId,
        [FromBody] ResourceRequest request)
    {
        TopFiveUser user = await GetCurrentUserAsync();

        Section? section = await _dbContext.Sections
            .Include(s => s.Resources)
            .FirstOrDefaultAsync(c => c.Id == sectionId);

        if (section is null)
        {
            return NotFound($"Section '{sectionId}' not found.");
        }

        // Check for duplicate resource more efficiently
        bool exists = await _dbContext.Resources
            .AnyAsync(x => x.Name == request.Name && x.Section.Id == sectionId);

        if (exists)
        {
            AppResponseInfo<string> response = new AppResponseInfo<string>(
                HttpStatusCode.Conflict,
                $"Resource with name '{request.Name}' in section '{sectionId}' already exists.");

            return Conflict(response);
        }

        Resource resource = new Resource
        {
            Name = request.Name,
            Url = request.Url,
            Section = section,
            Owner = user,
            UpVotes = 0,
            DownVotes = 0,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync();

        return Ok(new AppResponseInfo<ResourceRequest>(
            HttpStatusCode.OK,
            "Resource created successfully.", request));
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateAsync([FromBody] ResourceRequest request)
    {
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync([FromQuery] uint id)
    {
        TopFiveUser user = await GetCurrentUserAsync();
        Resource? resource = await _dbContext.Resources
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null)
        {
            return NotFound($"Resource with ID '{id}' does not exist.");
        }

        if (resource.Owner.Id != user.Id)
        {
            return Forbid("You do not have permission to delete this resource.");
        }

        _dbContext.Resources.Remove(resource);
        await _dbContext.SaveChangesAsync();

        return Ok(new AppResponseInfo<IdResponse>(
            HttpStatusCode.OK,
            "Resource deleted successfully.", new IdResponse(id)));
    }
}

public readonly struct IdResponse(uint id)
{
    public uint Id { get; } = id;
}

[method: JsonConstructor]
public readonly struct ResourceRequest(
    uint? id,
    string name,
    Uri url)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Id { get; } = id;
    
    [Required]
    [MinLength(3)]
    public string Name { get; } = name;
    
    [Url]
    public Uri Url { get; } = url;
}

public readonly struct ResourceResponse(
    Resource resource)
{
    public uint Id { get; } = resource.Id;
    
    public string Name { get; } = resource.Name;
    
    public Uri Url { get; } = resource.Url; 
    
    public DateTime CreatedAt { get; } = resource.CreatedAt;

    public ulong UpVotes { get; } = resource.UpVotes;
    
    public ulong DownVotes { get; } = resource.DownVotes;

    public ulong TotalVotes { get; }  = resource.TotalVotes;

    public double Score { get; }  = resource.Score;

    public UserResponse? Owner { get; } = new(resource.Owner);
}