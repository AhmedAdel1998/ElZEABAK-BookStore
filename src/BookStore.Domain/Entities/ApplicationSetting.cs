using BookStore.Domain.Common;

namespace BookStore.Domain.Entities;

/// <summary>
/// Stores a whitelisted application settings group as a persistent database override.
/// </summary>
public sealed class ApplicationSetting : BaseEntity
{
    private ApplicationSetting()
    {
        Key = string.Empty;
        Value = string.Empty;
        Category = string.Empty;
        DataType = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ApplicationSetting"/> class.</summary>
    public ApplicationSetting(string key, string value, string category, string dataType, string description, bool isEncrypted, bool isSystemSetting, string? updatedBy)
    {
        Key = key.Trim();
        Value = value;
        Category = category.Trim();
        DataType = dataType.Trim();
        Description = description.Trim();
        IsEncrypted = isEncrypted;
        IsSystemSetting = isSystemSetting;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Gets the stable settings group key.</summary>
    public string Key { get; private set; }

    /// <summary>Gets the serialized setting value.</summary>
    public string Value { get; private set; }

    /// <summary>Gets the settings category.</summary>
    public string Category { get; private set; }

    /// <summary>Gets the expected serialized data type.</summary>
    public string DataType { get; private set; }

    /// <summary>Gets the human-readable description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the stored value is encrypted.</summary>
    public bool IsEncrypted { get; private set; }

    /// <summary>Gets a value indicating whether the setting is managed by the system.</summary>
    public bool IsSystemSetting { get; private set; }

    /// <summary>Gets the last user who updated the setting.</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>Updates the persisted value.</summary>
    public void Update(string value, string dataType, string description, bool isEncrypted, bool isSystemSetting, string? updatedBy)
    {
        Value = value;
        DataType = dataType.Trim();
        Description = description.Trim();
        IsEncrypted = isEncrypted;
        IsSystemSetting = isSystemSetting;
        UpdatedBy = updatedBy;
        MarkUpdated();
    }
}
