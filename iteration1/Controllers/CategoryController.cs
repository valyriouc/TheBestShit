using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iteration1.Controllers;

// todo: make transfer objects to not disclose sensitive information

public sealed class CategoryController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [AllowAnonymous]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync()
    {
        List<Category> categories = await _dbContext.Categories
            .Include(c => c.Owner)
            .Include(c => c.Sections)
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCategoriesAsync()
    {
        var user = await GetCurrentUserAsync();
        var categories = await _dbContext.Categories
            .Include(c => c.Owner)
            .Include(c => c.Sections)
            .Where(x => x.Owner.Id == user.Id)
            .ToListAsync();

        return Ok(categories);
    }


    [HttpPost("create")]
    public async Task<IActionResult> CreateAsync([FromBody] CategoryRequest request)
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
        Category newCategory = new() { Name = request.Name, Description = request.Description, Owner = user};
        
        _dbContext.Categories.Add(newCategory);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new AppResponseInfo<Category>(
            HttpStatusCode.OK,
            $"{nameof(Category)} created successfully.", newCategory));
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

        Category? category = await _dbContext.Categories
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == request.Id.Value);
        if (category is null)
        {
            return NotFound($"Category with id {request.Id.Value} not found.");
        }

        if (category.Owner.Id != (await GetCurrentUserAsync()).Id)
        {
            return Forbid("You do not have permission to update this category.");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        await _dbContext.SaveChangesAsync();
        return Ok(new AppResponseInfo<Category>(
            HttpStatusCode.OK,
            "Category updated successfully.", category));
    }
}

[method: JsonConstructor]
public readonly struct CategoryRequest(uint? id, string name, string description)
{
    [Key] public uint? Id { get; } = id;
    
    public string Name { get; } = name;
    
    public string Description { get; } = description;
}