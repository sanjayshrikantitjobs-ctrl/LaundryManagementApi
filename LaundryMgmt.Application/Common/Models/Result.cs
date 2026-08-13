namespace LaundryMgmt.Application.Common.Models;

public class Result
{
    public bool Succeeded { get; }
    public string[] Errors { get; }

    protected Result(bool succeeded, string[] errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(params string[] errors) => new(false, errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool succeeded, T? value, string[] errors) : base(succeeded, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());
    public new static Result<T> Failure(params string[] errors) => new(false, default, errors);
}

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }
}
