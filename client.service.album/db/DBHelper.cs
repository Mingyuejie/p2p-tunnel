using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.db
{
    public class DBHelper<T> where T : class, new()
    {
        private IDbConnection Connection()
        {
            return new SqliteConnection($"data source=.\\db\\album.db");
        }

        public async Task<T> Get(int id)
        {
            using IDbConnection connection = Connection();
            return await connection.GetAsync<T>(id);
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            using IDbConnection connection = Connection();
            return await connection.GetAllAsync<T>();
        }

        public async Task<int> QueryCount(string where = "", object param = null)
        {
            using IDbConnection connection = Connection();
            return await connection.QueryFirstAsync<int>($"SELECT count(1) FROM {typeof(T).Name} WHERE 1=1 {where}", param);
        }

        public async Task<IEnumerable<T>> Query(string where = "",string order = "",string limit = "", object param = null)
        {
            using IDbConnection connection = Connection();
            return await connection.QueryAsync<T>($"SELECT * FROM {typeof(T).Name} WHERE 1=1 {where} {order} {limit}", param);
        }

        public async Task<int> Execute(string sql, object param = null)
        {
            using IDbConnection connection = Connection();
            return await connection.ExecuteAsync(sql, param);
        }

        public async Task<int> Update(string set,string where, object param = null)
        {
            return await Execute($"UPDATE {typeof(T).Name} SET {set} WHERE 1=1 AND {where}", param);
        }

        public async Task<int> Delete(string where, object param = null)
        {
            return await Execute($"DELETE FROM {typeof(T).Name} WHERE {where}", param);
        }


        public async Task<int> Add(T model)
        {
            using IDbConnection connection = Connection();
            return await connection.InsertAsync(model);
        }

        public async Task<bool> Update(T model)
        {
            using IDbConnection connection = Connection();
            return await connection.UpdateAsync(model);
        }

        public async Task<bool> Delete(T model)
        {
            using IDbConnection connection = Connection();
            return await connection.DeleteAsync(model);
        }
    }

    public class PageWrap<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public T Data { get; set; }
    }
}
