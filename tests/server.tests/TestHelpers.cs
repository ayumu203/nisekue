using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using server.infrastructure;

namespace server.tests;

public static class TestHelpers
{
    public static AppDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        return new SqliteTestAppDbContext(options);
    }

    public class SqliteDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => TestHelpers.CreateSqliteDbContext(connection);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(TestHelpers.CreateSqliteDbContext(connection));
    }

    private sealed class SqliteTestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<long>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<long?>();
        }
    }
}
