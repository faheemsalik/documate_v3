using Documate.Domain;
using Documate.Extensions;
using Documate.Models;

using Elasticsearch.Net;

using Microsoft.EntityFrameworkCore;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Documate.Data
{
    //---------------------------------------------------------------------------------------------------------------------------------------
    // Interface Class
    //---------------------------------------------------------------------------------------------------------------------------------------
    #region Interface Class
    public partial interface IRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity> GetEntityById(object id);
        IQueryable<TEntity> GetEntities(Expression<Func<TEntity, bool>> predicate = null, params Expression<Func<TEntity, object>>[] navigationProperties);

        //void InsertOrUpdate(TEntity entity, ref RepoResult ret);
        Task<TEntity> InsertOrUpdate(TEntity entity, bool bulkOperation = false);
        Task<bool> Delete(TEntity entity, bool bulkOperation);
        Task<bool> Delete(int id, bool bulkOperation);
        Task<bool> Delete(Expression<Func<TEntity, bool>> predicate = null);

        int GetMaxId(Func<TEntity, int> column);

        //TEntity UpdateBody(IEnumerable<TEntity> oldEntities, IEnumerable<TEntity> newEntities);

        DbSet<TEntity> Entities { get; }
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableWithTracking { get; }

        IQueryable<TEntity> Reporting { get; }
    }

    #endregion

    //---------------------------------------------------------------------------------------------------------------------------------------
    // Class
    //---------------------------------------------------------------------------------------------------------------------------------------
    public partial class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly DBContext dbContext;
        private DbSet<TEntity> _entities;

        //==============================================================================================================================================
        public GenericRepository(DBContext context)
        {
            this.dbContext = context;
        }

        //==============================================================================================================================================
        public virtual async Task<TEntity> GetEntityById(object id)
        {
            //see some suggested performance optimization (not tested)
            //http://stackoverflow.com/questions/11686225/dbset-find-method-ridiculously-slow-compared-to-singleordefault-on-id/11688189#comment34876113_11688189

            return await this.Entities.FindAsync(id);
        }

        //==============================================================================================================================================
        public IQueryable<TEntity> GetEntities(Expression<Func<TEntity, bool>> predicate = null, params Expression<Func<TEntity, object>>[] navigationProperties)
        {
            IQueryable<TEntity> dbQuery = this.Entities;

            if (navigationProperties != null && navigationProperties.Length > 0) dbQuery.IncludeNavigationProperties(navigationProperties);

            if (predicate == null)
                return dbQuery.AsNoTracking();
            else
                return dbQuery.AsNoTracking().Where(predicate);
        }

        //==============================================================================================================================================
        public virtual async Task<TEntity> InsertOrUpdate(TEntity entity, bool bulkOperation = false)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var errors = entity.ValidateEntity();
            try
            {
                if (!errors.Any())
                {
                    if (await Exists(entity))
                    {
                        entity.UpdatedOnUtc = DateTime.Now.ToUniversalTime();
                        dbContext.Entry(entity).State = EntityState.Modified;
                    }
                    else
                    {
                        entity.CreatedOnUtc = DateTime.Now.ToUniversalTime();
                        entity.UpdatedOnUtc = DateTime.Now.ToUniversalTime(); 
                        this.Entities.Add(entity);
                        if (bulkOperation) return entity;
                    }

                    var result = await this.dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var strError = errors.Aggregate("", (current, e) => current + (e + Environment.NewLine));
                throw new Exception(strError);
            }
            return entity;
        }

        //==============================================================================================================================================
        public virtual async Task<bool> Delete(TEntity entity, bool bulkOperation = false)
        {
            if (entity == null)
                throw new Exception("The entity can't be null");

            var result = false;
            if (entity != null && await Exists(entity) == true)
            {
                dbContext.Entry(entity).State = EntityState.Deleted;
                this.Entities.Remove(entity);
                if (!bulkOperation)
                    this.dbContext.SaveChanges();
            }
            else
                throw new Exception("Record does not found to delete");
            return result;
        }

        //==============================================================================================================================================
        public virtual async Task<bool> Delete(int id, bool bulkOperation = false)
        {
            var entity = await GetEntityById(id);
            return await Delete(entity, bulkOperation);
        }

        //==============================================================================================================================================
        public virtual async Task<bool> Delete(Expression<Func<TEntity, bool>> predicate = null)
        {
            var entities = await GetEntities(predicate).ToListAsync();
            await entities.ForEachAsync(async entity =>
            {
                await Delete(entity, true);
            });

            await dbContext.SaveChangesAsync();

            return true;
        }

        //==============================================================================================================================================
        public int GetMaxId(Func<TEntity, int> column)
        {
            if (this.Entities.Count() > 0)
                return (int)(this.Entities.Select(column).Max());
            else
                return 0;
        }

        //==============================================================================================================================================
        public virtual IQueryable<TEntity> Table { get { return this.Entities.AsNoTracking(); } }

        //==============================================================================================================================================
        public virtual IQueryable<TEntity> TableWithTracking { get { return this.Entities; } }

        //==============================================================================================================================================
        public virtual IQueryable<TEntity> Reporting { get { return this.Entities.AsNoTracking(); } }

        //==============================================================================================================================================
        public virtual DbSet<TEntity> Entities
        {
            get
            {
                if (_entities == null) _entities = dbContext.Set<TEntity>();
                return _entities;
            }
        }

         
        //==============================================================================================================================================
        public async Task<Boolean> Exists(TEntity entity)
        {
            //var objContext = ((IObjectContextAdapter)this.dbContext).ObjectContext;
            //var objSet = objContext.CreateObjectSet<TEntity>();
            //var entityKey = objContext.CreateEntityKey(objSet.EntitySet.Name, entity);

            //Object foundEntity;
            //var exists = objContext.TryGetObjectByKey(entityKey, out foundEntity);
            //// TryGetObjectByKey attaches a found entity
            //// Detach it here to prevent side-effects
            //if (exists)
            //{
            //    objContext.Detach(foundEntity);
            //}

            //return this.Entities<TEntity>().Local.Any(e => e == entity);
            //return (exists);

            return await this.GetEntityById(entity.Id) != null;
            
        }

        //==============================================================================================================================================
        /// <summary>
        /// Update entity
        /// </summary>
        /// <param filename="entity">Entity</param>
        //public AjaxActionResult UpdateBody(IEnumerable<TEntity> oldEntities, IEnumerable<TEntity> newEntities)
        //{
        //    AjaxActionResult ret = new AjaxActionResult();
        //    try
        //    {
        //        if (newEntities == null && oldEntities == null)
        //            throw new ArgumentNullException("entity");

        //        List<int> OldbodyIdsList = oldEntities.Select(X => X.Id).ToList();
        //        List<int> NewbodyIdsList = newEntities.Select(X => X.Id).ToList();

        //        // Removing deleted body records
        //        IEnumerable<TEntity> bodyToDelete = oldEntities.Where(X => !(NewbodyIdsList.Contains(X.Id))).ToList();
        //        if (bodyToDelete.Count() > 0)
        //        {
        //            ret = this.Delete(bodyToDelete);
        //            if (ret.success == false) return ret;
        //        }

        //        // Adding new body records
        //        IEnumerable<TEntity> bodyToAdd = newEntities.Where(X => (!(OldbodyIdsList.Contains(X.Id))) || X.Id == 0).ToList();
        //        foreach (TEntity body in bodyToAdd)
        //        {
        //            ret = this.InsertOrUpdate(body);
        //            if (ret.success == false) break;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ret.success = false;
        //    }

        //    return ret;
        //}

    }

    public static class RepositoryExtensionMethods
    {
        public static void IncludeNavigationProperties<TEntity>(this IQueryable<TEntity> dbQuery, params Expression<Func<TEntity, object>>[] navigationProperties) where TEntity : class
        {
            foreach (Expression<Func<TEntity, object>> navigationProperty in navigationProperties)
                dbQuery = dbQuery.Include<TEntity, object>(navigationProperty);
        }
    }
}
