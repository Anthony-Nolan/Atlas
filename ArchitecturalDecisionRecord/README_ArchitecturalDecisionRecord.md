# Overview

This folder contains an Architectural Decision Record, or ADR, of the architectural decisions made in the development of the ATLAS project. 

See https://github.com/joelparkerhenderson/architecture_decision_record for more information on the ADR pattern.

## Guidelines for adding ADRs

- ADRs should be *immutable* - if a decision is made that invalidates a previous one, a new ADR should be created to track it. 

The template `000-template.md` can be used as a template for new files.

New ADRs should be numbered to ensure they are displayed in order of addition. 

Each file aims to give an indication of: 
- What technical decision has been made.
- When it was made.
- Rationale behind the decision. 

## ATLAS Phases

The ADRs in Atlas are split into phases, representing discrete development teams. 

### Phase 0 - Anthony Nolan Search Algorithm

ATLAS was originally ported from Anthony Nolan's internal matching algorithm, and as such inherited several technical decisions from that project.
 
### Phase 1 

The initial phase of work on ATLAS as a standalone product.

### Phase 2 

Phase 1 covered enough work to deliver ATLAS as a product suitable to replace Anthony Nolan's internal matching algorithm, but did not fully 
cover the all work necessary to scale the algorithm to a significantly larger dataset, as well as some other features required to prepare ATLAS for international use. 