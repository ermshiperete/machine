﻿using System.Threading.Tasks;
using NUnit.Framework;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	[TestFixture]
	public class DataAccessExtensionsTests
	{
		[Test]
		public async Task GetNewerRevisionByEngineIdAsync_Insert()
		{
			var buildRepo = new MemoryBuildRepository();
			Task task = Task.Run(async () =>
			{
				await Task.Delay(10);
				var build = new Build {EngineId = "engine1", CurrentStep = 1};
				await buildRepo.InsertAsync(build);
			});
			EntityChange<Build> change = await buildRepo.GetNewerRevisionByEngineIdAsync("engine1", 0);
			await task;
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Insert));
			Assert.That(change.Entity.Revision, Is.EqualTo(0));
			Assert.That(change.Entity.CurrentStep, Is.EqualTo(1));
		}

		[Test]
		public async Task GetNewerRevisionAsync_Update()
		{
			var buildRepo = new MemoryBuildRepository();
			var build = new Build {EngineId = "engine1"};
			await buildRepo.InsertAsync(build);
			Task task = Task.Run(async () =>
				{
					await Task.Delay(10);
					build.CurrentStep = 1;
					await buildRepo.UpdateAsync(build);
				});
			EntityChange<Build> change = await buildRepo.GetNewerRevisionAsync(build.Id, 1);
			await task;
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Update));
			Assert.That(change.Entity.Revision, Is.EqualTo(1));
			Assert.That(change.Entity.CurrentStep, Is.EqualTo(1));
		}

		[Test]
		public async Task GetNewerRevisionAsync_Delete()
		{
			var buildRepo = new MemoryBuildRepository();
			var build = new Build {EngineId = "engine1"};
			await buildRepo.InsertAsync(build);
			Task task = Task.Run(async () =>
				{
					await Task.Delay(10);
					await buildRepo.DeleteAsync(build);
				});
			EntityChange<Build> change = await buildRepo.GetNewerRevisionAsync(build.Id, 1);
			await task;
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Delete));
		}

		[Test]
		public async Task GetNewerRevisionAsync_DoesNotExist()
		{
			var buildRepo = new MemoryBuildRepository();
			EntityChange<Build> change = await buildRepo.GetNewerRevisionAsync("build1", 1);
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Delete));
		}
	}
}
