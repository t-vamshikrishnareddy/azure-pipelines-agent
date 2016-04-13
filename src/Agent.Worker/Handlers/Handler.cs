using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    public interface IHandler : IAgentService
    {
        IExecutionContext ExecutionContext { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        string TaskDirectory { get; set; }

        Task RunAsync();
    }

    public abstract class Handler : AgentService
    {
        protected ICommandHandler CommandHandler { get; private set; }
        protected Dictionary<string, string> Environment { get; private set; }

        public IExecutionContext ExecutionContext { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public string TaskDirectory { get; set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            CommandHandler = hostContext.GetService<ICommandHandler>();
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        protected void AddEndpointsToEnvironment()
        {
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Endpoints, nameof(ExecutionContext.Endpoints));

            // Add the endpoints to the environment variable dictionary.
            foreach (ServiceEndpoint endpoint in ExecutionContext.Endpoints)
            {
                ArgUtil.NotNull(endpoint, nameof(endpoint));
                if (endpoint.Id == Guid.Empty &&
                    !string.Equals(endpoint.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Is this a production scenario?
                    continue;
                }

                string partialKey =
                    (endpoint.Id != Guid.Empty ? endpoint.Id.ToString() : ServiceEndpoints.SystemVssConnection)
                    .ToUpperInvariant();
                AddEnvironmentVariable(
                    key: $"ENDPOINT_URL_{partialKey}",
                    value: endpoint.Url?.ToString());
                AddEnvironmentVariable(
                    key: $"ENDPOINT_AUTH_{partialKey}",
                    // Note, JsonUtility.ToString will not null ref if the auth object is null.
                    value: JsonUtility.ToString(endpoint.Authorization));
                if (endpoint.Id != Guid.Empty)
                {
                    AddEnvironmentVariable(
                        key: $"ENDPOINT_DATA_{partialKey}",
                        // Note, JsonUtility.ToString will not null ref if the data object is null.
                        value: JsonUtility.ToString(endpoint.Data));
                }
            }
        }

        protected void AddInputsToEnvironment()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            // Add the inputs to the environment variable dictionary.
            foreach (KeyValuePair<string, string> pair in Inputs)
            {
                AddEnvironmentVariable(
                    key: $"INPUT_{pair.Key?.Replace(' ', '_').ToUpperInvariant()}",
                    value: pair.Value);
            }
        }

        protected void AddVariablesToEnvironment()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Environment, nameof(Environment));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Variables, nameof(ExecutionContext.Variables));

            // Add the public variables to the environment variable dictionary.
            foreach (KeyValuePair<string, string> pair in ExecutionContext.Variables.Public)
            {
                AddEnvironmentVariable(pair.Key, pair.Value);
            }
        }

        private void AddEnvironmentVariable(string key, string value)
        {
            ArgUtil.NotNullOrEmpty(key, nameof(key));
            Trace.Verbose($"Setting env '{key}' to '{value}'.");
            Environment[key] = value ?? string.Empty;
        }
    }
}