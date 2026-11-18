namespace PlantillaBackend.Application.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa.") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors?.ToList() ?? new List<string>()
    };
}
