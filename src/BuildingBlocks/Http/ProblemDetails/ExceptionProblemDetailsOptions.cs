namespace BuildingBlocks.Http.ProblemDetails;

public sealed class ExceptionProblemDetailsOptions
{
    private readonly Dictionary<Type, Func<Exception, (int StatusCode, string Title, string Detail)>> _mappings = [];

    public IReadOnlyDictionary<Type, Func<Exception, (int StatusCode, string Title, string Detail)>> Mappings =>
        _mappings;

    public ExceptionProblemDetailsOptions Map<TException>(int statusCode, string title)
        where TException : Exception
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        _mappings[typeof(TException)] = exception => (statusCode, title, exception.Message);
        return this;
    }

    public ExceptionProblemDetailsOptions Map<TException>(
        Func<TException, (int StatusCode, string Title, string Detail)> mapper)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(mapper);

        _mappings[typeof(TException)] = exception => mapper((TException)exception);
        return this;
    }
}
