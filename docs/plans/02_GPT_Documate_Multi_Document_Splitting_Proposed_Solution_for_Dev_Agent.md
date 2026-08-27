# Documate Multi-Document Splitting — Proposed Architecture for Development

## 1. Purpose

Documate can receive one uploaded file containing multiple business documents. The system must identify document boundaries, group pages belonging to the same logical document, split/store the resulting documents separately, and then perform the normal structured extraction for each final document.

The solution must work across:

- thousands of companies
- thousands of suppliers
- highly variable document layouts
- invoices, credit notes, debit notes, delivery notes, purchase orders, receipts, etc.
- native and scanned PDFs
- OCR imperfections
- single-page and multi-page documents

The goal is high accuracy with controlled LLM cost.

---

# 2. Core Architectural Position

The recommended architecture is an **adaptive, LLM-assisted document identity and boundary resolution pipeline**.

It combines ideas from several strategies:

- page-level semantic understanding
- local cross-page reasoning
- document identity anchors
- evidence-based boundary decisions
- adaptive model escalation
- reuse of extraction results for single-page documents

The system should **not** rely on universal regex/rule-based semantic extraction. Deterministic logic should handle mechanical operations and consume structured evidence from the LLM.

The LLM should provide semantic understanding, while the application remains responsible for actual document grouping, PDF splitting, storage, state management, and final decisions.

---

# 3. Clarification: Option E

Option E is essentially a combination/refinement of the strongest parts of the earlier options.

It combines:

1. **Early page semantic understanding**
2. **Page/document fingerprinting**
3. **Cross-page continuity analysis**
4. **Adaptive extraction**
5. **Escalation to stronger models only when evidence is insufficient**

It should not be understood as simply running several independent pipelines in sequence.

The intended architecture is one adaptive pipeline where different capabilities are reused.

---

# 4. High-Level Pipeline

```text
Uploaded File
      |
      v
Split into Physical Pages
      |
      v
OCR / Textract
      |
      v
Early LLM Understanding
      |
      v
Page Semantic Fingerprint
      |
      v
Identity / Continuity / Boundary Engine
      |
      +------------------------------+
      |                              |
      v                              v
High confidence                 Uncertain
      |                              |
      v                              v
Accept decision              Stronger/premium model
      |                              |
      +---------------+--------------+
                      |
                      v
              Final Document Groups
                      |
          +-----------+-----------+
          |                       |
          v                       v
     Single-page              Multi-page
          |                       |
          v                       v
   Reuse first-call          Full extraction
   extraction result         on combined pages
          |                       |
          +-----------+-----------+
                      |
                      v
              Store Final Documents
```

---

# 5. Important Architectural Principle

The system should distinguish between:

### Semantic Understanding

> What does this page mean?

and

### Document Boundary Resolution

> Does this page belong to the current document or start a new one?

The LLM performs semantic understanding.

The application/segmentation engine evaluates the resulting evidence and owns the final document grouping.

The LLM should not manipulate PDF files or directly perform database/storage operations.

---

# 6. Early LLM Understanding

The early LLM stage is important because universal deterministic semantic extraction has proven unreliable across Documate's wide range of documents.

However, the early LLM should **not automatically receive the entire OCR text of the page**.

## Recommended input reduction

Most useful segmentation signals are expected near the:

- top/header region
- bottom/footer region

The page number, when available, is usually near the footer.

Therefore the early understanding stage should initially use a **reduced OCR representation** rather than blindly sending the complete page text.

### Proposed default extraction regions

Approximately:

```text
Top:    30%–60% of page text/content
Bottom: ~5% of page text/content
```

The exact percentage should be adaptive.

### Adaptive principle

If the page contains little text:

```text
Use a larger percentage of the page.
```

If the page is text-heavy:

```text
Use a smaller percentage while preserving the most informative header/footer content.
```

The implementation should not hard-code a universal "50%" rule.

It should construct an information-focused segmentation representation.

---

# 7. Why Not Use Only Fixed Percentages?

Document layouts vary widely.

Document identity may appear:

- top-left
- top-right
- inside a small header block
- below customer information
- in a compact title section
- occasionally in other regions

Therefore, percentage-based extraction is only a starting heuristic.

