using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduScan.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentLrn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Lrn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Track = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GuardianPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActiveUser = table.Column<bool>(type: "bit", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "FullName", "Grade", "GuardianPhone", "IsActiveUser", "LastActiveAt", "Lrn", "Section", "Track" },
                values: new object[] { 1, "DELA CRUZ, JUAN MIGUEL B.", "Grade 11", "0917-849-2104", false, null, "108492190042", "Sampaguita", "TVL - Agri-Fishery Arts" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_Lrn",
                table: "Students",
                column: "Lrn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceLogs");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
