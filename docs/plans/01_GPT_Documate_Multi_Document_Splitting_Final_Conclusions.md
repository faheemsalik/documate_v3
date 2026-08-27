# Documate Multi-Document File Splitting --- Final Architecture Conclusions

## 1. Problem

Documate can receive a single file containing multiple documents. The
system must:

1.  Detect document boundaries.
2.  Group pages belonging to the same document.
3.  Store each resulting document separately.
4.  Run the normal structured-data extraction process on each final
    document.

The solution must work across thousands of companies, suppliers,
layouts, document types, and OCR quality levels.

------------------------------------------------------------------------

## 2. Core Architectural Decision

The system should use an **LLM-first semantic understanding approach**,
rather than trying to make deterministic rules/regex reliably understand
all document formats.

This is based on practical experience: a pure rule-based semantic
extraction approach has already proven difficult to make reliably
general-purpose.

However, the LLM should not directly manipulate files or own the final
storage/splitting operation.

The division of responsibility is:

-   **OCR/Textract:** obtain page text and layout information.
-   **LLM:** understand page/document semantics and produce structured
    evidence.
-   **Application/Segmentation Engine:** make grouping decisions from
    that evidence and physically split/store documents.
-   **Full Extraction LLM:** perform complete structured extraction when
    the final document is known.

------------------------------------------------------------------------

## 3. Important Distinction: Semantic Understanding vs. Segmentation

The LLM should be used early, immediately after OCR, but its first
responsibility is not simply:

> "Split this PDF."

Instead, it should understand the page and produce structured document
intelligence.

For example:

``` json
{
  "documentIntelligence": {
    "documentType": "invoice",
    "primaryDocumentNumber": "INV-10293",
    "referencedDocuments": [],
    "startsDocument": true,
    "continuesPrevious": false,
    "documentComplete": true,
    "pageNumber": null,
    "totalPages": null
  }
}
```

The application then uses this information to determine document
boundaries.

This keeps semantic reasoning and file manipulation separate.

------------------------------------------------------------------------

## 4. Primary Document Identity Is the Strongest General Signal

Document numbers are one of the most valuable segmentation signals.

Examples:

-   Invoice number
-   Credit note number
-   Debit note number
-   Delivery note number
-   Purchase order number
-   Receipt number
-   Shipment number
-   Order number
-   Other document-specific identifiers

Example:

``` text
Page 1 → INV-1001
Page 2 → INV-1001
Page 3 → INV-1002
Page 4 → INV-1002
Page 5 → INV-1003
```

Likely grouping:

``` text
INV-1001 → Pages 1-2
INV-1002 → Pages 3-4
INV-1003 → Page 5
```

### Critical refinement

A document number must be distinguished from a referenced document
number.

Example:

``` text
Invoice No: INV-1001
Against Invoice: INV-0998
Customer PO: PO-2001
Delivery Note: DN-551
```

Here:

-   `INV-1001` = primary identity
-   `INV-0998` = reference
-   `PO-2001` = reference
-   `DN-551` = reference

Therefore, the semantic role of identifiers matters, not merely their
presence.

------------------------------------------------------------------------

## 5. Document Type Alone Is Not a Reliable Segmentation Signal

Invoices, credit notes, and delivery notes can be visually and
semantically similar.

For example:

``` text
Invoice
Credit Note
Delivery Note
```

may all contain:

-   item tables
-   quantities
-   prices
-   amounts
-   totals
-   customer information

Therefore, a document-type change should not automatically create a
boundary.

Document type is useful for:

-   selecting the extraction schema
-   interpreting identifiers
-   providing contextual evidence
-   validating extraction

But **primary document identity and continuation evidence are stronger
segmentation signals**.

------------------------------------------------------------------------

## 6. Page Fingerprint / Semantic Profile

The first LLM processing stage should create a rich semantic
representation of each page.

A page semantic profile can contain:

``` json
{
  "documentType": "invoice",
  "primaryDocumentNumber": "INV-1001",
  "referencedDocuments": [
    {
      "type": "purchase_order",
      "number": "PO-500"
    }
  ],
  "vendor": "ABC Traders",
  "customer": "XYZ Ltd",
  "date": "2026-08-18",
  "currency": "PKR",
  "pageNumber": null,
  "totalPages": null,
  "startsDocument": true,
  "continuesPrevious": false,
  "documentComplete": true
}
```

This should be considered **evidence**, not merely a similarity score.

A useful final document fingerprint can then be derived after pages are
grouped.

------------------------------------------------------------------------

## 7. Do Not Depend on Visual Similarity

Global logo/header/footer matching is weak for Documate because the
system deals with thousands of unrelated companies and document
templates.

Visual/layout similarity can still be used as supporting evidence, but
it should not be the foundation.

Useful supporting signals include:

-   semantic content similarity
-   vendor/customer consistency
-   document identity consistency
-   page numbering
-   continuation language
-   line-item continuation
-   totals appearing only on the final page
-   layout similarity
-   visual similarity

