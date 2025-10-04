using System.Security.Claims;
using iteration1.Models;
using iteration1.Response;
using Microsoft.EntityFrameworkCore;

namespace iteration1.services;

public interface IService<T, TReq>
{
    public IAsyncEnumerable<T> GetAsync();
    
    public Task<AppResponseInfo<T>> UpdateAsync(TReq request);
    
    public Task<AppResponseInfo<T>> AddAsync(TReq request);
    
    public Task<AppResponseInfo<T>> DeleteAsync(int id);
}

public static class Extensions
{
    public static async Task<TopFiveUser> GetCurrentUser(this HttpContext httpContext, ApplicationDbContext dbContext)
    {
        List<Claim> claims = httpContext.User.Claims.ToList();
        string email = claims.Find(x => x.Type == ClaimTypes.Email)?.Value ??
                       throw new InvalidOperationException("User ID claim not found.");
        
        TopFiveUser? user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Email == email);
        return user ?? throw new InvalidOperationException("Current user not found in the database.");
    }
}