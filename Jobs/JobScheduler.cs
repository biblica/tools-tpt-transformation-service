using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TptMain.Jobs
{
    /// <summary>
    /// Job execution scheduler.
    ///
    /// As C# uses a system-wide thread pool, this class makes sure that parallelism is bounded by a configuration value.
    /// </summary>
    public class JobScheduler : IDisposable
    {
        /// <summary>
        /// Max concurrent jobs config key.
        /// </summary>
        private const string MaxConcurrentJobsKey = "Jobs:MaxConcurrent";

        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobScheduler> _logger;

        /// <summary>
        /// System configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Max concurrent jobs (configured).
        /// </summary>
        private readonly int _maxConcurrentJobs;

        /// <summary>
        /// Semaphore for tracking max concurrent tasks.
        /// </summary>
        private readonly SemaphoreSlim _taskSemaphore;

        /// <summary>
        /// Pending preview jobs.
        /// </summary>
        private readonly BlockingCollection<JobWorkflow> _jobList;

        /// <summary>
        /// Active and pending preview jobs.
        /// </summary>
        private readonly IDictionary<string, JobWorkflow> _jobMap;

        /// <summary>
        /// Cancellation token for entire scheduler.
        /// </summary>
        private readonly CancellationTokenSource _tokenSource;

        /// <summary>
        /// Thread that iterates queue and runs tasks.
        /// </summary>
        private readonly Thread _schedulerThread;

        /// <summary>
        /// JobScheduler Constructor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public JobScheduler(
            ILogger<JobScheduler> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _maxConcurrentJobs = int.Parse(_configuration[MaxConcurrentJobsKey]
                ?? throw new ArgumentNullException(MaxConcurrentJobsKey));
            _taskSemaphore = new SemaphoreSlim(_maxConcurrentJobs);
            _jobList = new BlockingCollection<JobWorkflow>();
            _jobMap = new ConcurrentDictionary<string, JobWorkflow>();

            _tokenSource = new CancellationTokenSource();
            _schedulerThread = new Thread(RunScheduler);
            _schedulerThread.Start();

            _logger.LogDebug("JobEntryScheduler()");
        }

        /// <summary>
        /// Kicks off the scheduler until cancelled or shutdown.
        ///
        /// This uses a semaphore and map to continually executes jobs based on a max thread limit and allow for job cancellation.
        /// </summary>
        private void RunScheduler()
        {
            _logger.LogDebug("RunScheduler().");
            try
            {
                while (true)
                {
                    _taskSemaphore.Wait();
                    var nextEntry = _jobList.Take();

                    // handle preemptive cancelation
                    if (nextEntry.IsJobCanceled)
                    {
                        _jobMap.Remove(nextEntry.Job.Id);
                        _taskSemaphore.Release();
                    }
                    else
                    {
                        // spin up task and move on
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
        /// <param name="nextEntry">SchedulerEntry to add (required).</param>
        public void AddEntry(JobWorkflow nextEntry)
        {
            _logger.LogDebug($"AddEntry() - nextEntry.Job.Id={nextEntry.Job.Id}.");

            _jobList.Add(nextEntry);
            _jobMap[nextEntry.Job.Id] = nextEntry;
        }

        /// <summary>
        /// Removes a job scheduled entry.
        /// </summary>
        /// <param name="jobId">ID of job entry to remove (required).</param>
        public void RemoveEntry(string jobId)
        {
            _logger.LogDebug($"RemoveEntry() - jobId={jobId}.");
            if (_jobMap.TryGetValue(jobId, out var jobEntry))
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
            _logger.LogDebug("Dispose().");
            _tokenSource.Cancel();

            _schedulerThread.Interrupt();
            _schedulerThread.Join();

            while (_jobList.TryTake(out var nextEntry))
            {
                nextEntry.CancelJob();
            }
            _jobMap.Clear();
        }
    }
}