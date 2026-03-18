using TranscriptExtractor.Core.Reports;

namespace TranscriptExtractor.Tests.Reports;

public class TranscriptReportComposerTests
{
    [Fact]
    public void Compose_MapsTimelineAllegationsStatementsObjectsRelationshipsAndLocations()
    {
        const string json = """
        {
          "transcript_metadata": {
            "transcript_id": "t1",
            "datetime": "2025-07-14T21:15:00Z",
            "location": "Brookfield",
            "interviewer": "Detective Stone",
            "source_type": "witness_interview"
          },
          "people": [
            { "person_id": "p1", "name": "Emily Carter", "dob": null, "roles": ["witness"], "assertion_type": "explicit", "confidence": 0.98 },
            { "person_id": "p2", "name": "Daniel Brooks", "dob": null, "roles": ["suspect"], "assertion_type": "explicit", "confidence": 0.91 }
          ],
          "locations": [
            { "location_id": "l1", "name": "Michael Turner's Residence", "address": "1427 Walnut Street", "type": "residence", "assertion_type": "explicit", "confidence": 0.93 }
          ],
          "objects": [
            { "object_id": "o1", "name": "Black SUV", "type": "vehicle", "assertion_type": "explicit", "confidence": 0.88 }
          ],
          "statements": [
            { "statement_id": "s1", "speaker_id": "p1", "verbatim_quote": null, "summary": "She saw an argument.", "context": "Outside the residence.", "confidence": 0.95 }
          ],
          "described_events": [
            { "event_id": "e1", "event_type": "incident", "description": "Argument outside the residence.", "datetime": "2025-07-14T21:15:00Z", "datetime_precision": "approximate", "participants": ["p1", "p2"], "location_id": "l1", "source_statement_id": "s1", "assertion_type": "explicit", "confidence": 0.9 }
          ],
          "relationship_claims": [
            { "relationship_id": "r1", "relationship_type": "neighbor", "subject": { "entity_type": "person", "entity_id": "p1" }, "object": { "entity_type": "person", "entity_id": "p2" }, "claim_context": "They live nearby.", "source_statement_id": "s1", "assertion_type": "explicit", "confidence": 0.7 }
          ],
          "allegations": [
            { "allegation_id": "a1", "alleged_perpetrator_id": "p2", "victim_id": "p1", "description": "Daniel Brooks threatened Emily Carter.", "datetime": null, "location_id": "l1", "reported_by": "p1", "assertion_type": "explicit", "confidence": 0.77 }
          ],
          "emotional_behavioral_cues": [],
          "contradictions": []
        }
        """;

        var report = TranscriptReportComposer.Compose(json);

        Assert.Equal("witness_interview", report.SourceType);
        Assert.Single(report.Timeline);
        Assert.Single(report.Allegations);
        Assert.Single(report.Statements);
        Assert.Single(report.Objects);
        Assert.Single(report.Relationships);
        Assert.Single(report.Locations);
        Assert.Single(report.VerifiedMapLocations);
        Assert.Equal("Emily Carter", report.Statements[0].SpeakerName);
        Assert.Equal("Michael Turner's Residence", report.Timeline[0].LocationName);
        Assert.True(report.Locations[0].IsVerifiedAddress);
        Assert.Equal("1427 Walnut Street", report.VerifiedMapLocations[0].Address);
    }

    [Fact]
    public void Compose_OnlyMarksStreetStyleAddressesAsVerifiedMapLocations()
    {
        const string json = """
        {
          "transcript_metadata": {
            "source_type": "witness_interview"
          },
          "people": [],
          "locations": [
            { "location_id": "l1", "name": "Silver Lantern Bar", "address": null, "type": "business", "assertion_type": "explicit", "confidence": 0.98 },
            { "location_id": "l2", "name": "Parking Lot", "address": "Parking lot behind the bar", "type": "other", "assertion_type": "explicit", "confidence": 0.81 },
            { "location_id": "l3", "name": "Marcus Residence", "address": "1427 Walnut Street, Las Vegas, NV", "type": "residence", "assertion_type": "explicit", "confidence": 0.93 }
          ],
          "objects": [],
          "statements": [],
          "described_events": [],
          "relationship_claims": [],
          "allegations": [],
          "emotional_behavioral_cues": [],
          "contradictions": []
        }
        """;

        var report = TranscriptReportComposer.Compose(json);

        Assert.Equal(3, report.Locations.Count);
        Assert.Single(report.VerifiedMapLocations);
        Assert.Equal("Marcus Residence", report.VerifiedMapLocations[0].Name);
        Assert.Equal("1427 Walnut Street, Las Vegas, NV", report.VerifiedMapLocations[0].Address);
        Assert.False(report.Locations[0].IsVerifiedAddress);
        Assert.False(report.Locations[1].IsVerifiedAddress);
        Assert.True(report.Locations[2].IsVerifiedAddress);
    }
}
