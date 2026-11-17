using System;
using System.Collections.Generic;
using EstateAgency.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Data;

public partial class EstateContext : DbContext
{
    public EstateContext(DbContextOptions<EstateContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<PropertyStatus> PropertyStatuses { get; set; }

    public virtual DbSet<PropertyType> PropertyTypes { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestStatus> RequestStatuses { get; set; }

    public virtual DbSet<RequestType> RequestTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Viewing> Viewings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("PK__Properti__735BA4631CB49B9C");

            entity.HasIndex(e => e.PropertyCode, "UQ__Properti__2CD96ABC7641B3BA").IsUnique();

            entity.HasIndex(e => e.Address, "UQ__Properti__751C8E5499ABC312").IsUnique();

            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.Address)
                .HasMaxLength(250)
                .HasColumnName("address");
            entity.Property(e => e.Area)
                .HasColumnType("decimal(8, 2)")
                .HasColumnName("area");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FreeViewings).HasColumnName("free_viewings");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(150)
                .HasColumnName("image_path");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("price");
            entity.Property(e => e.PropertyCode)
                .HasMaxLength(20)
                .HasColumnName("property_code");
            entity.Property(e => e.Rooms).HasColumnName("rooms");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Manager).WithMany(p => p.Properties)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__manag__6477ECF3");

            entity.HasOne(d => d.Status).WithMany(p => p.Properties)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__statu__6383C8BA");

            entity.HasOne(d => d.Type).WithMany(p => p.Properties)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__type___628FA481");
        });

        modelBuilder.Entity<PropertyStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Property__3683B531E4BE7745");

            entity.HasIndex(e => e.StatusName, "UQ__Property__501B37539DBF6598").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<PropertyType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__Property__2C0005989634F471");

            entity.HasIndex(e => e.TypeName, "UQ__Property__543C4FD9F267FD34").IsUnique();

            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Requests__18D3B90F2946D08E");

            entity.HasIndex(e => new { e.PropertyId, e.CreatedAt }, "ix_request_property_date");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CommissionAmount)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("commission_amount");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Client).WithMany(p => p.Requests)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requests__client__6D0D32F4");

            entity.HasOne(d => d.Property).WithMany(p => p.Requests)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requests__proper__6E01572D");

            entity.HasOne(d => d.Status).WithMany(p => p.Requests)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requests__status__6FE99F9F");

            entity.HasOne(d => d.Type).WithMany(p => p.Requests)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requests__type_i__6EF57B66");
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__RequestS__3683B531B135254F");

            entity.HasIndex(e => e.StatusName, "UQ__RequestS__501B37532F406CC1").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__RequestT__2C00059841CAC12F");

            entity.HasIndex(e => e.TypeName, "UQ__RequestT__543C4FD9063B2BB2").IsUnique();

            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.CommissionRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("commission_rate");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CCBE387C60");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__783254B16ABCBD97").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F32B9ED9A");

            entity.HasIndex(e => e.Login, "UQ__Users__7838F272E34F3517").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164DB8B6D77").IsUnique();

            entity.HasIndex(e => e.Phone, "UQ__Users__B43B145FA1C6D244").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(60)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .HasColumnName("last_name");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(60)
                .HasColumnName("middle_name");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__role_id__5CD6CB2B");
        });

        modelBuilder.Entity<Viewing>(entity =>
        {
            entity.HasKey(e => e.ViewingId).HasName("PK__Viewings__F7AD429B7E402BB3");

            entity.HasIndex(e => new { e.ManagerId, e.ScheduledAt }, "uq_viewings_manager_time").IsUnique();

            entity.Property(e => e.ViewingId).HasColumnName("viewing_id");
            entity.Property(e => e.ActualEndAt)
                .HasPrecision(0)
                .HasColumnName("actual_end_at");
            entity.Property(e => e.ActualStartAt)
                .HasPrecision(0)
                .HasColumnName("actual_start_at");
            entity.Property(e => e.Feedback).HasColumnName("feedback");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.Result)
                .HasMaxLength(100)
                .HasColumnName("result");
            entity.Property(e => e.ScheduledAt)
                .HasPrecision(0)
                .HasColumnName("scheduled_at");

            entity.HasOne(d => d.Manager).WithMany(p => p.Viewings)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Viewings__manage__778AC167");

            entity.HasOne(d => d.Request).WithMany(p => p.Viewings)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK__Viewings__reques__76969D2E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
