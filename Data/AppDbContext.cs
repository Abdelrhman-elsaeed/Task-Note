using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using TaskNote.Models;
using TaskNote.Services;

namespace TaskNote.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ISettingsService _settingsService;

        public AppDbContext(DbContextOptions<AppDbContext> options, ISettingsService settingsService)
            : base(options)
        {
            _settingsService = settingsService;
        }

        public DbSet<Folder> Folders { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Column> Columns { get; set; } = null!;
        public DbSet<TaskItem> Tasks { get; set; } = null!;
        public DbSet<TimerHistoryItem> TimerHistory { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = _settingsService.CurrentSettings.DatabasePath;
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            optionsBuilder.AddInterceptors(new SqliteBusyTimeoutInterceptor());
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Folder>()
                .HasMany(f => f.Projects)
                .WithOne(p => p.Folder)
                .HasForeignKey(p => p.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Columns)
                .WithOne(c => c.Project)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Column>()
                .HasMany(c => c.Tasks)
                .WithOne(t => t.Column)
                .HasForeignKey(t => t.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA busy_timeout = 5000;";
                command.ExecuteNonQuery();
            }
            base.ConnectionOpened(connection, eventData);
        }

        public override async Task ConnectionOpenedAsync(
            DbConnection connection, 
            ConnectionEndEventData eventData, 
            CancellationToken cancellationToken = default)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA busy_timeout = 5000;";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }
    }
}