The system should combine these signals rather than depend on one
universal template.

------------------------------------------------------------------------

## 8. Blank Pages

Blank separator pages are uncommon, but if a genuinely blank page is
detected, it should be treated as a very strong separator.

This is a cheap and deterministic optimization.

------------------------------------------------------------------------

## 9. Page Numbers

Page numbers are not consistently available, but when they are present
they provide strong evidence.

Examples:

``` text
Page 1 of 3
Page 2 of 3
Page 3 of 3
```

or:

``` text
Page 1/2
Page 2/2
```

These signals should be extracted and used when available.

They should not be required because many real-world documents do not
contain them.

------------------------------------------------------------------------

## 10. Single-Page Documents and the 90% Optimization

A major observation is that approximately 90% of documents are usually
single-page documents.

This suggests an important optimization:

**Do not unnecessarily build a separate segmentation-and-extraction
pipeline for every single-page document.**

For a page that is confidently a complete standalone document, the
initial LLM call can perform both:

1.  Document intelligence / segmentation information.
2.  Full structured-data extraction.

Example:

``` text
Page
 ↓
LLM
 ├── document intelligence
 └── full structured extraction
 ↓
Complete single-page document
 ↓
DONE
```

There is no reason to run another full extraction call if the first call
already produced the final structured data.

------------------------------------------------------------------------

## 11. Do Not Hard-Code the 90% Assumption

The 90% single-page figure should be treated as an optimization
hypothesis, not as a permanent architectural assumption.

Documate should measure real production data such as:

-   percentage of single-page documents
-   percentage of multi-page documents
-   average pages per document
-   document type
-   customer/tenant
-   supplier/vendor
-   input source
-   OCR quality
-   frequency of missing document numbers

The architecture should adapt based on actual data.

------------------------------------------------------------------------

## 12. Do Not Fully Extract Every Page and Merge by Default

An alternative strategy was considered:

``` text
File
 ↓
Treat every page as a separate document
 ↓
Full extraction
 ↓
Discover multi-page relationships
 ↓
Merge extracted documents
```

This can be economical if most documents are single-page, but it has a
serious weakness.

A multi-page invoice can produce several incomplete structured objects:

``` text
Page 1 → partial invoice
Page 2 → partial invoice
Page 3 → partial invoice
```

Merging those objects reliably creates another difficult problem:

-   Which line items belong together?
-   Which totals override which?
-   Which repeated header values should be retained?
-   How should conflicts be resolved?
-   Which page contains the final totals?
-   Is a page actually a continuation or a separate document?

Therefore, the preferred approach is **not full extraction of every page
followed by unconditional merging**.

Instead, the first stage should provide enough semantic information to
establish document grouping.

------------------------------------------------------------------------

## 13. Adaptive Extraction Strategy

The preferred strategy is:

### Case A --- Known document type + confirmed single-page input

If the input workflow already guarantees:

-   one document only
-   one page
-   document type already known

then Documate can simply run the existing normal extraction process.

There is no need for unnecessary classification or segmentation logic.

The known document type should be passed into the extraction process.

### Case B --- Unknown or potentially multi-document input

Use the early semantic LLM stage to determine:

-   document type
-   primary document number
-   references
-   document boundary signals
-   continuation status
-   completeness
-   page indicators

Then establish document groups.

### Case C --- Multi-page document

Once pages are confidently grouped into one document, run the full
structured extraction against the combined document.

This avoids producing multiple fake/incomplete final documents.

------------------------------------------------------------------------

## 14. Document Hint Optimization

A document-type hint is highly beneficial when the input workflow
guarantees a single document.

Example:

``` text
Input:
Document type = Invoice
Document count = 1
Single page = guaranteed
```

In this case:

``` text
No classification needed
No segmentation needed
No boundary detection needed
```

The system can go directly to the existing extraction workflow.

However, if only the **document type** is known but the file may contain
multiple documents, the hint does not eliminate segmentation.

For example:

``` text
Input type = Invoice

Page 1 → INV-1001
Page 2 → INV-1001
Page 3 → INV-1002
```

The type is known, but document identity is still required to determine
boundaries.

Therefore:

> A known document type can eliminate classification, but it does not
> eliminate document identity and continuation analysis when the file
> may contain multiple documents.

------------------------------------------------------------------------

## 15. LLM Output Should Contain Two Logical Sections

The initial LLM response should conceptually contain:

### A. Document Intelligence

Used by the segmentation engine:

``` json
{
  "documentIntelligence": {
    "documentType": "invoice",
    "primaryDocumentNumber": "INV-1001",
    "referencedDocuments": [],
    "startsDocument": true,
    "continuesPrevious": false,
    "documentComplete": true,
    "pageNumber": null,
    "totalPages": null
  }
}
```

### B. Business Structured Data

Used by the normal Documate extraction process:

``` json
{
  "documentData": {
    "...": "..."
  }
}
```

