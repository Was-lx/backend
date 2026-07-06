using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WaslX.Persistance.Data;

namespace WaslX.Api.Extensions;

/// <summary>
/// First-run database bootstrap: creates the databases if they don't exist yet
/// so the app is runnable out of the box without manual SQL or `dotnet ef` steps.
/// </summary>
public static class DatabaseStartupExtensions
{
    public static async Task InitializeDatabasesAsync(this WebApplication app)
    {
        // Hangfire creates its own tables but NOT the database itself, and its
        // server connects at host start — so the database must exist beforehand.
        var hangfireConnection = app.Configuration.GetConnectionString("HangfireConnection");
        if (!string.IsNullOrWhiteSpace(hangfireConnection))
            await EnsureDatabaseExistsAsync(hangfireConnection);

        // Create the app database (if missing) and apply all pending EF Core
        // migrations, including the seeded roles and SuperAdmin account.
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Creates the target database if it doesn't exist by connecting to the
    /// server's <c>master</c> database and issuing a guarded CREATE DATABASE.
    /// </summary>
    private static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
            return;

        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        // Database name comes from trusted config; escape ']' defensively anyway.
        var safeName = databaseName.Replace("]", "]]");

        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@name) IS NULL CREATE DATABASE [{safeName}];";
        command.Parameters.Add(new SqlParameter("@name", databaseName));
        await command.ExecuteNonQueryAsync();
    }
}
