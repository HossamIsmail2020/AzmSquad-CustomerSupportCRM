using System.Text.RegularExpressions;
using CustomerSupportCRM.Application.Common;

namespace CustomerSupportCRM.Application.Customers;

public static partial class CustomerValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[0-9+\-\s()]{6,20}$")]
    private static partial Regex PhoneRegex();

    public static ServiceError? Validate(string fullName, string email, string? phone)
    {
        var fields = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length is < 2 or > 200)
        {
            fields["fullName"] = ["Full name is required and must be between 2 and 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex().IsMatch(email))
        {
            fields["email"] = ["A valid email address is required."];
        }

        if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex().IsMatch(phone))
        {
            fields["phone"] = ["Phone number format is invalid."];
        }

        return fields.Count == 0 ? null : ServiceError.Validation("Validation failed.", fields);
    }
}
