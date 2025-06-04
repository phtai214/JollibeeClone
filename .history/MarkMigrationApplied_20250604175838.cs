using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Data;

class Program
{
    static void Main(string[] args)
    {
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=JollibeeClone;Trusted_Connection=true;MultipleActiveResultSets=true";
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        using var context = new AppDbContext(optionsBuilder.Options);
        
        try
        {
            // Check if migration history table exists and create if not
            context.Database.EnsureCreated();
            
            // Execute raw SQL to mark migration as applied
            var sql = @"
                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250527030009_InitialCreate')
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES ('20250527030009_InitialCreate', '8.0.0');
                    PRINT 'Migration marked as applied successfully!';
                END
                ELSE
                BEGIN
                    PRINT 'Migration already marked as applied.';
                END";
            
            context.Database.ExecuteSqlRaw(sql);
            
            Console.WriteLine("Success! Migration has been marked as applied.");
            Console.WriteLine("You can now run: dotnet run");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
} 