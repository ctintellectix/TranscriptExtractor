using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Validation;

namespace TranscriptExtractor.Tests.Core;

public class ExtractionDocumentValidatorTests
{
    [Fact]
    public void Validate_ReturnsSuccessForDocumentWithResolvedReferences()
    {
        var document = new ExtractionDocument(
            transcriptId: Guid.NewGuid(),
            extractionJobId: Guid.NewGuid(),
            json: """
            {
              "transcript_metadata": {
                "transcript_id": "t1",
                "datetime": null,
                "location": null,
                "interviewer": null,
                "source_type": "witness_interview"
              },
              "people": [
                {
                  "person_id": "p1",
                  "name": "Emily Carter",
                  "dob": null,
                  "roles": ["witness"],
                  "assertion_type": "explicit",
                  "confidence": 0.99
                }
              ],
              "locations": [
                {
                  "location_id": "l1",
                  "name": "Walnut Street",
                  "address": null,
                  "type": "other",
                  "assertion_type": "explicit",
                  "confidence": 0.9
                }
              ],
              "objects": [],
              "statements": [
                {
                  "statement_id": "s1",
                  "speaker_id": "p1",
                  "verbatim_quote": null,
                  "summary": "She saw an argument.",
                  "context": "Outside the residence.",
                  "confidence": 0.9
                }
              ],
              "described_events": [
                {
                  "event_id": "e1",
                  "event_type": "incident",
                  "description": "Argument outside the residence.",
                  "datetime": null,
                  "datetime_precision": "unknown",
                  "participants": ["p1"],
                  "location_id": "l1",
                  "source_statement_id": "s1",
                  "assertion_type": "explicit",
                  "confidence": 0.9
                }
              ],
              "relationship_claims": [],
              "allegations": [],
              "emotional_behavioral_cues": [],
              "contradictions": []
            }
            """);

        var result = ExtractionDocumentValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ReturnsErrorWhenStatementReferencesMissingSpeaker()
    {
        var document = new ExtractionDocument(
            transcriptId: Guid.NewGuid(),
            extractionJobId: Guid.NewGuid(),
            json: """
            {
              "transcript_metadata": {
                "transcript_id": "t1",
                "datetime": null,
                "location": null,
                "interviewer": null,
                "source_type": "witness_interview"
              },
              "people": [],
              "locations": [],
              "objects": [],
              "statements": [
                {
                  "statement_id": "s1",
                  "speaker_id": "p404",
                  "verbatim_quote": null,
                  "summary": "She saw an argument.",
                  "context": "Outside the residence.",
                  "confidence": 0.9
                }
              ],
              "described_events": [],
              "relationship_claims": [],
              "allegations": [],
              "emotional_behavioral_cues": [],
              "contradictions": []
            }
            """);

        var result = ExtractionDocumentValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("speaker_id", StringComparison.Ordinal));
    }
}
