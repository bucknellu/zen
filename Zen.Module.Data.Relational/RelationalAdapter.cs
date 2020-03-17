﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using Zen.Base.Extension;
using Zen.Base.Module;
using Zen.Base.Module.Data;
using Zen.Base.Module.Data.Adapter;
using Zen.Module.Data.Relational.Builder;
using Zen.Module.Data.Relational.Mapper;

namespace Zen.Module.Data.Relational
{
    public abstract class RelationalAdapter : DataAdapterPrimitive, IRelationalStatements
    {
        public StatementMasks Masks = null;
        public Dictionary<string, string> MemberMap = new Dictionary<string, string>();
        public StatementBuilder StatementBuilder = null;

        public void Map<T>() where T : Data<T>
        {
            var cat = new ColumnAttributeTypeMapper<T>();
            SqlMapper.SetTypeMap(typeof(T), cat);

            MemberDescriptors =
                (from pInfo in typeof(T).GetProperties()
                    let p1 = pInfo.GetCustomAttributes(false).OfType<ColumnAttribute>().ToList()
                    let field = p1.Count != 0 ? p1[0].Name ?? pInfo.Name : pInfo.Name
                    let length = p1.Count != 0 ? (p1[0].Length != 0 ? p1[0].Length : 0) : 0
                    let serializable = p1.Count != 0 && p1[0].Serialized
                    select new KeyValuePair<string, MemberDescriptor>(pInfo.Name, new MemberDescriptor {Field = field, Length = length, Serializable = serializable})
                ).ToDictionary(x => x.Key, x => x.Value);

            MemberMap = MemberDescriptors.Select(i => new KeyValuePair<string, string>(i.Key, i.Value.Field)).ToDictionary(i => i.Key, i => i.Value);

            var mapEntry = MemberDescriptors.FirstOrDefault(p => p.Value.Field.ToLower().Equals(Info<T>.Settings.KeyMemberName.ToLower()));

            KeyMember = mapEntry.Key;
            KeyColumn = mapEntry.Value.Field;
        }

        public virtual void PrepareCachedStatements<T>() where T : Data<T>
        {
            var setName = Info<T>.Configuration.SetName;

            // "SELECT COUNT(*) FROM {0}"
            Statements.RowCount = Statements.RowCount.format(setName);

            // "SELECT * FROM {0} WHERE {1} = {2}"
            Statements.GetSingleByIdentifier =
                Statements.GetSingleByIdentifier.format(setName, KeyColumn,
                                                        Masks.InlineParameter.format(
                                                            Masks.Parameter.format(KeyColumn)));

            // "SELECT * FROM {0} WHERE {1} IN ({2})"
            Statements.GetManyByIdentifier =
                Statements.GetManyByIdentifier.format(setName, KeyColumn,
                                                      Masks.InlineParameter.format(
                                                          Masks.Parameter.format(Masks.Keywords.Keyset)));

            // "SELECT * FROM {0}"
            Statements.GetAll = Statements.GetAll.format(setName);

            // "SELECT * FROM {0} WHERE {1}"
            Statements.AllFields = Statements.AllFields.format(setName, "{0}");
        }

        #region Custom Members

        public T1 QuerySingleValue<T, T1>(string statement) where T : Data<T>
        {
            using (var conn = GetConnection<T>())
            {
                var ret = conn.Query<T1>(statement).FirstOrDefault();
                return ret;
            }
        }

        public void Execute<T>(string statement) where T : Data<T>
        {
            using (var conn = GetConnection<T>()) { conn.Execute(statement); }
        }

        public List<TU> RawQuery<T, TU>(string statement, object parameters) where T : Data<T>
        {
            try
            {
                using (var conn = GetConnection<T>())
                {
                    conn.Open();

                    var o = conn.Query(statement, parameters)
                        .Select(a => (IDictionary<string, object>) a)
                        .ToList();
                    conn.Close();

                    var ret = o.Select(refObj => refObj.GetObject<TU>(MemberMap)).ToList();

                    return ret;
                }
            } catch (Exception e) { throw new DataException(Info<T>.Settings.TypeQualifiedName + " RelationalAdapter: Error while issuing statements to the database.", e); }
        }

        public List<TU> AdapterQuery<T, TU>(string statement, Mutator mutator = null) where T : Data<T>
        {
            if (mutator == null) mutator = new Mutator {Transform = new QueryTransform {Statement = statement}};

            var builder = mutator.ToSqlBuilderTemplate();

            return RawQuery<T, TU>(builder.RawSql, builder.Parameters);
        }

