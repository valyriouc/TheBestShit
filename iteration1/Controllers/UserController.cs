using iteration1.Models;
using iteration1.services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace iteration1.Controllers;

public class UserController(ApplicationDbContext dbContext) : AppBaseController(dbContext)
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserInfoAsync()
    {
        TopFiveUser user = await HttpContext.GetCurrentUser(_dbContext);
        return Ok(new UserResponse(user));
    }
}

public readonly struct UserResponse(TopFiveUser user)
{
    public string Id { get; } = user.Id;

    public string Email { get; } = user.Email ?? string.Empty;

    public string UserName { get; } = user.UserName ?? string.Empty;

    public bool TwoFactorEnabled { get; } = user.TwoFactorEnabled;
}