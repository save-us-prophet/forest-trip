using Microsoft.EntityFrameworkCore;

using ShareInvest.Models;

namespace ShareInvest.Data;

public class ForestTripContext : DbContext
{
    public DbSet<ForestRetreat> ForestRetreat
    {
        get; set;
    }

    public DbSet<Cabin> Cabin
    {
        get; set;
    }

    public DbSet<Reservation> Reservations
    {
        get; set;
    }

    public DbSet<Policy> Policy
    {
        get; set;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder

            .Entity<Policy>(buildAction =>
            {
                buildAction.HasKey(keyExpression => keyExpression.ResortId);
                buildAction.Property(propertyExpression => propertyExpression.ResortName).IsRequired();
                buildAction.ToTable(nameof(Policy));
            })

            .Entity<Cabin>(buildAction =>
            {
                buildAction.HasKey(keyExpression => new
                {
                    keyExpression.Id,
                    keyExpression.Name
                });
                buildAction.ToTable(nameof(Cabin));
            })

            .Entity<Reservation>(buildAction =>
            {
                buildAction.HasKey(keyExpression => new
                {
                    keyExpression.StartDate,
                    keyExpression.ForestRetreat,
                    keyExpression.CabinName
                });
                buildAction.Property(propertyExpression => propertyExpression.Region).IsRequired();
                buildAction.Property(propertyExpression => propertyExpression.Policy).IsRequired();
                buildAction.ToTable(nameof(Reservations));
            })

            .Entity<ForestRetreat>(buildAction =>
            {
                buildAction.HasKey(keyExpression => keyExpression.Id);
                buildAction.Property(propertyExpression => propertyExpression.Name).IsRequired();
                buildAction.Property(propertyExpression => propertyExpression.Region).IsRequired();
                buildAction.ToTable(nameof(ForestRetreat));
            });

        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        _ = builder.UseSqlite(Properties.Resources.DB);
    }
}