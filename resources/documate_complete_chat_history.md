# Documate Product Strategy — Complete Chat History

**Thread date:** August 29–30, 2026  
**Topic:** Documate differentiation, document AI, document memory, and defensibility against in-house AI-agent development.

---

## User — August 29, 2026, 18:12 UTC

> i need some reference saas apps for inspiration of my document content extraction application UI.

## Assistant

Absolutely. For a **document content extraction SaaS**, I’d look at products in a few adjacent categories rather than only “document extraction” tools. The best inspiration will come from apps that combine **file upload → processing → extracted data → review/edit → export/API**.

### Good SaaS references

- **Nanonets** — document processing workflows; dashboard, processing states, extraction review.
- **Rossum** — human-in-the-loop extraction; document + extracted-fields workspace.
- **Docsumo** — OCR + structured extraction; upload flows, document viewer, validation.
- **Parseur** — email/document parsing; simple SaaS information architecture.

### Also worth studying

- **Adobe Acrobat** — document viewer and annotation patterns.
- **Google Drive** — file management and upload UX.
- **Dropbox** — clean file-centric navigation.
- **Notion** — content hierarchy and side panels.
- **Airtable** — presenting extracted information as structured data.

### Suggested Documate UI

A useful overall information architecture would be:

```text
┌──────────────────────────────────────────────────────────────┐
│ Logo          Documents   Collections   API        Avatar   │
├──────────────┬───────────────────────────────────────────────┤
│              │                                               │
│ + Upload     │  Documents                    Search 🔍       │
│              │                                               │
│ All          │  ┌─────────────────────────────────────────┐ │
│ Processing   │  │ invoice-1042.pdf        ✓ Extracted    │ │
│ Review       │  │ invoice-1041.pdf        ◐ Processing   │ │
│ Completed    │  │ contract-acme.pdf       ! Review       │ │
│              │  └─────────────────────────────────────────┘ │
│              │                                               │
└──────────────┴───────────────────────────────────────────────┘
```

The **core extraction screen** could be:

```text
┌───────────────────────┬──────────────────────────────────────┐
│                       │ Extracted content                    │
│                       │                                      │
│                       │ Invoice number   INV-1042            │
│    DOCUMENT           │ Customer         Acme Corp            │
│    PREVIEW            │ Date             Aug 21, 2026         │
│                       │ Total            $12,480.00           │
│                       │                                      │
│  [PDF/image viewer]   │ Line items                           │
│                       │ ┌──────────────────────────────────┐ │
│                       │ │ Product │ Qty │ Price │ Total    │ │
│                       │ │ Server  │  2  │ $500  │ $1,000   │ │
│                       │ └──────────────────────────────────┘ │
│                       │                                      │
│                       │ [Edit] [Approve] [Export JSON]       │
└───────────────────────┴──────────────────────────────────────┘
```

**Key reference:** Rossum is especially relevant if the product has a human-review step because its central pattern—**document on one side, structured extracted information on the other**—maps closely to this type of application.

---

## User — August 29, 2026, 18:13 UTC

> find more screenshots for rossum

## Assistant

Several useful Rossum UI areas were identified:

### 1. Document validation / extraction screen

The most relevant screen for Documate is the **document preview + extracted fields + validation/review** workspace. Rossum’s validation screen uses a customizable/resizable annotation sidebar while keeping the document view unobstructed.

### 2. Document inbox / dashboard

Rossum’s **All Documents** dashboard is useful for:
- configurable columns
- filtering
- search
- uploads
- queue-based organization

### 3. Upload / ingestion

Rossum’s upload flow is minimal:
- choose destination
- drag/drop or select files
- process

It also supports email/API ingestion, which is relevant to a developer-oriented SaaS.

### 4. Analytics dashboard

Rossum provides reporting around document volume, turnaround time, corrections, automation rate, and related processing metrics.

### 5. Schema / extraction configuration

Rossum’s schema tooling lets users customize captured fields, labels, IDs, data types, thresholds, visibility, and export names.

### Priority for Documate

The three most valuable screens to study are:

1. **Inbox:** Upload → Processing → Needs Review → Completed
2. **Extraction workspace:** Document/PDF ↔ Extracted structured content
3. **Schema builder:** Define fields → preview extraction → test against document → save schema

---

