using System.Text.Json.Serialization;
using iteration1.Models;
using Microsoft.AspNetCore.Identity;

namespace iteration1;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

        builder.Services.AddIdentityApiEndpoints<TopFiveUser>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        
        builder.Services.AddDbContext<ApplicationDbContext>();
        builder.Services.AddSingleton<IEmailSender<TopFiveUser>, NoOpEmailSender>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("VueDev", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")  // Vue dev server (Vite default port)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }
        
        WebApplication app = builder.Build();

        app.UseHttpsRedirection();

        if (app.Environment.IsDevelopment())
        {
            app.UseCors("VueDev");
        }
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapIdentityApi<TopFiveUser>();
        app.MapDefaultControllerRoute();

        app.MapFallbackToFile("index.html");
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