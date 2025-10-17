using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;

namespace ClienteEmail.Models;

public class Pasta
    {
    public virtual int Id { get; set; }
    public virtual ContaEmail? ContaEmail { get; set; }
    public virtual string? GraphFolderId { get; set; }
    public virtual string? MaterializedPath { get; set; }
    public virtual string? Nome { get; set; }
    public virtual int TotalItemCount { get; set; }
    public virtual int UnreadItemCount { get; set; }
    public virtual DateTime? SyncTimestamp { get; set; }
    public virtual Pasta? PastaPai { get; set; }
    public virtual IList<Pasta>? PastasFilhas { get; set; } = new List<Pasta>();
    public virtual IList<Email>? Emails { get; set; } = new List<Email>();
    }

public class PastaMap : ClassMap<Pasta>
    {
    public PastaMap()
        {
        Table("pastas");
        Id(x => x.Id).GeneratedBy.Identity();

        References(p => p.ContaEmail).Column("conta_id").Not.Nullable();

        Map(p => p.GraphFolderId).Length(256).Not.Nullable().UniqueKey("UQ_GraphId_ContaId");
        Map(p => p.Nome).Length(250).Not.Nullable();
        Map(p => p.MaterializedPath).Length(1024).Not.Nullable();
        Map(p => p.TotalItemCount);
        Map(p => p.UnreadItemCount);
        Map(p => p.SyncTimestamp).Nullable();

        References(p => p.PastaPai)
            .Column("pasta_pai_id")
            .Nullable(); // Pastas raiz não têm pai

        // Lado "Um-para-Muitos": Coleção de pastas filhas
        HasMany(p => p.PastasFilhas)
            .KeyColumn("pasta_pai_id") // A coluna na tabela que aponta para o pai
            .Inverse() // Informa ao NHibernate que o outro lado (PastaPai) gerencia a atualização da coluna
            .Cascade.AllDeleteOrphan() // Gerencia o ciclo de vida das filhas automaticamente
            .OrderBy("MaterializedPath"); // Opcional: sempre carrega as filhas em ordem alfabética

        // Lado "Um-para-Muitos": Coleção de pastas filhas
        HasMany(p => p.Emails)
            .KeyColumn("email_id") // A coluna na tabela que aponta para o pai
            .Inverse() // Informa ao NHibernate que o outro lado (PastaPai) gerencia a atualização da coluna
            .Cascade.AllDeleteOrphan(); // Gerencia o ciclo de vida das filhas automaticamente
        }
    }