namespace Petatu.Domain.Common;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static readonly Error NullValue = new(
        "General.Null",
        "Null value provided / Not expected null value",
        ErrorType.Failure);

    public Error(string code, string description, ErrorType errorType)
    {
        Code = code;
        Description = description;
        ErrorType = errorType;
    }

    public string Code { get; }
    public string Description { get; }
    public ErrorType ErrorType { get; }
}
