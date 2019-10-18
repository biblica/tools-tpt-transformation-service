using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tools_tpt_transformation_service.Jobs
{
    public class JobScheduler : IDisposable
    {
        private readonly ILogger<JobManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _maxConcurrency;
        private readonly SemaphoreSlim _taskSemaphore;
        private readonly BlockingCollection<SchedulerEntry> _jobList;
        private readonly IDictionary<String, SchedulerEntry> _jobMap;
        private readonly Thread _schedulerThread;

        public JobScheduler(
            ILogger<JobManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _maxConcurrency = int.Parse(_configuration.GetValue<string>("MaxConcurrentServerRequests") ?? "4");
            _taskSemaphore = new SemaphoreSlim(_maxConcurrency);
            _jobList = new BlockingCollection<SchedulerEntry>();
            _jobMap = new ConcurrentDictionary<string, SchedulerEntry>();
            _schedulerThread = new Thread(() => { RunScheduler(); });
            _schedulerThread.Start();
            _logger.LogDebug("JobEntryScheduler()");
        }

        private void RunScheduler()
        {
            try
            {
                while (true)
                {
                    _taskSemaphore.Wait();
                    SchedulerEntry nextEntry = _jobList.Take();

                    if (nextEntry.IsJobCanceled())
                    {
                        _jobMap.Remove(nextEntry.Job.Id);
                        _taskSemaphore.Release();
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                nextEntry.RunJob();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Can't run job: {nextEntry.Job.Id}");
                            }
                            finally
                            {
                                _jobMap.Remove(nextEntry.Job.Id);
                                _taskSemaphore.Release();
                            }
                        }, nextEntry.CancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Can't dequeue job");
            }
        }

        public void AddEntry(SchedulerEntry nextEntry)
        {
            _jobList.Add(nextEntry);
            _jobMap[nextEntry.Job.Id] = nextEntry;
        }

        public void RemoveEntry(string jobId)
        {
            if (_jobMap.TryGetValue(jobId, out SchedulerEntry jobEntry))
            {
                _jobMap.Remove(jobId);
                jobEntry.CancelJob();
            }
        }

        public void Dispose()
        {
            _schedulerThread.Abort();
            _schedulerThread.Join();

            while (_jobList.TryTake(out SchedulerEntry nextEntry))
            {
                nextEntry.CancelJob();
            }
            _jobMap.Clear();
        }
    }
}