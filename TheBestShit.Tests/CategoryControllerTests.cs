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

public class CategoryControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CategoryController _controller;
    private readonly TopFiveUser _testUser;
    private readonly SqliteConnection _connection;

    public CategoryControllerTests()
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
        _context.SaveChanges();

        _controller = new CategoryController(_context);
        SetupControllerContext(_controller, _testUser);
    }

    private static void SetupControllerContext(CategoryController controller, TopFiveUser user)
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
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        // Arrange
        var category1 = new Category
        {
            Name = "Category 1",
            Description = "Description 1",
            Owner = _testUser
        };
        var category2 = new Category
        {
            Name = "Category 2",
            Description = "Description 2",
            Owner = _testUser
        };
        _context.Categories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        Assert.Equal(2, categories.Count());
    }

    [Fact]
    public async Task GetAllAsync_NoCategories_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        Assert.Empty(categories);
    }

    [Fact]
    public async Task GetAllAsync_IncludesOwnerAndSections()
    {
        // Arrange
        var category = new Category
        {
            Name = "Test Category",
            Description = "Test Description",
            Owner = _testUser
        };
        var section = new Section
        {
            Name = "Test Section",
            Description = "Test Section Description",
            Category = category,
            Owner = _testUser
        };
        _context.Categories.Add(category);
        _context.Sections.Add(section);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        var categoryList = categories.ToList();
        Assert.Single(categoryList);
        Assert.NotNull(categoryList[0].Owner);
        Assert.NotNull(categoryList[0].Sections);
        Assert.Single(categoryList[0].Sections);
    }

    #endregion

    #region GetMyCategoriesAsync Tests

    [Fact]
    public async Task GetMyCategoriesAsync_ReturnsOnlyCurrentUserCategories()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var myCategory = new Category
        {
            Name = "My Category",
            Description = "My Description",
            Owner = _testUser
        };
        var otherCategory = new Category
        {
            Name = "Other Category",
            Description = "Other Description",
            Owner = otherUser
        };
        _context.Categories.AddRange(myCategory, otherCategory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyCategoriesAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        var categoryList = categories.ToList();
        Assert.Single(categoryList);
        Assert.Equal("My Category", categoryList[0].Name);
        Assert.Equal(_testUser.Id, categoryList[0].Owner.Id);
    }

    [Fact]
    public async Task GetMyCategoriesAsync_NoOwnedCategories_ReturnsEmptyList()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var otherCategory = new Category
        {
            Name = "Other Category",
            Description = "Other Description",
            Owner = otherUser
        };
        _context.Categories.Add(otherCategory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyCategoriesAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        Assert.Empty(categories);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCategory()
    {
        // Arrange
        var request = new CategoryRequest(null, "New Category", "New Description");

        // Act
        var result = await _controller.CreateAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<Category>>(okResult.Value);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("created successfully", response.Message);

        // Verify category was created
        var createdCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == "New Category");
        Assert.NotNull(createdCategory);
        Assert.Equal("New Description", createdCategory.Description);
        Assert.Equal(_testUser.Id, createdCategory.Owner.Id);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CategoryRequest(null, "", "Description");

        // Act
        var result = await _controller.CreateAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot be empty", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateAsync_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CategoryRequest(null, "   ", "Description");

        // Act
        var result = await _controller.CreateAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot be empty", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var existingCategory = new Category
        {
            Name = "Duplicate Name",
            Description = "Description",
            Owner = _testUser
        };
        _context.Categories.Add(existingCategory);
        await _context.SaveChangesAsync();

        var request = new CategoryRequest(null, "Duplicate Name", "New Description");

        // Act
        var result = await _controller.CreateAsync(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "Original Name",
            Description = "Original Description",
            Owner = _testUser
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CategoryRequest(category.Id, "Updated Name", "Updated Description");

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AppResponseInfo<Category>>(okResult.Value);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Category updated successfully.", response.Message);

        // Verify category was updated
        var updatedCategory = await _context.Categories.FindAsync(category.Id);
        Assert.NotNull(updatedCategory);
        Assert.Equal("Updated Name", updatedCategory.Name);
        Assert.Equal("Updated Description", updatedCategory.Description);
    }

    [Fact]
    public async Task UpdateAsync_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CategoryRequest(1, "", "Description");

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot be empty", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateAsync_NullId_ReturnsBadRequest()
    {
        // Arrange
        var request = new CategoryRequest(null, "Name", "Description");

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("id is required", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var request = new CategoryRequest(99999, "Name", "Description");

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ReturnsForbid()
    {
        // Arrange
        var otherUser = new TopFiveUser
        {
            Id = "other-user-id",
            UserName = "otheruser",
            Email = "other@example.com"
        };
        _context.Users.Add(otherUser);

        var category = new Category
        {
            Name = "Other's Category",
            Description = "Description",
            Owner = otherUser
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CategoryRequest(category.Id, "Updated Name", "Updated Description");

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateAsync_Owner_CanUpdate()
    {
        // Arrange
        var category = new Category
        {
            Name = "My Category",
            Description = "My Description",
            Owner = _testUser
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CategoryRequest(category.Id, "Updated Name", "Updated Description");

        // Act
        var result = await _controller.UpdateAsync(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var updatedCategory = await _context.Categories.FindAsync(category.Id);
        Assert.NotNull(updatedCategory);
        Assert.Equal("Updated Name", updatedCategory.Name);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CategoryLifecycle_CreateUpdateGetAll_WorksCorrectly()
    {
        // Act 1: Create
        var createRequest = new CategoryRequest(null, "Lifecycle Category", "Lifecycle Description");
        var createResult = await _controller.CreateAsync(createRequest);
        Assert.IsType<OkObjectResult>(createResult);

        var createdCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == "Lifecycle Category");
        Assert.NotNull(createdCategory);

        // Act 2: Update
        var updateRequest = new CategoryRequest(createdCategory.Id, "Updated Lifecycle", "Updated Description");
        var updateResult = await _controller.UpdateAsync(updateRequest);
        Assert.IsType<OkObjectResult>(updateResult);

        // Act 3: GetAll should include the updated category
        var getAllResult = await _controller.GetAllAsync();
        var okResult = Assert.IsType<OkObjectResult>(getAllResult);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        var categoryList = categories.ToList();

        Assert.Contains(categoryList, c => c.Name == "Updated Lifecycle");
    }

    [Fact]
    public async Task CategoryWithSections_GetMyCategories_IncludesSections()
    {
        // Arrange
        var category = new Category
        {
            Name = "Category with Sections",
            Description = "Description",
            Owner = _testUser
        };
        var section1 = new Section
        {
            Name = "Section 1",
            Description = "Desc 1",
            Owner = _testUser,
            Category = category
        };
        var section2 = new Section
        {
            Name = "Section 2",
            Description = "Desc 2",
            Owner = _testUser,
            Category = category
        };
        _context.Categories.Add(category);
        _context.Sections.AddRange(section1, section2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyCategoriesAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
        var categoryList = categories.ToList();
        Assert.Single(categoryList);
        Assert.Equal(2, categoryList[0].Sections.Count);
    }

    #endregion
}
