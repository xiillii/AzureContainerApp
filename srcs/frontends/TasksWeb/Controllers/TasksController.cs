using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using TasksWeb.Services;

namespace TasksWeb.Controllers;

public class TasksController : Controller
{
    private readonly TasksApiClient _tasksApi;
    private readonly AuthService _authService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(TasksApiClient tasksApi, AuthService authService, ILogger<TasksController> logger)
    {
        _tasksApi = tasksApi;
        _authService = authService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var tasks = await _tasksApi.GetTasksAsync();
            return View(tasks ?? new List<TaskItem>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks");
            ViewBag.ErrorMessage = "Failed to load tasks. Please try again.";
            return View(new List<TaskItem>());
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskItem task)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid task data";
            return RedirectToAction("Index");
        }

        try
        {
            await _tasksApi.CreateTaskAsync(task);
            TempData["SuccessMessage"] = "Task created successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task");
            TempData["ErrorMessage"] = $"Failed to create task: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Update(int id, TaskItem task)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != task.Id)
        {
            TempData["ErrorMessage"] = "Task ID mismatch";
            return RedirectToAction("Index");
        }

        try
        {
            await _tasksApi.UpdateTaskAsync(id, task);
            TempData["SuccessMessage"] = "Task updated successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task");
            TempData["ErrorMessage"] = $"Failed to update task: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            await _tasksApi.DeleteTaskAsync(id);
            TempData["SuccessMessage"] = "Task deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task");
            TempData["ErrorMessage"] = $"Failed to delete task: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var tasks = await _tasksApi.GetTasksAsync();
            var task = tasks?.FirstOrDefault(t => t.Id == id);
            
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                await _tasksApi.UpdateTaskAsync(id, task);
                TempData["SuccessMessage"] = "Task status updated";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle task");
            TempData["ErrorMessage"] = $"Failed to update task: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
