namespace Khyout.Api.Contracts;

public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details = null);

public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data) => new(true, data, null);

    public static ApiResponse<object?> OkEmpty() => new(true, null, null);
}
