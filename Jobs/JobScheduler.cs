using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tools_tpt_transformation_service.Jobs
{
    /// <summary>
    /// Job execution scheduler. As C# uses a system-wide thread pool, this class makes sure that pararellism is bounded by a configuration value.
    /// </summary>
    public class JobScheduler : IDisposable
    {
        private readonly ILogger<JobManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _maxConcurrentJobs;
        private readonly SemaphoreSlim _taskSemaphore;
        private readonly BlockingCollection<SchedulerEntry> _jobList;
        private readonly IDictionary<String, SchedulerEntry> _jobMap;
        private readonly CancellationTokenSource _tokenSource;
        private readonly Thread _schedulerThread;

        /// <summary>
        /// JobScheduler Constructor.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configuration">Service configuration.</param>
        public JobScheduler(
            ILogger<JobManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _maxConcurrentJobs = int.Parse(_configuration.GetValue<string>("Jobs:MaxConcurrent") ?? "4");
            _taskSemaphore = new SemaphoreSlim(_maxConcurrentJobs);
            _jobList = new BlockingCollection<SchedulerEntry>();
            _jobMap = new ConcurrentDictionary<string, SchedulerEntry>();

            _tokenSource = new CancellationTokenSource();
            _schedulerThread = new Thread(() => { RunScheduler(); });
            _schedulerThread.Start();

            _logger.LogDebug("JobEntryScheduler()");
        }

        /// <summary>
        /// Kicks off the scheduler until cancelled or shutdown. This uses a semaphore and map to continually executes jobs based on a max thread limit and allow for job cancellation.
        /// </summary>
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

                    if (_tokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Can't dequeue job");
            }
        }

        /// <summary>
        /// Adds a new scheduler entry for a job.
        /// </summary>
        /// <param name="nextEntry">SchedulerEntry to add.</param>
        public void AddEntry(SchedulerEntry nextEntry)
        {
            _jobList.Add(nextEntry);
            _jobMap[nextEntry.Job.Id] = nextEntry;
        }

        /// <summary>
        /// Removes a job scheduled entry.
        /// </summary>
        /// <param name="jobId">ID of job entry to remove.</param>
        public void RemoveEntry(string jobId)
        {
            if (_jobMap.TryGetValue(jobId, out SchedulerEntry jobEntry))
            {
                _jobMap.Remove(jobId);
                jobEntry.CancelJob();
            }
        }

        /// <summary>
        /// Disposes of class resources.
        /// </summary>
        public void Dispose()
        {
            _tokenSource.Cancel();

            _schedulerThread.Interrupt();
            _schedulerThread.Join();

            while (_jobList.TryTake(out SchedulerEntry nextEntry))
            {
                nextEntry.CancelJob();
            }
            _jobMap.Clear();
        }
    }
}