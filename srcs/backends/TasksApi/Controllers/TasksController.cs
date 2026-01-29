using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Security.Claims;
using TasksApi.Data;

namespace TasksApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TasksDbContext _context;
    private readonly ILogger<TasksController> _logger;

    public TasksController(TasksDbContext context, ILogger<TasksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        _logger.LogInformation("User {Username} fetching tasks", username);
        
        var tasks = await _context.Tasks.ToListAsync();
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> CreateTask(TaskItem task)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        task.CreatedBy = username;
        task.CreatedAt = DateTime.UtcNow;

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} created by {Username}", task.Id, username);

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, TaskItem task)
    {
        if (id != task.Id)
        {
            return BadRequest();
        }

        var existingTask = await _context.Tasks.FindAsync(id);
        if (existingTask == null)
        {
            return NotFound();
        }

        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.IsCompleted = task.IsCompleted;
        existingTask.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} updated", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Tasks.AnyAsync(e => e.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} deleted", id);

        return NoContent();
    }
}
