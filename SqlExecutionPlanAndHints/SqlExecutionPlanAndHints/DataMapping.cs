using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlExecutionPlanAndHints
{
	public class DataMapping : ClassMapping<Data>
	{
		public DataMapping()
		{
			Table("ALL_RND_CELLS");

			Id(x => x.CellID, c =>
			{
				c.Column("CELLID");
				c.Generator(Generators.Sequence, g => g.Params(new { sequence = "ALL_RND_CELLS_SEQ" }));
				c.UnsavedValue(-9999);
				c.Type(NHibernateUtil.Int32);
			});

			Property(x => x.Cell, c =>
			{
				c.Column("CELL");
				c.Type(NHibernateUtil.AnsiString);
				c.NotNullable(false);
			});
		}
	}
}
