using System;
using System.Collections.Generic;
using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRUDAPI.Infrastructure.Persistence.context;

public partial class BaseContext : DbContext
{
    

    public BaseContext(DbContextOptions<BaseContext> options)
        : base(options)
    {
    }


    public virtual DbSet<UsuarioAU> UsuariosAU { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");


        modelBuilder.Entity<UsuarioAU>(entity =>
        {
            // Cambiar nombre de constraint
            entity.HasKey(e => e.Id).HasName("PK_UsuariosAU");

            // Cambiar nombre de tabla
            entity.ToTable("UsuariosAU");

            entity.Property(e => e.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("Nombre")
                .IsUnicode(true);

            entity.Property(e => e.Constrasena)
                .HasMaxLength(300)
                .HasColumnName("Contrasena") // Corrección ortográfica
                .IsUnicode(true);

            entity.Property(e => e.Email)
                .HasMaxLength(300)
                .HasColumnName("Email")
                .IsUnicode(true);

            // Agregar índice único para email
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_UsuariosAU_Email");
        });

        // Configurar RefreshToken si no está configurado
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_RefreshTokens");
            entity.ToTable("RefreshTokens");

            entity.Property(e => e.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            // Agregar configuraciones adicionales según tu entidad RefreshToken
            // entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            // entity.Property(e => e.ExpiryDate).IsRequired();
            // entity.Property(e => e.UserId).IsRequired();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
