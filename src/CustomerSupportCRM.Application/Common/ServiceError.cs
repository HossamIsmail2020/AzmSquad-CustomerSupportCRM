namespace CustomerSupportCRM.Application.Common;

public enum ServiceErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public record ServiceError(ServiceErrorType Type, string Message, IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static ServiceError NotFound(string message) => new(ServiceErrorType.NotFound, message);
    public static ServiceError Conflict(string message) => new(ServiceErrorType.Conflict, message);
    public static ServiceError Forbidden(string message) => new(ServiceErrorType.Forbidden, message);
    public static ServiceError Validation(string message, IReadOnlyDictionary<string, string[]>? fields = null) =>
        new(ServiceErrorType.Validation, message, fields);
}
