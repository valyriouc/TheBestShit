using System.Security.Claims;
using iteration1;
using iteration1.Controllers;
using iteration1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TheBestShit.Tests;

public class UserControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserController _controller;
    private readonly TopFiveUser _testUser;
    private readonly SqliteConnection _connection;

    public UserControllerTests()
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
            EmailConfirmed = true,
            TwoFactorEnabled = false
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        _controller = new UserController(_context);
        SetupControllerContext(_controller, _testUser);
    }

    private static void SetupControllerContext(UserController controller, TopFiveUser user)
    {
        List<Claim> claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email)
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

    #region GetCurrentUserInfoAsync Tests

    [Fact]
    public async Task GetCurrentUserInfoAsync_ReturnsCurrentUserInfo()
    {
        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(_testUser.Id, userResponse.Id);
        Assert.Equal(_testUser.Email, userResponse.Email);
        Assert.Equal(_testUser.UserName, userResponse.UserName);
        Assert.Equal(_testUser.TwoFactorEnabled, userResponse.TwoFactorEnabled);
    }

    [Fact]
    public async Task GetCurrentUserInfoAsync_ReturnsCorrectEmail()
    {
        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal("test@example.com", userResponse.Email);
    }

    [Fact]
    public async Task GetCurrentUserInfoAsync_ReturnsCorrectUserName()
    {
        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal("testuser", userResponse.UserName);
    }

    [Fact]
    public async Task GetCurrentUserInfoAsync_ReturnsTwoFactorEnabledStatus()
    {
        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.False(userResponse.TwoFactorEnabled);
    }

    [Fact]
    public async Task GetCurrentUserInfoAsync_WithTwoFactorEnabled_ReturnsTrue()
    {
        // Arrange
        _testUser.TwoFactorEnabled = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.True(userResponse.TwoFactorEnabled);
    }

    [Fact]
    public async Task GetCurrentUserInfoAsync_WithNullUserName_ReturnsEmptyString()
    {
        // Arrange
        _testUser.UserName = null;
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(string.Empty, userResponse.UserName);
    }

    #endregion

    #region AuthCheckAsync Tests

    [Fact]
    public async Task AuthCheckAsync_ReturnsOk()
    {
        // Act
        var result = await _controller.AuthCheckAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task AuthCheckAsync_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _controller.AuthCheckAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dictionary = Assert.IsType<Dictionary<string, string>>(okResult.Value);
        Assert.Empty(dictionary);
    }

    [Fact]
    public async Task AuthCheckAsync_CanBeCalledMultipleTimes()
    {
        // Act
        var result1 = await _controller.AuthCheckAsync();
        var result2 = await _controller.AuthCheckAsync();
        var result3 = await _controller.AuthCheckAsync();

        // Assert
        Assert.IsType<OkObjectResult>(result1);
        Assert.IsType<OkObjectResult>(result2);
        Assert.IsType<OkObjectResult>(result3);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task MultipleUsers_EachGetsOwnInfo()
    {
        // Arrange
        var user2 = new TopFiveUser
        {
            Id = "user2-id",
            UserName = "user2",
            Email = "user2@example.com",
            TwoFactorEnabled = true
        };
        _context.Users.Add(user2);
        await _context.SaveChangesAsync();

        var controller2 = new UserController(_context);
        SetupControllerContext(controller2, user2);

        // Act
        var result1 = await _controller.GetCurrentUserInfoAsync();
        var result2 = await controller2.GetCurrentUserInfoAsync();

        // Assert
        var okResult1 = Assert.IsType<OkObjectResult>(result1);
        var userResponse1 = Assert.IsType<UserResponse>(okResult1.Value);
        Assert.Equal("testuser", userResponse1.UserName);
        Assert.False(userResponse1.TwoFactorEnabled);

        var okResult2 = Assert.IsType<OkObjectResult>(result2);
        var userResponse2 = Assert.IsType<UserResponse>(okResult2.Value);
        Assert.Equal("user2", userResponse2.UserName);
        Assert.True(userResponse2.TwoFactorEnabled);
    }

    [Fact]
    public async Task UserResponse_ContainsAllRequiredFields()
    {
        // Act
        var result = await _controller.GetCurrentUserInfoAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);

        // Verify all properties are present and have values
        Assert.NotNull(userResponse.Id);
        Assert.NotNull(userResponse.Email);
        Assert.NotNull(userResponse.UserName);
        Assert.False(string.IsNullOrEmpty(userResponse.Id));
        Assert.False(string.IsNullOrEmpty(userResponse.Email));
        Assert.False(string.IsNullOrEmpty(userResponse.UserName));
    }

    [Fact]
    public async Task AuthCheck_WorksForAuthenticatedUser()
    {
        // Act
        var authCheckResult = await _controller.AuthCheckAsync();
        var userInfoResult = await _controller.GetCurrentUserInfoAsync();

        // Assert - Both should succeed for authenticated user
        Assert.IsType<OkObjectResult>(authCheckResult);
        Assert.IsType<OkObjectResult>(userInfoResult);
    }

    #endregion

    #region UserResponse Tests

    [Fact]
    public void UserResponse_ConstructorSetsPropertiesCorrectly()
    {
        // Arrange
        var user = new TopFiveUser
        {
            Id = "response-test-id",
            Email = "response@test.com",
            UserName = "responseuser",
            TwoFactorEnabled = true
        };

        // Act
        var response = new UserResponse(user);

        // Assert
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.UserName, response.UserName);
        Assert.Equal(user.TwoFactorEnabled, response.TwoFactorEnabled);
    }

    [Fact]
    public void UserResponse_HandlesNullEmailGracefully()
    {
        // Arrange
        var user = new TopFiveUser
        {
            Id = "null-email-id",
            Email = null,
            UserName = "nullemail",
            TwoFactorEnabled = false
        };

        // Act
        var response = new UserResponse(user);

        // Assert
        Assert.Equal(string.Empty, response.Email);
    }

    [Fact]
    public void UserResponse_HandlesNullUserNameGracefully()
    {
        // Arrange
        var user = new TopFiveUser
        {
            Id = "null-username-id",
            Email = "email@test.com",
            UserName = null,
            TwoFactorEnabled = false
        };

        // Act
        var response = new UserResponse(user);

        // Assert
        Assert.Equal(string.Empty, response.UserName);
    }

    #endregion
}