# Strategic Discussion: Documate vs. In-House AI Development

## User — August 30, 2026, 02:16 UTC

> documate clients are vanishing as they are considering developing it by their own. Ai agentic development has made it easier to develop such kind of tool inhouse. what kind of enhancement can we do in documate which may lead to keep it still difficult to develop or make them feasiable to keep it using? you can check nanonets, rossum, graip ai etc what are their specialities?

## Assistant

The central strategic issue is bigger than improving OCR or extraction UI.

If customers believe they can assemble:

> OCR + LLM/VLM + JSON schema + workflow + API + React UI

quickly, then extraction accuracy alone is not enough of a moat.

The key question becomes:

> **What can Documate build around document intelligence that is expensive, risky, and continuously difficult for a customer to reproduce internally?**

The competitor landscape suggests that the market is moving beyond basic extraction.

### Nanonets

Nanonets is moving toward an **AI workforce / agentic automation** model:
- document intelligence
- agent builder
- business rules/context graphs
- human exception handling
- ERP/CRM connectors
- reconciliation
- analytics
- document generation
- AI agents that perform actions

The important lesson is that the moat is not OCR alone; it is the combination of **documents + rules + agents + integrations + execution + feedback**.

### Rossum

Rossum is strong around **enterprise transactional documents** such as invoices, POs, bills of lading, packing lists, and financial documents.

Its lifecycle is broadly:

> Ingest → classify → extract → validate → enrich → approve → communicate → export

Important strengths include:
- duplicate detection
- document splitting
- master-data matching
- ERP validation
- business rules
- approval workflows
- email communication
- audit trails
- analytics
- human-in-the-loop
- continuous learning

A particularly valuable concept is **confidence-driven human review**: users review only the uncertain fields/documents instead of checking everything.

### Graip.AI

Graip.AI pushes further into **process-specific document agents**, with capabilities such as:

> Classify → Extract → Validate → Search → Collect → Confirm → Respond → Transform → Reconcile

Its positioning emphasizes business processes and integrations rather than only extraction.

### Initial strategic recommendation

A potential architecture was proposed:

```text
                    DOCUMATE
                       │
        ┌──────────────┴──────────────┐
        │                             │
   DOCUMENT AI                 BUSINESS AI
        │                             │
  Understand                    Reason
  Extract                      Validate
  Classify                     Reconcile
  Search                       Decide
  Compare                      Act
        │                             │
        └──────────────┬──────────────┘
                       │
                HUMAN OVERSIGHT
                       │
              Exceptions / Approval
                       │
                       ▼
                BUSINESS SYSTEMS
            ERP / CRM / DMS / Email
```

Potential features discussed:
1. Multi-document reconciliation
2. Business rules engine
3. Confidence-based human review
4. Evidence/source citations
5. Continuous learning from corrections
6. Ask Documate / document knowledge search
7. Document comparison/version intelligence
8. Workflow automation
9. ERP/CRM/DMS integrations
10. Audit trail + analytics
11. Documate Agents

The strategic positioning considered was:

> **“The intelligence layer that turns documents into verified business outcomes.”**

However, the next user response identified a critical limitation to this direction.

---

# Strategic Correction: Do Not Overlap the ERP

## User — August 30, 2026, 02:36 UTC

> the most of the features you are suggesting are actually part of ERP/ system itself. to enable documate to that, they need to share alot of data to documate first. for that they'll need to do the development to make sure that everything is shared so that documate can do the reconciliation the same with the business brain idea. documate need to have access to their system because making decisions are part of their own system/ERP. So moving our solution to overlap those areas... how and why would they like to do these things documate?

## Assistant

This objection was accepted as correct.

The strategic direction should **not** be to turn Documate into an ERP-adjacent business brain if doing so requires customers to expose ERP data, master data, business rules, APIs, write-back permissions, and other internal systems.

The better question is:

> **What can Documate do with the data customers are already willing to give us, without requiring them to redesign or expose their core system?**

### Core insight

The ERP already owns:
- vendors
- customers
- products
- POs
- invoices
- approvals
- accounting rules
- inventory
- payments
- workflow
- master data
- business decisions

**Do not compete with that.**

Instead:

> **Documate should own the messy, unstructured information that exists before it reaches the ERP.**

That is where the ERP is weak and where manual effort is high.

