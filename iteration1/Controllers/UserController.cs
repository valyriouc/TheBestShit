using iteration1.Models;
using Microsoft.AspNetCore.Mvc;

namespace iteration1.Controllers;

public class UserController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserInfoAsync()
    {
        TopFiveUser user = await GetCurrentUserAsync();
        return Ok(user);
    }
}