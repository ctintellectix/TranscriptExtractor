namespace TranscriptExtractor.Core.Reports;

public sealed class TranscriptReportViewModel
{
    public string SourceType { get; set; } = string.Empty;
    public List<TranscriptTimelineItemViewModel> Timeline { get; set; } = new();
    public List<TranscriptAllegationViewModel> Allegations { get; set; } = new();
    public List<TranscriptStatementViewModel> Statements { get; set; } = new();
    public List<TranscriptObjectViewModel> Objects { get; set; } = new();
    public List<TranscriptRelationshipViewModel> Relationships { get; set; } = new();
    public List<TranscriptLocationViewModel> Locations { get; set; } = new();
    public List<TranscriptMapLocationViewModel> VerifiedMapLocations { get; set; } = new();
}

public sealed class TranscriptTimelineItemViewModel
{
    public string Description { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
}

public sealed class TranscriptAllegationViewModel
{
    public string Description { get; set; } = string.Empty;
}

public sealed class TranscriptStatementViewModel
{
    public string SpeakerName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class TranscriptObjectViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed class TranscriptRelationshipViewModel
{
    public string SubjectName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
}

public sealed class TranscriptLocationViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsVerifiedAddress { get; set; }
}

public sealed class TranscriptMapLocationViewModel
{
    public int MarkerNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
