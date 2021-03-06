﻿using System;

namespace PubComp.Caching.Core
{
    /// <summary>
    /// A layered cache e.g. level1 = in-memory cache that falls back to level2 = distributed cache
    /// </summary>
    public class LayeredCache : ICache
    {
        private readonly String name;
        private ICache level1;
        private ICache level2;
        private readonly Object sync = new Object();
        private readonly LayeredCachePolicy policy;
        private readonly CacheSynchronizer synchronizer;

        public LayeredCache(String name, LayeredCachePolicy policy)
            : this(
                name,
                (policy != null ? policy.Level1CacheName : null),
                (policy != null ? policy.Level2CacheName : null))
        {
            this.policy = policy;
        }

        /// <summary>
        /// Creates a layered cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level1CacheName">Name of first cache to check (e.g. in-memory cache), should be registered in CacheManager</param>
        /// <param name="level2CacheName">Name of fallback cache (e.g. distributed cache), should be registered in CacheManager</param>
        public LayeredCache(String name, String level1CacheName, String level2CacheName)
        {
            this.name = name;

            // ReSharper disable once LocalVariableHidesMember
            var level1 = CacheManager.GetCache(level1CacheName);
            if (level1 == null)
                throw new ApplicationException("Cache is not registered: level1CacheName=" + level1CacheName);

            // ReSharper disable once LocalVariableHidesMember
            var level2 = CacheManager.GetCache(level2CacheName);
            if (level2 == null)
                throw new ApplicationException("Cache is not registered: level2CacheName=" + level2CacheName);

            if (level2 == level1)
            {
                throw new ApplicationException(
                    string.Format("level2 must not be the same as level1, received {0}={1}, {2}={3}, which map to {4} and {5}",
                        "level1CacheName", level1CacheName, "level2CacheName", level2CacheName, level1.Name, level2.Name));
            }

            this.level1 = level1;
            this.level2 = level2;

            this.policy = new LayeredCachePolicy { Level1CacheName = level1CacheName, Level2CacheName = level1CacheName };
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy.SyncProvider);
        }
        
        /// <summary>
        /// Creates a layered cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level1">First cache to check (e.g. in-memory cache)</param>
        /// <param name="level2">Fallback cache (e.g. distributed cache)</param>
        public LayeredCache(String name, ICache level1, ICache level2)
        {
            this.name = name;

            if (level1 == null)
                throw new ApplicationException("innerCache1 must not be null");

            if (level2 == null)
                throw new ApplicationException("innerCache2 must not be null");

            if (level2 == level1)
            {
                throw new ApplicationException(
                    string.Format("Cache2 must not be the same as cache2, received {0}={1} and {2}={3}",
                        "level1", level1.Name, "level2", level2.Name));
            }

            this.level1 = level1;
            this.level2 = level2;

            this.policy = new LayeredCachePolicy { Level1CacheName = level1.Name, Level2CacheName = level2.Name };
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy.SyncProvider);
        }

        public string Name { get { return this.name; } }

        protected ICache Level1 { get { return this.level1; } }

        protected ICache Level2 { get { return this.level2; } }

        protected LayeredCachePolicy Policy { get { return this.policy; } }

        private TValue GetterWrapper<TValue>(String key, Func<TValue> getter)
        {
            return this.level2.Get(key, getter);
        }
        
        public bool TryGet<TValue>(string key, out TValue value)
        {
            if (this.level1.TryGet(key, out value))
                return true;

            if (this.level2.TryGet(key, out value))
            {
                this.level1.Set(key, value);
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void Set<TValue>(String key, TValue value)
        {
            this.level2.Set(key, value);
            this.level1.Set(key, value);
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            return this.level1.Get(key, () => GetterWrapper(key, getter));
        }

        public void Clear(String key)
        {
            this.level2.Clear(key);
            this.level1.Clear(key);
        }

        public void ClearAll()
        {
            this.level2.ClearAll();
            this.level1.ClearAll();
        }
    }
}
