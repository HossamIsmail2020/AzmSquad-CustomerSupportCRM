using CustomerSupportCRM.Application.Customers;
using Xunit;

namespace CustomerSupportCRM.Application.Tests;

public class CustomerValidatorTests
{
    [Fact]
    public void Valid_input_produces_no_error()
    {
        var error = CustomerValidator.Validate("Jane Doe", "jane@example.com", "+201234567890");
        Assert.Null(error);
    }

    [Fact]
    public void Valid_input_with_no_phone_produces_no_error()
    {
        var error = CustomerValidator.Validate("Jane Doe", "jane@example.com", null);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Missing_or_too_short_name_is_rejected(string name)
    {
        var error = CustomerValidator.Validate(name, "jane@example.com", null);
        Assert.NotNull(error);
        Assert.True(error!.Fields!.ContainsKey("fullName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Invalid_email_is_rejected(string email)
    {
        var error = CustomerValidator.Validate("Jane Doe", email, null);
        Assert.NotNull(error);
        Assert.True(error!.Fields!.ContainsKey("email"));
    }

    [Fact]
    public void Invalid_phone_format_is_rejected()
    {
        var error = CustomerValidator.Validate("Jane Doe", "jane@example.com", "not-a-phone!!");
        Assert.NotNull(error);
        Assert.True(error!.Fields!.ContainsKey("phone"));
    }
}
