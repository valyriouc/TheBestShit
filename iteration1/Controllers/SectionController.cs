using System.ComponentModel.DataAnnotations;
using System.Net;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iteration1.Controllers;

public class SectionController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync()
    {
        List<SectionResponse> sections = await dbContext.Sections
            .Include(s => s.Owner)
            .Include(s => s.Category)
            .Include(s => s.Resources)
            .Select(x => new SectionResponse(x))
            .ToListAsync();
        return Ok(sections);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAsync()
    {
        TopFiveUser user = await GetCurrentUserAsync();
        var sections = await _dbContext.Sections
            .Include(s => s.Owner)
            .Include(s => s.Category)
            .Include(s => s.Resources)
            .Where(x => x.Owner == user)
            .Select(x => new SectionResponse(x))
            .ToListAsync();
        return Ok(sections);
    }

    [HttpPost("{sectionId}/add/{resourceId}")]
    public async Task<IActionResult> AddAsync(uint sectionId, uint resourceId)
    {
        TopFiveUser user = await GetCurrentUserAsync();

        Section? section = await _dbContext.Sections
            .Include(s => s.Owner)
            .Include(s => s.Resources)
            .FirstOrDefaultAsync(x => x.Id == sectionId);

        if (section == null)
        {
            return NotFound(new AppResponseInfo<string>(
                HttpStatusCode.NotFound,
                "Section not found"));
        }

        Resource? resource = await _dbContext.Resources
            .Include(r => r.Section)
            .FirstOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null)
        {
            return NotFound(new AppResponseInfo<string>(
                HttpStatusCode.NotFound,
                "Resource not found"));
        }

        if (!section.PublicEdit && section.Owner.Id != user.Id)
        {
            return Forbid();
        }

        // Check if resource already belongs to a section
        if (resource.Section != null)
        {
            return Conflict(new AppResponseInfo<string>(
                HttpStatusCode.Conflict,
                $"Resource already belongs to section '{resource.Section.Name}'"));
        }

        section.Resources.Add(resource);
        await _dbContext.SaveChangesAsync();
        return Ok(new AppResponseInfo<string>(
            HttpStatusCode.OK,
            "Resource added successfully"));
    }
    
    [HttpPost("create/{categoryId}")]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] uint categoryId,
        [FromBody] SectionRequest section)
    {
        if (string.IsNullOrWhiteSpace(section.Name))
        {
            var response = new AppResponseInfo<string>(
                HttpStatusCode.BadRequest,
                "Section name cannot be empty");
            return BadRequest(response);
        }
        
        Category? category = await _dbContext.Categories
            .Include(category => category.Sections)
            .Include(category => category.Owner)
            .FirstOrDefaultAsync(x => x.Id == categoryId);
        
        if (category == null)
        {
            AppResponseInfo<string> response = new AppResponseInfo<string>(
                HttpStatusCode.NotFound,
                "Category not found");
            return NotFound(response);
        }
        
        bool exists = category.Sections.Any(x =>  x.Name == section.Name);
        if (exists)
        {
            return Conflict(
                "Section with the same name already exists");
        }
        
        TopFiveUser user =  await GetCurrentUserAsync();
        if (!category.PublicEdit && category.Owner != user)
        {
            return Forbid();
        }
        
        Section newSection = new()
        {
            Name = section.Name,
            Description = section.Description,
            Owner = user,
            Category = category,
            PublicEdit = section.PublicEdit,
        };

        category.Sections.Add(newSection);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new AppResponseInfo<SectionRequest>(
            HttpStatusCode.OK,
            "Section created successfully",
            section));
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditAsync([FromBody] Section section)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAsync(uint id)
    {
        throw new NotImplementedException();
    }
}

public readonly struct SectionRequest(
    string name,
    string description,
    bool publicEdit=false,
    uint? id=null)
{
    [Key] public uint? Id { get; } = id;

    [Required]
    [MinLength(3)]
    public string Name { get; } = name;
    
    public bool PublicEdit { get; } = publicEdit;

    public string Description { get; } = description;
}

public readonly struct SectionResponse(
    Section section)
{
    public uint Id { get; } = section.Id;
    
    public string Name { get; } = section.Name;
    
    public string Description { get; }  = section.Description;

    public bool PublicEdit { get; } = section.PublicEdit;

    public UserResponse Owner { get; } = new UserResponse(section.Owner);

}