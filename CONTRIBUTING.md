# Contributing to AI Orchestration Multi-Agent Framework

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Follow project coding standards

## Development Workflow

### Backend Development

1. **Fork and Clone**
   ```powershell
   git clone https://github.com/your-username/my-agents.git
   cd my-agents
   ```

2. **Create a Branch**
   ```powershell
   git checkout -b feature/your-feature-name
   ```

3. **Make Changes**
   - Follow clean architecture principles
   - Write unit tests for new features
   - Ensure all tests pass
   - Follow C# coding conventions

4. **Commit**
   ```powershell
   git add .
   git commit -m "feat: add your feature description"
   ```

5. **Push and Create PR**
   ```powershell
   git push origin feature/your-feature-name
   ```

### Frontend/UI Development

1. **Setup Frontend**
   ```powershell
   cd ui
   corepack enable
   pnpm install
   ```

2. **Create a Branch**
   ```powershell
   git checkout -b feature/ui-your-feature-name
   ```

3. **Make Changes**
   - Follow clean architecture in frontend (Domain → Application → Presentation)
   - Use TypeScript with strict mode
   - Write tests for Pinia stores and composables
   - Use design system components
   - Ensure accessibility

4. **Test Your Changes**
   ```powershell
   pnpm lint          # Run linting
   pnpm test          # Run tests
   pnpm build         # Build all apps
   ```

5. **Commit and Push**
   ```powershell
   git add .
   git commit -m "feat(ui): add your feature description"
   git push origin feature/ui-your-feature-name
   ```

## Commit Message Convention

Follow conventional commits format:

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `test:` Test additions or modifications
- `refactor:` Code refactoring
- `perf:` Performance improvements
- `chore:` Build process or auxiliary tool changes

## Coding Standards

### C# Style Guide

- Use PascalCase for public members
- Use camelCase for private fields with `_` prefix
- Use `async/await` for asynchronous code
- Follow SOLID principles
- Use dependency injection
- Write XML documentation for public APIs

### Clean Architecture Rules

- Dependencies point inward (Presentation → Application → Domain)
- Infrastructure implements interfaces from Application/Domain
- No direct dependencies from Domain to Infrastructure
- Use interfaces for all external integrations

### Testing Requirements

- Unit tests for all business logic (>80% coverage)
- Integration tests for inter-agent communication
- Mock external dependencies
- Use xUnit, FluentAssertions, and Moq

### UI/Frontend Standards

- Use TypeScript for all UI code
- Follow Vue 3 Composition API patterns
- Use Pinia for state management with clean architecture
- Write unit tests for Pinia stores and composables
- Use design system components from `@agents/design-system`
- Follow Tailwind CSS conventions (no inline styles)
- Ensure accessibility (ARIA labels, keyboard navigation)
- Test in both light and dark themes
- Run `pnpm lint` before committing

### Prompt Development

- Include metadata (name, version, description)
- Define input/output schemas
- Version prompts semantically
- Test prompts with both Ollama and Azure OpenAI
- Document prompt parameters

## Pull Request Process

1. Ensure all tests pass
2. Update documentation if needed
3. Add/update tests for new functionality
4. Request review from maintainers
5. Address review feedback
6. Squash commits if requested

## Questions?

Open an issue or discussion for questions about contributing.
