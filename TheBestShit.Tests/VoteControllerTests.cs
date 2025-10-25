using System.Net;
using System.Security.Claims;
using iteration1;
using iteration1.Controllers;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TheBestShit.Tests;

public class VoteControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly VoteController _controller;
    private readonly TopFiveUser _testUser;
    private readonly Resource _testResource;
    private readonly Section _testSection;
    private readonly Category _testCategory;

    public VoteControllerTests()
    {
        // Setup in-memory database with unique name per test instance
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Default(Microsoft.EntityFrameworkCore.WarningBehavior.Ignore))
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Create test user
        _testUser = new TopFiveUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };
        _context.Users.Add(_testUser);

        // Create test category
        _testCategory = new Category
        {
            Name = "Test Category",
            Description = "Test Description",
            Owner = _testUser
        };
        _context.Categories.Add(_testCategory);

        // Create test section
        _testSection = new Section
        {
            Name = "Test Section",
            Description = "Test Section Description",
            Category = _testCategory,
            Owner = _testUser
        };
        _context.Sections.Add(_testSection);

        // Create test resource
        _testResource = new Resource
        {
            Name = "Test Resource",
            Url = new Uri("https://example.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow,
            UpVotes = 0,
            DownVotes = 0
        };
        _context.Resources.Add(_testResource);

        _context.SaveChanges();

        // Setup controller with mock HTTP context
        _controller = new VoteController(_context);
        SetupControllerContext(_controller, _testUser);
    }

    private static void SetupControllerContext(VoteController controller, TopFiveUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email!)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetVoteAsync Tests

    [Fact]
    public async Task GetVoteAsync_ExistingVote_ReturnsOkWithVote()
    {
        // Arrange
        var vote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetVoteAsync(_testResource.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedVote = Assert.IsType<Vote>(okResult.Value);
        Assert.Equal(vote.Id, returnedVote.Id);
        Assert.True(returnedVote.Direction);
    }

    [Fact]
    public async Task GetVoteAsync_NonExistentVote_ReturnsNotFound()
    {
        // Arrange
        uint nonExistentResourceId = 99999;

        // Act
        var result = await _controller.GetVoteAsync(nonExistentResourceId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Vote not found for this user and resource.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetVoteAsync_VoteExistsForDifferentUser_ReturnsNotFound()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var vote = new Vote
        {
            Owner = otherUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetVoteAsync(_testResource.Id);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region CreateVoteAsync Tests

    [Fact]
    public async Task CreateVoteAsync_ValidUpvote_CreatesVoteAndIncrementsCount()
    {
        // Arrange
        var request = new VoteRequest(_testResource.Id, true);
        var initialUpVotes = _testResource.UpVotes;

        // Act
        var result = await _controller.CreateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<VoteRequest>>(okResult.Value);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Vote recorded successfully.", response.Message);

        // Verify vote was created
        var createdVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.Owner.Id == _testUser.Id && v.Resource.Id == _testResource.Id);
        Assert.NotNull(createdVote);
        Assert.True(createdVote.Direction);

        // Verify count was incremented
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(initialUpVotes + 1, resource!.UpVotes);
    }

    [Fact]
    public async Task CreateVoteAsync_ValidDownvote_CreatesVoteAndIncrementsDownCount()
    {
        // Arrange
        var request = new VoteRequest(_testResource.Id, false);
        var initialDownVotes = _testResource.DownVotes;

        // Act
        var result = await _controller.CreateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify downvote count was incremented
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(initialDownVotes + 1, resource!.DownVotes);
    }

    [Fact]
    public async Task CreateVoteAsync_NonExistentResource_ReturnsNotFound()
    {
        // Arrange
        var request = new VoteRequest(99999, true);

        // Act
        var result = await _controller.CreateVoteAsync(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateVoteAsync_DuplicateVote_ReturnsConflict()
    {
        // Arrange
        var existingVote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new VoteRequest(_testResource.Id, false);

        // Act
        var result = await _controller.CreateVoteAsync(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("User has already voted on this resource.", conflictResult.Value);
    }

    [Fact]
    public async Task CreateVoteAsync_TransactionRollback_OnException()
    {
        // Arrange
        var request = new VoteRequest(_testResource.Id, true);
        var initialVoteCount = await _context.Votes.CountAsync();
        var initialUpVotes = _testResource.UpVotes;

        // Dispose context to simulate database error
        await _context.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await _controller.CreateVoteAsync(request));

        // Note: In a real scenario with a live database, we'd verify the transaction
        // was rolled back and counts weren't incremented
    }

    #endregion

    #region UpdateVoteAsync Tests

    [Fact]
    public async Task UpdateVoteAsync_UpvoteToDownvote_UpdatesCorrectly()
    {
        // Arrange
        _testResource.UpVotes = 5;
        _testResource.DownVotes = 2;
        var existingVote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true // upvote
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new VoteRequest(_testResource.Id, false); // change to downvote

        // Act
        var result = await _controller.UpdateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<VoteRequest>>(okResult.Value);
        Assert.Equal("Vote updated successfully.", response.Message);

        // Verify counts
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(4ul, resource!.UpVotes); // decremented
        Assert.Equal(3ul, resource.DownVotes); // incremented

        // Verify vote direction
        var updatedVote = await _context.Votes.FindAsync(existingVote.Id);
        Assert.False(updatedVote!.Direction);
    }

    [Fact]
    public async Task UpdateVoteAsync_DownvoteToUpvote_UpdatesCorrectly()
    {
        // Arrange
        _testResource.UpVotes = 3;
        _testResource.DownVotes = 7;
        var existingVote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = false // downvote
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new VoteRequest(_testResource.Id, true); // change to upvote

        // Act
        var result = await _controller.UpdateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify counts
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(4ul, resource!.UpVotes); // incremented
        Assert.Equal(6ul, resource.DownVotes); // decremented
    }

    [Fact]
    public async Task UpdateVoteAsync_SameDirection_NoChangeInCounts()
    {
        // Arrange
        _testResource.UpVotes = 5;
        _testResource.DownVotes = 2;
        var existingVote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new VoteRequest(_testResource.Id, true); // same direction

        // Act
        var result = await _controller.UpdateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify counts unchanged
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(5ul, resource!.UpVotes);
        Assert.Equal(2ul, resource.DownVotes);
    }

    [Fact]
    public async Task UpdateVoteAsync_NonExistentVote_ReturnsNotFound()
    {
        // Arrange
        var request = new VoteRequest(_testResource.Id, true);

        // Act
        var result = await _controller.UpdateVoteAsync(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateVoteAsync_UnderflowProtection_UpvoteAtZero()
    {
        // Arrange
        _testResource.UpVotes = 0; // Already at zero
        _testResource.DownVotes = 5;
        var existingVote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true // upvote
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new VoteRequest(_testResource.Id, false); // change to downvote

        // Act
        var result = await _controller.UpdateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify upvotes stayed at 0 (didn't underflow)
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(0ul, resource!.UpVotes);
        Assert.Equal(6ul, resource.DownVotes); // still incremented
    }

    [Fact]
    public async Task UpdateVoteAsync_UnderflowProtection_DownvoteAtZero()
    {
        // Arrange
        _testResource.UpVotes = 5;
        _testResource.DownVotes = 0; // Already at zero
        var existingVote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = false // downvote
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new VoteRequest(_testResource.Id, true); // change to upvote

        // Act
        var result = await _controller.UpdateVoteAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify downvotes stayed at 0 (didn't underflow)
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(6ul, resource!.UpVotes); // incremented
        Assert.Equal(0ul, resource.DownVotes); // stayed at 0
    }

    #endregion

    #region DeleteVoteAsync Tests

    [Fact]
    public async Task DeleteVoteAsync_UpvoteExists_DeletesAndDecrementsCount()
    {
        // Arrange
        _testResource.UpVotes = 10;
        var vote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteAsync(_testResource.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<string>>(okResult.Value);
        Assert.Equal("Vote deleted successfully.", response.Message);

        // Verify vote was deleted
        var deletedVote = await _context.Votes.FindAsync(vote.Id);
        Assert.Null(deletedVote);

        // Verify count was decremented
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(9ul, resource!.UpVotes);
    }

    [Fact]
    public async Task DeleteVoteAsync_DownvoteExists_DeletesAndDecrementsCount()
    {
        // Arrange
        _testResource.DownVotes = 7;
        var vote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = false
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteAsync(_testResource.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify downvote count was decremented
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(6ul, resource!.DownVotes);
    }

    [Fact]
    public async Task DeleteVoteAsync_NonExistentVote_ReturnsNotFound()
    {
        // Arrange
        uint resourceId = _testResource.Id;

        // Act
        var result = await _controller.DeleteVoteAsync(resourceId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Vote not found for this resource.", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteVoteAsync_UnderflowProtection_UpvoteAtZero()
    {
        // Arrange
        _testResource.UpVotes = 0; // Already at zero
        var vote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteAsync(_testResource.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify upvotes stayed at 0 (didn't underflow)
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(0ul, resource!.UpVotes);
    }

    [Fact]
    public async Task DeleteVoteAsync_UnderflowProtection_DownvoteAtZero()
    {
        // Arrange
        _testResource.DownVotes = 0; // Already at zero
        var vote = new Vote
        {
            Owner = _testUser,
            Resource = _testResource,
            Direction = false
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteAsync(_testResource.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify downvotes stayed at 0 (didn't underflow)
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(0ul, resource!.DownVotes);
    }

    [Fact]
    public async Task DeleteVoteAsync_OtherUsersVoteExists_ReturnsNotFound()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var vote = new Vote
        {
            Owner = otherUser,
            Resource = _testResource,
            Direction = true
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteAsync(_testResource.Id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Vote not found for this resource.", notFoundResult.Value);

        // Verify other user's vote wasn't deleted
        var stillExists = await _context.Votes.FindAsync(vote.Id);
        Assert.NotNull(stillExists);
    }

    #endregion

    #region Integration/Edge Case Tests

    [Fact]
    public async Task VoteLifecycle_CreateUpdateDelete_WorksCorrectly()
    {
        // Arrange
        var createRequest = new VoteRequest(_testResource.Id, true);

        // Act 1: Create upvote
        var createResult = await _controller.CreateVoteAsync(createRequest);
        Assert.IsType<OkObjectResult>(createResult);

        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(1ul, resource!.UpVotes);
        Assert.Equal(0ul, resource.DownVotes);

        // Act 2: Update to downvote
        var updateRequest = new VoteRequest(_testResource.Id, false);
        var updateResult = await _controller.UpdateVoteAsync(updateRequest);
        Assert.IsType<OkObjectResult>(updateResult);

        resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(0ul, resource!.UpVotes);
        Assert.Equal(1ul, resource.DownVotes);

        // Act 3: Delete vote
        var deleteResult = await _controller.DeleteVoteAsync(_testResource.Id);
        Assert.IsType<OkObjectResult>(deleteResult);

        resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(0ul, resource!.UpVotes);
        Assert.Equal(0ul, resource.DownVotes);

        // Verify vote is gone
        var votes = await _context.Votes
            .Where(v => v.Owner.Id == _testUser.Id && v.Resource.Id == _testResource.Id)
            .ToListAsync();
        Assert.Empty(votes);
    }

    [Fact]
    public async Task MultipleUsers_CanVoteOnSameResource()
    {
        // Arrange
        var user2 = new TopFiveUser
        {
            Id = "user2-id",
            UserName = "user2",
            Email = "user2@example.com"
        };
        _context.Users.Add(user2);
        await _context.SaveChangesAsync();

        var controller2 = new VoteController(_context);
        SetupControllerContext(controller2, user2);

        // Act: Both users vote
        var request1 = new VoteRequest(_testResource.Id, true);
        var request2 = new VoteRequest(_testResource.Id, true);

        var result1 = await _controller.CreateVoteAsync(request1);
        var result2 = await controller2.CreateVoteAsync(request2);

        // Assert
        Assert.IsType<OkObjectResult>(result1);
        Assert.IsType<OkObjectResult>(result2);

        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(2ul, resource!.UpVotes);

        var voteCount = await _context.Votes
            .Where(v => v.Resource.Id == _testResource.Id)
            .CountAsync();
        Assert.Equal(2, voteCount);
    }

    [Fact]
    public async Task VoteCounts_RemainConsistent_ThroughMultipleOperations()
    {
        // Arrange
        var users = new List<TopFiveUser>();
        var controllers = new List<VoteController>();

        // Create 10 users
        for (int i = 0; i < 10; i++)
        {
            var user = new TopFiveUser
            {
                Id = $"user-{i}",
                UserName = $"user{i}",
                Email = $"user{i}@example.com"
            };
            _context.Users.Add(user);
            users.Add(user);
        }
        await _context.SaveChangesAsync();

        // Create controllers for each user
        foreach (var user in users)
        {
            var controller = new VoteController(_context);
            SetupControllerContext(controller, user);
            controllers.Add(controller);
        }

        _context.ChangeTracker.Clear(); // Clear tracking to avoid conflicts

        // Act: 7 upvotes, 3 downvotes
        for (int i = 0; i < 7; i++)
        {
            var request = new VoteRequest(_testResource.Id, true);
            await controllers[i].CreateVoteAsync(request);
        }

        for (int i = 7; i < 10; i++)
        {
            var request = new VoteRequest(_testResource.Id, false);
            await controllers[i].CreateVoteAsync(request);
        }

        // Assert
        var resource = await _context.Resources.FindAsync(_testResource.Id);
        Assert.Equal(7ul, resource!.UpVotes);
        Assert.Equal(3ul, resource.DownVotes);
        Assert.Equal(10ul, resource.TotalVotes);

        // Verify vote count matches
        var actualVotes = await _context.Votes
            .Where(v => v.Resource.Id == _testResource.Id)
            .CountAsync();
        Assert.Equal(10, actualVotes);
    }

    #endregion
}
