using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public class RecruitmentPipeline
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<RecruitmentPipelineVersion> _versions = new();
    public IReadOnlyList<RecruitmentPipelineVersion> Versions => _versions.AsReadOnly();

    private RecruitmentPipeline()
    {
        TenantId = default;
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public RecruitmentPipeline(
        Guid id,
        TenantId tenantId,
        string code,
        string nameEn,
        string nameAr,
        bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (tenantId == default || tenantId.Value == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Pipeline code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English name is required.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic name is required.", nameof(nameAr));

        Id = id;
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        IsActive = isActive;
        CreatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static RecruitmentPipeline Reconstitute(
        Guid id,
        TenantId tenantId,
        string code,
        string nameEn,
        string nameAr,
        bool isActive,
        DateTime createdAtUtc,
        uint rowVersion,
        IEnumerable<RecruitmentPipelineVersion>? versions = null)
    {
        var pipeline = new RecruitmentPipeline
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            IsActive = isActive,
            CreatedAtUtc = createdAtUtc,
            RowVersion = rowVersion
        };
        if (versions != null)
        {
            pipeline._versions.AddRange(versions);
        }
        return pipeline;
    }

    public void AddVersion(RecruitmentPipelineVersion version)
    {
        if (version == null) throw new ArgumentNullException(nameof(version));
        if (_versions.Any(v => v.VersionNumber == version.VersionNumber))
            throw new InvalidOperationException($"Pipeline version {version.VersionNumber} already exists.");

        _versions.Add(version);
    }
}

public class RecruitmentPipelineVersion
{
    public Guid Id { get; private set; }
    public Guid PipelineId { get; private set; }
    public int VersionNumber { get; private set; }
    public bool IsImmutable { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<RecruitmentStage> _stages = new();
    public IReadOnlyList<RecruitmentStage> Stages => _stages.OrderBy(s => s.StageOrder).ToList().AsReadOnly();

    private RecruitmentPipelineVersion()
    {
    }

    public RecruitmentPipelineVersion(
        Guid id,
        Guid pipelineId,
        int versionNumber,
        bool isImmutable = false)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (pipelineId == Guid.Empty) throw new ArgumentException("PipelineId cannot be empty.", nameof(pipelineId));
        if (versionNumber <= 0) throw new ArgumentException("Version number must be positive.", nameof(versionNumber));

        Id = id;
        PipelineId = pipelineId;
        VersionNumber = versionNumber;
        IsImmutable = isImmutable;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static RecruitmentPipelineVersion Reconstitute(
        Guid id,
        Guid pipelineId,
        int versionNumber,
        bool isImmutable,
        DateTime createdAtUtc,
        IEnumerable<RecruitmentStage>? stages = null)
    {
        var version = new RecruitmentPipelineVersion
        {
            Id = id,
            PipelineId = pipelineId,
            VersionNumber = versionNumber,
            IsImmutable = isImmutable,
            CreatedAtUtc = createdAtUtc
        };
        if (stages != null)
        {
            version._stages.AddRange(stages);
        }
        return version;
    }

    public void AddStage(RecruitmentStage stage)
    {
        if (IsImmutable)
            throw new InvalidOperationException("Cannot add stages to an immutable pipeline version.");
        if (stage == null) throw new ArgumentNullException(nameof(stage));
        if (_stages.Any(s => s.Code.Equals(stage.Code, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Stage code '{stage.Code}' already exists in this pipeline version.");

        _stages.Add(stage);
    }

    public void MarkImmutable()
    {
        IsImmutable = true;
    }
}

public class RecruitmentStage
{
    public Guid Id { get; private set; }
    public Guid PipelineVersionId { get; private set; }
    public int StageOrder { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public StageKind StageKind { get; private set; }

    private RecruitmentStage()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public RecruitmentStage(
        Guid id,
        Guid pipelineVersionId,
        int stageOrder,
        string code,
        string nameEn,
        string nameAr,
        StageKind stageKind)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (pipelineVersionId == Guid.Empty) throw new ArgumentException("PipelineVersionId cannot be empty.", nameof(pipelineVersionId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Stage code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("Stage English name is required.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Stage Arabic name is required.", nameof(nameAr));

        Id = id;
        PipelineVersionId = pipelineVersionId;
        StageOrder = stageOrder;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        StageKind = stageKind;
    }

    public static RecruitmentStage Reconstitute(
        Guid id,
        Guid pipelineVersionId,
        int stageOrder,
        string code,
        string nameEn,
        string nameAr,
        StageKind stageKind)
    {
        return new RecruitmentStage
        {
            Id = id,
            PipelineVersionId = pipelineVersionId,
            StageOrder = stageOrder,
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            StageKind = stageKind
        };
    }
}
