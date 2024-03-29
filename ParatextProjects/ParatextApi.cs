﻿/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using TptMain.Exceptions;
using TptMain.Models;
using TptMain.ParatextProjects.Models;
using TptMain.Util;

namespace TptMain.ParatextProjects
{
    /// <summary>
    /// Paratext API request wrapper class used for handling requests to the Paratext Registry API and the responses that come back.
    /// </summary>
    public class ParatextApi : IDisposable
    {
        /// <summary>
        /// Type-specific logger.
        /// </summary>
        private readonly ILogger<ParatextApi> _logger;

        /// <summary>
        /// System configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// HTTP Client used to make calls against the Paratext Registry API.
        /// </summary>
        private readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// The Paratext project member roles that are allowed access to the typesetting preview of projects.
        /// </summary>
        private readonly List<MemberRole> _allowedMemberRoles;

        /// <summary>
        /// Cache for API responses to reduce load and time on API requests.
        /// 
        /// MemoryCache is thread safe.
        /// </summary>
        private readonly MemoryCache _apiCache = MemoryCache.Default;

        /// <summary>
        /// Semaphore using to handle thread safety when gathering projects.
        /// </summary>
        private static readonly SemaphoreSlim ProjectsSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// How long cached items have until they expire in seconds.
        /// </summary>
        private readonly double _cacheItemAgeInSec;

        /// <summary>
        /// Cache Key for accessing or storing Paratext projects.
        /// </summary>
        public const string ProjectsCacheKey = "Projects";

        /// <summary>
        /// HTTP Client getter for consistent usage.
        /// </summary>
        public virtual HttpClient HttpClient => _httpClient;

        /// <summary>
        /// API Cache for consistent usage.
        /// </summary>
        public virtual MemoryCache ApiCache => _apiCache;

        /// <summary>
        /// Paratext API ctor. Initialized using ASP.NET Dependency Injection.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configuration">Configuration context for pulling needed parameters.</param>
        public ParatextApi(
            ILogger<ParatextApi> logger,
            IConfiguration configuration)
        {
            // validate and set inputs
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Setup HttpClient
            _httpClient.BaseAddress = new Uri(_configuration.GetValue<string>(ConfigConsts.ParatextApiServerUriKey));

            // Set Auth header
            _httpClient.DefaultRequestHeaders.Authorization = ParatextApi.CreateBasicAuthHeader(
                _configuration.GetValue<string>(ConfigConsts.ParatextApiUsernameKey), _configuration.GetValue<string>(ConfigConsts.ParatextApiPasswordKey));

            // Member roles that are allowed access to generating previews.
            var section = _configuration.GetSection(ConfigConsts.ParatextApiAllowedMemberRolesKey);
            _allowedMemberRoles = section.Get<List<MemberRole>>();
            _ = _allowedMemberRoles ?? throw new ArgumentException($"Configuration parameter '{ConfigConsts.ParatextApiAllowedMemberRolesKey}'");

            // Age of project cache items in seconds
            _cacheItemAgeInSec = _configuration.GetValue<double>(ConfigConsts.ParatextApiProjectCacheAgeInSecKey);
        }

        /// <summary>
        /// Checks if user is authorized on a Paratext project to generate a typesetting preview. If the user isn't authorized, the job will be marked as such.
        /// </summary>
        /// <param name="previewJob">Preview job that contains user and the project to check if user is authorized.</param>
        public virtual void IsUserAuthorizedOnProject(PreviewJob previewJob)
        {
            // Validate input
            _ = previewJob ?? throw new ArgumentException(nameof(previewJob));

            var isAuthorized = IsUserAuthorizedOnProject(previewJob.User, previewJob.BibleSelectionParams.ProjectName).Result;

            if (!isAuthorized)
            {
                throw new PreviewJobException(previewJob, $"User '{previewJob.User}' is unauthorized to generate a typesetting preview for project '{previewJob.BibleSelectionParams.ProjectName}'. Authorized Paratext roles: '{String.Join(", ", _allowedMemberRoles)}'");
            }
        }

        /// <summary>
        /// Checks if user is authorized on a Paratext project to generate a typesetting preview.
        /// </summary>
        /// <param name="user">User to validate if authorized.</param>
        /// <param name="projectShortname">Shortname of the project to validate against.</param>
        /// <returns>True: User is authorized on specified project; False, otherwise.</returns>
        public virtual async Task<Boolean> IsUserAuthorizedOnProject(string user, string projectShortname)
        {
            _logger.LogDebug($"Looking if user '{user}' is authorized to generate a typesetting preview for project 'projectShortname'");

            // Validate input
            _ = user ?? throw new ArgumentException(nameof(user));
            _ = projectShortname ?? throw new ArgumentException(nameof(projectShortname));

            // Get authorized users for specified project
            var allowedMembers = await GetAllowedProjectMembersAsync(projectShortname);

            // Review if the user is in the allowed members list.
            var foundMember = allowedMembers.Find(member => member.UserName.Equals(user, StringComparison.InvariantCultureIgnoreCase));

            return foundMember != null;
        }

