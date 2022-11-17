namespace Platform.Application.Interfaces
{
    public interface IGenericRepositoryAsync<T> where T : class
    {
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken, bool withDeleted = false);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);
        Task<T> AddAsync(T entity,CancellationToken cancellationToken);
        Task DeleteAsync(int id,CancellationToken cancellationToken);
    }
    
}
