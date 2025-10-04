using System.Net;
using iteration1;
using iteration1.Controllers;
using iteration1.Models;
using iteration1.Response;
using iteration1.services;
using Microsoft.EntityFrameworkCore;

public interface ICategoryService : IService<Category, CategoryRequest>;

public class CategoryService(
    HttpContext httpContext,
    ApplicationDbContext dbContext) : ICategoryService
{
    public IAsyncEnumerable<Category> GetAsync() => 
        dbContext.Categories.AsAsyncEnumerable();

    public async Task<AppResponseInfo<Category>> UpdateAsync(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new AppResponseInfo<Category>(
                HttpStatusCode.BadRequest,
                "Category name cannot be empty."); 
        }

        bool exists = await dbContext.Categories.AnyAsync(c => c.Name == request.Name);
        
        if (exists)
        {
            return new AppResponseInfo<Category>(
                HttpStatusCode.Conflict,
                "Category with the same name already exists.");
        }

        var user = await httpContext.GetCurrentUser(dbContext);
        Category newCategory = new() { Name = request.Name, Description = request.Description, Owner = user };
        
        dbContext.Categories.Add(newCategory);
        await dbContext.SaveChangesAsync();
        
        return new AppResponseInfo<Category>(
            HttpStatusCode.OK,
            "Category created successfully.",
            newCategory);

    }

    public async Task<AppResponseInfo<Category>> AddAsync(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new AppResponseInfo<Category>(
                HttpStatusCode.BadRequest,
                "Category name cannot be empty.");
        }

        bool exists = await dbContext.Categories.AnyAsync(c => c.Name == request.Name);
        
        if (exists)
        {
            return new AppResponseInfo<Category>(
                HttpStatusCode.Conflict,
                "Category with the same name already exists.");
        }

        var user = await httpContext.GetCurrentUser(dbContext);
        Category newCategory = new() { Name = request.Name, Description = request.Description, Owner = user };
        
        dbContext.Categories.Add(newCategory);
        await dbContext.SaveChangesAsync();
        
        return new AppResponseInfo<Category>(
            HttpStatusCode.OK,
            "Category created successfully.",
            newCategory);
    }

    public Task<AppResponseInfo<Category>> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}