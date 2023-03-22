internal interface IExternalSortService
{
    Task SortAsync(CancellationToken cancellationToken = default);
}