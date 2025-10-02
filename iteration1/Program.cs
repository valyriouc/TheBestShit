using iteration1.Models;
using Microsoft.AspNetCore.Identity;

namespace iteration1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddIdentityApiEndpoints<TopFiveUser>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        
        builder.Services.AddDbContext<ApplicationDbContext>();
        builder.Services.AddSingleton<IEmailSender<TopFiveUser>, NoOpEmailSender>();
        
        WebApplication app = builder.Build();

        app.UseHttpsRedirection();
        
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapIdentityApi<TopFiveUser>();
        app.MapDefaultControllerRoute();
        
        app.Run();
    }
}

public class NoOpEmailSender : IEmailSender<TopFiveUser>
{
    public Task SendConfirmationLinkAsync(TopFiveUser user, string email, string confirmationLink)
    {
        // No-op: do nothing
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(TopFiveUser user, string email, string resetLink)
    {
        // No-op: do nothing
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(TopFiveUser user, string email, string resetCode)
    {
        // No-op: do nothing
        return Task.CompletedTask;
    }
}