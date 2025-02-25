using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Utility
{
    public class HangfireJobActivator : JobActivator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HangfireJobActivator(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public override object ActivateJob(Type jobType)
        {
            var scope = _serviceScopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService(jobType);
        }
    }

}
