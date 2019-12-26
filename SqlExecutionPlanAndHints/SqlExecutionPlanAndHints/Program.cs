using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Id;
using NHibernate.Mapping;
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
		enum OperatonMode
		{
			Default = 0, // delete all data and recreate
			KeepData, //  store current data
			InsertData, // insert data to current data
			Clean // clean table and sequence
		}
		static OperatonMode OpMode;
		static readonly int ROWCOUNT = 2000;
		static readonly int CELLLENGTH = 6;
		static IConfigurationRoot appConfig { get; set; }
		static ISessionFactory sessionFactory { get; set; }

		static NhConfiguration nhConfig;
		// -k nothing to do
		// -i only insert data no create table and sequence
		// -c drop all data from database
		// if no argument then default operation mode is applied
		static void Main(string[] args)
		{
			StartUpActions();
			// var identifier = nhConfig.GetClassMapping(typeof(CellObjectWithOptim)).Identifier as SimpleValue;
			var argumentLength = args.Length;
			if (argumentLength == 1)
			{
				var keepMode = "-k";
				var insertMode = "-i";
				var cleanMode = "-c";
				var param1 = args[0].ToLowerInvariant();
				if (param1.StartsWith(keepMode))
				{
					OpMode = OperatonMode.KeepData;
				}
				else if (param1.StartsWith(insertMode))
				{
					OpMode = OperatonMode.InsertData;
				}

				else if (param1.StartsWith(cleanMode))
				{
					OpMode = OperatonMode.Clean;
				}
			}
			
			var session = sessionFactory.OpenSession();
			if(OpMode == OperatonMode.Clean)
			{
				var identifierData = nhConfig.GetClassMapping(typeof(Data)).Identifier as SimpleValue;
				var identifierDataWithOptim = nhConfig.GetClassMapping(typeof(DataWithOptim)).Identifier as SimpleValue;
				session.CreateSQLQuery($"drop table {identifierData.Table}");
				session.CreateSQLQuery($"drop table {identifierDataWithOptim.Table}");
				// also drop sequence
				session.Close();
				return;
			}
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
			modelMapper.AddMapping<DataWithOptimMapping>();
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
			if (OpMode == OperatonMode.KeepData)
			{
				NhTable table = nhConfig.GetClassMapping(typeof(Data)).RootTable;
				if (!TableExist(session, table.Name))
				{
					OpMode = OperatonMode.Default;
				}
			}

			if (OpMode == OperatonMode.Default)
			{
				new SchemaExport(nhConfig).Create(true, true);
			}

			if (OpMode == OperatonMode.Default || OpMode == OperatonMode.InsertData)
			{
				using (var tx = session.BeginTransaction())
				{
					var randomCellNames = RandomString(CELLLENGTH).ToList();
					randomCellNames.ForEach(s => {
						var cellData = new Data
						{
							Cell = s
						};
						var cellDataOptim = new DataWithOptim
						{
							Cell = s
						};
						session.Save(cellData);
						session.Save(cellDataOptim);
					});
					tx.Commit();

				}
			}

		}

		public static IList<string> RandomString(int length)
		{
			IList<string> randomCells = new List<string>();
			var rowcount = Enumerable.Range(0, ROWCOUNT - 1).ToList();

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
