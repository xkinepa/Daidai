﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dekopon.Miscs;
using Dapper;

namespace Dekopon.Repository
{
    public abstract class CrudRepositoryBase<T> : RepositoryBase<T>, ICrudRepository<T>
    {
        protected CrudRepositoryBase(IDatabaseManager databaseManager) : base(databaseManager)
        {
        }

        public IList<T> FindAll(IList<T> entities)
        {
            var (query, parameters) = QueryBuilder.FindAll(EntityDefinition, entities);
            return Conn.Query<T>(query, parameters).ToList();
        }

        public T Get(T entity)
        {
            var (query, parameters) = QueryBuilder.Find(EntityDefinition, entity);
            return Conn.QuerySingleOrDefault<T>(query, parameters);
        }

        public long Add(T entity)
        {
            var (query, @params) = QueryBuilder.Insert(EntityDefinition, entity);
            var id = Conn.ExecuteScalar<long>(query, @params);
            if (id > 0)
            {
                EntityDefinition.IdColumn?.Setter(entity, id);
            }

            return id;
        }

        public int AddAll(IList<T> entities, int chunk = 100)
        {
            return Chunk(entities, chunk).Select(it =>
            {
                var (query, parameters) = QueryBuilder.InsertAll(EntityDefinition, it);
                return Conn.Execute(query, parameters);
            }).Sum();
        }

        public int Update(T entity)
        {
            var (query, parameters) = QueryBuilder.Update(EntityDefinition, entity);
            return Conn.Execute(query, parameters);
        }

        public int UpdateAll(IList<T> entities, int chunk = 100)
        {
            return Chunk(entities, chunk).Select(it =>
            {
                var (query, parameters) = QueryBuilder.UpdateAll(EntityDefinition, it);
                return Conn.Execute(query, parameters);
            }).Sum();
        }

        public int Delete(T entity)
        {
            var (query, parameters) = QueryBuilder.Delete(EntityDefinition, entity);
            return Conn.Execute(query, parameters);
        }

        public int DeleteAll(IList<T> entities)
        {
            var (query, parameters) = QueryBuilder.DeleteAll(EntityDefinition, entities);
            return Conn.Execute(query, parameters);
        }

        public IList<T> FindByIdIn(IList<long> ids) => FindAll(ids.Select(CreateEntity).ToList());

        public T GetById(long id) => Get(CreateEntity(id));

        public int DeleteById(long id) => Delete(CreateEntity(id));

        public int DeleteByIdIn(IList<long> ids) => DeleteAll(ids.Select(CreateEntity).ToList());

        private T CreateEntity(long id)
        {
            Assertion.NotNull(EntityDefinition.IdColumn, $"entity has no id column");

            var entity = Activator.CreateInstance<T>();
            EntityDefinition.IdColumn.Setter(entity, id);
            return entity;
        }

        private IEnumerable<IEnumerable<TV>> Chunk<TV>(IEnumerable<TV> data, int chunk)
        {
            return data.Select((it, i) => new {it, g = i / chunk,})
                .GroupBy(it => it.g, it => it.it);
        }
    }
}