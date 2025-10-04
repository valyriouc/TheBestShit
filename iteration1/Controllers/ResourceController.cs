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
        List<Resource> resources = await _dbContext.Resources
            .Where(x => x.Section.Name == section)
            .ToListAsync();

        return Ok(resources);
    }

    [AllowAnonymous]
    [HttpGet("five")]
    public async Task<IActionResult> GetTopFiveAsync(
        [FromQuery] string section)
    {
        List<Resource> resources = await _dbContext.Resources
            .Where(x => x.Section.Name == section)
            .OrderBy(x => x.Score)
            .Take(5)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ResourceRequest request)
    {
        TopFiveUser user = await GetCurrentUserAsync();
        
        Section? section = await _dbContext.Sections
            .FirstOrDefaultAsync(c => c.Name == request.SectionName);

        if (section is null)
        {
            return NotFound($"Category '{request.SectionName}' not found.");
        }

        Resource? existing = await _dbContext.Resources.FirstOrDefaultAsync(
            x => x.Name == request.Name && x.Section == section);

        if (existing is not null)
        {
            return NotFound($"Resource with name '{existing.Name}' in category '{request.SectionName}' already exists.");
        }
        
        Resource resource = new Resource
        {
            Name = request.Name,
            Url = request.Url,
            Section = section,
            Owner = user,
            UpVotes = 0,
            DownVotes = 0
        };
        
        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new AppResponseInfo<ResourceRequest>(
            HttpStatusCode.OK,
            "Resource created successfully.", request));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] ResourceRequest request)
    {
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync([FromQuery] uint id)
    {
        TopFiveUser user = await GetCurrentUserAsync();
        Resource? resource = await _dbContext.Resources.FindAsync(id);

        if (resource is null)
        {
            return NotFound($"Resource with ID '{id}' does not exist.");
        }
        
        if (resource.Owner != user)
        {
            return Forbid("You do not have permission to delete this resource.");
        }
        
        _dbContext.Resources.Remove(resource);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new AppResponseInfo<IdResponse>(
            HttpStatusCode.OK,
            "Resource deleted successfully.", new IdResponse(id)));
    }
    
    private readonly struct IdResponse(uint id)
    {
        public uint Id { get; } = id;
    }
}

[method: JsonConstructor]
public readonly struct ResourceRequest(
    uint? id,
    string name,
    Uri url,
    string sectionName)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Id { get; } = id;
    
    [Required]
    [MinLength(3)]
    public string Name { get; } = name;
    
    [Url]
    public Uri Url { get; } = url;

    public string SectionName { get; } = sectionName;
}