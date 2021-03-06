﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoDb;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class NoDbEngineRepository : NoDbRepository<Engine>, IEngineRepository
	{
		public NoDbEngineRepository(IBasicCommands<Engine> commands, IBasicQueries<Engine> queries)
			: base(commands, queries)
		{
		}

		public async Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			IEnumerable<Engine> engines = await Queries.GetAllAsync(NoDbProjectId);
			return engines.SingleOrDefault(e => e.IsShared && e.SourceLanguageTag == sourceLanguageTag
				&& e.TargetLanguageTag == targetLanguageTag);
		}

		public async Task<Engine> GetByProjectIdAsync(string projectId)
		{
			IEnumerable<Engine> engines = await Queries.GetAllAsync(NoDbProjectId);
			return engines.SingleOrDefault(e => e.Projects.Contains(projectId));
		}
	}
}
