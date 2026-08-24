namespace Workforce.Modules.Attendance.Domain;

public enum ClockType
{
    In = 1,
    Out = 2,
    BreakStart = 3,
    BreakEnd = 4
}

public enum ClockSource
{
    BiometricDevice = 1,
    MobileApp = 2,
    WebPortal = 3,
    ApiIntegration = 4,
    ManualEntry = 5
}

public enum AttendanceStatus
{
    Open = 1,
    Exception = 2,
    Reviewed = 3,
    Approved = 4,
    Locked = 5
}

public enum AttendanceExceptionType
{
    MissingClockIn = 1,
    MissingClockOut = 2,
    UnexpectedAbsence = 3,
    ScheduleMismatch = 4,
    ExcessiveLateness = 5
}

public enum AttendanceExceptionStatus
{
    Open = 1,
    Resolved = 2,
    Waived = 3
}
