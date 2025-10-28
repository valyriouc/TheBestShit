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

public class SectionControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SectionController _controller;
    private readonly TopFiveUser _testUser;
    private readonly Category _testCategory;
    private readonly SqliteConnection _connection;

    public SectionControllerTests()
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
            Owner = _testUser,
            PublicEdit = true
        };
        _context.Categories.Add(_testCategory);

        _context.SaveChanges();

        _controller = new SectionController(_context);
        SetupControllerContext(_controller, _testUser);
    }

    private static void SetupControllerContext(SectionController controller, TopFiveUser user)
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
    public async Task GetAllAsync_ReturnsAllSections()
    {
        // Arrange
        var section1 = new Section
        {
            Name = "Section 1",
            Description = "Description 1",
            Owner = _testUser,
            Category = _testCategory
        };
        var section2 = new Section
        {
            Name = "Section 2",
            Description = "Description 2",
            Owner = _testUser,
            Category = _testCategory
        };
        _context.Sections.AddRange(section1, section2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        Assert.Equal(2, sections.Count());
    }

    [Fact]
    public async Task GetAllAsync_NoSections_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        Assert.Empty(sections);
    }

    [Fact]
    public async Task GetAllAsync_IncludesOwnerCategoryAndResources()
    {
        // Arrange
        var section = new Section
        {
            Name = "Test Section",
            Description = "Test Description",
            Owner = _testUser,
            Category = _testCategory
        };
        var resource = new Resource
        {
            Name = "Test Resource",
            Url = new Uri("https://example.com"),
            Section = section,
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sections.Add(section);
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        var sectionList = sections.ToList();
        Assert.Single(sectionList);
        Assert.NotNull(sectionList[0].Owner);
    }

    #endregion

    #region GetMyAsync Tests

    [Fact]
    public async Task GetMyAsync_ReturnsOnlyCurrentUserSections()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var mySection = new Section
        {
            Name = "My Section",
            Description = "My Description",
            Owner = _testUser,
            Category = _testCategory
        };
        var otherSection = new Section
        {
            Name = "Other Section",
            Description = "Other Description",
            Owner = otherUser,
            Category = _testCategory
        };
        _context.Sections.AddRange(mySection, otherSection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        var sectionList = sections.ToList();
        Assert.Single(sectionList);
        Assert.Equal("My Section", sectionList[0].Name);
    }

    [Fact]
    public async Task GetMyAsync_NoOwnedSections_ReturnsEmptyList()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var otherSection = new Section
        {
            Name = "Other Section",
            Description = "Other Description",
            Owner = otherUser,
            Category = _testCategory
        };
        _context.Sections.Add(otherSection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        Assert.Empty(sections);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesSection()
    {
        // Arrange
        var request = new SectionRequest("New Section", "New Description", false, null);

        // Act
        var result = await _controller.CreateAsync(_testCategory.Id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<SectionRequest>>(okResult.Value);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Section created successfully", response.Message);

        // Verify section was created
        var createdSection = await _context.Sections
            .FirstOrDefaultAsync(s => s.Name == "New Section");
        Assert.NotNull(createdSection);
        Assert.Equal("New Description", createdSection.Description);
        Assert.Equal(_testUser.Id, createdSection.Owner.Id);
        Assert.Equal(_testCategory.Id, createdSection.Category.Id);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new SectionRequest("", "Description", false, null);

        // Act
        var result = await _controller.CreateAsync(_testCategory.Id, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<string>>(badRequestResult.Value);
        Assert.Equal("Section name cannot be empty", response.Message);
    }

    [Fact]
    public async Task CreateAsync_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var request = new SectionRequest("   ", "Description", false, null);

        // Act
        var result = await _controller.CreateAsync(_testCategory.Id, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<string>>(badRequestResult.Value);
        Assert.Equal("Section name cannot be empty", response.Message);
    }

    [Fact]
    public async Task CreateAsync_NonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        var request = new SectionRequest("New Section", "Description", false, null);

        // Act
        var result = await _controller.CreateAsync(99999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<string>>(notFoundResult.Value);
        Assert.Equal("Category not found", response.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSectionNameInCategory_ReturnsConflict()
    {
        // Arrange
        var existingSection = new Section
        {
            Name = "Duplicate Section",
            Description = "Description",
            Owner = _testUser,
            Category = _testCategory
        };
        _context.Sections.Add(existingSection);
        await _context.SaveChangesAsync();

        var request = new SectionRequest("Duplicate Section", "New Description", false, null);

        // Act
        var result = await _controller.CreateAsync(_testCategory.Id, request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateAsync_CategoryNotPublicEditAndNotOwner_ReturnsForbid()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var privateCategory = new Category
        {
            Name = "Private Category",
            Description = "Description",
            Owner = otherUser,
            PublicEdit = false
        };
        _context.Categories.Add(privateCategory);
        await _context.SaveChangesAsync();

        var request = new SectionRequest("New Section", "Description", false, null);

        // Act
        var result = await _controller.CreateAsync(privateCategory.Id, request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateAsync_PublicEditCategory_AllowsNonOwnerToCreate()
    {
        // Arrange
        var request = new SectionRequest("New Section", "Description", false, null);

        // Act - testCategory has PublicEdit = true by default
        var result = await _controller.CreateAsync(_testCategory.Id, request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateAsync_Owner_CanCreateInPrivateCategory()
    {
        // Arrange
        var privateCategory = new Category
        {
            Name = "Private Category",
            Description = "Description",
            Owner = _testUser,
            PublicEdit = false
        };
        _context.Categories.Add(privateCategory);
        await _context.SaveChangesAsync();

        var request = new SectionRequest("New Section", "Description", false, null);

        // Act
        var result = await _controller.CreateAsync(privateCategory.Id, request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateAsync_SetsPublicEditFlagCorrectly()
    {
        // Arrange
        var request = new SectionRequest("Public Section", "Description", true, null);

        // Act
        await _controller.CreateAsync(_testCategory.Id, request);

        // Assert
        var createdSection = await _context.Sections
            .FirstOrDefaultAsync(s => s.Name == "Public Section");
        Assert.NotNull(createdSection);
        Assert.True(createdSection.PublicEdit);
    }

    #endregion

    #region EditAsync Tests

    [Fact]
    public async Task EditAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var section = new Section
        {
            Name = "Test Section",
            Description = "Description",
            Owner = _testUser,
            Category = _testCategory
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => _controller.EditAsync(section));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ThrowsNotImplementedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => _controller.DeleteAsync(1));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SectionLifecycle_CreateGetMy_WorksCorrectly()
    {
        // Arrange
        var createRequest = new SectionRequest("Lifecycle Section", "Lifecycle Description", false, null);

        // Act 1: Create
        var createResult = await _controller.CreateAsync(_testCategory.Id, createRequest);
        Assert.IsType<OkObjectResult>(createResult);

        // Act 2: GetMy should include the created section
        var getMyResult = await _controller.GetMyAsync();
        var okResult = Assert.IsType<OkObjectResult>(getMyResult);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        var sectionList = sections.ToList();

        // Assert
        Assert.Contains(sectionList, s => s.Name == "Lifecycle Section");
    }

    [Fact]
    public async Task SectionWithResources_GetAll_IncludesAllData()
    {
        // Arrange
        var section = new Section
        {
            Name = "Section with Resources",
            Description = "Description",
            Owner = _testUser,
            Category = _testCategory
        };
        var resource1 = new Resource
        {
            Name = "Resource 1",
            Url = new Uri("https://example1.com"),
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow,
            Section = section
        };
        var resource2 = new Resource
        {
            Name = "Resource 2",
            Url = new Uri("https://example2.com"),
            Owner = _testUser,
            CreatedAt = DateTime.UtcNow,
            Section = section
        };
        _context.Sections.Add(section);
        _context.Resources.AddRange(resource1, resource2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponse>>(okResult.Value);
        var sectionList = sections.ToList();
        Assert.Single(sectionList);
        Assert.NotNull(sectionList[0].Owner);
    }

    [Fact]
    public async Task MultipleUsers_CanCreateSectionsInPublicCategory()
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

        var controller2 = new SectionController(_context);
        SetupControllerContext(controller2, user2);

        var request1 = new SectionRequest("Section 1", "Description 1", false, null);
        var request2 = new SectionRequest("Section 2", "Description 2", false, null);

        // Act
        var result1 = await _controller.CreateAsync(_testCategory.Id, request1);
        var result2 = await controller2.CreateAsync(_testCategory.Id, request2);

        // Assert
        Assert.IsType<OkObjectResult>(result1);
        Assert.IsType<OkObjectResult>(result2);

        var sections = await _context.Sections
            .Where(s => s.Category.Id == _testCategory.Id)
            .ToListAsync();
        Assert.Equal(2, sections.Count);
    }

    #endregion
}
