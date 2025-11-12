# Prompt Authoring Guide

This guide covers best practices for creating, managing, and versioning prompts for AI agents in the framework.

## Table of Contents

- [Overview](#overview)
- [Prompt File Format](#prompt-file-format)
- [Prompt Structure](#prompt-structure)
- [Authoring Best Practices](#authoring-best-practices)
- [Prompt Engineering Techniques](#prompt-engineering-techniques)
- [Versioning Prompts](#versioning-prompts)
- [Testing Prompts](#testing-prompts)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)

## Overview

Prompts are the core instructions that guide agent behavior. In this framework:
- Prompts are stored as YAML files with metadata and content
- Prompts are version-controlled in a GitHub repository
- Agents load prompts dynamically at runtime
- Prompts can be updated without redeploying agents

## Prompt File Format

### Basic Structure

```yaml
---
# Metadata section
name: prompt-name
version: 1.0.0
description: Brief description of what this prompt does
model_requirements:
  min_tokens: 4096
  temperature: 0.7
  max_tokens: 2000
input_schema:
  - name: parameter_name
    type: string
    required: true
    description: Parameter description
output_schema:
  type: object
  properties:
    result: string
    confidence: number
---
# Prompt content section
Your prompt instructions go here.

Use {{parameter_name}} for variable substitution.
```

### Metadata Fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Unique identifier for the prompt |
| `version` | Yes | Semantic version (major.minor.patch) |
| `description` | Yes | Clear explanation of prompt purpose |
| `model_requirements` | No | LLM configuration parameters |
| `input_schema` | Yes | Expected input parameters |
| `output_schema` | Yes | Expected output format |
| `tags` | No | Categories or labels for organization |
| `author` | No | Prompt author/team |
| `last_updated` | No | Last modification date |

### Model Requirements

```yaml
model_requirements:
  min_tokens: 4096              # Minimum context window
  max_tokens: 2000              # Maximum response length
  temperature: 0.7              # Creativity (0.0-1.0)
  top_p: 0.9                    # Nucleus sampling
  frequency_penalty: 0.0        # Reduce repetition
  presence_penalty: 0.0         # Encourage new topics
```

### Input Schema

Define expected inputs with validation:

```yaml
input_schema:
  - name: user_query
    type: string
    required: true
    description: The user's question or request
    max_length: 1000
    
  - name: context
    type: string
    required: false
    description: Additional context for the query
    
  - name: priority
    type: enum
    values: [low, normal, high, urgent]
    required: false
    default: normal
    
  - name: tags
    type: array
    items: string
    required: false
```

### Output Schema

Define expected output structure:

```yaml
output_schema:
  type: object
  required_properties: [result, confidence]
  properties:
    result:
      type: string
      description: The main response
    confidence:
      type: number
      minimum: 0.0
      maximum: 1.0
      description: Confidence score
    metadata:
      type: object
      description: Additional information
```

## Prompt Structure

### Best Structure Template

```yaml
---
name: agent-action-prompt
version: 1.0.0
description: Clear, concise description
model_requirements:
  min_tokens: 4096
  temperature: 0.7
input_schema:
  - name: input_param
    type: string
    required: true
output_schema:
  type: object
  properties:
    output_field: string
---
# Role Definition
You are [specific role with clear expertise].

# Task Description
Your task is to [clear, specific objective].

# Guidelines
1. [Specific guideline]
2. [Specific guideline]
3. [Specific guideline]

# Input
{{input_param}}

# Output Format
Provide your response as JSON:
{
  "output_field": "your response here"
}

# Constraints
- [Specific constraint]
- [Specific constraint]
```

## Authoring Best Practices

### 1. Be Specific and Clear

❌ **Bad**: "Analyze the data"
✅ **Good**: "Analyze the provided JSON data and identify the top 3 anomalies based on statistical variance"

### 2. Define the Role Clearly

```yaml
---
You are an expert DevOps engineer with 10+ years of experience in CI/CD pipeline optimization.
Your specialty is identifying bottlenecks and suggesting actionable improvements.
```

### 3. Provide Context and Examples

```yaml
---
# Example of good input:
Input: "Our build takes 45 minutes. Logs show npm install takes 20 minutes."

# Example of good output:
{
  "analysis": "npm install is the primary bottleneck",
  "recommendations": [
    "Implement npm caching in CI pipeline",
    "Use npm ci instead of npm install"
  ],
  "expected_improvement": "15-20 minute reduction"
}
```

### 4. Use Clear Variable Names

❌ **Bad**: `{{d}}`, `{{x}}`, `{{val}}`
✅ **Good**: `{{ticket_description}}`, `{{user_query}}`, `{{error_log}}`

### 5. Specify Output Format

Always define the exact output format expected:

```yaml
---
Return your response as JSON with this exact structure:
{
  "summary": "brief summary (max 100 chars)",
  "details": "detailed explanation",
  "confidence": 0.95,
  "tags": ["tag1", "tag2"]
}

Do not include any text outside the JSON structure.
```

### 6. Include Constraints and Boundaries

```yaml
---
Constraints:
- Response must be under 500 words
- Use professional, technical language
- Include specific file names and line numbers when relevant
- Do not make assumptions about code you cannot see
```

## Prompt Engineering Techniques

### 1. Chain of Thought

Encourage step-by-step reasoning:

```yaml
---
Analyze the issue using the following steps:

Step 1: Identify the error type and severity
Step 2: Trace the error to its root cause
Step 3: Determine impact on system functionality
Step 4: Propose 2-3 specific solutions

Provide your reasoning for each step.
```

### 2. Few-Shot Learning

Provide examples of desired behavior:

```yaml
---
Example 1:
Input: "API returns 500 error"
Output: {
  "category": "Server Error",
  "priority": "high",
  "suggested_action": "Check server logs for stack trace"
}

Example 2:
Input: "Button is misaligned"
Output: {
  "category": "UI Bug",
  "priority": "low",
  "suggested_action": "Review CSS styles for button component"
}

Now process this input:
{{ticket_description}}
```

### 3. Role Playing

Define a specific persona:

```yaml
---
You are a senior software architect reviewing a pull request.
You value clean code, proper separation of concerns, and comprehensive testing.
You provide constructive, specific feedback with code examples.
```

### 4. Structured Output

Use JSON Schema or specific formats:

```yaml
---
Return a structured test plan in JSON:
{
  "test_suites": [
    {
      "name": "Suite name",
      "test_cases": [
        {
          "name": "Test case name",
          "type": "unit|integration|e2e",
          "priority": "critical|high|medium|low",
          "steps": ["step1", "step2"],
          "expected_result": "description"
        }
      ]
    }
  ],
  "estimated_coverage": "percentage"
}
```

### 5. Negative Instructions

Tell the model what NOT to do:

```yaml
---
Do NOT:
- Make assumptions about code you haven't seen
- Suggest changes without explaining the rationale
- Provide vague or generic advice
- Include placeholder code like "// TODO" or "// Your code here"
```

## Versioning Prompts

### Semantic Versioning

Follow semantic versioning (MAJOR.MINOR.PATCH):

- **MAJOR**: Breaking changes to input/output schema
- **MINOR**: New features, backward-compatible changes
- **PATCH**: Bug fixes, clarifications, optimizations

### Version History

Include changelog in prompt metadata:

```yaml
---
name: notification-formatter
version: 2.1.0
changelog:
  - version: 2.1.0
    date: 2025-11-11
    changes:
      - Added support for Slack Block Kit formatting
      - Improved email HTML generation
  - version: 2.0.0
    date: 2025-11-01
    changes:
      - BREAKING: Changed output schema to include metadata
      - Added priority-based formatting
  - version: 1.0.0
    date: 2025-10-15
    changes:
      - Initial release
---
```

### Backward Compatibility

When making breaking changes:

1. Increment MAJOR version
2. Keep old version available
3. Provide migration guide
4. Deprecate old version gradually

## Testing Prompts

### Unit Testing

Test prompts with sample inputs:

```yaml
# test/prompts/notification-formatter.test.yaml
test_cases:
  - name: Email notification
    input:
      message: "Deployment completed successfully"
      channel: "email"
      priority: "normal"
    expected_output:
      formatted_message: "[Success] Deployment completed successfully"
      subject: "Deployment Notification"
    
  - name: Urgent SMS notification
    input:
      message: "Critical error detected"
      channel: "sms"
      priority: "urgent"
    expected_output:
      formatted_message: "URGENT: Critical error detected"
```

### A/B Testing

Compare prompt versions:

```yaml
experiments:
  - name: Conciseness Test
    prompts:
      - version: 1.0.0
        description: Original verbose prompt
      - version: 2.0.0
        description: Concise prompt
    metrics:
      - response_length
      - accuracy
      - user_satisfaction
```

### Quality Metrics

Track prompt performance:

- **Accuracy**: Percentage of correct responses
- **Consistency**: Response variance across similar inputs
- **Latency**: Average response time
- **Token Usage**: Average tokens per request
- **User Satisfaction**: Feedback ratings

## Examples

### Example 1: Code Review Agent

```yaml
---
name: code-reviewer
version: 1.0.0
description: Reviews pull requests and provides constructive feedback
model_requirements:
  min_tokens: 8192
  temperature: 0.3
  max_tokens: 2000
input_schema:
  - name: code_diff
    type: string
    required: true
    description: Git diff of the changes
  - name: pr_description
    type: string
    required: false
    description: Pull request description
output_schema:
  type: object
  properties:
    overall_assessment: string
    issues: array
    suggestions: array
    approval_status: string
---
You are a senior software engineer conducting a code review.
Your goal is to ensure code quality, maintainability, and best practices.

# Code Diff
{{code_diff}}

# PR Description
{{pr_description}}

Review the code changes and provide:

1. **Overall Assessment**: High-level evaluation (2-3 sentences)

2. **Issues**: List specific problems with:
   - File name and line number
   - Problem description
   - Severity (critical/major/minor)
   - Suggested fix

3. **Suggestions**: Positive recommendations for:
   - Performance improvements
   - Code clarity enhancements
   - Test coverage additions

4. **Approval Status**: One of:
   - "approved" - Ready to merge
   - "approved_with_comments" - Minor issues, can merge
   - "changes_requested" - Must address issues before merge

Return as JSON:
{
  "overall_assessment": "string",
  "issues": [
    {
      "file": "path/to/file.cs",
      "line": 42,
      "severity": "major",
      "description": "...",
      "suggested_fix": "..."
    }
  ],
  "suggestions": ["...", "..."],
  "approval_status": "approved|approved_with_comments|changes_requested"
}

Focus on:
- Security vulnerabilities
- Performance bottlenecks
- Code duplication
- Missing error handling
- Inadequate test coverage

Do NOT:
- Be overly pedantic about style preferences
- Suggest changes without clear rationale
- Approve code with security issues
```

### Example 2: Ticket Triager

```yaml
---
name: ticket-triager
version: 1.0.0
description: Classifies and prioritizes support tickets
model_requirements:
  min_tokens: 4096
  temperature: 0.5
input_schema:
  - name: ticket_title
    type: string
    required: true
  - name: ticket_description
    type: string
    required: true
  - name: user_tier
    type: enum
    values: [free, standard, premium, enterprise]
    required: true
output_schema:
  type: object
  properties:
    category: string
    priority: string
    estimated_effort: string
    assigned_team: string
    suggested_response: string
---
You are an experienced technical support manager specializing in ticket triage.

# Ticket Information
Title: {{ticket_title}}
Description: {{ticket_description}}
User Tier: {{user_tier}}

Analyze the ticket and provide:

1. **Category**: bug, feature_request, question, documentation, infrastructure

2. **Priority**: 
   - P0 (Critical): System down, data loss, security breach
   - P1 (High): Major feature broken, affecting multiple users
   - P2 (Medium): Minor feature issue, workaround available
   - P3 (Low): Enhancement, cosmetic issue

3. **Estimated Effort**: small (< 2h), medium (2-8h), large (> 8h)

4. **Assigned Team**: frontend, backend, devops, qa, product

5. **Suggested Response**: Draft a professional response acknowledging the issue

Priority Guidelines:
- Enterprise users: Increase priority by 1 level
- Free users: Do not exceed P2 priority
- Security issues: Always P0 regardless of tier
- Feature requests: Typically P3 unless affecting revenue

Return as JSON:
{
  "category": "bug",
  "priority": "P1",
  "estimated_effort": "medium",
  "assigned_team": "backend",
  "suggested_response": "Thank you for reporting this issue..."
}
```

### Example 3: Test Spec Generator

```yaml
---
name: test-spec-generator
version: 1.0.0
description: Generates comprehensive test specifications from feature descriptions
model_requirements:
  min_tokens: 8192
  temperature: 0.7
input_schema:
  - name: feature_description
    type: string
    required: true
  - name: acceptance_criteria
    type: string
    required: false
output_schema:
  type: object
  properties:
    test_strategy: string
    test_cases: array
    edge_cases: array
    non_functional_tests: array
---
You are a QA engineer specialized in test-driven development and comprehensive test planning.

# Feature Description
{{feature_description}}

# Acceptance Criteria
{{acceptance_criteria}}

Generate a complete test specification including:

## 1. Test Strategy
High-level approach to testing this feature (2-3 paragraphs)

## 2. Functional Test Cases
For each test case provide:
- **Name**: Descriptive test name
- **Type**: unit, integration, or e2e
- **Priority**: critical, high, medium, low
- **Preconditions**: Setup required
- **Steps**: Detailed test steps
- **Expected Result**: What should happen
- **Test Data**: Specific data needed

## 3. Edge Cases
Unusual scenarios that should be tested

## 4. Non-Functional Tests
- Performance requirements
- Security considerations
- Accessibility requirements
- Cross-browser compatibility

Return as JSON:
{
  "test_strategy": "...",
  "test_cases": [
    {
      "name": "...",
      "type": "unit|integration|e2e",
      "priority": "critical|high|medium|low",
      "preconditions": "...",
      "steps": ["step1", "step2"],
      "expected_result": "...",
      "test_data": "..."
    }
  ],
  "edge_cases": ["..."],
  "non_functional_tests": {
    "performance": ["..."],
    "security": ["..."],
    "accessibility": ["..."]
  }
}

Guidelines:
- Cover happy path, error scenarios, and edge cases
- Include both positive and negative test cases
- Consider boundary conditions
- Think about concurrent users and race conditions
```

## Troubleshooting

### Issue: LLM not following output format

**Solution**:
- Make output format requirements more explicit
- Add examples of correct output
- Use phrases like "You MUST return JSON" or "IMPORTANT: Follow this exact format"
- Lower temperature to reduce creativity

### Issue: Inconsistent responses

**Solution**:
- Reduce temperature (try 0.3-0.5)
- Add more specific constraints
- Provide more examples
- Include "step-by-step" reasoning instructions

### Issue: Response too long or short

**Solution**:
- Specify exact word/character limits
- Adjust `max_tokens` in model requirements
- Add constraints like "Keep response under 200 words"

### Issue: Prompt not loading

**Solution**:
- Verify YAML syntax is valid
- Check prompt file name matches expected format
- Ensure version exists in repository
- Review prompt loader logs

### Issue: Variables not substituting

**Solution**:
- Use correct syntax: `{{variable_name}}`
- Verify variable names match input schema
- Check for typos in variable names

## Further Reading

- [Architecture Overview](architecture.md)
- [Agent Development Guide](agent-development.md)
- [Deployment Guide](deployment.md)
- [Operations Runbook](operations.md)
- [OpenAI Prompt Engineering Guide](https://platform.openai.com/docs/guides/prompt-engineering)
