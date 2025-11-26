namespace littleShop.catalog.Shared;

public class ServiceResult<T>
{
    public T? Data { get; set; }
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = [];
    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static ServiceResult<T> Failure(string error) => new() { Succeeded = false, Errors = [error] };
}

public class ServiceResult
{
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = [];
    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(string error) => new() { Succeeded = false, Errors = [error] };
}