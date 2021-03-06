﻿using PubComp.Caching.Core;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheNotifierConfig : CacheNotificationsConfig
    {
        public RedisCacheNotifierPolicy Policy { get; set; }

        public override ICacheNotifier CreateCacheNotifications(string cachename)
        {
            return new RedisCacheNotifier(cachename, this.Policy);
        }
    }
}
