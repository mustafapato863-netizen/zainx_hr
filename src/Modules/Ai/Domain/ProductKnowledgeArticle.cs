using System;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Curated platform product knowledge explaining ZainX domain workflows and behavior.
/// </summary>
public sealed class ProductKnowledgeArticle
{
    public Guid Id { get; }
    public string TopicCode { get; }
    public string TitleEn { get; }
    public string TitleAr { get; }
    public string ContentEn { get; }
    public string ContentAr { get; }
    public string Category { get; }
    public string TagsJson { get; }

    public ProductKnowledgeArticle(
        Guid id,
        string topicCode,
        string titleEn,
        string titleAr,
        string contentEn,
        string contentAr,
        string category,
        string? tagsJson = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TopicCode = topicCode ?? throw new ArgumentNullException(nameof(topicCode));
        TitleEn = titleEn ?? throw new ArgumentNullException(nameof(titleEn));
        TitleAr = titleAr ?? throw new ArgumentNullException(nameof(titleAr));
        ContentEn = contentEn ?? string.Empty;
        ContentAr = contentAr ?? string.Empty;
        Category = category ?? "General";
        TagsJson = tagsJson ?? "[]";
    }
}
