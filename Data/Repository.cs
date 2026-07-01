using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace TaskNote.Data
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly IDbContextFactory<AppDbContext> ContextFactory;

        public Repository(IDbContextFactory<AppDbContext> contextFactory)
        {
            ContextFactory = contextFactory;
        }

        public async Task<List<T>> GetAllAsync()
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            return await context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            return await context.Set<T>().FindAsync(id);
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var entry = context.Entry(entity);
            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey != null)
            {
                var keyValues = primaryKey.Properties
                    .Select(p => entry.Property(p.Name).CurrentValue)
                    .ToArray();

                var dbEntity = await context.Set<T>().FindAsync(keyValues);
                if (dbEntity != null)
                {
                    context.Entry(dbEntity).CurrentValues.SetValues(entity);
                    await context.SaveChangesAsync();
                    return;
                }
            }

            context.Set<T>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            using var context = await ContextFactory.CreateDbContextAsync();
            var entry = context.Entry(entity);
            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey != null)
            {
                var keyValues = primaryKey.Properties
                    .Select(p => entry.Property(p.Name).CurrentValue)
                    .ToArray();

                var dbEntity = await context.Set<T>().FindAsync(keyValues);
                if (dbEntity != null)
                {
                    context.Set<T>().Remove(dbEntity);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                context.Set<T>().Remove(entity);
                await context.SaveChangesAsync();
            }
        }
    }
}
