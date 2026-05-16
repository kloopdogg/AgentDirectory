using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentDirectory.Data;

/// <summary>
/// Used by dotnet-ef at design time to create migrations without a running app.
/// </summary>
public class AgentDbContextFactory : IDesignTimeDbContextFactory<AgentDbContext>
{
    public AgentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AgentDirectoryDb;Trusted_Connection=True;")
            .Options;

        return new AgentDbContext(options);
    }
}
