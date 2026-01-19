using System.Collections.Concurrent;
using System.Text;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowClaude.Agent;

/// <summary>
/// Claude AI Agent service implementation
/// Uses Anthropic's messages API for agent execution
/// </summary>
public class ClaudeAgentService : IAgentService
{
    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
    
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<ClaudeAgentService> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningExecutions = new();

    public ClaudeAgentService(string apiKey, ILogger<ClaudeAgentService>? logger = null)
    {
        _apiKey = apiKey;
        _logger = logger ?? new NullLogger<ClaudeAgentService>();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.anthropic.com/"),
            DefaultRequestHeaders =
            {
                { "x-api-key", apiKey },
                { "anthropic-version", "2023-06-01" },
                { "anthropic-dangerous-direct-browser-access", "true" }
            }
        };
    }

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(AgentRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningExecutions[executionId] = cts;

        var events = new List<AgentEvent>();

        try
        {
            // Build the messages payload
            var messages = new List<object>();
            
            // Add system prompt based on mode
            var systemPrompt = request.Mode == ChatMode.Plan
                ? @"You are in PLAN MODE. You analyze requests, ask clarifying questions, 
                   and create structured plans. You MUST NOT execute any tools. 
                   When ready, present a clear plan for user approval before execution."
                : @"You are an AI coding assistant. You can execute shell commands, 
                   read and write files, and perform git operations to help users 
                   with their software development tasks.";
            
            messages.Add(new
            {
                role = "user",
                content = request.Message
            });

            var payload = new
            {
                model = "claude-sonnet-4-20250514",
                max_tokens = 4096,
                messages,
                system = systemPrompt,
                tools = GetToolsForMode(request.Mode)
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to Claude API");
            
            using var response = await _httpClient.PostAsync("v1/messages", content, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                events.Add(new AgentErrorEvent
                {
                    Error = $"API Error: {response.StatusCode} - {error}"
                });
            }
            else
            {
                // Stream the response
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                        continue;

                    var data = line["data: ".Length..];
                    
                    if (data == "[DONE]")
                        break;

                    try
                    {
                        var chunk = JsonConvert.DeserializeObject<AnthropicResponse>(data);
                        if (chunk?.Delta?.Text != null)
                        {
                            events.Add(new AgentMessageEvent
                            {
                                Content = chunk.Delta.Text,
                                Role = MessageRole.Assistant
                            });
                        }

                        if (chunk?.Delta?.PartialJson != null)
                        {
                            // Parse tool calls from partial JSON
                            var toolEvents = ParseToolCalls(chunk.Delta.PartialJson);
                            events.AddRange(toolEvents);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse chunk: {Data}", data);
                    }
                }

                events.Add(new AgentCompletedEvent
                {
                    Success = true,
                    Summary = "Agent execution completed"
                });
            }
        }
        catch (OperationCanceledException)
        {
            events.Add(new AgentCompletedEvent
            {
                Success = false,
                Summary = "Execution was cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution failed");
            events.Add(new AgentErrorEvent
            {
                Error = ex.Message
            });
        }
        finally
        {
            _runningExecutions.TryRemove(executionId, out _);
        }

        foreach (var evt in events)
        {
            yield return evt;
        }
    }

    public IAsyncEnumerable<AgentEvent> ExecutePlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would retrieve the approved plan
        // and execute it in Agent mode
        return SendMessageAsync(new AgentRequest
        {
            Message = "Execute the approved plan",
            WorkspaceId = planId,
            Mode = ChatMode.Agent
        }, cancellationToken);
    }

    public Task CancelExecutionAsync(string executionId)
    {
        if (_runningExecutions.TryRemove(executionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        return Task.CompletedTask;
    }

    private IEnumerable<AgentEvent> ParseToolCalls(string partialJson)
    {
        // Parse tool calls from the streaming response
        // This is a simplified version - a full implementation would properly parse the JSON
        if (partialJson.Contains("\"type\":\"tool_use\""))
        {
            var toolMatch = System.Text.RegularExpressions.Regex.Match(partialJson, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            var inputMatch = System.Text.RegularExpressions.Regex.Match(partialJson, "input:\\s*\\{([^}]+)\\}");

            if (toolMatch.Success)
            {
                yield return new AgentToolCallEvent
                {
                    ToolName = toolMatch.Groups[1].Value,
                    Input = inputMatch.Success ? $"{{{inputMatch.Groups[1].Value}}}" : null,
                    Status = ToolCallStatus.Running
                };
            }
        }
        else if (partialJson.Contains("\"type\":\"tool_result\""))
        {
            var outputMatch = System.Text.RegularExpressions.Regex.Match(partialJson, "content:\\s*\"([^\"]*)\"");
            yield return new AgentToolCallEvent
            {
                ToolName = "result",
                Output = outputMatch.Success ? outputMatch.Groups[1].Value : null,
                Status = ToolCallStatus.Completed
            };
        }
    }

    private object[] GetToolsForMode(ChatMode mode)
    {
        if (mode == ChatMode.Plan)
        {
            // Plan mode has limited/no tool access
            return Array.Empty<object>();
        }

        // Agent mode has full tool access
        return new object[]
        {
            new
            {
                name = "Bash",
                description = "Run a bash command",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "The command to run" },
                        timeout = new { type = "integer", description = "Timeout in seconds" }
                    },
                    required = new[] { "command" }
                }
            },
            new
            {
                name = "Read",
                description = "Read a file",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "File path to read" }
                    },
                    required = new[] { "path" }
                }
            },
            new
            {
                name = "Write",
                description = "Write to a file",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "File path to write" },
                        content = new { type = "string", description = "Content to write" }
                    },
                    required = new[] { "path", "content" }
                }
            },
            new
            {
                name = "Glob",
                description = "Find files matching a pattern",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        pattern = new { type = "string", description = "Glob pattern" }
                    },
                    required = new[] { "pattern" }
                }
            },
            new
            {
                name = "Grep",
                description = "Search for text in files",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query" },
                        path = new { type = "string", description = "Directory to search" }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "Edit",
                description = "Edit a file",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "File path" },
                        find = new { type = "string", description = "Text to find" },
                        replace = new { type = "string", description = "Replacement text" }
                    },
                    required = new[] { "path", "find", "replace" }
                }
            },
            new
            {
                name = "WebFetch",
                description = "Fetch a web page",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        url = new { type = "string", description = "URL to fetch" }
                    },
                    required = new[] { "url" }
                }
            },
            new
            {
                name = "WebSearch",
                description = "Search the web",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query" }
                    },
                    required = new[] { "query" }
                }
            }
        };
    }

    private class AnthropicResponse
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("delta")]
        public Delta? Delta { get; set; }
    }

    private class Delta
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }

        [JsonProperty("partial_json")]
        public string? PartialJson { get; set; }
    }
}