### Recommended architecture

```text
EMAIL       PDF       IMAGE       EXCEL       SCAN
  │           │          │           │          │
  └───────────┴──────────┴───────────┴──────────┘
                          │
                          ▼
                 ┌──────────────────┐
                 │     DOCUMATE     │
                 │                  │
                 │ Understand       │
                 │ Extract          │
                 │ Structure        │
                 │ Validate         │
                 │ Enrich           │
                 │ Transform        │
                 │ Package          │
                 └────────┬─────────┘
                          │
                          ▼
                    ERP / CRM / DMS
```

The ERP remains the **system of record**.

Documate becomes the **system of understanding**.

### Better product concept

Instead of:

> Document → AI → Decision

use:

> **Document → AI → System-ready data**

The customer does not need to give Documate their ERP database. They only need to tell Documate what information the downstream system needs.

### Example

A supplier sends:
- quote.pdf
- technical_spec.pdf
- delivery_terms.pdf

The old process is:

> Email → employee opens PDFs → reads quote → copies into Excel → reads technical document → checks requirements → copies into ERP → manager reviews

Documate can instead:

> Email → understand all documents → classify → extract → normalize → detect inconsistencies → create ERP-ready payload → existing customer workflow

The ERP remains responsible for business decisions.

### Important differentiation

Documate should own:

**Understand**
- document structure
- semantic structure
- cross-document context
- normalization
- confidence
- provenance

**Transform**
- turn messy information into the exact format the customer's system expects

**Verify**
- extraction correctness
- internal consistency
- document trustworthiness

**Deliver**
- API
- JSON
- CSV
- Excel
- XML
- webhook
- SFTP
- other system-ready outputs

### Output contracts

A powerful concept proposed was allowing the customer to define an **Output Contract**:

```text
Supplier Onboarding

Required:
✓ Company name
✓ Registration number
✓ Address
✓ Tax number
✓ Bank information
✓ Contact
✓ Certifications

Rules:
- Registration number must exist
- Country is ISO code
- Currency must be ISO 4217
- Certifications must include expiry date
```

Documate handles the messy input and produces the expected output.

### Why this counters the AI-agent threat

An internal team can easily build:

> PDF → LLM → JSON

But production-grade document understanding requires:
- arbitrary inputs
- document classification
- structure discovery
- table understanding
- provenance
- confidence
- human correction
- schema versioning
- output contracts
- monitoring
- exception handling
- continuous improvement

The moat is therefore **operational complexity and accumulated document intelligence**, not secret AI technology.

### Strong boundary for Documate

**ERP owns:**
> What should happen?

**Documate owns:**
> What does this incoming information actually say?

**ERP owns:**
> Is this transaction allowed?

**Documate owns:**
> Is this document trustworthy and correctly understood?

**ERP owns:**
> How do we post it?

**Documate owns:**
> How do we turn messy input into something postable?

This minimizes integration resistance.

---

# Document Memory

## User — August 30, 2026, 08:02 UTC

> what benefits can i take from document memory?

## Assistant

The biggest benefit of **Document Memory** is not simply “Documate remembers old documents.”

It is:

> **Documate can use everything it has learned from previous documents to process the next document better, faster, and with less human intervention.**

The key idea is:

> **Every document teaches Documate how to handle the next one.**

### Example

Without memory:

> Every document is a new problem.

With memory:

> **Every document teaches Documate how to handle the next one.**

For a supplier with 1,000 invoices:

```text
Invoice arrives
       ↓
AI extracts
       ↓
Human corrects fields
       ↓
Documate learns
```

Later:

```text
Invoice arrives
       ↓
Documate recognizes supplier
       ↓
Recognizes their layout
       ↓
Knows where fields normally appear
       ↓
Knows their terminology
       ↓
Knows common variations
       ↓
Extracts automatically
       ↓
Human only sees exceptions
```

## 1. Supplier-specific intelligence

Documate can build a profile of a recurring document source:

```text
ABC INDUSTRIES

Documents processed: 4,281
Known layouts: 7
Languages: 2
Fields learned: 42

Extraction accuracy: 99.1%

Common variations
────────────────────────────
"Order Ref" → PO Number
"Net Amount" → Subtotal
"Payable" → Total

Known patterns
────────────────────────────
Invoice number → Header
PO → Header
Line items → Pages 2–4
Total → Last page

Recent changes
────────────────────────────
⚠ New invoice layout detected
```