        #endregion

        #region Implementation of IRelationalStatements

        public virtual bool UseIndependentStatementsForKeyExtraction { get; } = false;
        public virtual bool UseNumericPrimaryKeyOnly { get; } = false;
        public virtual bool UseOutputParameterForInsertedKeyExtraction { get; } = false;
        public virtual RelationalStatements Statements { get; } = new RelationalStatements();
        public Dictionary<string, MemberDescriptor> MemberDescriptors { get; set; } = new Dictionary<string, MemberDescriptor>();

        public class MemberDescriptor
        {
            public string Field;
            public long Length;
            public bool Serializable;
        }

        public string KeyMember { get; set; }
        public string KeyColumn { get; set; }
        public Dictionary<string, KeyValuePair<string, string>> SchemaElements { get; set; }

        public virtual DbConnection GetConnection<T>() where T : Data<T> { return null; }
        public virtual void RenderSchemaEntityNames<T>() where T : Data<T> { }
        public virtual void ValidateSchema<T>() where T : Data<T> { }

        #endregion

        #region Overrides of DataAdapterPrimitive

        public override void Setup<T>(Settings settings) { }
        public override void Initialize<T>() { }

        public override long Count<T>(Mutator mutator = null) { return QuerySingleValue<T, long>(Statements.RowCount); }

        public override T Get<T>(string key, Mutator mutator = null)
        {
            var statement = Statements.GetSingleByIdentifier;
            var parameter = new Dictionary<string, object> {{Masks.Parameter.format(KeyColumn), key}};

            return RawQuery<T, T>(statement, parameter).FirstOrDefault();
        }

        public override IEnumerable<T> Get<T>(IEnumerable<string> keys, Mutator mutator = null)
        {
            var keySet = keys as string[] ?? keys.ToArray();

            if (!keySet.Any()) return new List<T>();

            var statement = Statements.GetManyByIdentifier;
            var parameter = new Dictionary<string, object> {{Masks.Parameter.format(Masks.Keywords.Keyset), keySet.ToArray()}};
            return RawQuery<T, T>(statement, parameter);
        }

        public override IEnumerable<T> Query<T>(string statement) { throw new NotImplementedException(); }

        public override IEnumerable<T> Query<T>(Mutator mutator = null) { return Query<T>(Statements.GetAll); }
        public override IEnumerable<TU> Query<T, TU>(string statement) { throw new NotImplementedException(); }

        public override IEnumerable<TU> Query<T, TU>(Mutator mutator = null) { return AdapterQuery<T, TU>(Statements.GetAll, mutator); }

        public override T Insert<T>(T model, Mutator mutator = null) { throw new NotImplementedException(); }
        public override T Save<T>(T model, Mutator mutator = null) { throw new NotImplementedException(); }
        public override T Upsert<T>(T model, Mutator mutator = null) { throw new NotImplementedException(); }
        public override void Remove<T>(string key, Mutator mutator = null) { throw new NotImplementedException(); }
        public override void Remove<T>(T model, Mutator mutator = null) { throw new NotImplementedException(); }
        public override void RemoveAll<T>(Mutator mutator = null) { throw new NotImplementedException(); }

        public override IEnumerable<T> BulkInsert<T>(IEnumerable<T> models, Mutator mutator = null) { throw new NotImplementedException(); }
        public override IEnumerable<T> BulkSave<T>(IEnumerable<T> models, Mutator mutator = null) { throw new NotImplementedException(); }
        public override IEnumerable<T> BulkUpsert<T>(IEnumerable<T> models, Mutator mutator = null) { throw new NotImplementedException(); }
        public override void BulkRemove<T>(IEnumerable<string> keys, Mutator mutator = null) { throw new NotImplementedException(); }
        public override void BulkRemove<T>(IEnumerable<T> models, Mutator mutator = null) { throw new NotImplementedException(); }

        public override IEnumerable<T> Where<T>(Expression<Func<T, bool>> predicate, Mutator mutator = null)
        {
            var parts = StatementBuilder.ToSql(predicate, MemberDescriptors);

            var statement = Statements.AllFields.format(parts.Sql);

            return RawQuery<T, T>(statement, parts.Parameters);
        }

        #endregion
    }
}