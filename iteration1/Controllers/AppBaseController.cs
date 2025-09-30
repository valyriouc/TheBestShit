using System.Security.Claims;
using iteration1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iteration1.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppBaseController(ApplicationDbContext dbContext) : ControllerBase
{
    protected ApplicationDbContext _dbContext = dbContext;
    
    protected async Task<TopFiveUser> GetCurrentUserAsync()
    {
        List<Claim> claims = HttpContext.User.Claims.ToList();
        string sid = claims.Find(x => x.Type == ClaimTypes.Sid)?.Value ??
                     throw new InvalidOperationException("User ID claim not found.");

        if (!uint.TryParse(sid, out uint userId))
        {
            throw new InvalidOperationException("Invalid User ID claim.");
        }
        
        TopFiveUser? user = await _dbContext.Users.FindAsync(userId);
        return user ?? throw new InvalidOperationException("Current user not found in the database.");
    }
}