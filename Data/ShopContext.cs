using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TRPO.ElectronicsStore.Models;

namespace TRPO.ElectronicsStore.Data;

public partial class ShopContext : DbContext
{

    // Передаёт настройки подключения базовому классу Entity Framework.
    public ShopContext(DbContextOptions<ShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    // Связывает классы с таблицами SQL и задаёт поля, ключи, индексы и связи между таблицами.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");

            entity.HasIndex(row => row.Name, "UQ_brands_name").IsUnique();

            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.Name)
                .HasMaxLength(100)
                .UseCollation("Latin1_General_100_CI_AS")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasIndex(row => row.Name, "UQ_categories_name").IsUnique();

            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.Name)
                .HasMaxLength(100)
                .UseCollation("Latin1_General_100_CI_AS")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasIndex(row => row.BrandId, "IX_products_brand_id");

            entity.HasIndex(row => row.CategoryId, "IX_products_category_id");

            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.BrandId).HasColumnName("brand_id");
            entity.Property(row => row.CategoryId).HasColumnName("category_id");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
            entity.Property(row => row.Description)
                .HasMaxLength(2000)
                .HasColumnName("description");
            entity.Property(row => row.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(row => row.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(row => row.Rating)
                .HasColumnType("decimal(2, 1)")
                .HasColumnName("rating");
            entity.Property(row => row.Stock).HasColumnName("stock");

            entity.HasOne(product => product.Brand).WithMany(item => item.Products)
                .HasForeignKey(product => product.BrandId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_brands");

            entity.HasOne(product => product.Category).WithMany(item => item.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_categories");

            entity.HasMany(product => product.Tags).WithMany(item => item.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductTag",
                    tagLink => tagLink.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("FK_product_tags_tags"),
                    productLink => productLink.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .HasConstraintName("FK_product_tags_products"),
                    linkTable =>
                    {
                        linkTable.HasKey("ProductId", "TagId");
                        linkTable.ToTable("product_tags");
                        linkTable.HasIndex(new[] { "TagId" }, "IX_product_tags_tag_id");
                        linkTable.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        linkTable.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");

            entity.HasIndex(row => row.Name, "UQ_tags_name").IsUnique();

            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.Name)
                .HasMaxLength(100)
                .UseCollation("Latin1_General_100_CI_AS")
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    // Позволяет дополнить настройки таблиц в другом файле этого partial-класса без изменения сгенерированного метода.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
