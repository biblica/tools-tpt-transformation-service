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
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobManager> _logger;

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
        private readonly IDictionary<String, JobWorkflow> _jobMap;

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
            ILogger<JobManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _maxConcurrentJobs = int.Parse(_configuration.GetValue<string>("Jobs:MaxConcurrent")
                ?? throw new ArgumentNullException("Jobs:MaxConcurrent"));
            _taskSemaphore = new SemaphoreSlim(_maxConcurrentJobs);
            _jobList = new BlockingCollection<JobWorkflow>();
            _jobMap = new ConcurrentDictionary<string, JobWorkflow>();

            _tokenSource = new CancellationTokenSource();
            _schedulerThread = new Thread(() => { RunScheduler(); });
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
            try
            {
                while (true)
                {
                    _taskSemaphore.Wait();
                    JobWorkflow nextEntry = _jobList.Take();

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
            _jobList.Add(nextEntry);
            _jobMap[nextEntry.Job.Id] = nextEntry;
        }

        /// <summary>
        /// Removes a job scheduled entry.
        /// </summary>
        /// <param name="jobId">ID of job entry to remove (required).</param>
        public void RemoveEntry(string jobId)
        {
            if (_jobMap.TryGetValue(jobId, out JobWorkflow jobEntry))
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

            while (_jobList.TryTake(out JobWorkflow nextEntry))
            {
                nextEntry.CancelJob();
            }
            _jobMap.Clear();
        }
    }
}