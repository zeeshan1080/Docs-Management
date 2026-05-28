using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<UserLocation> UserLocations => Set<UserLocation>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<FolderShare> FolderShares => Set<FolderShare>();
    public DbSet<DocumentShare> DocumentShares => Set<DocumentShare>();
    public DbSet<EmployeeRegistrationRequest> EmployeeRegistrationRequests => Set<EmployeeRegistrationRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion(typeof(UtcDateTimeConverter));
        configurationBuilder.Properties<DateTime?>().HaveConversion(typeof(UtcNullableDateTimeConverter));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserLocation>(e =>
        {
            e.HasKey(x => new { x.UserId, x.LocationId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Folder>(e =>
        {
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentFolderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Document>(e =>
        {
            e.HasOne(x => x.Folder).WithMany(x => x.Documents).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FolderShare>(e =>
        {
            e.HasOne(x => x.Folder).WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DocumentShare>(e =>
        {
            e.HasOne(x => x.Document).WithMany(x => x.DocumentShares).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployeeRegistrationRequest>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RequestedRole).WithMany().HasForeignKey(x => x.RequestedRoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RequestedLocation).WithMany().HasForeignKey(x => x.RequestedLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
