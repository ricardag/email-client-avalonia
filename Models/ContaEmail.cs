using System.Collections.Generic;
using FluentNHibernate.Mapping;

namespace ClienteEmail.Models;

public enum TipoContaEmail
    {
    Selecione = 0,
    Office365 = 1,
    Gmail = 2
    }

public class ContaEmail
    {
    public virtual int Id { get; set; }
    public virtual TipoContaEmail TipoConta { get; set; }
    public virtual string? Nome { get; set; }
    public virtual string? UserName { get; set; }
    public virtual string? EmailAddress { get; set; }
    public virtual IList<Pasta> Pastas { get; set; } = [];
    }

public class ContaEmailMap : ClassMap<ContaEmail>
    {
    public ContaEmailMap()
        {
        Table("contas_email");
        Id(x => x.Id).GeneratedBy.Identity();
        Map(x => x.Nome);
        Map(x => x.UserName);
        Map(x => x.EmailAddress, "email_address").Length(255).Not.Nullable().Unique();
        Map(x => x.TipoConta, "tipo_conta").Not.Nullable().CustomType<TipoContaEmail>();

        HasMany(p => p.Pastas)
            .KeyColumn("conta_email_id")
            .Inverse()
            .Cascade.AllDeleteOrphan();
        }
    }