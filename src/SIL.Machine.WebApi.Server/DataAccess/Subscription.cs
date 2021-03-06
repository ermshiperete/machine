using System;
using System.Collections.Generic;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	internal class Subscription<TKey, TEntity> : DisposableBase where TEntity : class, IEntity<TEntity>
	{
		private readonly AsyncReaderWriterLock _repoLock;
		private readonly IDictionary<TKey, ISet<Action<EntityChange<TEntity>>>> _changeListeners;
		private readonly TKey _key;
		private readonly Action<EntityChange<TEntity>> _listener;

		public Subscription(AsyncReaderWriterLock repoLock,
			IDictionary<TKey, ISet<Action<EntityChange<TEntity>>>> changeListeners, TKey key,
			Action<EntityChange<TEntity>> listener)
		{
			_repoLock = repoLock;
			_changeListeners = changeListeners;
			_key = key;
			_listener = listener;

			if (!_changeListeners.TryGetValue(_key, out ISet<Action<EntityChange<TEntity>>> listeners))
			{
				listeners = new HashSet<Action<EntityChange<TEntity>>>();
				_changeListeners[_key] = listeners;
			}
			listeners.Add(listener);
		}

		protected override void DisposeManagedResources()
		{
			using (_repoLock.WriterLock())
			{
				ISet<Action<EntityChange<TEntity>>> listeners = _changeListeners[_key];
				listeners.Remove(_listener);
				if (listeners.Count == 0)
					_changeListeners.Remove(_key);
			}
		}
	}
}