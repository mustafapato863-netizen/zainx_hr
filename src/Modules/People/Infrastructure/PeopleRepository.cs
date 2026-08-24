using System;
using System.Collections.Generic;
using System.Text.Json;
using Npgsql;
using Workforce.Modules.People.Application;
using Workforce.Modules.People.Domain;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.People.Infrastructure;

public class PeopleRepository
{
    private readonly string _connectionString;
    private readonly IPiiEncryptionService _piiEncryptionService;

    public PeopleRepository(string connectionString, IPiiEncryptionService? piiEncryptionService = null)
    {
        _connectionString = connectionString;
        _piiEncryptionService = piiEncryptionService ?? new AesPiiEncryptionService();
    }

    public async Task<PagedResult<EmployeeSummaryDto>> QueryDirectoryAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        string? searchTerm,
        Guid? departmentId,
        string? status,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (pageNumber - 1) * pageSize;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var whereClause = "WHERE e.tenant_id = @tenantId";
        if (legalEntityId.HasValue)
        {
            whereClause += " AND e.legal_entity_id = @legalEntityId";
        }
        
        string? searchHash = null;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmedSearch = searchTerm.Trim();
            if (trimmedSearch.Length >= 6 && long.TryParse(trimmedSearch, out _))
            {
                searchHash = _piiEncryptionService.ComputeSearchHash(trimmedSearch);
            }

