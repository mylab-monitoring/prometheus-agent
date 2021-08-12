using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyLab.PrometheusAgent.Tools
{
    class ScrapeConfigProviderAggregator : IScrapeConfigProvider
    {
        private readonly IScrapeConfigProvider[] _originProviders;
        private readonly TimeSpan? _expiry;
        private ScrapeConfig _cachedCfg;
        private DateTime _cfgActuality;

        public ScrapeConfigProviderAggregator(IEnumerable<IScrapeConfigProvider> originProviders, TimeSpan? expiry)
        {
            _originProviders = originProviders.ToArray();
            _expiry = expiry;
        }

        public async Task<ScrapeConfig> LoadAsync()
        {
            var lastAutoCfgOldSec = DateTime.Now - _cfgActuality;
            var expired = _expiry < lastAutoCfgOldSec;

            if (_cachedCfg == null || (_expiry.HasValue && expired))
            {
                var jobs = new List<ScrapeConfigJob>();

                foreach (var scrapeConfigProvider in _originProviders)
                {
                    var cfg = await scrapeConfigProvider.LoadAsync();
                    jobs.AddRange(cfg.Jobs);
                }

                _cachedCfg = new ScrapeConfig
                {
                    Jobs = jobs.ToArray()
                };
                _cfgActuality = DateTime.Now;
            }

            return _cachedCfg;
        }
    }
}