The system should preserve OCR coordinates/layout information and be designed so the selection strategy can evolve toward **semantic regions** instead of fixed percentages.

Potential future strategy:

```text
Header candidates
+
Document identity candidates
+
Customer/vendor candidates
+
Footer/page-number candidates
```

This would be more robust than fixed percentages.

---

# 8. Early LLM Output: Full Semantic Fingerprint

The early LLM should produce a **full semantic fingerprint**, not merely document type.

Example:

```json
{
  "documentIntelligence": {
    "documentType": "invoice",

    "primaryIdentity": {
      "type": "invoice_number",
      "value": "INV-1001",
      "confidence": 0.99
    },

    "references": [
      {
        "type": "purchase_order",
        "value": "PO-500",
        "role": "reference"
      }
    ],

    "vendor": "ABC Traders",
    "customer": "XYZ Ltd",
    "date": "2026-08-18",

    "pageIndicators": {
      "pageNumber": null,
      "totalPages": null
    },

    "boundaryAssessment": {
      "startsNewDocument": true,
      "continuesPrevious": false
    },

    "completionAssessment": {
      "documentComplete": true
    }
  }
}
```

The exact schema must be refined during implementation.

---

# 9. Primary Document Identity

The strongest general signal is **primary document identity**.

Possible identities include:

- invoice number
- credit note number
- debit note number
- delivery note number
- purchase order number
- receipt number
- shipment number
- sales order number
- other document-specific identifiers

Example:

```text
Page 1 → INV-1001
Page 2 → INV-1001
Page 3 → INV-1002
Page 4 → INV-1002
Page 5 → INV-1003
```

Likely grouping:

```text
Document A → INV-1001 → Pages 1-2
Document B → INV-1002 → Pages 3-4
Document C → INV-1003 → Page 5
```

---

# 10. Primary Identity Must Be Distinguished from References

This is critical.

Example:

```text
Invoice No: INV-1001
Against Invoice: INV-0998
Customer PO: PO-2001
Delivery Note: DN-551
```

The system must understand:

```text
INV-1001 → primary document identity
INV-0998 → reference
PO-2001  → reference
DN-551   → reference
```

A number's mere presence is not enough.

The semantic role matters.

---

# 11. Strongest Continuity Signal

For Documate, the strongest continuation signal is:

> **same primary document reference/number + same customer/vendor**

Example:

```text
Page 1:
Invoice No = INV-1001
Vendor = ABC Traders
Customer = XYZ Ltd

Page 2:
Invoice No = INV-1001
Vendor = ABC Traders
Customer = XYZ Ltd
```

This is very strong evidence that Page 2 belongs to the same document.

The combination is more reliable than relying on totals or generic document-type similarity.

### Why not use grand totals as a primary continuity signal?

Because many document types naturally do not have them.

Delivery Notes are an important example.

Therefore:

```text
same primary identity
+
same vendor/customer
```

should be treated as a dominant continuation signal.

---

# 12. Document Type Is a Supporting Signal

Document types such as:

- Invoice
- Credit Note
- Delivery Note

can be highly visually and structurally similar.

Therefore:

```text
same type
```

does not mean:

```text
same document
```

and:

```text
different type
```

does not automatically mean:

```text
new document
```

Document type is primarily useful for:

- selecting the extraction schema
- interpreting identifiers
- providing semantic context
- validating the final extraction

Identity and continuity evidence should have higher authority.

---

# 13. Other Useful Signals

Signals should be treated as evidence rather than independent absolute rules.

## Very strong

- new primary document identity
- same primary document identity
- same primary identity + vendor/customer
- explicit page `X of Y`
- explicit `page X/Y`
- blank separator page

## Strong/medium

- continuation wording
- "continued" indicators
- continuing line-item table
- same vendor/customer
- reference relationships
- document completion indicators

## Supporting

- document type
- date
- currency
- amount patterns
- structural/layout similarity
- visual similarity
- embeddings

## Weak

- logo similarity
- generic header similarity
- generic footer similarity

Logo/header/footer similarity must not be treated as the core segmentation mechanism because Documate processes documents from thousands of unrelated companies.

---

# 14. Page Numbering

Page numbering is useful when available.

Examples:

```text
Page 1 of 3
Page 2 of 3
Page 3 of 3
```

or:

