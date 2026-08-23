using Npgsql;
using Workforce.Modules.People.Application;
using Workforce.Modules.People.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.People.Infrastructure;

public class PeopleRepository
{
    private readonly string _connectionString;

    public PeopleRepository(string connectionString)
    {
        _connectionString = connectionString;
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
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClause += @" AND (
                p.first_name_en ILIKE @search OR 
                p.last_name_en ILIKE @search OR 
                p.first_name_ar ILIKE @search OR 
                p.last_name_ar ILIKE @search OR 
                e.employee_number ILIKE @search OR
                p.primary_email ILIKE @search
            )";
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
                p.primary_email, p.phone_number, p.national_identifier,
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
            var rawNationalId = reader.GetString(10);
            var maskedId = MaskNationalId(rawNationalId);

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
                FullNameEn = $"{reader.GetString(4)} {reader.GetString(5)}".Trim(),
                FullNameAr = $"{reader.GetString(6)} {reader.GetString(7)}".Trim(),
                PrimaryEmail = reader.GetString(8),
                PhoneNumber = reader.GetString(9),
                MaskedNationalId = maskedId,
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

    public async Task<EmployeeProfileDto?> GetEmployeeProfileAsync(Guid employmentId, TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT 
                e.id, e.person_id, e.tenant_id, e.legal_entity_id, e.employee_number,
                p.first_name_en, p.last_name_en, p.first_name_ar, p.last_name_ar,
                p.gender, p.nationality, p.date_of_birth, p.national_identifier,
                p.primary_email, p.phone_number,
                e.status, e.hire_date, e.probation_end_date, e.termination_date, e.termination_reason, e.row_version
            FROM people.employments e
            INNER JOIN people.persons p ON e.person_id = p.id
            WHERE e.id = @id AND e.tenant_id = @tenantId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", employmentId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        EmployeeProfileDto? profile = null;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                var dob = DateOnly.FromDateTime(reader.GetDateTime(11));
                var rawId = reader.GetString(12);

                profile = new EmployeeProfileDto
                {
                    Id = reader.GetGuid(0),
                    PersonId = reader.GetGuid(1),
                    TenantId = reader.GetGuid(2).ToString(),
                    LegalEntityId = reader.GetGuid(3).ToString(),
                    EmployeeNumber = reader.GetString(4),
                    FirstNameEn = reader.GetString(5),
                    LastNameEn = reader.GetString(6),
                    FirstNameAr = reader.GetString(7),
                    LastNameAr = reader.GetString(8),
                    FullNameEn = $"{reader.GetString(5)} {reader.GetString(6)}".Trim(),
                    FullNameAr = $"{reader.GetString(7)} {reader.GetString(8)}".Trim(),
                    Gender = reader.GetString(9),
                    Nationality = reader.GetString(10),
                    MaskedDateOfBirth = $"****-**-{dob.Day:D2}",
                    MaskedNationalId = MaskNationalId(rawId),
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
            // Insert Person
            const string personSql = @"
                INSERT INTO people.persons (
                    id, tenant_id, first_name_en, last_name_en, first_name_ar, last_name_ar,
                    date_of_birth, gender, nationality, national_identifier, primary_email, phone_number,
                    created_at, updated_at
                ) VALUES (
                    @id, @tenantId, @fnEn, @lnEn, @fnAr, @lnAr,
                    @dob, @gender, @nationality, @natId, @email, @phone,
                    @createdAt, @updatedAt
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
            personCmd.Parameters.AddWithValue("natId", person.NationalIdentifier);
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
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Verify and increment RowVersion on Employment
            const string updateEmpSql = @"
                UPDATE people.employments
                SET updated_at = NOW(),
                    row_version = row_version + 1
                WHERE id = @id AND row_version = @expectedVersion;
            ";
            await using var empCmd = new NpgsqlCommand(updateEmpSql, conn, tx);
            empCmd.Parameters.AddWithValue("id", employmentId);
            empCmd.Parameters.AddWithValue("expectedVersion", (int)expectedRowVersion);
            var affected = await empCmd.ExecuteNonQueryAsync(ct);
            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                return false; // Concurrency conflict
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
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Fetch plaintext value
        string selectCol = fieldName.ToLowerInvariant() switch
        {
            "nationalid" or "nationalidentifier" => "p.national_identifier",
            "dateofbirth" or "dob" => "p.date_of_birth::text",
            _ => throw new ArgumentException($"Unsupported sensitive field: '{fieldName}'.")
        };

        var sql = $@"
            SELECT {selectCol}
            FROM people.employments e
            INNER JOIN people.persons p ON e.person_id = p.id
            WHERE e.id = @id AND e.tenant_id = @tenantId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", employmentId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var val = await cmd.ExecuteScalarAsync(ct);
        if (val == null || val == DBNull.Value) return null;

        // Log to immutable sensitive audit
        const string auditSql = @"
            INSERT INTO people.sensitive_pii_audit (
                id, tenant_id, actor_user_id, employment_id, field_name, purpose, correlation_id, timestamp
            ) VALUES (
                @id, @tenantId, @actor, @empId, @field, @purpose, @correlation, NOW()
            );
        ";
        await using var auditCmd = new NpgsqlCommand(auditSql, conn);
        auditCmd.Parameters.AddWithValue("id", Guid.NewGuid());
        auditCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        auditCmd.Parameters.AddWithValue("actor", actorUserId);
        auditCmd.Parameters.AddWithValue("empId", employmentId);
        auditCmd.Parameters.AddWithValue("field", fieldName);
        auditCmd.Parameters.AddWithValue("purpose", string.IsNullOrWhiteSpace(purpose) ? "Administrative verification" : purpose);
        auditCmd.Parameters.AddWithValue("correlation", correlationId);
        await auditCmd.ExecuteNonQueryAsync(ct);

        return val.ToString();
    }

    private static string MaskNationalId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 4) return "****";
        var prefix = raw.Substring(0, 3);
        return $"{prefix}{new string('*', Math.Max(0, raw.Length - 3))}";
    }
}
