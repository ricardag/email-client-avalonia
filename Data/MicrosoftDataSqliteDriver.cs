using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NHibernate.Dialect;
using NHibernate.Dialect.Schema;
using NHibernate.Driver;

namespace ClienteEmail.Data;

/// <summary>
///     Driver customizado para usar Microsoft.Data.Sqlite ao invés de System.Data.SQLite
/// </summary>
public class MicrosoftDataSqliteDriver : ReflectionBasedDriver
    {
    public MicrosoftDataSqliteDriver() : base(
        "Microsoft.Data.Sqlite",
        "Microsoft.Data.Sqlite",
        "Microsoft.Data.Sqlite.SqliteConnection",
        "Microsoft.Data.Sqlite.SqliteCommand")
        {
        }

    public override bool UseNamedPrefixInSql => true;

    public override bool UseNamedPrefixInParameter => true;

    public override string NamedPrefix => "@";

    public override bool SupportsMultipleOpenReaders => false;
    }

/// <summary>
///     Dialect customizado para Microsoft.Data.Sqlite que não tenta acessar GetSchema
/// </summary>
public class MicrosoftDataSqliteDialect : SQLiteDialect
    {
    public override IDataBaseSchema GetDataBaseSchema(DbConnection connection)
        {
        // Retorna um schema vazio para evitar o erro de GetSchema
        return new EmptyDataBaseSchema();
        }
    }

/// <summary>
///     Schema vazio que não tenta acessar collections não suportadas
/// </summary>
public class EmptyDataBaseSchema : AbstractDataBaseSchema
    {
    public EmptyDataBaseSchema() : base(null)
        {
        }

    public override bool StoresMixedCaseQuotedIdentifiers => true;

    public override ITableMetadata GetTableMetadata(DataRow rs, bool extras)
        {
        return null;
        }

    public override ISet<string> GetReservedWords()
        {
        return new HashSet<string>();
        }

    public override DataTable GetTables(string catalog, string schemaPattern, string tableNamePattern, string[] types)
        {
        // Retorna uma DataTable vazia
        var table = new DataTable();
        table.Columns.Add("TABLE_NAME", typeof(string));
        return table;
        }

    public override DataTable GetColumns(string catalog, string schemaPattern, string tableNamePattern,
        string columnNamePattern)
        {
        // Retorna uma DataTable vazia
        var table = new DataTable();
        table.Columns.Add("COLUMN_NAME", typeof(string));
        return table;
        }

    public override DataTable GetIndexInfo(string catalog, string schemaPattern, string tableName)
        {
        // Retorna uma DataTable vazia
        var table = new DataTable();
        return table;
        }

    public override DataTable GetIndexColumns(string catalog, string schemaPattern, string tableName, string indexName)
        {
        // Retorna uma DataTable vazia
        var table = new DataTable();
        return table;
        }

    public override DataTable GetForeignKeys(string catalog, string schema, string table)
        {
        // Retorna uma DataTable vazia
        var table1 = new DataTable();
        return table1;
        }
    }