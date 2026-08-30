using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class RoomEnemyPoolEntityConfiguration : IEntityTypeConfiguration<RoomEnemyPoolEntity>
{
    public void Configure(EntityTypeBuilder<RoomEnemyPoolEntity> builder)
    {
        ConfigurePool(builder, "catalog_room_enemy_pools");
    }

    private static void ConfigurePool(EntityTypeBuilder<RoomEnemyPoolEntity> builder, string tableName)
    {
        RoomPoolConfiguration.Configure(builder, tableName);
    }
}

public sealed class RoomEnemyPoolEntryEntityConfiguration : IEntityTypeConfiguration<RoomEnemyPoolEntryEntity>
{
    public void Configure(EntityTypeBuilder<RoomEnemyPoolEntryEntity> builder)
    {
        RoomPoolEntryConfiguration.Configure(builder, "catalog_room_enemy_pool_entries");
        builder.Property(e => e.PoolId).HasColumnName("pool_id");
        builder.Property(e => e.EnemyDefinitionKey).HasColumnName("enemy_definition_key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.MinCount).HasColumnName("min_count");
        builder.Property(e => e.MaxCount).HasColumnName("max_count");
        builder.HasOne(e => e.Pool).WithMany(e => e.Entries).HasForeignKey(e => e.PoolId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoomRewardPoolEntityConfiguration : IEntityTypeConfiguration<RoomRewardPoolEntity>
{
    public void Configure(EntityTypeBuilder<RoomRewardPoolEntity> builder)
    {
        RoomPoolConfiguration.Configure(builder, "catalog_room_reward_pools");
    }
}

public sealed class RoomRewardPoolEntryEntityConfiguration : IEntityTypeConfiguration<RoomRewardPoolEntryEntity>
{
    public void Configure(EntityTypeBuilder<RoomRewardPoolEntryEntity> builder)
    {
        RoomPoolEntryConfiguration.Configure(builder, "catalog_room_reward_pool_entries");
        builder.Property(e => e.PoolId).HasColumnName("pool_id");
        builder.Property(e => e.RewardTemplateKey).HasColumnName("reward_template_key").HasMaxLength(160);
        builder.Property(e => e.ItemDefinitionKey).HasColumnName("item_definition_key").HasMaxLength(160);
        builder.HasOne(e => e.Pool).WithMany(e => e.Entries).HasForeignKey(e => e.PoolId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoomLawPoolEntityConfiguration : IEntityTypeConfiguration<RoomLawPoolEntity>
{
    public void Configure(EntityTypeBuilder<RoomLawPoolEntity> builder)
    {
        RoomPoolConfiguration.Configure(builder, "catalog_room_law_pools");
    }
}

public sealed class RoomLawPoolEntryEntityConfiguration : IEntityTypeConfiguration<RoomLawPoolEntryEntity>
{
    public void Configure(EntityTypeBuilder<RoomLawPoolEntryEntity> builder)
    {
        RoomPoolEntryConfiguration.Configure(builder, "catalog_room_law_pool_entries");
        builder.Property(e => e.PoolId).HasColumnName("pool_id");
        builder.Property(e => e.LawDefinitionKey).HasColumnName("law_definition_key").HasMaxLength(160).IsRequired();
        builder.HasOne(e => e.Pool).WithMany(e => e.Entries).HasForeignKey(e => e.PoolId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoomCursePoolEntityConfiguration : IEntityTypeConfiguration<RoomCursePoolEntity>
{
    public void Configure(EntityTypeBuilder<RoomCursePoolEntity> builder)
    {
        RoomPoolConfiguration.Configure(builder, "catalog_room_curse_pools");
    }
}

public sealed class RoomCursePoolEntryEntityConfiguration : IEntityTypeConfiguration<RoomCursePoolEntryEntity>
{
    public void Configure(EntityTypeBuilder<RoomCursePoolEntryEntity> builder)
    {
        RoomPoolEntryConfiguration.Configure(builder, "catalog_room_curse_pool_entries");
        builder.Property(e => e.PoolId).HasColumnName("pool_id");
        builder.Property(e => e.CurseDefinitionKey).HasColumnName("curse_definition_key").HasMaxLength(160).IsRequired();
        builder.HasOne(e => e.Pool).WithMany(e => e.Entries).HasForeignKey(e => e.PoolId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal static class RoomPoolConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder, string tableName)
        where TEntity : RoomPoolEntityBase
    {
        builder.ToTable(tableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}

internal static class RoomPoolEntryConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder, string tableName)
        where TEntity : RoomPoolEntryEntityBase
    {
        builder.ToTable(tableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Weight).HasColumnName("weight");
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.RequiredTag).HasColumnName("required_tag").HasMaxLength(128);
        builder.Property(e => e.ExcludedTag).HasColumnName("excluded_tag").HasMaxLength(128);
    }
}
