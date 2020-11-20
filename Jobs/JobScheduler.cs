using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// Job execution scheduler.
    ///
    /// As C# uses a system-wide thread pool, this class makes sure that parallelism is bounded by configuration
    /// without regularly creating threads.
    ///
    /// Jobs submitted to the scheduler are either active or pending, as follows:
    /// - Active jobs are those currently being executed.
    /// - Pending jobs are those that aren't yet being executed. They may be waiting on other jobs to complete or
    /// be there in the instant between being enqueued and the Wait() or Take() in RunScheduler() returning.
    /// 
    /// _jobQueue keeps track of pending jobs. _jobMap keeps track of both active and pending jobs to enable tracking
    /// them down for cancellation by the user.
    /// 
    /// This may also be accomplished more simply for pending jobs (only) by non-destructively iterating the queue, but
    /// there also must be a way to cancel active jobs, necessitating either this map or a different container for
    /// just active jobs.
    /// 
    /// With the map there ends up being a single, efficient approach for jobs of either kind, vs one cancellation approach
    /// for pending and another for active jobs. The net complexity and performance win is therefore with the paired queue
    /// and map.
    /// </summary>
    public class JobScheduler : IDisposable
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobScheduler> _logger;

        /// <summary>
        /// Semaphore for tracking max concurrent tasks.
        /// </summary>
        private readonly SemaphoreSlim _taskSemaphore;

        /// <summary>
        /// Pending preview jobs.
        /// </summary>
        private readonly BlockingCollection<JobWorkflow> _jobQueue;

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
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _taskSemaphore = new SemaphoreSlim(int.Parse(configuration[ConfigConsts.MaxConcurrentJobsKey]
                                                         ?? throw new ArgumentNullException(ConfigConsts.MaxConcurrentJobsKey)));
            _jobQueue = new BlockingCollection<JobWorkflow>();
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
                    var nextEntry = _jobQueue.Take();

                    // handle preemptive cancellation
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
        public virtual void AddEntry(JobWorkflow nextEntry)
        {
            _logger.LogDebug($"AddEntry() - nextEntry.Job.Id={nextEntry.Job.Id}.");

            _jobQueue.Add(nextEntry);
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

            while (_jobQueue.TryTake(out var nextEntry))
            {
                nextEntry.CancelJob();
            }
            _jobMap.Clear();
        }
    }
}