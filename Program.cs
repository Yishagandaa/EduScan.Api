using Microsoft.EntityFrameworkCore;
using EduScan.Api.Data;
using EduScan.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=EduScanDb;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure CORS policy so Expo Go, react-native-webview, and web dashboard clients can interact seamlessly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EduScan Minimal API", Version = "v1" });
});

var app = builder.Build();

// Ensure Database is created and initial seed applied on application start
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating/migrating the database.");
    }
}

// Enable Swagger UI in development and production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduScan API v1");
    c.RoutePrefix = "swagger";
});

// Enable CORS
app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/", () => Results.Ok(new
{
    app = "EduScan.Api",
    status = "Online",
    time = DateTime.Now,
    endpoints = new[]
    {
        "POST /api/auth/login",
        "POST /api/attendance/scan",
        "GET /api/admin/metrics",
        "GET /api/student/{lrn}/history",
        "GET /api/students",
        "GET /swagger"
    }
})).WithName("Root").WithTags("System");

app.MapGet("/api/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        status = "Healthy",
        databaseConnected = canConnect,
        serverTime = DateTime.Now
    });
}).WithName("HealthCheck").WithTags("System");

// ==========================================
// 1. AUTHENTICATION / APP USER REGISTRATION
// ==========================================
app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Lrn))
    {
        return Results.BadRequest(new { message = "LRN is required." });
    }

    var student = await db.Students.FirstOrDefaultAsync(s => s.Lrn == request.Lrn.Trim());
    if (student == null)
    {
        return Results.NotFound(new { message = $"Student with LRN '{request.Lrn}' not found." });
    }

    // Mark student as active app user and update last active time without creating individual login logs
    student.IsActiveUser = true;
    student.LastActiveAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        success = true,
        message = "Student verified & logged in successfully.",
        student
    });
}).WithName("StudentLogin").WithTags("Auth");

// ==========================================
// 2. ATTENDANCE SCAN PROCESSING
// ==========================================
app.MapPost("/api/attendance/scan", async (ScanRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Lrn))
    {
        return Results.BadRequest(new { message = "LRN is required for attendance scan." });
    }

    var student = await db.Students.FirstOrDefaultAsync(s => s.Lrn == request.Lrn.Trim());
    if (student == null)
    {
        return Results.NotFound(new { message = $"Student with LRN '{request.Lrn}' not registered in database." });
    }

    // Evaluate server time against the 07:30:00 AM cutoff
    var now = DateTime.Now;
    var cutoff = new TimeSpan(7, 30, 0);
    var status = (now.TimeOfDay <= cutoff) ? "ON TIME" : "LATE";
    var gate = string.IsNullOrWhiteSpace(request.GateNumber) ? "Gate 1" : request.GateNumber.Trim();

    var log = new AttendanceLog
    {
        StudentLrn = student.Lrn,
        ScannedAt = now,
        Status = status,
        GateNumber = gate
    };

    db.AttendanceLogs.Add(log);
    await db.SaveChangesAsync();

    var formattedTime = now.ToString("hh:mm:ss tt");

    return Results.Ok(new ScanResponse(
        Success: true,
        Message: $"Gate attendance recorded: {status}",
        Status: status,
        FormattedTime: formattedTime,
        ScannedAt: now,
        Lrn: student.Lrn,
        FullName: student.FullName,
        Grade: student.Grade,
        Section: student.Section,
        Track: student.Track,
        GateNumber: log.GateNumber
    ));
}).WithName("ScanAttendance").WithTags("Attendance");

// ==========================================
// 3. ADMIN METRICS DASHBOARD
// ==========================================
app.MapGet("/api/admin/metrics", async (AppDbContext db) =>
{
    var totalStudents = await db.Students.CountAsync();
    var activeUsers = await db.Students.CountAsync(s => s.IsActiveUser);

    var today = DateTime.Today;
    var presentToday = await db.AttendanceLogs
        .Where(a => a.ScannedAt.Date == today && a.Status == "ON TIME")
        .Select(a => a.StudentLrn)
        .Distinct()
        .CountAsync();

    var lateToday = await db.AttendanceLogs
        .Where(a => a.ScannedAt.Date == today && a.Status == "LATE")
        .Select(a => a.StudentLrn)
        .Distinct()
        .CountAsync();

    var todayLogs = await db.AttendanceLogs
        .Where(a => a.ScannedAt.Date == today)
        .OrderByDescending(a => a.ScannedAt)
        .Take(50)
        .ToListAsync();

    var studentMap = await db.Students.ToDictionaryAsync(s => s.Lrn, s => s);

    var recentLogs = todayLogs.Select(l =>
    {
        studentMap.TryGetValue(l.StudentLrn, out var st);
        return new RecentAttendanceLogDto(
            Id: l.Id,
            Lrn: l.StudentLrn,
            FullName: st?.FullName ?? "Unknown Student",
            Grade: st?.Grade ?? "Grade 11",
            Section: st?.Section ?? "Section",
            GateNumber: l.GateNumber,
            Status: l.Status,
            ScannedAt: l.ScannedAt
        );
    }).ToList();

    return Results.Ok(new MetricsResponse(
        TotalStudents: totalStudents,
        ActiveUsers: activeUsers,
        PresentToday: presentToday,
        LateToday: lateToday,
        RecentLogs: recentLogs
    ));
}).WithName("GetAdminMetrics").WithTags("Admin");

// ==========================================
// 4. STUDENT ATTENDANCE HISTORY
// ==========================================
app.MapGet("/api/student/{lrn}/history", async (string lrn, AppDbContext db) =>
{
    var trimmedLrn = lrn.Trim();
    var student = await db.Students.FirstOrDefaultAsync(s => s.Lrn == trimmedLrn);
    if (student == null)
    {
        return Results.NotFound(new { message = $"Student with LRN '{lrn}' not found." });
    }

    var history = await db.AttendanceLogs
        .Where(a => a.StudentLrn == trimmedLrn)
        .OrderByDescending(a => a.ScannedAt)
        .ToListAsync();

    return Results.Ok(new StudentHistoryResponse(
        Student: student,
        AttendanceHistory: history
    ));
}).WithName("GetStudentHistory").WithTags("Student");

// ==========================================
// 5. STUDENT DIRECTORY (LIST & CREATE)
// ==========================================
app.MapGet("/api/students", async (AppDbContext db) =>
{
    var students = await db.Students.OrderBy(s => s.FullName).ToListAsync();
    return Results.Ok(students);
}).WithName("GetAllStudents").WithTags("Student");

app.MapPost("/api/students", async (Student student, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(student.Lrn) || string.IsNullOrWhiteSpace(student.FullName))
    {
        return Results.BadRequest(new { message = "LRN and FullName are required." });
    }

    var existing = await db.Students.AnyAsync(s => s.Lrn == student.Lrn.Trim());
    if (existing)
    {
        return Results.Conflict(new { message = $"Student with LRN '{student.Lrn}' already exists." });
    }

    student.Lrn = student.Lrn.Trim();
    student.FullName = student.FullName.Trim();
    db.Students.Add(student);
    await db.SaveChangesAsync();

    return Results.Created($"/api/student/{student.Lrn}/history", student);
}).WithName("CreateStudent").WithTags("Student");

// ==========================================
// HOST BINDING FOR EXPO GO, RENDER & LOCAL NETWORK
// ==========================================
var listenUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(listenUrl))
{
    app.Run();
}
else
{
    app.Run("http://0.0.0.0:5005");
}
