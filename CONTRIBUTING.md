# Contributing to AI Orchestration Multi-Agent Framework

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Follow project coding standards

## Development Workflow

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
