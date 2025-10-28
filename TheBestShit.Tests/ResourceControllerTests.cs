using System.Net;
using System.Security.Claims;
using iteration1;
using iteration1.Controllers;
using iteration1.Models;
using iteration1.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TheBestShit.Tests;

public class ResourceControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ResourceController _controller;
    private readonly TopFiveUser _testUser;
    private readonly Category _testCategory;
    private readonly Section _testSection;
    private readonly SqliteConnection _connection;

    public ResourceControllerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .ConfigureWarnings(x => x.Default(WarningBehavior.Ignore))
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

        _context.SaveChanges();

        _controller = new ResourceController(_context);
        SetupControllerContext(_controller, _testUser);
    }

    private static void SetupControllerContext(ResourceController controller, TopFiveUser user)
    {
        List<Claim> claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email!)
        };

        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

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
        _connection.Dispose();
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithSectionName_ReturnsResourcesInSection()
    {
        // Arrange
        var resource1 = new Resource
        {
            Name = "Resource 1",
            Url = new Uri("https://example1.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        
        var resource2 = new Resource
        {
            Name = "Resource 2",
            Url = new Uri("https://example2.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Resources.AddRange(resource1, resource2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync(_testSection.Name);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        Assert.Equal(2, resources.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithNonExistentSection_ReturnsEmptyList()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "Resource 1",
            Url = new Uri("https://example1.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync("NonExistentSection");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        Assert.Empty(resources);
    }

    [Fact]
    public async Task GetAllAsync_WithNullSectionName_ReturnsEmptyList()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "Resource 1",
            Url = new Uri("https://example1.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        Assert.Empty(resources);
    }

    #endregion

    #region GetMyAsync Tests

    [Fact]
    public async Task GetMyAsync_ReturnsCurrentUserResources()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var myResource = new Resource
        {
            Name = "My Resource",
            Url = new Uri("https://mine.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        var otherResource = new Resource
        {
            Name = "Other Resource",
            Url = new Uri("https://other.com"),
            Section = _testSection,
            Owner = otherUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.AddRange(myResource, otherResource);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        var resourceList = resources.ToList();
        Assert.Single(resourceList);
        Assert.Equal("My Resource", resourceList[0].Name);
    }

    [Fact]
    public async Task GetMyAsync_NoResources_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetMyAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        Assert.Empty(resources);
    }

    #endregion

    #region GetTopFiveAsync Tests

    [Fact]
    public async Task GetTopFiveAsync_ReturnsTop5ResourcesOrderedByScore()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var resource = new Resource
            {
                Name = $"Resource {i}",
                Url = new Uri($"https://example{i}.com"),
                Section = _testSection,
                Owner = _testUser,
                CreatedAt = DateTime.UtcNow,
                UpVotes = (ulong)(10 - i), // Higher scores first
                DownVotes = 0
            };
            _context.Resources.Add(resource);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTopFiveAsync(_testSection.Name);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        var resourceList = resources.ToList();
        
        Assert.Equal(5, resourceList.Count);

        // Verify ordering by score (descending)
        for (int i = 0; i < resourceList.Count - 1; i++)
        {
            Assert.True(resourceList[i].Score >= resourceList[i + 1].Score);
        }
    }

    [Fact]
    public async Task GetTopFiveAsync_LessThan5Resources_ReturnsAllResources()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var resource = new Resource
            {
                Name = $"Resource {i}",
                Url = new Uri($"https://example{i}.com"),
                Section = _testSection,
                Owner = _testUser,
                CreatedAt = DateTime.UtcNow,
                UpVotes = (ulong)i,
                DownVotes = 0
            };
            _context.Resources.Add(resource);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTopFiveAsync(_testSection.Name);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        Assert.Equal(3, resources.Count());
    }

    [Fact]
    public async Task GetTopFiveAsync_NonExistentSection_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetTopFiveAsync("NonExistentSection");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        Assert.Empty(resources);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesResource()
    {
        // Arrange
        var request = new ResourceRequest(null, "New Resource", new Uri("https://new.com"));

        // Act
        var result = await _controller.CreateAsync(_testSection.Id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<ResourceRequest>>(okResult.Value);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Resource created successfully.", response.Message);

        // Verify resource was created in database
        var createdResource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Name == "New Resource");
        Assert.NotNull(createdResource);
        Assert.Equal("https://new.com/", createdResource.Url.ToString());
        Assert.Equal(_testUser.Id, createdResource.Owner.Id);
        Assert.Equal(_testSection.Id, createdResource.Section.Id);
    }

    [Fact]
    public async Task CreateAsync_NonExistentSection_ReturnsNotFound()
    {
        // Arrange
        var request = new ResourceRequest(null, "New Resource", new Uri("https://new.com"));

        // Act
        var result = await _controller.CreateAsync(99999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateAsync_DuplicateResourceName_ReturnsConflict()
    {
        // Arrange
        var existingResource = new Resource
        {
            Name = "Duplicate Resource",
            Url = new Uri("https://existing.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.Add(existingResource);
        await _context.SaveChangesAsync();

        var request = new ResourceRequest(null, "Duplicate Resource", new Uri("https://new.com"));

        // Act
        var result = await _controller.CreateAsync(_testSection.Id, request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<string>>(conflictResult.Value);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already exists", response.Message);
    }

    [Fact]
    public async Task CreateAsync_SetsInitialVotesToZero()
    {
        // Arrange
        var request = new ResourceRequest(null, "New Resource", new Uri("https://new.com"));

        // Act
        await _controller.CreateAsync(_testSection.Id, request);

        // Assert
        var createdResource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Name == "New Resource");
        Assert.NotNull(createdResource);
        Assert.Equal(0ul, createdResource.UpVotes);
        Assert.Equal(0ul, createdResource.DownVotes);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var request = new ResourceRequest(null, "New Resource", new Uri("https://new.com"));
        var beforeCreate = DateTime.UtcNow;

        // Act
        await _controller.CreateAsync(_testSection.Id, request);

        // Assert
        var createdResource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Name == "New Resource");
        Assert.NotNull(createdResource);
        Assert.True(createdResource.CreatedAt >= beforeCreate);
        Assert.True(createdResource.CreatedAt <= DateTime.UtcNow);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ReturnsOk()
    {
        // Arrange
        var request = new ResourceRequest(1, "Updated", new Uri("https://updated.com"));

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesResource()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "To Delete",
            Url = new Uri("https://delete.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        var resourceId = resource.Id;

        // Act
        IActionResult result = await _controller.DeleteAsync(resourceId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        AppResponseInfo<IdResponse> response = Assert.IsType<AppResponseInfo<IdResponse>>(okResult.Value);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Resource deleted successfully.", response.Message);

        // Verify resource was deleted
        var deletedResource = await _context.Resources.FindAsync(resourceId);
        Assert.Null(deletedResource);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteAsync(99999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("does not exist", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsForbid()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var resource = new Resource
        {
            Name = "Other's Resource",
            Url = new Uri("https://other.com"),
            Section = _testSection,
            Owner = otherUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteAsync(resource.Id);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_Owner_CanDelete()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "My Resource",
            Url = new Uri("https://mine.com"),
            Section = _testSection,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        var resourceId = resource.Id;

        // Act
        var result = await _controller.DeleteAsync(resourceId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify deletion
        var deletedResource = await _context.Resources.FindAsync(resourceId);
        Assert.Null(deletedResource);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ResourceLifecycle_CreateAndDelete_WorksCorrectly()
    {
        // Arrange
        var createRequest = new ResourceRequest(null, "Lifecycle Resource", new Uri("https://lifecycle.com"));

        // Act 1: Create
        var createResult = await _controller.CreateAsync(_testSection.Id, createRequest);
        Assert.IsType<OkObjectResult>(createResult);

        var createdResource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Name == "Lifecycle Resource");
        Assert.NotNull(createdResource);

        // Act 2: Delete
        var deleteResult = await _controller.DeleteAsync(createdResource.Id);
        Assert.IsType<OkObjectResult>(deleteResult);

        // Assert
        var deletedResource = await _context.Resources.FindAsync(createdResource.Id);
        Assert.Null(deletedResource);
    }

    [Fact]
    public async Task GetTopFiveAsync_WithVotes_OrdersCorrectly()
    {
        // Arrange - Create resources with different vote counts
        var resources = new[]
        {
            new Resource { Name = "Low Score", Url = new Uri("https://low.com"), Section = _testSection, Owner = _testUser, CreatedAt = DateTime.UtcNow, UpVotes = 5, DownVotes = 10 },
            new Resource { Name = "High Score", Url = new Uri("https://high.com"), Section = _testSection, Owner = _testUser, CreatedAt = DateTime.UtcNow, UpVotes = 100, DownVotes = 5 },
            new Resource { Name = "Medium Score", Url = new Uri("https://med.com"), Section = _testSection, Owner = _testUser, CreatedAt = DateTime.UtcNow, UpVotes = 50, DownVotes = 10 },
        };
        _context.Resources.AddRange(resources);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTopFiveAsync(_testSection.Name);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceResponse>>(okResult.Value);
        var resourceList = returnedResources.ToList();

        // First should be high score, last should be low score
        Assert.Equal("High Score", resourceList[0].Name);
        Assert.Equal("Low Score", resourceList[^1].Name);
    }

    #endregion
}