```text
1 / 2
2 / 2
```

This should be extracted as semantic evidence.

It is not mandatory because many documents have no page numbering.

---

# 15. Blank Pages

Blank pages are uncommon but should be treated as a strong mechanical separator if detected.

This requires no LLM.

```text
Blank Page
    |
    v
Strong Boundary
```

---

# 16. LLM Model Strategy

Different models should be used for different levels of difficulty.

Do not use a premium model for every page if a lower-cost model can provide the necessary semantic fingerprint accurately.

## Recommended model tiers

### Tier 1 — Normal early understanding

Use a low-cost/non-premium model.

Purpose:

- semantic fingerprint
- primary document identity
- references
- document type if unknown
- vendor/customer
- page indicators
- continuation assessment
- completeness assessment

This should be the default.

### Tier 2 — Uncertain cases

Use a stronger/premium model when:

- primary identity is ambiguous
- references may be confused with primary identity
- OCR is poor
- continuation decision conflicts with identity evidence
- a page appears to be a continuation but lacks document number
- adjacent pages provide contradictory evidence
- the segmentation confidence is low

### Tier 3 — Exceptional cases

Use larger-context/multimodal reasoning for difficult files where:

- multiple pages are ambiguous
- text-only OCR is insufficient
- layout/image evidence is needed
- local page context does not resolve the boundary

The system should escalate only the necessary pages or local page window, not the entire file whenever possible.

---

# 17. Cross-Page LLM Reasoning

Independent page calls are not always sufficient.

A continuation page may contain:

- no document number
- only additional line items
- no totals
- no obvious document title

Therefore, the LLM should sometimes see **local neighboring pages**.

Example:

```text
Page N-1
Page N
Page N+1
```

The model can assess the middle page using local context.

The system should prefer a local/sliding-window strategy over putting an entire large packet into one LLM context.

---

# 18. Anchor-Based Segmentation

Strong document identities should act as **anchors**.

Example:

```text
INV-1001
    |
    +-- Page 1
    +-- Page 2
    +-- Page 3

INV-1002
    |
    +-- Page 4
    +-- Page 5
```

Once a strong primary identity is established, pages can be attached to that active document until strong evidence indicates a new anchor.

This reduces unnecessary repeated global reasoning.

---

# 19. Boundary and Completeness Are Different

These must be separate fields/questions.

### Boundary

> Does this page start a new document?

### Continuation

> Does this page belong to the previous/current document?

### Completeness

> Does this page finish the document?

Examples:

### Single-page document

```text
startsNewDocument = true
continuesPrevious = false
documentComplete = true
```

### First page of multi-page document

```text
startsNewDocument = true
continuesPrevious = false
documentComplete = false
```

### Middle continuation page

```text
startsNewDocument = false
continuesPrevious = true
documentComplete = false
```

### Final continuation page

```text
startsNewDocument = false
continuesPrevious = true
documentComplete = true
```

This model is more expressive than a simple "split/no split" decision.

---

# 20. Known Document Type Hint

A supplied document-type hint can provide a significant optimization, but only under the correct conditions.

## Case A — Single document is guaranteed

For example:

```text
Input:
Document count = 1
Document type = Invoice
Single document/page workflow = guaranteed
```

Then the system does not need segmentation or document classification.

It can directly run the normal extraction workflow.

This can follow the previous existing Documate processing path.

## Case B — Document type is known but file may contain multiple documents

Example:

```text
Expected type = Invoice

Page 1 → INV-1001
Page 2 → INV-1001
Page 3 → INV-1002
```

Classification is unnecessary, but segmentation still requires:

- primary document identity
- continuation
- vendor/customer relationships
- page indicators
- other boundary evidence

Therefore:

> A known document type can eliminate classification, but it does not eliminate segmentation when multiple documents are possible.

---

# 21. Single-Page Optimization

If a page is confidently identified as:

```text
startsNewDocument = true
documentComplete = true
```

then the first LLM call can perform:

1. document intelligence
2. full structured extraction

in the same call.

No second full-extraction call is necessary.

Conceptually:

```text
Page
  |
  v
First LLM call
  |
  +--> Semantic fingerprint
  |
  +--> Full structured extraction
  |
  v
Final single-page document
```

This is a major cost and latency optimization.

---

