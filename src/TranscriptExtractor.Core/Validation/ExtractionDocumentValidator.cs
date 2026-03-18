using System.Text.Json;
using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Core.Validation;

public static class ExtractionDocumentValidator
{
    public static ExtractionDocumentValidationResult Validate(ExtractionDocument document)
    {
        var result = new ExtractionDocumentValidationResult();

        if (string.IsNullOrWhiteSpace(document.Json))
        {
            result.Errors.Add("Extraction document JSON is empty.");
            return result;
        }

        using var parsed = JsonDocument.Parse(document.Json);
        var root = parsed.RootElement;

        var people = GetIds(root, "people", "person_id");
        var locations = GetIds(root, "locations", "location_id");
        var objects = GetIds(root, "objects", "object_id");
        var statements = GetIds(root, "statements", "statement_id");
        var events = GetIds(root, "described_events", "event_id");

        ValidateStatements(root, people, result);
        ValidateDescribedEvents(root, people, locations, statements, result);
        ValidateRelationshipClaims(root, people, locations, objects, events, statements, result);
        ValidateAllegations(root, people, locations, result);
        ValidateCues(root, people, statements, result);
        ValidateContradictions(root, people, statements, result);

        return result;
    }

    private static void ValidateStatements(JsonElement root, HashSet<string> people, ExtractionDocumentValidationResult result)
    {
        if (!root.TryGetProperty("statements", out var statements) || statements.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var statement in statements.EnumerateArray())
        {
            EnsureExists("statements.speaker_id", GetString(statement, "speaker_id"), people, result);
        }
    }

    private static void ValidateDescribedEvents(
        JsonElement root,
        HashSet<string> people,
        HashSet<string> locations,
        HashSet<string> statements,
        ExtractionDocumentValidationResult result)
    {
        if (!root.TryGetProperty("described_events", out var events) || events.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in events.EnumerateArray())
        {
            if (item.TryGetProperty("participants", out var participants) && participants.ValueKind == JsonValueKind.Array)
            {
                foreach (var participant in participants.EnumerateArray())
                {
                    EnsureExists("described_events.participants", participant.GetString(), people, result);
                }
            }

            EnsureExists("described_events.location_id", GetString(item, "location_id"), locations, result, allowNull: true);
            EnsureExists("described_events.source_statement_id", GetString(item, "source_statement_id"), statements, result);
        }
    }

    private static void ValidateRelationshipClaims(
        JsonElement root,
        HashSet<string> people,
        HashSet<string> locations,
        HashSet<string> objects,
        HashSet<string> events,
        HashSet<string> statements,
        ExtractionDocumentValidationResult result)
    {
        if (!root.TryGetProperty("relationship_claims", out var claims) || claims.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var claim in claims.EnumerateArray())
        {
            ValidateEntityReference(claim, "subject", people, locations, objects, events, result);
            ValidateEntityReference(claim, "object", people, locations, objects, events, result);
            EnsureExists("relationship_claims.source_statement_id", GetString(claim, "source_statement_id"), statements, result);
        }
    }

    private static void ValidateAllegations(
        JsonElement root,
        HashSet<string> people,
        HashSet<string> locations,
        ExtractionDocumentValidationResult result)
    {
        if (!root.TryGetProperty("allegations", out var allegations) || allegations.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var allegation in allegations.EnumerateArray())
        {
            EnsureExists("allegations.alleged_perpetrator_id", GetString(allegation, "alleged_perpetrator_id"), people, result);
            EnsureExists("allegations.victim_id", GetString(allegation, "victim_id"), people, result);
            EnsureExists("allegations.reported_by", GetString(allegation, "reported_by"), people, result);
            EnsureExists("allegations.location_id", GetString(allegation, "location_id"), locations, result, allowNull: true);
        }
    }

    private static void ValidateCues(
        JsonElement root,
        HashSet<string> people,
        HashSet<string> statements,
        ExtractionDocumentValidationResult result)
    {
        if (!root.TryGetProperty("emotional_behavioral_cues", out var cues) || cues.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var cue in cues.EnumerateArray())
        {
            EnsureExists("emotional_behavioral_cues.person_id", GetString(cue, "person_id"), people, result);
            EnsureExists("emotional_behavioral_cues.observed_during_statement_id", GetString(cue, "observed_during_statement_id"), statements, result);
        }
    }

    private static void ValidateContradictions(
        JsonElement root,
        HashSet<string> people,
        HashSet<string> statements,
        ExtractionDocumentValidationResult result)
    {
        if (!root.TryGetProperty("contradictions", out var contradictions) || contradictions.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var contradiction in contradictions.EnumerateArray())
        {
            EnsureExists("contradictions.person_id", GetString(contradiction, "person_id"), people, result);

            if (contradiction.TryGetProperty("statement_ids", out var statementIds) && statementIds.ValueKind == JsonValueKind.Array)
            {
                foreach (var statementId in statementIds.EnumerateArray())
                {
                    EnsureExists("contradictions.statement_ids", statementId.GetString(), statements, result);
                }
            }
        }
    }

    private static void ValidateEntityReference(
        JsonElement claim,
        string propertyName,
        HashSet<string> people,
        HashSet<string> locations,
        HashSet<string> objects,
        HashSet<string> events,
        ExtractionDocumentValidationResult result)
    {
        if (!claim.TryGetProperty(propertyName, out var entity) || entity.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add($"relationship_claims.{propertyName} is missing.");
            return;
        }

        var entityType = GetString(entity, "entity_type");
        var entityId = GetString(entity, "entity_id");

        var source = entityType switch
        {
            "person" => people,
            "location" => locations,
            "object" => objects,
            "event" => events,
            _ => null
        };

        if (source is null)
        {
            result.Errors.Add($"relationship_claims.{propertyName}.entity_type '{entityType}' is unsupported.");
            return;
        }

        EnsureExists($"relationship_claims.{propertyName}.entity_id", entityId, source, result);
    }

    private static HashSet<string> GetIds(JsonElement root, string collectionName, string idPropertyName)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        if (!root.TryGetProperty(collectionName, out var collection) || collection.ValueKind != JsonValueKind.Array)
        {
            return ids;
        }

        foreach (var item in collection.EnumerateArray())
        {
            var id = GetString(item, idPropertyName);
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static void EnsureExists(
        string path,
        string? id,
        HashSet<string> knownIds,
        ExtractionDocumentValidationResult result,
        bool allowNull = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            if (!allowNull)
            {
                result.Errors.Add($"{path} is missing.");
            }

            return;
        }

        if (!knownIds.Contains(id))
        {
            result.Errors.Add($"{path} references missing id '{id}'.");
        }
    }
}
