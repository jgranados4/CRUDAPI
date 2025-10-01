using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUDAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixRefresh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_UsuarioAUResponseDto_UsuarioId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UsuarioAUResponseDto");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_UsuariosAU_UsuarioId",
                table: "RefreshTokens",
                column: "UsuarioId",
                principalTable: "UsuariosAU",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_UsuariosAU_UsuarioId",
                table: "RefreshTokens");

            migrationBuilder.CreateTable(
                name: "UsuarioAUResponseDto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioAUResponseDto", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_UsuarioAUResponseDto_UsuarioId",
                table: "RefreshTokens",
                column: "UsuarioId",
                principalTable: "UsuarioAUResponseDto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