# 22. Do Not Default to Full Extraction on Every Page

The alternative strategy of:

```text
Extract every page as a complete document
        |
        v
Discover multi-page relationships later
        |
        v
Merge extracted documents
```

should not be the primary architecture.

The problem is that continuation pages can produce incomplete document objects.

Example:

```text
Invoice
Page 1 → header + items
Page 2 → more items + totals
```

Independent extraction creates partial records that then have to be merged and reconciled.

This creates a second hard AI/data-integrity problem.

Instead:

- use the first LLM call to establish semantic evidence
- group pages
- run full extraction on the combined document when necessary

For truly complete single-page documents, reuse the first extraction result.

---

# 23. Reduced Early-LLM Input vs Full Extraction Input

The early understanding task and the final extraction task have different information requirements.

### Early understanding

Primarily needs:

- header/identity content
- vendor/customer
- document number
- references
- document type context
- footer/page number
- continuation clues

Therefore, reduced input should be tested.

### Full extraction

Needs:

- complete text
- tables
- all line items
- totals
- taxes
- addresses
- terms
- other required fields

Therefore, full OCR/page content should remain available for the extraction stage.

---

# 24. Recommended Initial Early-Understanding Input Strategy

Start with:

```text
Top: 30%–60%
Bottom: ~5%
```

where percentages are applied adaptively based on page content density.

Do not assume that exactly 50% is always sufficient.

Log failures where the relevant identifier was outside the selected regions.

Use production data to tune this later.

Potential next generation:

```text
OCR
  |
  +-- Header candidate region
  +-- Primary identity candidate region
  +-- Vendor/customer candidate region
  +-- Footer/page-number region
```

This would be more robust than fixed percentages.

---

# 25. Semantic Fingerprint vs Final Business Extraction

The early LLM response should contain two logical sections.

```json
{
  "documentIntelligence": {
    "...": "..."
  },

  "documentData": {
    "...": "..."
  }
}
```

For single-page complete documents:

```text
documentData
```

can become the final result.

For multi-page documents:

```text
documentIntelligence
```

helps grouping, while the final extraction is run on the combined pages.

This maximizes reuse of the LLM's work.

---

# 26. Evidence Should Be Returned

The LLM should provide structured evidence rather than only a conclusion.

Example:

```json
{
  "boundaryAssessment": {
    "startsNewDocument": true,
    "evidence": [
      {
        "signal": "new_primary_document_number",
        "value": "INV-1002"
      },
      {
        "signal": "new_document_header",
        "value": "Invoice"
      }
    ]
  }
}
```

Continuation:

```json
{
  "continuationAssessment": {
    "continuesPrevious": true,
    "evidence": [
      {
        "signal": "same_primary_document_number",
        "value": "INV-1001"
      },
      {
        "signal": "same_vendor"
      },
      {
        "signal": "same_customer"
      },
      {
        "signal": "continuing_line_items"
      }
    ]
  }
}
```

This makes the system:

- explainable
- debuggable
- measurable
- easier to improve

---

# 27. Do Not Trust LLM Confidence Directly

An LLM may return:

```text
confidence = 0.98
```

but that value should not be treated as a statistically calibrated probability.

Instead, create an application-level segmentation confidence from multiple signals.

Example:

```text
New primary identity          very strong
Same vendor/customer          strong
Explicit page numbering       strong
LLM continuation assessment  supporting
Visual continuity             supporting
```

The application combines these into its own final confidence/decision.

---

# 28. Recommended Decision Hierarchy

A conceptual hierarchy:

```text
1. Primary document identity
2. Same primary identity
3. Same primary identity + vendor/customer
4. Explicit page numbering
5. Continuation evidence
6. Reference relationships
7. Vendor/customer continuity
8. Document completion evidence
9. Document type
10. Layout/visual similarity
11. Generic embedding similarity
```

This is not necessarily a fixed numeric scoring formula in V1. It is a prioritization of evidence.

---

# 29. Ambiguous Cases

When evidence conflicts, do not force a decision.

Examples:

```text
New document number detected
BUT
number might be a reference

OR

No document number
BUT
strong continuation evidence

OR

Same vendor/customer
BUT
different primary number
```

Escalation path:

