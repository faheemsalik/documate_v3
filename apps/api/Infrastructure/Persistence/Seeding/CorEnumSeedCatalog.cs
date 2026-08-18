namespace Documate.Api.Infrastructure.Persistence.Seeding;

/// <summary>Phase 1 system CorEnumType / CorEnum definitions (EnumKey → display).</summary>
public static class CorEnumSeedCatalog
{
    public static IReadOnlyList<(string TypeKey, string TypeName, IReadOnlyList<(string Key, string Name)> Values)> Types { get; } =
    [
        ("provider_mode", "Provider mode", [
            ("mode_1", "Mode 1"),
            ("mode_2", "Mode 2"),
        ]),
        ("provider_category", "Provider category", [
            ("ocr", "OCR"),
            ("llm", "LLM"),
            ("meta", "Meta"),
            ("other", "Other"),
        ]),
        ("allowlist_mode", "Email allowlist mode", [
            ("open", "Open"),
            ("allowlist_preferred", "Allowlist preferred"),
            ("allowlist_enforced", "Allowlist enforced"),
        ]),
        ("workflow_mode", "Queue workflow mode", [
            ("inherit_agent_default", "Inherit agent default"),
            ("override", "Override"),
            ("disabled", "Disabled"),
        ]),
        ("allowlist_match_type", "Allowlist match type", [
            ("email", "Email"),
            ("domain", "Domain"),
        ]),
        ("intake_source", "Intake source", [
            ("api", "API"),
            ("email", "Email"),
            ("api_sync", "API sync-wait"),
        ]),
        ("file_public_status", "File public status", [
            ("received", "Received"),
            ("processing", "Processing"),
            ("ready", "Ready"),
            ("partial_ready", "Partial ready"),
            ("failed", "Failed"),
            ("rejected", "Rejected"),
            ("cancelled", "Cancelled"),
        ]),
        ("file_internal_stage", "File internal stage", [
            ("received", "Received"),
            ("normalize", "Normalize"),
            ("split", "Split"),
            ("classify", "Classify"),
            ("route", "Route"),
            ("extract", "Extract"),
            ("complete", "Complete"),
        ]),
        ("document_public_status", "Document public status", [
            ("received", "Received"),
            ("processing", "Processing"),
            ("ready", "Ready"),
            ("failed", "Failed"),
            ("rejected", "Rejected"),
            ("cancelled", "Cancelled"),
        ]),
        ("document_internal_stage", "Document internal stage", [
            ("received", "Received"),
            ("extract", "Extract"),
            ("validate", "Validate"),
            ("post_process", "Post-process"),
            ("deliver", "Deliver"),
            ("complete", "Complete"),
        ]),
        ("work_subject_type", "Work subject type", [
            ("file", "File"),
            ("document", "Document"),
            ("batch", "Batch"),
            ("intake_rejection", "Intake rejection"),
            ("queue", "Queue"),
        ]),
        ("work_event_type", "Work event type", [
            ("status_changed", "Status changed"),
            ("webhook_attempted", "Webhook attempted"),
            ("webhook_succeeded", "Webhook succeeded"),
            ("webhook_failed", "Webhook failed"),
            ("cancelled", "Cancelled"),
            ("reprocess_requested", "Reprocess requested"),
        ]),
        ("webhook_delivery_status", "Webhook delivery status", [
            ("not_configured", "Not configured"),
            ("pending", "Pending"),
            ("succeeded", "Succeeded"),
            ("exhausted", "Exhausted"),
            ("skipped", "Skipped"),
        ]),
    ];
}
