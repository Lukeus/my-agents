using Agents.Infrastructure.Prompts.Models;
using Agents.Infrastructure.Prompts.Services;
using FluentAssertions;
using Xunit;

namespace Agents.Tests.Unit.Infrastructure;

public class PromptValidatorTests
{
    private readonly PromptValidator _validator;

    public PromptValidatorTests()
    {
        _validator = new PromptValidator();
    }

    [Fact]
    public void ValidatePrompt_WithValidPrompt_ShouldReturnValid()
    {
        // Arrange
        var prompt = CreateValidPrompt();

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePrompt_WithMissingName_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.Name = string.Empty;

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name is required"));
    }

    [Fact]
    public void ValidatePrompt_WithMissingVersion_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.Version = string.Empty;

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("version is required"));
    }

    [Fact]
    public void ValidatePrompt_WithInvalidSemanticVersion_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.Version = "invalid-version";

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid semantic version"));
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.1.3")]
    [InlineData("1.0.0-beta")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("1.0.0+build.123")]
    [InlineData("1.0.0-beta+build.123")]
    public void ValidatePrompt_WithValidSemanticVersion_ShouldPass(string version)
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.Version = version;

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePrompt_WithMissingDescription_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.Description = string.Empty;

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("description is required"));
    }

    [Fact]
    public void ValidatePrompt_WithEmptyContent_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Content = string.Empty;

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("content is required"));
    }

    [Fact]
    public void ValidatePrompt_WithUndefinedTemplateVariable_ShouldReturnWarning()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Content = "Test {{undefined_variable}} content";
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "defined_param", Type = "string", Required = true }
        };

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeTrue(); // Warnings don't make it invalid
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("undefined_variable") && w.Contains("not defined in input schema"));
    }

    [Fact]
    public void ValidatePrompt_WithUnusedRequiredParameter_ShouldReturnWarning()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Content = "Test content";
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "unused_param", Type = "string", Required = true }
        };

        // Act
        var result = _validator.ValidatePrompt(prompt);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("unused_param") && w.Contains("not used in template"));
    }

    [Fact]
    public void ValidateInputs_WithAllRequiredParameters_ShouldReturnValid()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "param1", Type = "string", Required = true },
            new() { Name = "param2", Type = "string", Required = false }
        };

        var inputs = new Dictionary<string, object>
        {
            ["param1"] = "value1"
        };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateInputs_WithMissingRequiredParameter_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "required_param", Type = "string", Required = true }
        };

        var inputs = new Dictionary<string, object>();

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("required_param") && e.Contains("missing"));
    }

    [Fact]
    public void ValidateInputs_WithNullSchema_ShouldReturnValid()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = null;
        var inputs = new Dictionary<string, object> { ["any"] = "value" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateInputs_StringParameter_WithMinLength_ShouldValidate()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "text", Type = "string", Required = true, MinLength = 5 }
        };

        var inputs = new Dictionary<string, object> { ["text"] = "abc" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at least 5 characters"));
    }

    [Fact]
    public void ValidateInputs_StringParameter_WithMaxLength_ShouldValidate()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "text", Type = "string", Required = true, MaxLength = 5 }
        };

        var inputs = new Dictionary<string, object> { ["text"] = "too long text" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at most 5 characters"));
    }

    [Fact]
    public void ValidateInputs_StringParameter_WithPattern_ShouldValidate()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "email", Type = "string", Required = true, Pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$" }
        };

        var inputs = new Dictionary<string, object> { ["email"] = "invalid-email" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("does not match required pattern"));
    }

    [Fact]
    public void ValidateInputs_NumberParameter_WithMin_ShouldValidate()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "count", Type = "number", Required = true, Min = 10 }
        };

        var inputs = new Dictionary<string, object> { ["count"] = 5 };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at least 10"));
    }

    [Fact]
    public void ValidateInputs_NumberParameter_WithMax_ShouldValidate()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "count", Type = "number", Required = true, Max = 10 }
        };

        var inputs = new Dictionary<string, object> { ["count"] = 15 };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at most 10"));
    }

    [Fact]
    public void ValidateInputs_BooleanParameter_WithNonBoolean_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "flag", Type = "boolean", Required = true }
        };

        var inputs = new Dictionary<string, object> { ["flag"] = "not a boolean" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("must be a boolean"));
    }

    [Fact]
    public void ValidateInputs_EnumParameter_WithValidValue_ShouldPass()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new()
            {
                Name = "status",
                Type = "enum",
                Required = true,
                Values = new List<string> { "active", "inactive", "pending" }
            }
        };

        var inputs = new Dictionary<string, object> { ["status"] = "active" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateInputs_EnumParameter_WithInvalidValue_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new()
            {
                Name = "status",
                Type = "enum",
                Required = true,
                Values = new List<string> { "active", "inactive", "pending" }
            }
        };

        var inputs = new Dictionary<string, object> { ["status"] = "unknown" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("must be one of: active, inactive, pending"));
    }

    [Fact]
    public void ValidateInputs_WithNullValue_AndNoDefault_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "required", Type = "string", Required = true, Default = null }
        };

        var inputs = new Dictionary<string, object> { ["required"] = null! };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be null"));
    }

    [Fact]
    public void ValidateInputs_WithOptionalParameterMissing_ShouldPass()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "optional", Type = "string", Required = false }
        };

        var inputs = new Dictionary<string, object>();

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_Merge_ShouldCombineErrorsAndWarnings()
    {
        // Arrange
        var result1 = new ValidationResult();
        result1.AddError("Error 1");
        result1.AddWarning("Warning 1");

        var result2 = new ValidationResult();
        result2.AddError("Error 2");
        result2.AddWarning("Warning 2");

        // Act
        result1.Merge(result2);

        // Assert
        result1.Errors.Should().HaveCount(2);
        result1.Warnings.Should().HaveCount(2);
        result1.Errors.Should().Contain(new[] { "Error 1", "Error 2" });
        result1.Warnings.Should().Contain(new[] { "Warning 1", "Warning 2" });
    }

    [Fact]
    public void ValidationResult_ToString_WithErrorsAndWarnings_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Test error");
        result.AddWarning("Test warning");

        // Act
        var text = result.ToString();

        // Assert
        text.Should().Contain("Errors");
        text.Should().Contain("Test error");
        text.Should().Contain("Warnings");
        text.Should().Contain("Test warning");
    }

    [Fact]
    public void ValidationResult_ToString_WithNoIssues_ShouldReturnValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        var text = result.ToString();

        // Assert
        text.Should().Be("Valid");
    }

    [Fact]
    public void ValidateInputs_WithInvalidRegexPattern_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "test", Type = "string", Required = true, Pattern = "[invalid(" }
        };

        var inputs = new Dictionary<string, object> { ["test"] = "value" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid regex pattern"));
    }

    [Fact]
    public void ValidateInputs_NumberParameter_WithInvalidNumber_ShouldReturnError()
    {
        // Arrange
        var prompt = CreateValidPrompt();
        prompt.Metadata.InputSchema = new List<PromptParameter>
        {
            new() { Name = "count", Type = "number", Required = true }
        };

        var inputs = new Dictionary<string, object> { ["count"] = "not a number" };

        // Act
        var result = _validator.ValidateInputs(prompt, inputs);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("must be a valid number"));
    }

    private Prompt CreateValidPrompt()
    {
        return new Prompt
        {
            Metadata = new PromptMetadata
            {
                Name = "test-prompt",
                Version = "1.0.0",
                Description = "Test prompt for validation",
                Author = "Test Author"
            },
            Content = "Test prompt content with {{variable}}",
            FilePath = "/tmp/test.prompt"
        };
    }
}
