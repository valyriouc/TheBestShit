using System.Security.Claims;
using iteration1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iteration1.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppBaseController(
    ApplicationDbContext dbContext) : ControllerBase
{
    protected ApplicationDbContext _dbContext = dbContext;
    
    protected async Task<TopFiveUser> GetCurrentUserAsync()
    {
        List<Claim> claims = HttpContext.User.Claims.ToList();
        string email = claims.Find(x => x.Type == ClaimTypes.Email)?.Value ??
                     throw new InvalidOperationException("User ID claim not found.");
        
        TopFiveUser? user = await _dbContext.Users.FirstOrDefaultAsync(
            x => x.Email == email);
        return user ?? throw new InvalidOperationException("Current user not found in the database.");
    }
}