This intelligence can be discovered automatically.

## 2. Reduce human review

Memory can help establish field-level confidence:

```text
Vendor: ABC Industries       99.9%
Invoice #: INV-82941          99.8%
PO #: PO-18392                99.7%
Currency: USD                 99.9%
Total: $18,420                 99.8%

⚠ Delivery date               71%
```

The reviewer can focus on:

> **Review 1 issue**

rather than reviewing the whole document.

## 3. Learn supplier terminology

Different suppliers may use:
- Purchase Order
- Order Reference
- Customer PO
- Our Reference
- PO Ref.

Documate can learn that these expressions map to the customer's desired field:

> PO Number

The customer-specific terminology becomes a learned dictionary.

## 4. Learn document layouts

If a supplier changes invoice design, Documate can recognize a new layout as a variation of a known document rather than treating it as a completely new problem.

## 5. Learn document relationships

Documate can understand that multiple attachments belong together:

```text
email
 ├── quote.pdf
 ├── pricing.xlsx
 └── terms.docx
```

Then it can connect later revised documents to earlier submissions.

## 6. Version intelligence

For contracts or recurring documents, memory can identify material changes:

```text
Payment terms
30 days → 45 days

Annual fee
$100,000 → $125,000

Termination notice
30 days → 60 days

Liability cap
$500K → $1M
```

This is more useful than a raw text diff.

## 7. Anomaly detection

Memory can establish a baseline.

If a supplier normally sends 2-page invoices and suddenly sends a 7-page document with:
- new sender
- new bank account
- new layout
- unusual currency
- unusually large amount

Documate can flag:

> **This document is unusual compared with what we've seen before.**

This does not require Documate to make the financial decision; it simply detects document-level anomalies.

## 8. Personalized extraction

Generic AI asks:

> What does this document say?

Documate can ask:

> **What does this document mean in the context of everything we've previously seen from this customer?**

For example:

```text
Document:
"Delivery: 15 days"

Previous documents:
"Delivery: 15 working days"

Interpretation:
15 working days

Confidence:
96%

Reason:
Consistent with 38 previous documents
from this supplier.
```

## 9. Document profiles

Expose the accumulated knowledge in the UI so the customer can see why Documate improves.

## 10. Memory reduces onboarding

A customer can begin with zero-shot processing and progressively move from:

> Learning phase

to:

> Patterns emerging

to:

> Supplier profiles established

to:

> Highly optimized processing

The system gets better because they use it.

## 11. Switching cost

An internal developer can build:

> PDF → OCR → LLM → JSON

But leaving Documate means potentially abandoning accumulated knowledge such as:

```text
12,000 suppliers
87,000 document patterns
millions of field corrections
customer-specific terminology
layout variations
document relationships
historical anomalies
processing behavior
```

The customer would have to recreate that knowledge.

## 12. Important distinction

Document Memory should not necessarily be branded as generic “Memory.”

Possible customer-facing names:
- **Document Intelligence**
- **Learned Document Context**
- **Documate Knowledge**

The underlying architecture can be memory, but the customer-facing value should be:

> **“Documate learns how your documents work.”**

## 13. Memory without ERP data

Useful memory can be built from:

**Documents**
- emails/attachments
- user corrections
- extraction schemas
- document layouts
- document relationships
- historical versions
- annotations
- accepted/rejected extractions

It does not require:
- customer database
- ERP database
- vendor master
- payment history
- inventory
- accounting rules

## 14. Suggested architecture

```text
                 DOCUMENT MEMORY
                       │
       ┌───────────────┼────────────────┐
       │               │                │
       ▼               ▼                ▼
   SUPPLIER         DOCUMENT          FIELD
   PROFILE           PROFILE          PROFILE
       │               │                │
       ▼               ▼                ▼
 Terminology       Layouts          Corrections
 Patterns          Versions         Confidence
 Variations        Relationships    Interpretations
       │               │                │
       └───────────────┼────────────────┘
                       │
                       ▼
               NEXT DOCUMENT
                       │
                       ▼
              Better processing
```

## 15. The compounding loop

