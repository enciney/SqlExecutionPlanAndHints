using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Id;
using NHibernate.Mapping.ByCode;
using NHibernate.Persister.Entity;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using NhConfiguration = NHibernate.Cfg.Configuration;
using NhTable = NHibernate.Mapping.Table;

namespace SqlExecutionPlanAndHints
{
	
	class Program
	{
		enum DataMode
		{
			Nothing = 0,
			Truncate,
			Drop,
			
		}
		static DataMode SessionDataMode;
		static readonly int ROWCOUNT = 2000;
		static readonly int CELLLENGTH = 6;
		static IConfigurationRoot appConfig { get; set; }
		static ISessionFactory sessionFactory { get; set; }

		static NhConfiguration nhConfig;

		// -t mean truncate all table and reset sequence
		// -d drop all data from database
		// if no argument then default behaviour applied (nothing to do)
		static void Main(string[] args)
		{
			var argumentLength = args.Length;
			if (argumentLength == 1 )
			{
				var truncateMode = "-t";
				var dropMode = "-d";
				var param1 = args[0].ToLowerInvariant();
				if (param1.StartsWith(truncateMode))
				{
					SessionDataMode = DataMode.Truncate;
				}
				else if (param1.StartsWith(dropMode))
				{
					SessionDataMode = DataMode.Drop;
				}
			}
			StartUpActions();
			var session = sessionFactory.OpenSession();
			CeateInitialData(session);
			// some stuff

			//FinishJob(session);
			session.Close();


		}


		static void StartUpActions()
		{
			// .netCore itself Dependency Injection
			var serviceProvider = new ServiceCollection()
				.AddSingleton<IConfigurationBuilder>(new ConfigurationBuilder()
			   .SetBasePath(Directory.GetCurrentDirectory())
			   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			   );
			var serviceBuilder = serviceProvider.BuildServiceProvider();
			appConfig = serviceBuilder.GetService<IConfigurationBuilder>().Build();
			var nhConfigPath = appConfig.GetValue<string>("nhConfig");
			#region autofac DI
			// classic Autofac
			//var builder = new ContainerBuilder();
			//builder.RegisterInstance(new ConfigurationBuilder()
			//   .SetBasePath(Directory.GetCurrentDirectory())
			//   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)).As<IConfigurationBuilder>();
			//var container = builder.Build();
			//using (var scope = container.BeginLifetimeScope())
			//{
			//	appConfig = scope.Resolve<IConfigurationBuilder>().Build();
			//}

			//nhConfig = appConfig.GetValue<string>("nhConfig");
			#endregion
			// try to configure nHibernate
			var configurationObj = new NHibernate.Cfg.Configuration();
			nhConfig = nhConfigPath == null ? configurationObj.Configure() : configurationObj.Configure(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nhConfigPath));
			
			var modelMapper = new ModelMapper();
			modelMapper.AddMapping<DataMapping>();
			nhConfig.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());
			sessionFactory = nhConfig.BuildSessionFactory();			
		}


		static bool TableExist(ISession session, string tableName)
		{
			var query = session.CreateSQLQuery($"select count(*) from user_tables where table_name in ('{tableName.ToLower()}','{tableName.ToUpper()}') ");
			var result = query.UniqueResult().ToString();
			return int.Parse(result) > 0 ? true : false; 
		}

		static void CeateInitialData(ISession session)
		{
			NhTable table = nhConfig.GetClassMapping(typeof(Data)).RootTable;
			// table and sequence creation
			if (!TableExist(session, table.Name))
			{
				new SchemaExport(nhConfig).Create(true, true);
			}
			using (var tx = session.BeginTransaction())
			{
				var rowCount = session.Query<Data>().Count();
				if(rowCount >= ROWCOUNT)
				{
					var randomCellNames = RandomString(CELLLENGTH).ToList();
					randomCellNames.ForEach(s => {
						var cellData = new Data
						{
							Cell = s
						};
						session.Save(cellData);
					});

					tx.Commit();
				}
			
			}
		}

		public static IList<string> RandomString(int length)
		{
			IList<string> randomCells = new List<string>();
			var rowcount = Enumerable.Range(0, ROWCOUNT-1).ToList();
			
			Random random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

			rowcount.ToList().ForEach(c => randomCells.Add(
				new string(Enumerable.Repeat(chars, length)
				.Select(s => s[random.Next(s.Length)]).ToArray())
			));


			return randomCells;
		}
	}
}
