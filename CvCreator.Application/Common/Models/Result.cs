namespace CvCreator.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
}
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}
