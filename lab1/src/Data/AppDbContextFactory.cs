using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace mywebapp.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Database=mywebapp;User=mywebapp;Password=mywebapp;",
                new MySqlServerVersion(new Version(10, 11))
            )
            .Options;

        return new AppDbContext(options);
    }
}