```text
          Document
             ↓
         Extraction
             ↓
       Human correction
             ↓
      ┌──────────────┐
      │ DOCUMATE     │
      │ LEARNS       │
      └──────┬───────┘
             ↓
      Document Memory
             ↓
       Next document
             ↓
      Better extraction
             ↓
      Less correction
             ↓
      More automation
             ↓
       More learning
             ↺
```

The strategic conclusion was:

> **Don't make Documate remember documents. Make Documate remember how each organization communicates through documents.**

A company's ERP knows **what the company has decided**.

Documate could know **how the outside world communicates information to that company**.

That boundary fits the earlier constraint: it creates compounding value without requiring deep access to the customer's ERP.

---

# Summary of the Chat

## 1. The original problem

Documate is losing clients because AI-assisted/agentic development has made it much easier for companies to build basic document extraction tools internally.

The basic product:

> Upload → OCR → extract fields → JSON

is increasingly commoditized.

## 2. What not to do

Do **not** simply turn Documate into another ERP/workflow/business-decision platform.

Doing so creates a major adoption problem because customers would have to:
- expose ERP data
- expose master data
- develop APIs
- share business rules
- grant write access
- redesign integrations
- let Documate participate in decisions that already belong to their systems

That increases friction and can make the product less attractive.

## 3. The stronger strategic position

Keep the ERP as the system of record.

Position Documate as the **system of understanding** or **unstructured-data gateway**.

The boundary becomes:

> **External messy information → Documate → clean/system-ready information → customer's existing system**

Documate does not need to own the customer's business decisions.

It needs to understand and structure the information that enters the business.

## 4. The core product opportunity

Documate should become exceptionally good at:

1. Understanding arbitrary documents
2. Discovering document structure
3. Extracting structured information
4. Understanding tables and multi-page documents
5. Connecting related documents
6. Normalizing values
7. Preserving evidence/provenance
8. Assigning confidence
9. Learning from corrections
10. Detecting document-level anomalies
11. Transforming output into customer-defined schemas
12. Delivering system-ready payloads

## 5. The biggest potential moat: Document Memory

Document Memory should allow Documate to learn:

- how each supplier formats documents
- what terminology they use
- where information normally appears
- what document variations exist
- how users correct extracted fields
- which documents belong together
- how documents change over time
- what normal looks like for each source
- which patterns consistently produce high-confidence extraction

This creates a compounding loop:

> **More documents → more learning → better extraction → fewer corrections → higher automation → more value → greater switching cost.**

## 6. Why Document Memory fits the strategy

Most importantly, this memory can be built largely from **documents and user interaction**, rather than requiring deep ERP access.

That means Documate can become increasingly valuable while keeping integration requirements relatively light.

The strategic moat is therefore not:

> “Our AI model is impossible to copy.”

It is:

> **“Documate has accumulated deep knowledge about how your organization's external document world works.”**

## 7. The proposed strategic positioning

Potential positioning directions discussed:

> **Documate — The intelligence layer for the unstructured world.**

or:

> **Documate — Turn documents into reliable, system-ready information.**

or:

> **Documate — The system of understanding between documents and your business systems.**

The strongest underlying concept is:

> **The ERP knows what the company has decided. Documate knows what the outside world is communicating to the company.**

## 8. Product direction in one diagram

```text
              OUTSIDE WORLD
                    │
       Email / PDF / Scan / Excel
       Word / Images / Attachments
                    │
                    ▼
             ┌─────────────┐
             │  DOCUMATE   │
             │             │
             │ Understand  │
             │ Extract     │
             │ Structure   │
             │ Normalize   │
             │ Verify      │
             │ Remember    │
             │ Learn       │
             │ Transform   │
             └──────┬──────┘
                    │
                    ▼
          SYSTEM-READY INFORMATION
                    │
                    ▼
             ERP / CRM / DMS
                    │
                    ▼
           Customer's own workflow
```

### Final strategic thesis

**Do not compete with the customer's ERP for ownership of business decisions.**

Instead, make Documate the layer that makes the **unstructured external world understandable to the customer's existing systems**.

And make that layer increasingly valuable through **Document Memory**:

> **Documate learns how each organization receives, interprets, corrects, and structures information through documents.**

That creates a defensible, compounding asset that is much harder to reproduce with a one-off internal AI project than basic extraction itself.
