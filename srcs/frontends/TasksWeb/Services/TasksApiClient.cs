using System.Net.Http.Json;
using Shared.Models;

namespace TasksWeb.Services;

public class TasksApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TasksApiClient> _logger;
    private readonly AuthService _authService;

    public TasksApiClient(HttpClient httpClient, ILogger<TasksApiClient> logger, AuthService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
    }

    private void SetAuthHeader()
    {
        var token = _authService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<TaskItem>?> GetTasksAsync()
    {
        try
        {
            SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<List<TaskItem>>("/api/tasks");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tasks");
            return null;
        }
    }

    public async Task<TaskItem?> GetTaskAsync(int id)
    {
        try
        {
            SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<TaskItem>($"/api/tasks/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching task {TaskId}", id);
            return null;
        }
    }

    public async Task<bool> CreateTaskAsync(TaskItem task)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("/api/tasks", task);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return false;
        }
    }

    public async Task<bool> UpdateTaskAsync(int id, TaskItem task)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.PutAsJsonAsync($"/api/tasks/{id}", task);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.DeleteAsync($"/api/tasks/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            return false;
        }
    }
}
