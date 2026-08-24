using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Domain;

public class WorkSchedule
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public TimeOnly ShiftStartTime { get; private set; }
    public TimeOnly ShiftEndTime { get; private set; }
    public int GracePeriodMinutes { get; private set; }
    public string TimeZoneId { get; private set; }
    public bool CrossesMidnight { get; private set; }
    public EffectivePeriod EffectivePeriod { get; private set; }
    public bool IsActive { get; private set; }

    private WorkSchedule() 
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        TimeZoneId = "UTC";
        EffectivePeriod = new EffectivePeriod(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public WorkSchedule(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        string nameEn,
        string nameAr,
        TimeOnly shiftStartTime,
        TimeOnly shiftEndTime,
        int gracePeriodMinutes,
        string timeZoneId,
        EffectivePeriod effectivePeriod,
        bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("NameEn is required.", nameof(nameEn));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        ShiftStartTime = shiftStartTime;
        ShiftEndTime = shiftEndTime;
        GracePeriodMinutes = Math.Max(0, gracePeriodMinutes);
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim();
        CrossesMidnight = shiftEndTime < shiftStartTime;
        EffectivePeriod = effectivePeriod;
        IsActive = isActive;
    }

    public int GetScheduledDurationMinutes()
    {
        if (CrossesMidnight)
        {
            var endMinutes = ShiftEndTime.Hour * 60 + ShiftEndTime.Minute;
            var startMinutes = ShiftStartTime.Hour * 60 + ShiftStartTime.Minute;
            return (1440 - startMinutes) + endMinutes;
        }

        return (ShiftEndTime.Hour * 60 + ShiftEndTime.Minute) - (ShiftStartTime.Hour * 60 + ShiftStartTime.Minute);
    }
}
