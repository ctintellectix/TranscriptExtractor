using System.Text.Json;

namespace TranscriptExtractor.Core.Reports;

public static class TranscriptReportComposer
{
    public static TranscriptReportViewModel Compose(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var peopleById = GetNamedEntities(root, "people", "person_id", "name");
        var locationsById = GetNamedEntities(root, "locations", "location_id", "name");

        var report = new TranscriptReportViewModel
        {
            SourceType = GetNestedString(root, "transcript_metadata", "source_type") ?? string.Empty
        };

        if (root.TryGetProperty("described_events", out var events) && events.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in events.EnumerateArray())
            {
                report.Timeline.Add(new TranscriptTimelineItemViewModel
                {
                    Description = GetString(item, "description") ?? string.Empty,
                    LocationName = ResolveName(locationsById, GetString(item, "location_id"))
                });
            }
        }

        if (root.TryGetProperty("allegations", out var allegations) && allegations.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in allegations.EnumerateArray())
            {
                report.Allegations.Add(new TranscriptAllegationViewModel
                {
                    Description = GetString(item, "description") ?? string.Empty
                });
            }
        }

        if (root.TryGetProperty("statements", out var statements) && statements.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in statements.EnumerateArray())
            {
                report.Statements.Add(new TranscriptStatementViewModel
                {
                    SpeakerName = ResolveName(peopleById, GetString(item, "speaker_id")),
                    Summary = GetString(item, "summary") ?? string.Empty
                });
            }
        }

        if (root.TryGetProperty("objects", out var objects) && objects.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in objects.EnumerateArray())
            {
                report.Objects.Add(new TranscriptObjectViewModel
                {
                    Name = GetString(item, "name") ?? string.Empty,
                    Type = GetString(item, "type") ?? string.Empty
                });
            }
        }

        if (root.TryGetProperty("relationship_claims", out var relationships) && relationships.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in relationships.EnumerateArray())
            {
                report.Relationships.Add(new TranscriptRelationshipViewModel
                {
                    SubjectName = ResolveRelationshipEntityName(item, "subject", peopleById, locationsById),
                    RelationshipType = GetString(item, "relationship_type") ?? string.Empty,
                    ObjectName = ResolveRelationshipEntityName(item, "object", peopleById, locationsById)
                });
            }
        }

        if (root.TryGetProperty("locations", out var locations) && locations.ValueKind == JsonValueKind.Array)
        {
            var markerNumber = 1;
            foreach (var item in locations.EnumerateArray())
            {
                var address = GetString(item, "address") ?? string.Empty;
                var isVerifiedAddress = LooksLikeStreetAddress(address);

                report.Locations.Add(new TranscriptLocationViewModel
                {
                    Name = GetString(item, "name") ?? string.Empty,
                    Address = address,
                    IsVerifiedAddress = isVerifiedAddress
                });

                if (isVerifiedAddress)
                {
                    report.VerifiedMapLocations.Add(new TranscriptMapLocationViewModel
                    {
                        MarkerNumber = markerNumber++,
                        Name = GetString(item, "name") ?? string.Empty,
                        Address = address
                    });
                }
            }
        }

        return report;
    }

    private static Dictionary<string, string> GetNamedEntities(JsonElement root, string collectionName, string idProperty, string nameProperty)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!root.TryGetProperty(collectionName, out var collection) || collection.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var item in collection.EnumerateArray())
        {
            var id = GetString(item, idProperty);
            var name = GetString(item, nameProperty);

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
            {
                map[id] = name;
            }
        }

        return map;
    }

    private static string ResolveRelationshipEntityName(
        JsonElement relationship,
        string propertyName,
        Dictionary<string, string> peopleById,
        Dictionary<string, string> locationsById)
    {
        if (!relationship.TryGetProperty(propertyName, out var entity) || entity.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var entityType = GetString(entity, "entity_type");
        var entityId = GetString(entity, "entity_id");

        return entityType switch
        {
            "person" => ResolveName(peopleById, entityId),
            "location" => ResolveName(locationsById, entityId),
            _ => entityId ?? string.Empty
        };
    }

    private static string ResolveName(Dictionary<string, string> map, string? id)
        => id is not null && map.TryGetValue(id, out var name) ? name : string.Empty;

    private static string? GetNestedString(JsonElement root, string objectName, string propertyName)
    {
        return root.TryGetProperty(objectName, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? GetString(nested, propertyName)
            : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool LooksLikeStreetAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        return address.Any(char.IsDigit) &&
               address.Any(char.IsLetter) &&
               address.Contains(' ', StringComparison.Ordinal);
    }
}
