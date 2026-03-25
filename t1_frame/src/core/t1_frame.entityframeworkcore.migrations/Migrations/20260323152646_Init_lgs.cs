using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t1_frame.entityframeworkcore.migrations.Migrations
{
    /// <inheritdoc />
    public partial class Init_lgs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "t1_user_account",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "t1_user_account");
        }
    }
}
