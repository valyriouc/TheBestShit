using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iteration1.Controllers;

public sealed class ResourceController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] string? category)
    {
        List<Resource> resources = await _dbContext.Resources
            .Where(x => x.Category.Name == category)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpGet("five")]
    public async Task<IActionResult> GetTopFiveAsync(
        [FromQuery] string category)
    {
        List<Resource> resources = await _dbContext.Resources
            .Where(x => x.Category.Name == category)
            .OrderBy(x => x.Score)
            .Take(5)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ResourceRequest request)
    {
        TopFiveUser user = await GetCurrentUserAsync();
        
        Category? category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Name == request.CategoryName);

        if (category is null)
        {
            return NotFound($"Category '{request.CategoryName}' not found.");
        }

        Resource? existing = await _dbContext.Resources.FirstOrDefaultAsync(
            x => x.Name == request.Name && x.Category == category);

        if (existing is not null)
        {
            return NotFound($"Resource with name '{existing.Name}' in category '{request.CategoryName}' already exists.");
        }
        
        Resource resource = new Resource
        {
            Name = request.Name,
            Url = request.Url,
            Category = category,
            Owner = user,
            UpVotes = 0,
            DownVotes = 0
        };
        
        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new AppResponseInfo<ResourceRequest>("Resource created successfully.", request));
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
        
        return Ok(new AppResponseInfo<IdResponse>("Resource deleted successfully.", new IdResponse(id)));
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
    string categoryName)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Id { get; } = id;
    
    [Required]
    [MinLength(3)]
    public string Name { get; } = name;
    
    [Url]
    public Uri Url { get; } = url;

    public string CategoryName { get; } = categoryName;
}