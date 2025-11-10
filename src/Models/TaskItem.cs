using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoListApp.Models;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;

    // Step 6: Tags
    public string Tags { get; set; } = string.Empty;

    // Step 7: Due Date
    public DateTime? DueDate { get; set; }

    // Helper property to get tags as a list
    public List<string> TagList => Tags
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim())
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Distinct()
        .ToList();

    // Helper property to check if task is overdue
    public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Now.Date && !IsCompleted;

    // Helper property for display
    public string DueDateDisplay => DueDate.HasValue ? DueDate.Value.ToString("MM/dd/yyyy") : "";
}