            whereClause += @" AND (
                p.first_name_en ILIKE @search OR 
                p.last_name_en ILIKE @search OR 
                p.first_name_ar ILIKE @search OR 
                p.last_name_ar ILIKE @search OR 
                e.employee_number ILIKE @search OR
                p.primary_email ILIKE @search" + 
                (searchHash != null ? " OR p.national_identifier_hash = @searchHash" : "") + 
            ")";
        }
        if (departmentId.HasValue)
        {
            whereClause += " AND a.organization_unit_id = @departmentId";
        }
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, true, out var statusEnum))
        {
            whereClause += " AND e.status = @status";
        }

        var countSql = $@"
            SELECT COUNT(*)
            FROM people.employments e
            INNER JOIN people.persons p ON e.person_id = p.id
            LEFT JOIN people.employment_assignments a ON e.id = a.employment_id AND a.is_current = TRUE
            {whereClause};
        ";

        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) countCmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm)) countCmd.Parameters.AddWithValue("search", $"%{searchTerm}%");
        if (searchHash != null) countCmd.Parameters.AddWithValue("searchHash", searchHash);
        if (departmentId.HasValue) countCmd.Parameters.AddWithValue("departmentId", departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, true, out var stVal))
        {
            countCmd.Parameters.AddWithValue("status", (int)stVal);
        }

        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        var querySql = $@"
            SELECT 
                e.id, e.tenant_id, e.legal_entity_id, e.employee_number,
                p.first_name_en, p.last_name_en, p.first_name_ar, p.last_name_ar,
                p.primary_email, p.phone_number, p.masked_national_identifier,
                e.status, e.hire_date, e.row_version,
                COALESCE(ou.name_en, 'Unassigned') as dept_en,
                COALESCE(ou.name_ar, 'غير محدد') as dept_ar,
                COALESCE(a.job_title_en, 'N/A') as job_en,
                COALESCE(a.job_title_ar, 'N/A') as job_ar,
                COALESCE(loc.name_en, 'HQ') as loc_en
            FROM people.employments e
            INNER JOIN people.persons p ON e.person_id = p.id
            LEFT JOIN people.employment_assignments a ON e.id = a.employment_id AND a.is_current = TRUE
            LEFT JOIN organization.organization_units ou ON a.organization_unit_id = ou.id
            LEFT JOIN organization.locations loc ON a.location_id = loc.id
            {whereClause}
            ORDER BY e.created_at DESC
            LIMIT @limit OFFSET @offset;
        ";

        await using var cmd = new NpgsqlCommand(querySql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm)) cmd.Parameters.AddWithValue("search", $"%{searchTerm}%");
        if (searchHash != null) cmd.Parameters.AddWithValue("searchHash", searchHash);
        if (departmentId.HasValue) cmd.Parameters.AddWithValue("departmentId", departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, true, out var stVal2))
        {
            cmd.Parameters.AddWithValue("status", (int)stVal2);
        }
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", offset);

        var items = new List<EmployeeSummaryDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new EmployeeSummaryDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                EmployeeNumber = reader.GetString(3),
                FirstNameEn = reader.GetString(4),
                LastNameEn = reader.GetString(5),
                FirstNameAr = reader.GetString(6),
                LastNameAr = reader.GetString(7),
                FullNameEn = $"{reader.GetString(4)} {reader.GetString(5)}",
                FullNameAr = $"{reader.GetString(6)} {reader.GetString(7)}",
                PrimaryEmail = reader.GetString(8),
                PhoneNumber = reader.GetString(9),
                MaskedNationalId = reader.GetString(10),
                Status = ((EmploymentStatus)reader.GetInt32(11)).ToString(),
                HireDate = DateOnly.FromDateTime(reader.GetDateTime(12)).ToString("yyyy-MM-dd"),
                RowVersion = (uint)reader.GetInt32(13),
                DepartmentNameEn = reader.GetString(14),
                DepartmentNameAr = reader.GetString(15),
                JobTitleEn = reader.GetString(16),
                JobTitleAr = reader.GetString(17),
                LocationNameEn = reader.GetString(18)
            });
        }

        return new PagedResult<EmployeeSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeProfileDto?> GetEmployeeProfileAsync(
        Guid employmentId,
        TenantId tenantId,
        LegalEntityId? legalEntityId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var empSql = @"
            SELECT 
                e.id, e.tenant_id, e.person_id, e.legal_entity_id, e.employee_number,
                p.first_name_en, p.last_name_en, p.first_name_ar, p.last_name_ar,
                p.gender, p.nationality, p.date_of_birth, p.masked_national_identifier,
                p.primary_email, p.phone_number,
                e.status, e.hire_date, e.probation_end_date, e.termination_date, e.termination_reason,
                e.row_version
            FROM people.employments e
            INNER JOIN people.persons p ON e.person_id = p.id
            WHERE e.id = @id AND e.tenant_id = @tenantId
        ";

        if (legalEntityId.HasValue)
        {
            empSql += " AND e.legal_entity_id = @legalEntityId";
        }

        await using var cmd = new NpgsqlCommand(empSql, conn);
        cmd.Parameters.AddWithValue("id", employmentId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        EmployeeProfileDto? profile = null;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                var rawDob = DateOnly.FromDateTime(reader.GetDateTime(11));
                var maskedNatId = reader.GetString(12);

                profile = new EmployeeProfileDto
                {
                    Id = reader.GetGuid(0),
                    TenantId = reader.GetGuid(1).ToString(),
                    PersonId = reader.GetGuid(2),
                    LegalEntityId = reader.GetGuid(3).ToString(),
                    EmployeeNumber = reader.GetString(4),
                    FirstNameEn = reader.GetString(5),
                    LastNameEn = reader.GetString(6),
                    FirstNameAr = reader.GetString(7),
                    LastNameAr = reader.GetString(8),
                    FullNameEn = $"{reader.GetString(5)} {reader.GetString(6)}",
                    FullNameAr = $"{reader.GetString(7)} {reader.GetString(8)}",
                    Gender = reader.GetString(9),
                    Nationality = reader.GetString(10),
                    MaskedDateOfBirth = _piiEncryptionService.MaskDateOfBirth(rawDob),
                    MaskedNationalId = maskedNatId,
                    PrimaryEmail = reader.GetString(13),
                    PhoneNumber = reader.GetString(14),
                    Status = ((EmploymentStatus)reader.GetInt32(15)).ToString(),
                    HireDate = DateOnly.FromDateTime(reader.GetDateTime(16)).ToString("yyyy-MM-dd"),
                    ProbationEndDate = reader.IsDBNull(17) ? null : DateOnly.FromDateTime(reader.GetDateTime(17)).ToString("yyyy-MM-dd"),
                    TerminationDate = reader.IsDBNull(18) ? null : DateOnly.FromDateTime(reader.GetDateTime(18)).ToString("yyyy-MM-dd"),
                    TerminationReason = reader.IsDBNull(19) ? null : reader.GetString(19),
                    RowVersion = (uint)reader.GetInt32(20)
                };
            }
        }

        if (profile == null) return null;

        // Fetch Assignments history
        const string assignSql = @"
            SELECT 
                a.id, a.employment_id, a.organization_unit_id, ou.name_en as dept_en, ou.name_ar as dept_ar,
                a.position_id, a.location_id, loc.name_en as loc_en, a.manager_employment_id,
                a.job_title_en, a.job_title_ar, a.effective_from, a.effective_to, a.is_current
            FROM people.employment_assignments a
            LEFT JOIN organization.organization_units ou ON a.organization_unit_id = ou.id
            LEFT JOIN organization.locations loc ON a.location_id = loc.id
            WHERE a.employment_id = @id
            ORDER BY a.effective_from DESC, a.created_at DESC;
        ";

        await using var assignCmd = new NpgsqlCommand(assignSql, conn);
        assignCmd.Parameters.AddWithValue("id", employmentId);

        await using (var assignReader = await assignCmd.ExecuteReaderAsync(ct))
        {
            while (await assignReader.ReadAsync(ct))
            {
                var assign = new EmployeeAssignmentDto
                {
                    Id = assignReader.GetGuid(0),
                    EmploymentId = assignReader.GetGuid(1),
                    OrganizationUnitId = assignReader.GetGuid(2),
                    DepartmentNameEn = assignReader.IsDBNull(3) ? "Unassigned" : assignReader.GetString(3),
                    DepartmentNameAr = assignReader.IsDBNull(4) ? "غير محدد" : assignReader.GetString(4),
                    PositionId = assignReader.IsDBNull(5) ? null : assignReader.GetGuid(5),
                    LocationId = assignReader.IsDBNull(6) ? null : assignReader.GetGuid(6),
                    LocationNameEn = assignReader.IsDBNull(7) ? "HQ" : assignReader.GetString(7),
                    ManagerEmploymentId = assignReader.IsDBNull(8) ? null : assignReader.GetGuid(8),
                    JobTitleEn = assignReader.GetString(9),
                    JobTitleAr = assignReader.GetString(10),
                    EffectiveFrom = DateOnly.FromDateTime(assignReader.GetDateTime(11)).ToString("yyyy-MM-dd"),
                    EffectiveTo = assignReader.IsDBNull(12) ? null : DateOnly.FromDateTime(assignReader.GetDateTime(12)).ToString("yyyy-MM-dd"),
                    IsCurrent = assignReader.GetBoolean(13)
                };

                if (assign.IsCurrent && profile.CurrentAssignment == null)
                {
                    profile.CurrentAssignment = assign;
                }
                profile.AssignmentHistory.Add(assign);
            }
        }

        return profile;
    }

    public async Task CreateEmployeeAsync(
        Person person,
        Employment employment,
        EmploymentAssignment assignment,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Insert Person with encrypted PII persistence
            const string personSql = @"
                INSERT INTO people.persons (
                    id, tenant_id, first_name_en, last_name_en, first_name_ar, last_name_ar,
                    date_of_birth, gender, nationality, national_identifier_encrypted, national_identifier_hash, masked_national_identifier,
                    primary_email, phone_number, created_at, updated_at
                ) VALUES (
                    @id, @tenantId, @fnEn, @lnEn, @fnAr, @lnAr,
                    @dob, @gender, @nationality, @natEnc, @natHash, @natMask,
                    @email, @phone, @createdAt, @updatedAt
                );
            ";
            await using var personCmd = new NpgsqlCommand(personSql, conn, tx);
            personCmd.Parameters.AddWithValue("id", person.Id);
            personCmd.Parameters.AddWithValue("tenantId", person.TenantId.Value);
            personCmd.Parameters.AddWithValue("fnEn", person.FirstNameEn);
            personCmd.Parameters.AddWithValue("lnEn", person.LastNameEn);
            personCmd.Parameters.AddWithValue("fnAr", person.FirstNameAr);
            personCmd.Parameters.AddWithValue("lnAr", person.LastNameAr);
            personCmd.Parameters.AddWithValue("dob", person.DateOfBirth.ToDateTime(TimeOnly.MinValue));
            personCmd.Parameters.AddWithValue("gender", person.Gender);
            personCmd.Parameters.AddWithValue("nationality", person.Nationality);
            personCmd.Parameters.AddWithValue("natEnc", person.NationalIdentifierEncrypted);
            personCmd.Parameters.AddWithValue("natHash", person.NationalIdentifierHash);
            personCmd.Parameters.AddWithValue("natMask", person.MaskedNationalIdentifier);
            personCmd.Parameters.AddWithValue("email", person.PrimaryEmail);
            personCmd.Parameters.AddWithValue("phone", person.PhoneNumber);
            personCmd.Parameters.AddWithValue("createdAt", person.CreatedAt);
            personCmd.Parameters.AddWithValue("updatedAt", person.UpdatedAt);
            await personCmd.ExecuteNonQueryAsync(ct);

            // Insert Employment
            const string empSql = @"
                INSERT INTO people.employments (
                    id, tenant_id, person_id, legal_entity_id, employee_number,
                    hire_date, probation_end_date, status, created_at, updated_at, row_version
                ) VALUES (
                    @id, @tenantId, @personId, @legalEntityId, @empNo,
                    @hireDate, @probationEnd, @status, @createdAt, @updatedAt, @rowVersion
                );
            ";
            await using var empCmd = new NpgsqlCommand(empSql, conn, tx);
            empCmd.Parameters.AddWithValue("id", employment.Id);
            empCmd.Parameters.AddWithValue("tenantId", employment.TenantId.Value);
            empCmd.Parameters.AddWithValue("personId", employment.PersonId);
            empCmd.Parameters.AddWithValue("legalEntityId", employment.LegalEntityId.Value);
            empCmd.Parameters.AddWithValue("empNo", employment.EmployeeNumber);
            empCmd.Parameters.AddWithValue("hireDate", employment.HireDate.ToDateTime(TimeOnly.MinValue));
            empCmd.Parameters.AddWithValue("probationEnd", employment.ProbationEndDate.HasValue ? employment.ProbationEndDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            empCmd.Parameters.AddWithValue("status", (int)employment.Status);
            empCmd.Parameters.AddWithValue("createdAt", employment.CreatedAt);
            empCmd.Parameters.AddWithValue("updatedAt", employment.UpdatedAt);
            empCmd.Parameters.AddWithValue("rowVersion", (int)employment.RowVersion);
            await empCmd.ExecuteNonQueryAsync(ct);

            // Insert Initial Assignment
            const string assignSql = @"
                INSERT INTO people.employment_assignments (
                    id, employment_id, organization_unit_id, position_id, location_id, manager_employment_id,
                    job_title_en, job_title_ar, effective_from, effective_to, is_current, created_at
                ) VALUES (
                    @id, @empId, @unitId, @posId, @locId, @mgrId,
                    @jobEn, @jobAr, @effFrom, @effTo, @isCurrent, @createdAt
                );
            ";
            await using var assignCmd = new NpgsqlCommand(assignSql, conn, tx);
            assignCmd.Parameters.AddWithValue("id", assignment.Id);
            assignCmd.Parameters.AddWithValue("empId", assignment.EmploymentId);
            assignCmd.Parameters.AddWithValue("unitId", assignment.OrganizationUnitId);
            assignCmd.Parameters.AddWithValue("posId", (object?)assignment.PositionId ?? DBNull.Value);
            assignCmd.Parameters.AddWithValue("locId", (object?)assignment.LocationId ?? DBNull.Value);
            assignCmd.Parameters.AddWithValue("mgrId", (object?)assignment.ManagerEmploymentId ?? DBNull.Value);
            assignCmd.Parameters.AddWithValue("jobEn", assignment.JobTitleEn);
            assignCmd.Parameters.AddWithValue("jobAr", assignment.JobTitleAr);
            assignCmd.Parameters.AddWithValue("effFrom", assignment.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            assignCmd.Parameters.AddWithValue("effTo", assignment.EffectiveTo.HasValue ? assignment.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            assignCmd.Parameters.AddWithValue("isCurrent", assignment.IsCurrent);
            assignCmd.Parameters.AddWithValue("createdAt", assignment.CreatedAt);
            await assignCmd.ExecuteNonQueryAsync(ct);

            // Atomic Outbox Domain Event
            var createdEvent = new EmployeeCreatedEvent(
                Guid.NewGuid(),
                employment.Id,
                employment.TenantId,
                employment.LegalEntityId,
                employment.EmployeeNumber,
                $"{person.FirstNameEn} {person.LastNameEn}",
                $"{person.FirstNameAr} {person.LastNameAr}",
                DateTime.UtcNow
            );

            const string outboxSql = @"
                INSERT INTO people.outbox_messages (
                    id, tenant_id, event_type, aggregate_type, aggregate_id, payload, occurred_at
                ) VALUES (
                    @id, @tenantId, @eventType, @aggType, @aggId, @payload::jsonb, @occurredAt
                );
            ";
            await using var outboxCmd = new NpgsqlCommand(outboxSql, conn, tx);
            outboxCmd.Parameters.AddWithValue("id", createdEvent.EventId);
            outboxCmd.Parameters.AddWithValue("tenantId", employment.TenantId.Value);
            outboxCmd.Parameters.AddWithValue("eventType", nameof(EmployeeCreatedEvent));
            outboxCmd.Parameters.AddWithValue("aggType", "Employment");
            outboxCmd.Parameters.AddWithValue("aggId", employment.Id);
            outboxCmd.Parameters.AddWithValue("payload", JsonSerializer.Serialize(createdEvent));
            outboxCmd.Parameters.AddWithValue("occurredAt", createdEvent.OccurredAt);
            await outboxCmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public class IdempotencyResult
    {
        public Guid PersonId { get; set; }
        public Guid EmploymentId { get; set; }
        public Guid AssignmentId { get; set; }
    }

    public async Task<IdempotencyResult?> GetHireIdempotencyAsync(string tenantId, Guid idempotencyKey, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        
        var sql = @"SELECT person_id, employment_id, assignment_id 
                    FROM people.hire_idempotency 
                    WHERE tenant_id = @tenantId AND idempotency_key = @key";
                    
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", Guid.Parse(tenantId));
        cmd.Parameters.AddWithValue("key", idempotencyKey);
        
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new IdempotencyResult
            {
                PersonId = reader.GetGuid(0),
                EmploymentId = reader.GetGuid(1),
                AssignmentId = reader.GetGuid(2)
            };
        }
        return null;
    }

    public async Task CreateEmployeeWithIdempotencyAsync(
        Person person,
        Employment employment,
        EmploymentAssignment assignment,
        Guid idempotencyKey,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Insert Idempotency Record First
            const string idempSql = @"
                INSERT INTO people.hire_idempotency (
                    idempotency_key, tenant_id, person_id, employment_id, assignment_id, created_at
                ) VALUES (
                    @idempKey, @tenantId, @personId, @employmentId, @assignmentId, NOW()
                ) ON CONFLICT DO NOTHING;
            ";
            await using var idempCmd = new NpgsqlCommand(idempSql, conn, tx);
            idempCmd.Parameters.AddWithValue("idempKey", idempotencyKey);
            idempCmd.Parameters.AddWithValue("tenantId", person.TenantId.Value);
            idempCmd.Parameters.AddWithValue("personId", person.Id);
            idempCmd.Parameters.AddWithValue("employmentId", employment.Id);
            idempCmd.Parameters.AddWithValue("assignmentId", assignment.Id);
            var affected = await idempCmd.ExecuteNonQueryAsync(ct);

            // If affected == 0, it means it already exists, so we should skip or throw
            if (affected == 0)
            {
                throw new InvalidOperationException("Idempotency key already exists.");
            }

            // Insert Person with encrypted PII persistence
            const string personSql = @"
                INSERT INTO people.persons (
                    id, tenant_id, first_name_en, last_name_en, first_name_ar, last_name_ar,
                    date_of_birth, gender, nationality, national_identifier_encrypted, national_identifier_hash, masked_national_identifier,
                    primary_email, phone_number, created_at, updated_at
                ) VALUES (
                    @id, @tenantId, @fnEn, @lnEn, @fnAr, @lnAr,
                    @dob, @gender, @nationality, @natEnc, @natHash, @natMask,
                    @email, @phone, @createdAt, @updatedAt
                );
            ";
            await using var personCmd = new NpgsqlCommand(personSql, conn, tx);
            personCmd.Parameters.AddWithValue("id", person.Id);
            personCmd.Parameters.AddWithValue("tenantId", person.TenantId.Value);
            personCmd.Parameters.AddWithValue("fnEn", person.FirstNameEn);
            personCmd.Parameters.AddWithValue("lnEn", person.LastNameEn);
            personCmd.Parameters.AddWithValue("fnAr", person.FirstNameAr);
            personCmd.Parameters.AddWithValue("lnAr", person.LastNameAr);
            personCmd.Parameters.AddWithValue("dob", person.DateOfBirth.ToDateTime(TimeOnly.MinValue));
            personCmd.Parameters.AddWithValue("gender", person.Gender);
            personCmd.Parameters.AddWithValue("nationality", person.Nationality);
            personCmd.Parameters.AddWithValue("natEnc", person.NationalIdentifierEncrypted);
            personCmd.Parameters.AddWithValue("natHash", person.NationalIdentifierHash);
            personCmd.Parameters.AddWithValue("natMask", person.MaskedNationalIdentifier);
            personCmd.Parameters.AddWithValue("email", person.PrimaryEmail);
            personCmd.Parameters.AddWithValue("phone", person.PhoneNumber);
            personCmd.Parameters.AddWithValue("createdAt", person.CreatedAt);
            personCmd.Parameters.AddWithValue("updatedAt", person.UpdatedAt);
            await personCmd.ExecuteNonQueryAsync(ct);

            // Insert Employment
            const string empSql = @"
                INSERT INTO people.employments (
                    id, tenant_id, person_id, legal_entity_id, employee_number,
                    hire_date, probation_end_date, status, created_at, updated_at, row_version
                ) VALUES (
                    @id, @tenantId, @personId, @legalEntityId, @empNo,
                    @hireDate, @probationEnd, @status, @createdAt, @updatedAt, @rowVersion
                );
            ";
            await using var empCmd = new NpgsqlCommand(empSql, conn, tx);
            empCmd.Parameters.AddWithValue("id", employment.Id);
            empCmd.Parameters.AddWithValue("tenantId", employment.TenantId.Value);
            empCmd.Parameters.AddWithValue("personId", employment.PersonId);
            empCmd.Parameters.AddWithValue("legalEntityId", employment.LegalEntityId.Value);
            empCmd.Parameters.AddWithValue("empNo", employment.EmployeeNumber);
            empCmd.Parameters.AddWithValue("hireDate", employment.HireDate.ToDateTime(TimeOnly.MinValue));
            empCmd.Parameters.AddWithValue("probationEnd", employment.ProbationEndDate.HasValue ? employment.ProbationEndDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            empCmd.Parameters.AddWithValue("status", (int)employment.Status);
            empCmd.Parameters.AddWithValue("createdAt", employment.CreatedAt);
            empCmd.Parameters.AddWithValue("updatedAt", employment.UpdatedAt);
            empCmd.Parameters.AddWithValue("rowVersion", (int)employment.RowVersion);
            await empCmd.ExecuteNonQueryAsync(ct);

            // Insert Initial Assignment
            const string assignSql = @"
                INSERT INTO people.employment_assignments (
                    id, employment_id, organization_unit_id, position_id, location_id, manager_employment_id,
                    job_title_en, job_title_ar, effective_from, effective_to, is_current, created_at
                ) VALUES (
                    @id, @empId, @unitId, @posId, @locId, @mgrId,
                    @jobEn, @jobAr, @effFrom, @effTo, @isCurrent, @createdAt
                );
            ";
            await using var assignCmd = new NpgsqlCommand(assignSql, conn, tx);
            assignCmd.Parameters.AddWithValue("id", assignment.Id);
            assignCmd.Parameters.AddWithValue("empId", assignment.EmploymentId);
            assignCmd.Parameters.AddWithValue("unitId", assignment.OrganizationUnitId);
            assignCmd.Parameters.AddWithValue("posId", (object?)assignment.PositionId ?? DBNull.Value);
            assignCmd.Parameters.AddWithValue("locId", (object?)assignment.LocationId ?? DBNull.Value);
            assignCmd.Parameters.AddWithValue("mgrId", (object?)assignment.ManagerEmploymentId ?? DBNull.Value);
            assignCmd.Parameters.AddWithValue("jobEn", assignment.JobTitleEn);
            assignCmd.Parameters.AddWithValue("jobAr", assignment.JobTitleAr);
            assignCmd.Parameters.AddWithValue("effFrom", assignment.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            assignCmd.Parameters.AddWithValue("effTo", assignment.EffectiveTo.HasValue ? assignment.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            assignCmd.Parameters.AddWithValue("isCurrent", assignment.IsCurrent);
            assignCmd.Parameters.AddWithValue("createdAt", assignment.CreatedAt);
            await assignCmd.ExecuteNonQueryAsync(ct);

            // Atomic Outbox Domain Event
            var createdEvent = new EmployeeCreatedEvent(
                Guid.NewGuid(),
                employment.Id,
                employment.TenantId,
                employment.LegalEntityId,
                employment.EmployeeNumber,
                $"{person.FirstNameEn} {person.LastNameEn}",
                $"{person.FirstNameAr} {person.LastNameAr}",
                DateTime.UtcNow
            );

            const string outboxSql = @"
                INSERT INTO people.outbox_messages (
                    id, tenant_id, event_type, aggregate_type, aggregate_id, payload, occurred_at
                ) VALUES (
                    @id, @tenantId, @eventType, @aggType, @aggId, @payload::jsonb, @occurredAt
                );
            ";
            await using var outboxCmd = new NpgsqlCommand(outboxSql, conn, tx);
            outboxCmd.Parameters.AddWithValue("id", createdEvent.EventId);
            outboxCmd.Parameters.AddWithValue("tenantId", employment.TenantId.Value);
            outboxCmd.Parameters.AddWithValue("eventType", nameof(EmployeeCreatedEvent));
            outboxCmd.Parameters.AddWithValue("aggType", "Employment");
            outboxCmd.Parameters.AddWithValue("aggId", employment.Id);
            outboxCmd.Parameters.AddWithValue("payload", JsonSerializer.Serialize(createdEvent));
            outboxCmd.Parameters.AddWithValue("occurredAt", createdEvent.OccurredAt);
            await outboxCmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> ChangeAssignmentAsync(
        Guid employmentId,
        EmploymentAssignment newAssignment,
        uint expectedRowVersion,
        LegalEntityId? legalEntityId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Verify optimistic concurrency and legal entity ownership
            var updateEmpSql = @"
                UPDATE people.employments
                SET row_version = row_version + 1,
                    updated_at = NOW()
                WHERE id = @id AND row_version = @expectedVersion
            ";

            if (legalEntityId.HasValue)
            {
                updateEmpSql += " AND legal_entity_id = @legalEntityId";
            }

            await using var empCmd = new NpgsqlCommand(updateEmpSql, conn, tx);
            empCmd.Parameters.AddWithValue("id", employmentId);
            empCmd.Parameters.AddWithValue("expectedVersion", (int)expectedRowVersion);
            if (legalEntityId.HasValue) empCmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

            var affected = await empCmd.ExecuteNonQueryAsync(ct);
            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                return false; // Concurrency conflict or unauthorized
            }

            // Close current assignment
            const string closeSql = @"
                UPDATE people.employment_assignments
                SET is_current = FALSE,
                    effective_to = @effTo
                WHERE employment_id = @empId AND is_current = TRUE;
            ";
            await using var closeCmd = new NpgsqlCommand(closeSql, conn, tx);
            closeCmd.Parameters.AddWithValue("empId", employmentId);
            closeCmd.Parameters.AddWithValue("effTo", newAssignment.EffectiveFrom.AddDays(-1).ToDateTime(TimeOnly.MinValue));
            await closeCmd.ExecuteNonQueryAsync(ct);

            // Insert new assignment
            const string insertSql = @"
                INSERT INTO people.employment_assignments (
                    id, employment_id, organization_unit_id, position_id, location_id, manager_employment_id,
                    job_title_en, job_title_ar, effective_from, effective_to, is_current, created_at
                ) VALUES (
                    @id, @empId, @unitId, @posId, @locId, @mgrId,
                    @jobEn, @jobAr, @effFrom, @effTo, TRUE, NOW()
                );
            ";
            await using var insertCmd = new NpgsqlCommand(insertSql, conn, tx);
            insertCmd.Parameters.AddWithValue("id", newAssignment.Id);
            insertCmd.Parameters.AddWithValue("empId", newAssignment.EmploymentId);
            insertCmd.Parameters.AddWithValue("unitId", newAssignment.OrganizationUnitId);
            insertCmd.Parameters.AddWithValue("posId", (object?)newAssignment.PositionId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("locId", (object?)newAssignment.LocationId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("mgrId", (object?)newAssignment.ManagerEmploymentId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("jobEn", newAssignment.JobTitleEn);
            insertCmd.Parameters.AddWithValue("jobAr", newAssignment.JobTitleAr);
            insertCmd.Parameters.AddWithValue("effFrom", newAssignment.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            insertCmd.Parameters.AddWithValue("effTo", newAssignment.EffectiveTo.HasValue ? newAssignment.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            await insertCmd.ExecuteNonQueryAsync(ct);

            // Insert Outbox Domain Event
            const string getTenantSql = "SELECT tenant_id FROM people.employments WHERE id = @id;";
            await using var getTenantCmd = new NpgsqlCommand(getTenantSql, conn, tx);
            getTenantCmd.Parameters.AddWithValue("id", employmentId);
            var tenantGuid = (Guid)(await getTenantCmd.ExecuteScalarAsync(ct))!;

            var assignEvent = new EmployeeAssignmentChangedEvent(
                Guid.NewGuid(),
                employmentId,
                new TenantId(tenantGuid),
                newAssignment.Id,
                newAssignment.OrganizationUnitId,
                newAssignment.JobTitleEn,
                newAssignment.EffectiveFrom,
                DateTime.UtcNow
            );

            const string outboxSql = @"
                INSERT INTO people.outbox_messages (
                    id, tenant_id, event_type, aggregate_type, aggregate_id, payload, occurred_at
                ) VALUES (
                    @id, @tenantId, @eventType, @aggType, @aggId, @payload::jsonb, @occurredAt
                );
            ";
            await using var outboxCmd = new NpgsqlCommand(outboxSql, conn, tx);
            outboxCmd.Parameters.AddWithValue("id", assignEvent.EventId);
            outboxCmd.Parameters.AddWithValue("tenantId", tenantGuid);
            outboxCmd.Parameters.AddWithValue("eventType", nameof(EmployeeAssignmentChangedEvent));
            outboxCmd.Parameters.AddWithValue("aggType", "Employment");
            outboxCmd.Parameters.AddWithValue("aggId", employmentId);
            outboxCmd.Parameters.AddWithValue("payload", JsonSerializer.Serialize(assignEvent));
            outboxCmd.Parameters.AddWithValue("occurredAt", assignEvent.OccurredAt);
            await outboxCmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<string?> RevealSensitiveFieldAsync(
        Guid employmentId,
        TenantId tenantId,
        Guid actorUserId,
        string fieldName,
        string purpose,
        string correlationId,
        LegalEntityId? legalEntityId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Allowlist only specific reveal-supported field names
        var normalizedField = fieldName.ToLowerInvariant();
        string selectCol;
        bool isEncryptedField = false;

        switch (normalizedField)
        {
            case "nationalid":
            case "nationalidentifier":
                selectCol = "p.national_identifier_encrypted";
                isEncryptedField = true;
                break;

            case "dateofbirth":
            case "dob":
                selectCol = "p.date_of_birth::text";
                break;

            case "phonenumber":
            case "phone":
                selectCol = "p.phone_number";
                break;

            case "primaryemail":
            case "email":
                selectCol = "p.primary_email";
                break;

            default:
                throw new ArgumentException($"Unsupported or non-allowlisted sensitive field: '{fieldName}'.");
        }

        var sql = $@"
            SELECT {selectCol}
            FROM people.employments e
            INNER JOIN people.persons p ON e.person_id = p.id
            WHERE e.id = @id AND e.tenant_id = @tenantId
        ";

        if (legalEntityId.HasValue)
        {
            sql += " AND e.legal_entity_id = @legalEntityId";
        }

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", employmentId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        var rawValueObj = await cmd.ExecuteScalarAsync(ct);
        if (rawValueObj == null || rawValueObj == DBNull.Value)
        {
            return null; // Unauthorized or not found
        }

        var rawValue = rawValueObj.ToString() ?? string.Empty;
        var plaintext = isEncryptedField ? _piiEncryptionService.Decrypt(rawValue) : rawValue;

        // Application-managed audit history: Insert audit record (NEVER write plaintext to audit trail)
        const string auditSql = @"
            INSERT INTO people.sensitive_pii_audit (
                id, tenant_id, actor_user_id, employment_id, field_name, purpose, correlation_id, timestamp
            ) VALUES (
                @id, @tenantId, @actorUserId, @empId, @fieldName, @purpose, @correlationId, NOW()
            );
        ";
        await using var auditCmd = new NpgsqlCommand(auditSql, conn);
        auditCmd.Parameters.AddWithValue("id", Guid.NewGuid());
        auditCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        auditCmd.Parameters.AddWithValue("actorUserId", actorUserId);
        auditCmd.Parameters.AddWithValue("empId", employmentId);
        auditCmd.Parameters.AddWithValue("fieldName", fieldName);
        auditCmd.Parameters.AddWithValue("purpose", string.IsNullOrWhiteSpace(purpose) ? "Operational Workforce Verification" : purpose);
        auditCmd.Parameters.AddWithValue("correlationId", correlationId);
        await auditCmd.ExecuteNonQueryAsync(ct);

        return plaintext;
    }
}
