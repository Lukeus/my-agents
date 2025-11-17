# Pull Request

## Description
<!-- Provide a clear and concise description of the changes -->

## Type of Change
<!-- Mark the relevant option with an "x" -->

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Refactoring (no functional changes, code improvement)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Test coverage improvement
- [ ] Infrastructure/DevOps change

## Related Issues
<!-- Link to related issues using #issue_number -->

Closes #
Related to #

## Changes Made
<!-- Provide a detailed list of changes -->

-
-
-

## Testing
<!--  Describe the tests you ran and their results -->

### Test Coverage
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing performed
- [ ] Test coverage maintained or improved

### Test Results
```
Total Tests: 
Passed: 
Failed:  
Coverage: %
```

## Code Quality Checklist

### General
- [ ] Code follows the project's coding conventions (.editorconfig)
- [ ] Self-review of code performed
- [ ] Code is self-documenting with clear variable/method names
- [ ] No commented-out code or debug statements left in
- [ ] No console.log, print, or similar debug statements left in production code

### Documentation
- [ ] XML documentation added for new public APIs (C#)
- [ ] JSDoc comments added for new TypeScript functions
- [ ] README updated (if applicable)
- [ ] Architecture diagrams updated (if applicable)
- [ ] API documentation updated (if applicable)

### Security
- [ ] No sensitive information (passwords, API keys, tokens) in code
- [ ] Input validation added for user-facing features
- [ ] SQL injection prevention verified (if applicable)
- [ ] XSS prevention verified (if applicable)
- [ ] Authentication/authorization properly implemented (if applicable)

### Performance
- [ ] No obvious performance issues introduced
- [ ] Database queries optimized (if applicable)
- [ ] Caching considered where appropriate
- [ ] No memory leaks introduced

### Error Handling
- [ ] Appropriate error handling added
- [ ] Errors logged with sufficient context
- [ ] User-friendly error messages provided
- [ ] Retry logic implemented for transient failures (if applicable)

### Testing
- [ ] All new code has tests
- [ ] All existing tests pass locally
- [ ] Edge cases and error scenarios tested
- [ ] Test names are descriptive and follow conventions

### Backend Specific (C#/.NET)
- [ ] Nullable reference types properly handled
- [ ] Async/await used correctly (no blocking calls)
- [ ] Dependency injection used appropriately
- [ ] LINQ queries are efficient
- [ ] Entity Framework queries avoid N+1 problems
- [ ] Domain events properly published (if applicable)

### Frontend Specific (TypeScript/Vue)
- [ ] Components are properly typed
- [ ] No prop drilling (use composables/stores if needed)
- [ ] Reactive state managed correctly
- [ ] Accessibility considerations addressed (ARIA labels, keyboard navigation)
- [ ] Responsive design verified

## Deployment Notes
<!-- Any special deployment considerations, migrations, configuration changes, etc. -->

### Database Changes
- [ ] No database changes
- [ ] Database migrations included
- [ ] Migration tested locally
- [ ] Rollback plan documented

### Configuration Changes
- [ ] No configuration changes
- [ ] Environment variables documented
- [ ] Configuration changes documented in README
- [ ] Default values provided

### Breaking Changes
<!-- If there are breaking changes, describe migration path -->

- [ ] No breaking changes
- [ ] Breaking changes documented
- [ ] Migration guide provided
- [ ] Affected consumers notified

## Screenshots/Videos
<!-- If applicable, add screenshots or videos to demonstrate the changes -->

## Performance Impact
<!-- Describe any performance impact, positive or negative -->

- [ ] No performance impact
- [ ] Performance improved
- [ ] Performance impact measured and acceptable
- [ ] Performance benchmarks included

## Reviewer Checklist
<!-- For reviewers -->

- [ ] Code follows project architecture (Clean Architecture, DDD patterns)
- [ ] Code is maintainable and readable
- [ ] Tests adequately cover the changes
- [ ] Documentation is clear and complete
- [ ] No obvious security vulnerabilities
- [ ] Performance is acceptable
- [ ] Error handling is appropriate
- [ ] Code is ready for production

## Additional Notes
<!-- Any additional information for reviewers -->
