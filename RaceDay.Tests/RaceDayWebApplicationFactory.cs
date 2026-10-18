

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Data;

namespace RaceDay.Tests
{
    public class RaceDayWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        // A separate temporary database for each factory.
        private readonly SqliteConnection _connection =
            new SqliteConnection("Data Source=:memory:");

        public RaceDayWebApplicationFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Tell Program.cs not to run normal
            // SQL Server migrations or Admin seeding.
            builder.UseSetting(
                "RaceDay:IntegrationTesting",
                "true");

            builder.ConfigureAppConfiguration(
                (context, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["RaceDay:IntegrationTesting"] =
                                "true",

                            ["ConnectionStrings:DefaultConnection"] =
                                "TEST_DATABASE_DISABLED"
                        });
                });

            builder.ConfigureTestServices(services =>
            {
                // Remove the application's SQL Server
                // DbContext configuration.
                services.RemoveAll<
                    DbContextOptions<ApplicationDbContext>>();

                services.RemoveAll<
                    IDbContextOptionsConfiguration<
                        ApplicationDbContext>>();

                services.RemoveAll<DbConnection>();

                // Register the SQLite in-memory connection.
                services.AddSingleton<DbConnection>(
                    _connection);

                services.AddDbContext<ApplicationDbContext>(
                    (serviceProvider, options) =>
                    {
                        var connection =
                            serviceProvider
                                .GetRequiredService<DbConnection>();

                        options.UseSqlite(connection);
                    });
            });
        }

        // Call this when an integration test needs
        // a ready-to-use isolated HTTP client.
        public System.Net.Http.HttpClient
            CreateIsolatedClient()
        {
            var client = CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // Build a fresh SQLite schema.
            // Do NOT run SQL Server migrations here.
            db.Database.EnsureCreated();

            return client;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing: disposing);

            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
