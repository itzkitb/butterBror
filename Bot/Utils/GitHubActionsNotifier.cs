using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bb.Utils
{
    public interface IGitHubActionsNotifier : IHostedService
    {
        event EventHandler<RunStatusChangedEventArgs> RunStatusChanged;
    }

    public class RunStatusChangedEventArgs : EventArgs
    {
        public string RunId { get; set; }
        public string Status { get; set; }
        public string Conclusion { get; set; }
        public string HtmlUrl { get; set; }
        public string Repository { get; set; }
        public string Branch { get; set; }
        public string Event { get; set; }
        public string Actor { get; set; }
    }

    public class GitHubActionsNotifier : BackgroundService, IGitHubActionsNotifier
    {
        public event EventHandler<RunStatusChangedEventArgs> RunStatusChanged;
        private readonly string _repo;
        private readonly string _token;
        private readonly TimeSpan _pollingInterval;
        private readonly HttpClient _httpClient;
        private string _lastRunId;

        public GitHubActionsNotifier(string repo, string token = null, TimeSpan pollingInterval = default)
        {
            _repo = repo;
            _token = token;
            _pollingInterval = pollingInterval == default ? TimeSpan.FromSeconds(10) : pollingInterval;
            _httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(_token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", _token);
            }
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubActionsNotifier/1.0");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForNewRuns(stoppingToken);
                }
                catch (Exception ex)
                {
                    Core.Bot.Logger.Write(ex);
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task CheckForNewRuns(CancellationToken stoppingToken)
        {
            string url = $"https://api.github.com/repos/{_repo}/actions/runs";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = await _httpClient.SendAsync(request, stoppingToken);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync(stoppingToken);
                var doc = JsonDocument.Parse(json);
                JsonElement runs = doc.RootElement.GetProperty("workflow_runs");

                if (runs.GetArrayLength() > 0)
                {
                    JsonElement latestRun = runs[0];
                    string runId = latestRun.GetProperty("id").GetInt64().ToString();
                    string status = latestRun.GetProperty("status").GetString();
                    string conclusion = latestRun.GetProperty("conclusion").GetString();
                    string htmlUrl = latestRun.GetProperty("html_url").GetString();
                    string branch = latestRun.GetProperty("head_branch").GetString();
                    string @event = latestRun.GetProperty("event").GetString();
                    string actor = latestRun.GetProperty("actor").GetProperty("login").GetString() ?? "unknown";

                    if (_lastRunId != runId)
                    {
                        _lastRunId = runId;
                        RunStatusChanged?.Invoke(this, new RunStatusChangedEventArgs
                        {
                            RunId = runId,
                            Status = status,
                            Conclusion = conclusion,
                            HtmlUrl = htmlUrl,
                            Repository = _repo,
                            Branch = branch,
                            Event = @event,
                            Actor = actor
                        });
                    }
                }
            }
        }
    }
}
