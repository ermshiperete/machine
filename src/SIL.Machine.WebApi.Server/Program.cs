﻿using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SIL.Machine.WebApi.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args)
		{
			IConfigurationRoot configuration = new ConfigurationBuilder()
				.AddCommandLine(args)
				.Build();
			return WebHost.CreateDefaultBuilder(args)
				.ConfigureServices(services => services.AddAutofac())
				.ConfigureAppConfiguration((context, config) =>
				{
					config.AddJsonFile("appsettings.user.json", true);
					config.AddJsonFile("secrets.json", true, true);
				})
				.UseConfiguration(configuration)
				.UseStartup<Startup>()
				.Build();
		}
	}
}
