﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xylab.Workflows.Legacy.Entities;
using Xylab.Workflows.Legacy.Services;

namespace Xylab.Workflows.Legacy.Activities
{
    public class SamplePingPong : IJobExecutorProvider
    {
        public string Type => "Sample.PingPong";

        public IJobExecutor Create(IServiceProvider serviceProvider)
        {
            return new Executor(
                serviceProvider.GetRequiredService<IJobFileProvider>());
        }

        private class Executor : IJobExecutor
        {
            private readonly IJobFileProvider _fileProvider;

            public Executor(IJobFileProvider fileProvider)
            {
                _fileProvider = fileProvider;
            }

            public async Task<JobStatus> ExecuteAsync(string arguments, Job entry, ILogger logger)
            {
                await _fileProvider.SaveOutputAsync(entry, arguments);
                logger.LogInformation("Pong! from {guid}", entry.JobId);
                return JobStatus.Finished;
            }
        }
    }
}
