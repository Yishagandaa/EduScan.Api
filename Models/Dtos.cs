namespace EduScan.Api.Models;

public record LoginRequest(string Lrn);

public record ScanRequest(string Lrn, string? GateNumber = "Gate 1");

public record ScanResponse(
    bool Success,
    string Message,
    string Status,
    string FormattedTime,
    DateTime ScannedAt,
    string Lrn,
    string FullName,
    string Grade,
    string Section,
    string Track,
    string GateNumber
);

public record RecentAttendanceLogDto(
    int Id,
    string Lrn,
    string FullName,
    string Grade,
    string Section,
    string GateNumber,
    string Status,
    DateTime ScannedAt
);

public record MetricsResponse(
    int TotalStudents,
    int ActiveUsers,
    int PresentToday,
    int LateToday,
    List<RecentAttendanceLogDto>? RecentLogs = null
);

public record StudentHistoryResponse(
    Student Student,
    List<AttendanceLog> AttendanceHistory
);

