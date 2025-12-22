using BitirmeProject.IdentityService.Infrastructure.DependencyInjection;
using BitirmeProject.IdentityService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Uygulama & altyapý
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();

// Db migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
