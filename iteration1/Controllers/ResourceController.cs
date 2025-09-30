using Microsoft.AspNetCore.Mvc;

namespace iteration1.Controllers;

public sealed class ResourceController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] string? category)
    {
        
    }

    [HttpGet("five")]
    public async Task<IActionResult> GetTopFiveAsync(
        [FromQuery] string category)
    {
        
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync()
    {
        
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAsync()
    {
        
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync()
    {
        
    }
}