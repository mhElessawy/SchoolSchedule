using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolSchedule.Migrations
{
    /// <inheritdoc />
    public partial class _5period : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(max)", nullable: true, computedColumnSql: "(CAST([Grade] AS VARCHAR) + '/' + CAST([Section] AS VARCHAR))", stored: true),
                    ClassCode = table.Column<string>(type: "nvarchar(max)", nullable: true, computedColumnSql: "(CAST([Grade] AS VARCHAR) + CAST([Section] AS VARCHAR))", stored: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.ClassId);
                });

            migrationBuilder.CreateTable(
                name: "Periods",
                columns: table => new
                {
                    PeriodId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periods", x => x.PeriodId);
                });

            migrationBuilder.CreateTable(
                name: "SchoolDays",
                columns: table => new
                {
                    DayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DayNameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolDays", x => x.DayId);
                });

            migrationBuilder.CreateTable(
                name: "SchoolSettings",
                columns: table => new
                {
                    SettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EducationalZone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentAcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentSemester = table.Column<int>(type: "int", nullable: true),
                    SemesterStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodsPerDay = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSettings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    IsHeadOfDepartment = table.Column<bool>(type: "bit", nullable: false),
                    IsSupervisor = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherId);
                    table.ForeignKey(
                        name: "FK_Teachers_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    DayId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Semester = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_Schedule_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Schedule_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "PeriodId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Schedule_SchoolDays_DayId",
                        column: x => x.DayId,
                        principalTable: "SchoolDays",
                        principalColumn: "DayId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Schedule_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Schedule_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "TeacherId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Classes",
                columns: new[] { "ClassId", "CreatedDate", "Grade", "IsActive", "Section" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1430), 6, true, 1 },
                    { 2, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1438), 6, true, 2 },
                    { 3, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1441), 6, true, 3 },
                    { 4, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1444), 6, true, 4 },
                    { 5, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1447), 6, true, 5 },
                    { 6, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1451), 6, true, 6 },
                    { 7, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1454), 6, true, 7 },
                    { 8, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1456), 6, true, 8 },
                    { 9, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1459), 6, true, 9 },
                    { 10, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1464), 7, true, 1 },
                    { 11, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1467), 7, true, 2 },
                    { 12, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1469), 7, true, 3 },
                    { 13, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1472), 7, true, 4 },
                    { 14, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1475), 7, true, 5 },
                    { 15, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1478), 7, true, 6 },
                    { 16, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1481), 7, true, 7 },
                    { 17, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1484), 7, true, 8 },
                    { 18, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1488), 7, true, 9 },
                    { 19, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1491), 8, true, 1 },
                    { 20, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1494), 8, true, 2 },
                    { 21, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1497), 8, true, 3 },
                    { 22, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1499), 8, true, 4 },
                    { 23, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1502), 8, true, 5 },
                    { 24, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1505), 8, true, 6 },
                    { 25, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1508), 8, true, 7 },
                    { 26, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1511), 8, true, 8 },
                    { 27, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1514), 8, true, 9 },
                    { 28, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1517), 9, true, 1 },
                    { 29, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1520), 9, true, 2 },
                    { 30, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1523), 9, true, 3 },
                    { 31, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1525), 9, true, 4 },
                    { 32, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1528), 9, true, 5 },
                    { 33, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1531), 9, true, 6 },
                    { 34, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1535), 9, true, 7 },
                    { 35, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1538), 9, true, 8 },
                    { 36, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1541), 9, true, 9 }
                });

            migrationBuilder.InsertData(
                table: "Periods",
                columns: new[] { "PeriodId", "EndTime", "IsActive", "PeriodNumber", "StartTime" },
                values: new object[,]
                {
                    { 1, new TimeSpan(0, 8, 15, 0, 0), true, 1, new TimeSpan(0, 7, 30, 0, 0) },
                    { 2, new TimeSpan(0, 9, 0, 0, 0), true, 2, new TimeSpan(0, 8, 15, 0, 0) },
                    { 3, new TimeSpan(0, 9, 45, 0, 0), true, 3, new TimeSpan(0, 9, 0, 0, 0) },
                    { 4, new TimeSpan(0, 10, 45, 0, 0), true, 4, new TimeSpan(0, 10, 0, 0, 0) },
                    { 5, new TimeSpan(0, 11, 30, 0, 0), true, 5, new TimeSpan(0, 10, 45, 0, 0) },
                    { 6, new TimeSpan(0, 12, 15, 0, 0), true, 6, new TimeSpan(0, 11, 30, 0, 0) },
                    { 7, new TimeSpan(0, 13, 0, 0, 0), true, 7, new TimeSpan(0, 12, 15, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "SchoolDays",
                columns: new[] { "DayId", "DayName", "DayNameEn", "DayOrder", "IsActive" },
                values: new object[,]
                {
                    { 1, "الأحد", "Sunday", 1, true },
                    { 2, "الاثنين", "Monday", 2, true },
                    { 3, "الثلاثاء", "Tuesday", 3, true },
                    { 4, "الأربعاء", "Wednesday", 4, true },
                    { 5, "الخميس", "Thursday", 5, true }
                });

            migrationBuilder.InsertData(
                table: "SchoolSettings",
                columns: new[] { "SettingId", "CreatedDate", "CurrentAcademicYear", "CurrentSemester", "EducationalZone", "PeriodsPerDay", "SchoolName", "SemesterStartDate", "UpdatedDate" },
                values: new object[] { 1, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(659), "2025/2026", 2, "الادارة العامة لمنطقة حولى التعليمية", 7, "مدرسة عبدالمحسن الخرافى المتوسطة للبنين", new DateTime(2026, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "SubjectId", "CreatedDate", "IsActive", "SubjectName", "SubjectNameEn" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1255), true, "التربية الإسلامية", "Islamic Education" },
                    { 2, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1265), true, "اللغة العربية", "Arabic Language" },
                    { 3, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1268), true, "اللغة الإنجليزية", "English Language" },
                    { 4, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1271), true, "الرياضيات", "Mathematics" },
                    { 5, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1369), true, "العلوم", "Science" },
                    { 6, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1373), true, "الاجتماعيات", "Social Studies" },
                    { 7, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1376), true, "الحاسوب", "Computer" },
                    { 8, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1378), true, "التربية البدنية", "Physical Education" },
                    { 9, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1381), true, "التربية الموسيقية", "Music" },
                    { 10, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1383), true, "التربية الفنية", "Art" },
                    { 11, new DateTime(2026, 3, 2, 20, 19, 47, 149, DateTimeKind.Local).AddTicks(1386), true, "الدراسات العملية", "Practical Studies" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Grade_Section",
                table: "Classes",
                columns: new[] { "Grade", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Periods_PeriodNumber",
                table: "Periods",
                column: "PeriodNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_ClassId_DayId_PeriodId_AcademicYear_Semester",
                table: "Schedule",
                columns: new[] { "ClassId", "DayId", "PeriodId", "AcademicYear", "Semester" },
                unique: true,
                filter: "[AcademicYear] IS NOT NULL AND [Semester] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_DayId",
                table: "Schedule",
                column: "DayId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_PeriodId",
                table: "Schedule",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_SubjectId",
                table: "Schedule",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_TeacherId",
                table: "Schedule",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDays_DayOrder",
                table: "SchoolDays",
                column: "DayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SubjectId",
                table: "Teachers",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedule");

            migrationBuilder.DropTable(
                name: "SchoolSettings");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Periods");

            migrationBuilder.DropTable(
                name: "SchoolDays");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