This allows one LLM call to serve multiple purposes when appropriate.

------------------------------------------------------------------------

## 16. Boundary vs. Completeness

These are two separate questions and should not be conflated.

### Boundary question

> Does this page start a new document?

Examples:

``` text
Page 1 → startsDocument = true
Page 2 → startsDocument = false
Page 3 → startsDocument = true
```

### Completeness question

> Does this page finish the document?

Examples:

``` text
Single-page invoice:
startsDocument = true
documentComplete = true

Page 1 of 3:
startsDocument = true
documentComplete = false

Page 2 of 3:
startsDocument = false
documentComplete = false

Page 3 of 3:
startsDocument = false
documentComplete = true
```

This distinction is central to correctly handling multi-page documents.

------------------------------------------------------------------------

## 17. Recommended High-Level Pipeline

``` text
                         UPLOADED FILE
                               │
                               ▼
                         SPLIT INTO PAGES
                               │
                               ▼
                          OCR / TEXTRACT
                               │
                               ▼
                  EARLY LLM SEMANTIC PROCESSING
                               │
                               ▼
                    PAGE SEMANTIC PROFILE
                               │
                ┌──────────────┼──────────────┐
                │              │              │
          New + Complete   Continuation    Uncertain
                │              │              │
                │              │              │
                ▼              ▼              ▼
             FINAL          Collect       Additional
           single-page      next pages    reasoning
            document            │
                │               ▼
                │        Compare semantic
                │          page profiles
                │               │
                └───────┬───────┘
                        ▼
                 DOCUMENT GROUPS
                        │
                        ▼
                FULL EXTRACTION
             (when not already done)
                        │
                        ▼
                 FINAL DOCUMENT
                        │
                        ▼
                 STORE SEPARATELY
```

------------------------------------------------------------------------

## 18. Core Design Principle

The architecture should follow this principle:

> **LLM creates semantic understanding; the application controls
> document grouping and file operations.**

More specifically:

> **Use the LLM early to understand document identity, references, type,
> continuation, and completeness. Reuse that understanding to make
> segmentation decisions and avoid unnecessary second extraction
> calls.**

The LLM should not directly manipulate files or be the only authority
for splitting.

------------------------------------------------------------------------

## 19. Cost Optimization Principles

Cost should be considered from the beginning.

The primary optimizations are:

### Reuse one LLM call

When a page is a complete single-page document:

``` text
Semantic understanding + full extraction
```

should ideally happen in the same call.

### Avoid classification when the type is already known

If a reliable document-type hint exists:

``` text
Known type → skip classification
```

### Avoid full extraction before grouping multi-page documents

For uncertain/multi-page candidates, establish grouping first and then
perform full extraction on the combined document.

### Keep segmentation input compact

The segmentation/semantic task does not necessarily require every byte
of OCR text.

Use relevant OCR text and layout regions where possible, while retaining
the full OCR locally for later extraction.

Do not hard-code a 50% text rule. Measure which page regions actually
contain useful identity information.

------------------------------------------------------------------------

## 20. What We Should Measure Before Finalizing the Implementation

A representative real-world corpus should be used to measure:

1.  How often a primary document number is present.
2.  How often the primary number is correctly recognized by OCR.
3.  How often the number is in the top portion of the page.
4.  How often numbers are references rather than primary identities.
5.  How often documents are genuinely multi-page.
6.  Average pages per document.
7.  How often continuation pages lack document numbers.
8.  How often page numbers are available.
9.  How often document type is already supplied by the input workflow.
10. How often the initial LLM incorrectly identifies document
    completeness.
11. How often semantic profiles are sufficient to determine boundaries.
12. LLM token usage and cost per uploaded file.
13. Processing latency.
14. Reprocessing rate.
15. Final segmentation accuracy.

These measurements should determine the exact optimization strategy.

------------------------------------------------------------------------

# Final Conclusion

The current preferred architecture for Documate is:

**OCR → early LLM semantic understanding → page semantic profile →
segmentation → full extraction where required → separate document
storage**

with an important optimization:

**For a confidently complete single-page document, the first LLM call
should perform both semantic/document intelligence and full structured
extraction, making that call the final extraction.**

Document identity---especially the **primary document number**---should
be one of the strongest segmentation signals.

Document type is useful but should not be treated as a reliable boundary
signal.

Visual similarity, layout similarity, logo/header/footer similarity, and
other fingerprints are supporting evidence rather than primary
mechanisms.

A supplied document-type hint should be used aggressively when the
workflow guarantees a single document, because classification becomes
unnecessary. If the file can still contain multiple documents, the type
hint does not remove the need for document identity and continuation
analysis.

The system should not attempt to solve universal document semantics with
regex/rules. Deterministic logic should handle mechanical operations and
consume the structured semantic evidence produced by the LLM.

The next architectural task is to design the **exact first-stage LLM
schema/prompt and page semantic profile**, because that becomes the
foundation for both segmentation and cost-efficient extraction.
