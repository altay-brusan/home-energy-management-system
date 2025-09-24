using System.Reflection;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Setup> Setups => Set<Setup>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DeviceComponent> DeviceComponents => Set<DeviceComponent>();

    public DbSet<SetupDevice> SetupDevices => Set<SetupDevice>();

    public DbSet<ExternalDevice> ExternalDevices => Set<ExternalDevice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