```text
Low-cost model
      |
      v
Evidence fusion
      |
      v
Uncertain?
      |
      +--> stronger model
      |
      +--> vision if necessary
      |
      +--> larger local context if necessary
```

This is where premium model spending should be concentrated.

---

# 30. What We Should Not Build

Do not make these the foundation:

### Pure regex/rule semantic extraction

It will not generalize across thousands of layouts.

### Logo/header/footer matching

Too weak and template-dependent.

### Document type as the primary splitter

Invoice vs credit note vs delivery note is not a reliable boundary.

### Generic embedding similarity as identity

Two separate invoices can be extremely similar.

### Full-file premium LLM reasoning by default

Too expensive and unnecessary for normal cases.

### Full extraction of every page followed by automatic merging

Creates complex partial-document reconciliation.

---

# 31. Recommended Production Architecture

```text
                            FILE
                             |
                             v
                       PHYSICAL PAGES
                             |
                             v
                        OCR / TEXTRACT
                             |
                             v
                 EARLY SEMANTIC LLM
                 (cost-optimized model)
                             |
                             v
                  PAGE SEMANTIC PROFILE
                             |
                             v
                IDENTITY / BOUNDARY ENGINE
                             |
              +--------------+--------------+
              |                             |
              v                             v
        HIGH CONFIDENCE                  UNCERTAIN
              |                             |
              |                     Strong/Premium LLM
              |                             |
              |                        Vision if needed
              |                             |
              +--------------+--------------+
                             |
                             v
                    FINAL DOCUMENT GROUPS
                             |
                  +----------+----------+
                  |                     |
                  v                     v
             Single Page           Multi Page
                  |                     |
                  v                     v
            Reuse first-call       Full extraction
               result              on combined pages
                  |                     |
                  +----------+----------+
                             |
                             v
                       FINAL DOCUMENT
                             |
                             v
                       STORE SEPARATELY
```

---

# 32. Development Recommendation

Build the first implementation around these components:

```text
1. Page extraction/OCR adapter
2. Reduced early-understanding text builder
3. Low-cost semantic LLM prompt
4. Semantic fingerprint schema
5. Identity/reference resolver
6. Page relationship evaluator
7. Boundary/continuation engine
8. Confidence/evidence model
9. Model escalation service
10. Final document grouping/splitting
11. Existing full extraction pipeline
12. Segmentation telemetry and evaluation dataset
```

The components should be independent enough that the LLM provider/model can be changed without rewriting the segmentation engine.

---

# 33. Metrics to Capture from the Beginning

For every uploaded file, log:

- page count
- detected document count
- actual/verified document count when available
- single-page percentage
- multi-page percentage
- primary identity detection success
- identity OCR quality
- continuation decisions
- boundary confidence
- number of escalations
- model used
- input/output tokens
- estimated cost
- processing time
- extraction reuse rate
- reprocessing rate
- human corrections
- false split
- missed split

These metrics will be essential for deciding whether the early reduced-text approach is sufficient and for optimizing model selection.

---

# 34. Immediate Technical Experiments

Before building the complete production pipeline, run a controlled evaluation against a representative corpus.

Test:

### Experiment A
Full page OCR to early LLM.

### Experiment B
Top 30% + bottom 5%.

### Experiment C
Adaptive top 30%–60% + bottom 5%.

Measure:

- primary identity extraction accuracy
- reference-vs-primary classification
- document type accuracy
- vendor/customer extraction
- continuation detection
- boundary accuracy
- tokens
- cost
- latency

Then test:

### Experiment D
Low-cost model vs premium model for early understanding.

### Experiment E
Independent pages vs local 3-page context.

### Experiment F
Text-only vs text + image for ambiguous cases.

These experiments should determine the production configuration rather than assumptions.

---

# 35. Final Design Principle

The central principle for the implementation team is:

> **Use inexpensive LLM semantic understanding early, extract a rich page-level fingerprint, use primary document identity + vendor/customer + continuity evidence as the strongest segmentation signals, and escalate only ambiguous cases to stronger models.**

And:

> **When a page is confidently a complete single-page document, combine the initial semantic understanding and full extraction in one LLM call and reuse that result as the final document.**

Finally:

> **The LLM supplies semantic evidence; the application owns document identity state, boundary decisions, grouping, PDF splitting, and storage.**
