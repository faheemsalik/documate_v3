# Documate Marketing Website — Full Business Plan

> **Type:** Product / marketing business plan (not Phase 1 engineering exploration)  
> **Status:** Draft for stakeholder review (2026-09-16)  
> **Repo home:** `documate_v3/docs/plans/19-documate-marketing-website-business-plan.md`  
> **Backlog link:** [18-remaining-work-backlog.md](./18-remaining-work-backlog.md) item #8  
> **Design reference:** [VoomHub](https://app.voomhub.com/) — visual language, section rhythm, conversion layout  
> **Tech mandate (locked intent):** React + **Next.js** for the public site  

When engineering starts, open a **Phase 1 exploration** from this plan (IA, brand tokens, hosting, CMS). Do not jump to DQ until exploration + implementation plan gates pass.

---

## 1. Executive summary

Build a **public Documate marketing website** that sells document intelligence (ingest → split/classify → extract → deliver) with a modern, conversion-oriented landing experience inspired by VoomHub’s clarity and structure—not a clone of VoomHub’s product.

The customer **app** (`apps/web`) stays separate. This site is for prospects, SEO, pricing narrative, and demos—not authenticated document ops.

---

## 2. Problem & opportunity

| Today | Gap |
|-------|-----|
| Product capability advancing (v3 API, email intake, multi-doc) | No dedicated marketing surface that matches the product story |
| Customer app UI is functional | Brand story and first impression live elsewhere or look dated |
| Competitors and sibling brands (e.g. VoomHub) show polished one-platform landings | Documate needs an equivalent **trust + clarity** front door |

**Opportunity:** A Next.js site that explains Documate in one scroll: problem → how it works → use cases → trust → CTA (demo / contact / trial).

---

## 3. Goals & success metrics

### Business goals

1. Credible public brand for Documate v3.  
2. Convert visitors → demo requests / waitlist / contact.  
3. Support sales with clear positioning vs “generic OCR” and vs legacy Documate.  
4. Shareable URLs for features (API, email intake, multi-doc, agents).

### Success metrics (first 90 days post-launch)

| Metric | Target (initial) |
|--------|------------------|
| Organic + direct sessions | Baseline + trend (set after week 2) |
| Demo / contact form submits | Track weekly; goal set with sales |
| Bounce on hero | Improve vs current site (if any) |
| Lighthouse Performance / Accessibility | ≥ 90 mobile on primary landing |
| Time to publish content change | &lt; 1 day for copy (CMS or MDX) |

---

## 4. Audience & positioning

### Primary audiences

1. **Ops / AP leads** — invoices, DNs, multi-doc packs, email-to-queue.  
2. **IT / integration owners** — External API, webhooks, Simplicity / ERP / HAPA.  
3. **Partner ISVs** — embed Documate behind their product.

### Positioning (draft)

> Documate turns messy document intake into structured, routable business data—split, classify, extract, and deliver with agents you control.

Differentiate: **File vs Document**, queue routes, email intake, multi-doc packs—not only “AI OCR.”

---

## 5. Design direction (VoomHub-inspired)

Reference experience: [app.voomhub.com](https://app.voomhub.com/).

### Borrow (patterns)

- Strong **hero** (one composition): brand-forward, short headline, one supporting line, CTA group  
- **Pillar / suite** sections with one job each  
- Trust strip (logos / industries)  
- Before/after or “why us” contrast  
- Clear **pricing or packaging** narrative (even if “Contact us”)  
- Footer IA: product, resources, trust/legal  
- Motion used for presence, not noise  

### Do not copy

- VoomHub product modules, pricing numbers, or trademarked copy  
- Purple-on-white / generic AI clichés if they conflict with Documate brand  
- Building an operations **app** inside the marketing site  

### Documate-specific visual anchors

- Document → structured JSON / queue metaphor  
- Split/classify of mixed PDFs  
- Email intake → channel  
- Agent / schema accuracy  

**Brand tokens:** Define CSS variables (color, type, radius, motion) in exploration; pick expressive fonts (avoid Inter/Roboto defaults). Prefer real product/atmosphere imagery over pure abstract gradients.

---

## 6. Recommended tech stack

| Layer | Choice | Notes |
|-------|--------|--------|
| Framework | **Next.js** (App Router) + **React** + TypeScript | Per mandate |
| Styling | Tailwind CSS + CSS variables | Fast landing iteration |
| Content | MDX and/or headless CMS (Sanity / Contentful) TBD | Decision in exploration |
| Animations | Framer Motion or CSS | 2–3 intentional motions on hero |
| Forms | Server Actions or form provider | Demo request / contact |
| Analytics | Privacy-friendly (Plausible / GA4) TBD | |
| Hosting | Vercel or existing Documate infra | Decision Required |
| SEO | Metadata API, OG images, sitemap | |

**Out of scope for v1 site:** Authenticated app shell, Documate API proxy for real processing, customer login (link out to app).

---

## 7. Information architecture (proposed v1)

| Route | Purpose |
|-------|---------|
| `/` | Hero, how it works, use cases, trust, CTA |
| `/product` or `/how-it-works` | File → Document → Agent → webhook |
| `/use-cases` | AP invoice, logistics DN, multi-doc, email intake |
| `/developers` | API overview + link to full API docs (#2 backlog) |
| `/pricing` | Packages or “Talk to us” |
| `/company` / `/contact` | About + form |
| `/legal/*` | Privacy, terms |

Optional later: blog, playbooks, status page.

---

## 8. Delivery phases (business, not DQ)

| Phase | Outcome | Gate |
|-------|---------|------|
| **B0 — Align** | Approve this business plan; brand name/domain | Stakeholder sign-off |
| **B1 — Exploration** | Engineering Phase 1: IA, brand, hosting, CMS | Plan sequence Phase 1 |
| **B2 — Implementation plan** | Component system, pages, forms, SEO | Phase 2 |
| **B3 — Dispatch queue** | Page/DQ build order | Phase 3 |
| **B4 — Soft launch** | Staging URL for sales | QA |
| **B5 — Public launch** | DNS + analytics + redirects | Marketing go |

---

## 9. Team & ownership

| Role | Responsibility |
|------|----------------|
| Product / founder | Positioning, pricing story, final copy |
| Design | Brand tokens, layout comps (or design-in-code) |
| Engineering | Next.js site, forms, SEO, deploy |
| Sales | CTA targets, demo process |
| Legal | Privacy / terms |

---

## 10. Budget & effort (rough)

| Item | Estimate band |
|------|----------------|
| Brand + landing comps | 3–8 days |
| Next.js shell + 5–7 pages | 1–2 weeks |
| Forms + analytics + SEO | 2–4 days |
| Legal pages | 1–2 days (content) |
| Soft launch polish | 2–3 days |

Refine after B1 exploration. Prefer one strong landing over many thin pages.

---

## 11. Risks & constraints

| Risk | Mitigation |
|------|------------|
| Scope creep into full product app | Hard split: marketing vs `apps/web` |
| Copy/pricing undecided | Soft-launch with Contact CTA only |
| Brand clash with customer app redesign (#1) | Shared token doc; sequence brand decisions once |
| API docs unfinished (#2) | `/developers` teaser + “docs coming” or Postman link |
| Over-cloning VoomHub | Design review against Documate-only brand test |

---

## 12. Decision Required (before Phase 1 engineering)

| ID | Question | Options (draft) |
|----|----------|-----------------|
| **W1** | Repo location | A) `apps/marketing` in monorepo · B) separate `documate-web` repo |
| **W2** | Domain | e.g. `documate.ai` / subdomain TBD |
| **W3** | CMS | A) MDX in repo · B) headless CMS |
| **W4** | Primary CTA | A) Book demo · B) Waitlist · C) Contact sales |
| **W5** | Pricing page | A) Public tiers · B) Contact-only |
| **W6** | Hosting | A) Vercel · B) existing cloud |

---

## 13. Relationship to other backlog items

| Item | Relationship |
|------|----------------|
| #1 Customer FE redesign | Same brand tokens preferred; different app |
| #2 API / guides | Linked from `/developers` |
| #3 Woodvale live | Website can cite “production-ready” after pilot sign-off |

---

## 14. Output contract

| | |
|--|--|
| **Finalized** | Intent: Next.js marketing site; VoomHub as **design reference**; separate from customer app; business phases B0–B5 |
| **Pending** | W1–W6 decisions |
| **Assumptions** | Sales will own demo funnel; API docs may trail site v1 |
| **Risks** | Brand/token drift vs app redesign; scope creep |
| **Readiness** | **Ready for stakeholder review** → then Phase 1 exploration when W1–W6 answered |
