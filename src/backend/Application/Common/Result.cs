namespace GymFlow.Application.Common;

/// <summary>
/// Patrón Result para evitar excepciones como flujo de control.
/// El Use Case retorna Result&lt;T&gt;; el Controller decide el código HTTP.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(bool isSuccess, T? value, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    public string? Extra { get; private set; }

    public static Result<T> SuccessWithExtra(T value, string extra) =>
        new Result<T>(true, value, null, 200) { Extra = extra };
}

    public static Result<T> Success(T value) =>
        new(true, value, null, 200);

    public static Result<T> NotFound(string message) =>
        new(false, default, message, 404);

    public static Result<T> Forbidden(string message) =>
        new(false, default, message, 403);

    public static Result<T> Conflict(string message) =>
        new(false, default, message, 409);

    public static Result<T> ValidationError(string message) =>
        new(false, default, message, 400);

    public static Result<T> InternalError(string message) =>
        new(false, default, message, 500);
}
