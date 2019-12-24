using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using System;
using System.Configuration;
using System.IO;

namespace SqlExecutionPlanAndHints
{
	
	class Program
	{
		static IConfigurationRoot appConfig { get; set; }
		static ISessionFactory sessionFactory { get; set; }
		static void Main(string[] args)
		{
			StartUpActions();
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
			var nhConfiguration = nhConfigPath == null ? configurationObj.Configure() : configurationObj.Configure(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nhConfigPath));

			sessionFactory = nhConfiguration.BuildSessionFactory();
			var session = sessionFactory.OpenSession();
			var count = session.CreateSQLQuery("select count(*) from all_cells").List();
			session.Close();
		}
	}
}
