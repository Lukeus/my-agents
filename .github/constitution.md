# My-Agents Constitution

## Core Principles

### I. Agent-First Architecture
Every feature begins as an independently deployable **agent module**.  
Agents must be self-contained, stateless where possible, event-driven, and follow clean-architecture boundaries.  
Each agent must clearly define: its inputs, outputs, capabilities, and failure modes.

### II. Universal Interface Contracts
All agents must expose a consistent interface surface:  
- **HTTP + JSON** for synchronous invocations  
- **Dapr Pub/Sub** for async event-driven messaging  
- **SQL Server 17** (latest compatible) for durable state, embeddings, and metadata  
- **C# (.NET 9)** for domain and application layers only  
No agent may define custom ad-hoc protocols. Interop is a first-class requirement.

### III. Test-First Agent Development
Tests define the agent contract before implementation:  
- Red → Green → Refactor  
- Contract tests validate use-case boundaries  
- Message schema tests ensure compatibility  
- SQL Server test containers run domain-level persistence tests  
Agents cannot be merged unless the full test suite validates the contract.

### IV. Integration + Event Flow Testing
All multi-agent features require integration validation:  
- Pub/Sub flows  
- SQL embeddings/indexing interactions  
- Cross-agent classification or enrichment pipelines  
- Feature/agent version handshakes  
Backward compatibility must be proven, not assumed.

### V. Observability, Versioning, and Minimalism
Each agent must ship with:  
- Structured logging (OpenTelemetry)  
- Distributed tracing across agent hops  
- Metrics for message throughput + failure rates  

Versioning follows **MAJOR.MINOR.PATCH**, with migration notes included in each release.  
Agents must be ruthlessly simple—favor clarity and maintainability over cleverness.

---

## Platform Requirements

### Technology + Runtime Standards
- **.NET 9** for all agent logic, use cases, domain models, and integration workflows  
- **Dapr** for all inter-service communication (Pub/Sub, secrets, bindings, state)  
- **MS Aspire** for local development orchestration  
- **SQL Server 17** for:  
  - metadata persistence  
  - vector embeddings  
  - classification tables  
  - rules/config state  

### Security + Compliance
- No agent may store secrets in code  
- All agent-to-agent communication must run through Dapr identity  
- SQL schemas must follow least-privilege access  
- Embeddings must not contain sensitive data unless masked or anonymized  

### Performance Standards
- Agents must respond synchronously within **<300ms** for standard workloads  
- Async processing pipelines must guarantee at-least-once semantics  
- Large classification processes must execute in streaming batches, not bulk loads  

---

## Development Workflow

### Branching + Review Process
- `main` is always deploy-ready  
- Work occurs in short-lived branches tied to specs  
- PRs require:  
  - contract tests  
  - use-case tests  
  - schema validation  
  - architectural boundary verification  

### Quality Gates
- Code must follow Clean Architecture rules (domain → application → interface → infra)  
- Any new agent must include a deployment manifest + Dapr component configs  
- SQL changes must ship with migration scripts  
- No circular dependencies across agents or libraries  
- All agents must include an automated self-test endpoint  

### Deployment
- Each agent deploys independently  
- Version bumps must be explicit  
- Event-driven workflows must include backward-compatible message schemas  
- Aspire is used for development preview; production uses Kubernetes or container apps  

---

## Governance

The **My-Agents Constitution** overrides any team-specific practices.  
All changes to the constitution require:  
1. A written proposal  
2. Impact analysis on existing agents  
3. Migration plan if breaking  
4. Approval through the governance group  

All PR reviewers must verify:  
- Agent boundaries are respected  
- Messaging contracts are stable  
- No feature violates clean architecture or platform standards  

Use the `/docs/architecture` folder for runtime guidance, patterns, and decision records.

**Version**: 1.0.0 | **Ratified**: 2025-11-16 | **Last Amended**: 2025-11-16
