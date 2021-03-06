﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.ObjectModel;
using SIL.Machine.Annotations;

namespace SIL.Machine.WebApi.Server.Services
{
	[TestFixture]
	public class EngineRunnerTests
	{
		[Test]
		public async Task StartBuildAsync_BatchTrainerCalled()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync();
				await env.EngineRunner.InitNewAsync();
				Build build = await env.EngineRunner.StartBuildAsync(env.Engine);
				Assert.That(build, Is.Not.Null);
				env.EngineRunner.WaitForBuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<ProgressData>>(), Arg.Any<Action>());
				env.BatchTrainer.Received().Save();
				build = await env.BuildRepository.GetAsync(build.Id);
				Assert.That(build, Is.Null);
			}
		}

		[Test]
		public async Task CancelBuildAsync_BatchTrainerCalled()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync();
				await env.EngineRunner.InitNewAsync();
				env.BatchTrainer.Train(Arg.Any<IProgress<ProgressData>>(), Arg.Do<Action>(checkCanceled =>
					{
						while (true)
							checkCanceled();
					}));
				Build build = await env.EngineRunner.StartBuildAsync(env.Engine);
				Assert.That(build, Is.Not.Null);
				await Task.Delay(10);
				await env.EngineRunner.CancelBuildAsync();
				env.EngineRunner.WaitForBuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<ProgressData>>(), Arg.Any<Action>());
				env.BatchTrainer.DidNotReceive().Save();
				build = await env.BuildRepository.GetAsync(build.Id);
				Assert.That(build, Is.Null);
			}
		}

		[Test]
		public async Task CommitAsync_LoadedInactive()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync();
				await env.EngineRunner.InitNewAsync();
				await env.EngineRunner.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await Task.Delay(10);
				await env.EngineRunner.CommitAsync();
				env.SmtModel.Received().Save();
				Assert.That(env.EngineRunner.IsLoaded, Is.False);
			}
		}

		[Test]
		public async Task CommitAsync_LoadedActive()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync(TimeSpan.FromHours(1));
				await env.EngineRunner.InitNewAsync();
				await env.EngineRunner.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await env.EngineRunner.CommitAsync();
				env.SmtModel.Received().Save();
				Assert.That(env.EngineRunner.IsLoaded, Is.True);
			}
		}

		[Test]
		public async Task TranslateAsync()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync(TimeSpan.FromHours(1));
				await env.EngineRunner.InitNewAsync();
				TranslationResult result = await env.EngineRunner.TranslateAsync("esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		private class TestEnvironment : DisposableBase
		{
			public TestEnvironment()
			{
				EngineRepository = new MemoryEngineRepository();
				BuildRepository = new MemoryBuildRepository();
				BatchTrainer = Substitute.For<ISmtBatchTrainer>();
				BatchTrainer.Stats.Returns(new SmtBatchTrainStats());
				SmtModel = Substitute.For<IInteractiveSmtModel>();
			}

			public Engine Engine { get; private set; }
			public EngineRunner EngineRunner { get; private set; }
			public IEngineRepository EngineRepository { get; }
			public IBuildRepository BuildRepository { get; }
			public ISmtBatchTrainer BatchTrainer { get; }
			public IInteractiveSmtModel SmtModel { get; }

			public async Task CreateEngineAsync(TimeSpan inactiveTimeout = default(TimeSpan))
			{
				Engine = new Engine
				{
					Id = "engine1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					IsShared = false,
					Projects = {"project1"}
				};
				await EngineRepository.InsertAsync(Engine);
				var options = new EngineOptions {InactiveEngineTimeout = inactiveTimeout};
				EngineRunner = new EngineRunner(new OptionsWrapper<EngineOptions>(options), EngineRepository,
					BuildRepository, CreateSmtModelFactory(), CreateRuleEngineFactory(), CreateTextCorpusFactory(),
					Substitute.For<ILogger<EngineRunner>>(), Engine.Id);
			}

			private ISmtModelFactory CreateSmtModelFactory()
			{
				var factory = Substitute.For<ISmtModelFactory>();

				var smtEngine = Substitute.For<IInteractiveSmtEngine>();
				var translationResult = new TranslationResult("esto es una prueba .".Split(),
					"this is a test .".Split(),
					new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
					new[]
					{
						TranslationSources.Smt,
						TranslationSources.Smt,
						TranslationSources.Smt,
						TranslationSources.Smt,
						TranslationSources.Smt
					},
					new WordAlignmentMatrix(5, 5)
					{
						[0, 0] = AlignmentType.Aligned,
						[1, 1] = AlignmentType.Aligned,
						[2, 2] = AlignmentType.Aligned,
						[3, 3] = AlignmentType.Aligned,
						[4, 4] = AlignmentType.Aligned
					},
					new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) });
				smtEngine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
				smtEngine.GetWordGraph(Arg.Any<IReadOnlyList<string>>()).Returns(new WordGraph(new[]
					{
						new WordGraphArc(0, 1, 1.0, "this is".Split(),
							new WordAlignmentMatrix(2, 2)
							{
								[0, 0] = AlignmentType.Aligned, [1, 1] = AlignmentType.Aligned
							},
							Range<int>.Create(0, 2), false, new[] { 1.0, 1.0 }),
						new WordGraphArc(1, 2, 1.0, "a test".Split(),
							new WordAlignmentMatrix(2, 2)
							{
								[0, 0] = AlignmentType.Aligned, [1, 1] = AlignmentType.Aligned
							},
							Range<int>.Create(2, 4), false, new[] { 1.0, 1.0 }),
						new WordGraphArc(2, 3, 1.0, new[] {"."},
							new WordAlignmentMatrix(1, 1)
							{
								[0, 0] = AlignmentType.Aligned
							},
							Range<int>.Create(4, 5), false, new[] { 1.0 })
					}, new[] { 3 }));
				smtEngine.GetBestPhraseAlignment(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>())
					.Returns(translationResult);
				SmtModel.CreateInteractiveEngine().Returns(smtEngine);

				SmtModel.CreateBatchTrainer(Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(),
					Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(), Arg.Any<ITextAlignmentCorpus>())
					.Returns(BatchTrainer);

				factory.Create(Arg.Any<string>()).Returns(SmtModel);
				return factory;
			}

			private IRuleEngineFactory CreateRuleEngineFactory()
			{
				var factory = Substitute.For<IRuleEngineFactory>();
				var engine = Substitute.For<ITranslationEngine>();
				engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(new TranslationResult(
					"esto es una prueba .".Split(),
					"this is a test .".Split(),
					new[] {1.0, 1.0, 1.0, 1.0, 1.0},
					new[]
					{
						TranslationSources.Transfer,
						TranslationSources.Transfer,
						TranslationSources.Transfer,
						TranslationSources.Transfer,
						TranslationSources.Transfer
					},
					new WordAlignmentMatrix(5, 5)
					{
						[0, 0] = AlignmentType.Aligned,
						[1, 1] = AlignmentType.Aligned,
						[2, 2] = AlignmentType.Aligned,
						[3, 3] = AlignmentType.Aligned,
						[4, 4] = AlignmentType.Aligned
					},
					new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }));
				factory.Create(Arg.Any<string>()).Returns(engine);
				return factory;
			}

			private ITextCorpusFactory CreateTextCorpusFactory()
			{
				var factory = Substitute.For<ITextCorpusFactory>();
				factory.CreateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<TextCorpusType>())
					.Returns(Task.FromResult<ITextCorpus>(new DictionaryTextCorpus(Enumerable.Empty<IText>())));
				return factory;
			}

			protected override void DisposeManagedResources()
			{
				EngineRunner.Dispose();
			}
		}
	}
}
