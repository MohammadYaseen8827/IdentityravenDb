using Platform.Application.Interfaces;
using Platform.Domain.Common;
using Raven.Client.Documents;

namespace Platform.Infrastructure.Repositories
{
    public class GenericRepositoryAsync<T> : IGenericRepositoryAsync<T> where T : class
    {
        protected GenericRepositoryAsync(IDocumentStore store)
        {
            Store = store;
        }

        protected IDocumentStore Store { get; }

        public  async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken, bool withDeleted = false)
        {
            using var session = Store.OpenAsyncSession();
            return await session.LoadAsync<T>(id.ToString());
        }
 

        public async Task<T> AddAsync(T entity,CancellationToken cancellationToken)
        {
            using var session = Store.OpenAsyncSession();
            session.StoreAsync(entity);
            session.SaveChangesAsync();
            return entity;
        }
 

        public async Task DeleteAsync(int id,CancellationToken cancellationToken)
        {
            using var session = Store.OpenAsyncSession();
            session.Delete(id.ToString());
            await session.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
        {
            using var session = Store.OpenAsyncSession();
                return await session.Query<T>().ToListAsync(cancellationToken);
        }
    }


}
