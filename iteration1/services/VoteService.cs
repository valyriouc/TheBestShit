using iteration1.Controllers;
using iteration1.Models;
using iteration1.Response;
using Microsoft.EntityFrameworkCore;

namespace iteration1.services;

public interface IVoteService : IService<Vote, VoteRequest>
{

}

public class VoteService(
    HttpContext httpContext,
    ApplicationDbContext dbContext) : IVoteService
{
    public IAsyncEnumerable<Vote> GetAsync()
    {
        throw new NotImplementedException();
    }

    public Task<AppResponseInfo<Vote>> UpdateAsync(VoteRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<AppResponseInfo<Vote>> AddAsync(VoteRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<AppResponseInfo<Vote>> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}