        /// <summary>
        /// Async method for retrieving allowed project members for project based on Paratext project shortname (EG: usNIV11).
        /// </summary>
        /// <param name="projectName">Paratext project shortname (EG: usNIV11).</param>
        /// <returns>Project members who are allowed to create typesetting previews.</returns>
        public virtual async Task<List<ProjectMember>> GetAllowedProjectMembersAsync(String projectName)
        {
            _logger.LogDebug($"Getting members of PT project '{projectName}'");

            // Validate input
            _ = projectName ?? throw new ArgumentException(nameof(projectName));

            // Get project ID from shortname
            var projectId = await GetProjectIdFromShortname(projectName);

            // Request project members from API.
            var allowedProjectMembers = new List<ProjectMember>();
            using (var projectMembersResponse = await HttpClient.GetAsync($"projects/{projectId}/members"))
            {
                if (projectMembersResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully got PT project members response");

                    // Read content and deserialize
                    var content = await projectMembersResponse.Content.ReadAsStringAsync();
                    var projectMembersList = JsonConvert.DeserializeObject<List<ProjectMember>>(content);

                    // Filter out each member that is allowed access to the project based on their role
                    foreach (var member in projectMembersList)
                    {
                        if (_allowedMemberRoles.Contains(member.Role))
                        {
                            allowedProjectMembers.Add(member);
                        }
                    }
                }
                else
                {
                    throw new HttpRequestException($"Got an error HTTP response retrieving project members. HTTP status code: '{projectMembersResponse.StatusCode}'. Phrase: '{projectMembersResponse.ReasonPhrase}'");
                }
            }

            return allowedProjectMembers;
        }

        /// <summary>
        /// Async method for retrieving available Paratext projects.
        /// </summary>
        /// <returns>Available projects.</returns>
        public virtual async Task<List<Project>> GetParatextProjectsAsync()
        {
            _logger.LogDebug("Getting list of Paratext projects");

            List<Project> projectsList = null;

            // Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, 
            // otherwise this thread waits here until the semaphore is released. 
            //
            // The projects request is one we'll use repeatedly, so we'll ensure we're retrieving and caching in a threadsafe manner.
            await ProjectsSemaphore.WaitAsync();
            try
            {

                // First, attempt to retrieve cached version of Paratext projects
                projectsList = (List<Project>)ApiCache.Get(ProjectsCacheKey);
                if (projectsList != null)
                {
                    _logger.LogDebug("Retrieved cached version of Paratext projects response");
                    return projectsList;
                }

                // Request projects from API 
                using (var projectsResponse = await HttpClient.GetAsync("projects"))
                {
                    if (projectsResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully got Paratext projects response");

                        var content = await projectsResponse.Content.ReadAsStringAsync();
                        projectsList = JsonConvert.DeserializeObject<List<Project>>(content);

                        ApiCache.Set(ProjectsCacheKey, projectsList, CreateDefaultCacheItemPolicy());
                    }
                    else
                    {
                        throw new HttpRequestException($"Got an error HTTP response retrieving projects. HTTP status code: '{projectsResponse.StatusCode}'. Phrase: '{projectsResponse.ReasonPhrase}'");
                    }
                }
            }
            finally
            {
                ProjectsSemaphore.Release();
            }

            return projectsList;
        }

        /// <summary>
        /// Helper function for retrieving a Paratext unique identifier from a project's shortname (usNIV11).
        /// </summary>
        /// <param name="shortname">The shortname of the project to find.</param>
        /// <returns>The associated Paratext project's ID.</returns>
        public virtual async Task<string> GetProjectIdFromShortname(string shortname)
        {
            // Validate input
            _ = shortname ?? throw new ArgumentException(nameof(shortname));

            var projects = await GetParatextProjectsAsync();

            // Find project with given short name
            var foundProject = projects.Find(project => project.Identification_ShortName.Equals(shortname, StringComparison.InvariantCultureIgnoreCase));

            // Return the associated project ID
            var projectId = foundProject.Identification_SystemId.Find(identifier => identifier.Type.Equals("paratext"));
            return projectId.Text;
        }

        /// <summary>
        /// Create a default CacheItemPolicy for caching API responses.
        /// </summary>
        /// <returns>Default CacheItemPolicy for caching API responses.</returns>
        private CacheItemPolicy CreateDefaultCacheItemPolicy() => new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_cacheItemAgeInSec)
        };

        /// <summary>
        /// Create an Basic Authentication Header object given credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Basic Authentication Header object given provided credentials.</returns>
        private static AuthenticationHeaderValue CreateBasicAuthHeader(String username, String password) => new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"))
            );

        public void Dispose()
        {
            _httpClient.Dispose();
            _apiCache.Dispose();
        }
    }
}
