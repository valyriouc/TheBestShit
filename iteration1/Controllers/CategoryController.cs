using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iteration1.Controllers;

public sealed class CategoryController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [AllowAnonymous]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync()
    {
        List<Category> categories = await _dbContext.Categories.ToListAsync();
        return Ok(categories);
    }

    [HttpPost("create")]
    public async Task<IActionResult> AddAsync([FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Category name cannot be empty.");
        }

        bool exists = await _dbContext.Categories.AnyAsync(c => c.Name == request.Name);
        
        if (exists)
        {
            return Conflict("Category with the same name already exists.");
        }
        
        var user = await GetCurrentUserAsync();
        Category newCategory = new() { Name = request.Name, Description = request.Description, Owner = user };
        
        _dbContext.Categories.Add(newCategory);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new AppResponseInfo<Category>("Category created successfully.", newCategory));
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateAsync([FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Category name cannot be empty.");
        }

        if (request.Id is null)
        {
            return BadRequest("Category id is required for update.");
        }
        
        Category? category = await _dbContext.Categories.FindAsync(request.Id.Value);
        if (category is null)
        {
            return NotFound($"Category with id {request.Id.Value} not found.");
        }
        
        if (category.Owner.Id != (await GetCurrentUserAsync()).Id)
        {
            return Forbid("You do not have permission to update this category.");
        }

        category.Name = request.Name;
        await _dbContext.SaveChangesAsync();
        return Ok(new AppResponseInfo<Category>("Category updated successfully.", category));
    }
}

[method: JsonConstructor]
public readonly struct CategoryRequest(uint? id, string name, string description)
{
    [Key] public uint? Id { get; } = id;
    
    public string Name { get; } = name;
    
    public string Description { get; } = description;
}