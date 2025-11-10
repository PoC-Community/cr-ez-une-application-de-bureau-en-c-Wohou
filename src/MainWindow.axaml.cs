using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TodoListApp.Models;

namespace TodoListApp;

public partial class MainWindow : Window
{
    private ObservableCollection<TaskItem> _tasks = new();
    private ObservableCollection<TaskItem> _filteredTasks = new();
    private string _currentFilter = "All";
    private string _currentTagFilter = "";
    private CancellationTokenSource? _autoSaveCts;
    private bool _isAutoSaveEnabled = false; // Disabled during initial load
    private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            TaskList.ItemsSource = _filteredTasks;

            AddButton.Click += OnAddClick;
            DeleteButton.Click += OnDeleteClick;
            CompleteAllButton.Click += OnCompleteAllClick;
            ClearCompletedButton.Click += OnClearCompletedClick;

            // Filter buttons (Step 7)
            FilterAllButton.Click += (s, e) => ApplyFilter("All");
            FilterTodayButton.Click += (s, e) => ApplyFilter("Today");
            FilterWeekButton.Click += (s, e) => ApplyFilter("Week");
            FilterOverdueButton.Click += (s, e) => ApplyFilter("Overdue");

            // Tag filter
            TagFilterInput.TextChanged += (s, e) => ApplyTagFilter(TagFilterInput.Text ?? "");

            // Step 9: Auto-save on changes
            _tasks.CollectionChanged += OnTasksChanged;

            // Load tasks on startup (Step 4)
            LoadTasksAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MainWindow constructor: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async void LoadTasksAsync()
    {
        // Wait for the file lock to be available
        await _fileLock.WaitAsync();

        try
        {
            // Get the path to the JSON file
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
            string jsonPath = Path.Combine(dataDir, "tasks.json");

            // Check if file exists
            if (!File.Exists(jsonPath))
            {
                // No file exists, start with empty list (graceful handling)
                return;
            }

            // Read and deserialize JSON with proper file sharing
            string jsonString;
            using (var fileStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            using (var reader = new StreamReader(fileStream))
            {
                jsonString = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                // Empty file, start with empty list
                return;
            }

            var loadedTasks = JsonSerializer.Deserialize<ObservableCollection<TaskItem>>(jsonString);

            if (loadedTasks != null)
            {
                // Clear existing tasks and add loaded ones
                _tasks.Clear();
                foreach (var task in loadedTasks)
                {
                    _tasks.Add(task);
                }

                // Refresh filtered view
                ApplyCurrentFilter();
            }
        }
        catch (JsonException)
        {
            // Invalid JSON: start with empty list (graceful handling)
            // Could log the error if logging was implemented
        }
        catch (Exception)
        {
            // Other errors (permissions, etc.): start with empty list (graceful handling)
            // Could log the error if logging was implemented
        }
        finally
        {
            // Release the file lock
            _fileLock.Release();

            // Enable auto-save after initial load is complete
            _isAutoSaveEnabled = true;
            UpdateSaveStatus("All changes saved ✓", "Green");
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TaskInput.Text))
        {
            var newTask = new TaskItem
            {
                Title = TaskInput.Text,
                Tags = TagsInput.Text ?? "",
                DueDate = DueDatePicker.SelectedDate?.DateTime
            };

            _tasks.Add(newTask);

            // Clear inputs
            TaskInput.Text = string.Empty;
            TagsInput.Text = string.Empty;
            DueDatePicker.SelectedDate = null;

            // Refresh filtered view
            ApplyCurrentFilter();

            // Auto-save is triggered by CollectionChanged event
        }
    }

    private void ApplyFilter(string filterType)
    {
        _currentFilter = filterType;
        ApplyCurrentFilter();
    }

    private void ApplyTagFilter(string tagFilter)
    {
        _currentTagFilter = tagFilter.Trim().ToLower();
        ApplyCurrentFilter();
    }

    private void ApplyCurrentFilter()
    {
        _filteredTasks.Clear();

        var filtered = _tasks.AsEnumerable();

        // Apply date filter
        switch (_currentFilter)
        {
            case "Today":
                filtered = filtered.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Now.Date);
                break;
            case "Week":
                var weekEnd = DateTime.Now.Date.AddDays(7);
                filtered = filtered.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= DateTime.Now.Date && t.DueDate.Value.Date <= weekEnd);
                break;
            case "Overdue":
                filtered = filtered.Where(t => t.IsOverdue);
                break;
            case "All":
            default:
                // No date filtering
                break;
        }

        // Apply tag filter
        if (!string.IsNullOrWhiteSpace(_currentTagFilter))
        {
            filtered = filtered.Where(t => t.Tags.ToLower().Contains(_currentTagFilter));
        }

        foreach (var task in filtered)
        {
            _filteredTasks.Add(task);
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (TaskList.SelectedItem is TaskItem selected)
        {
            _tasks.Remove(selected);
            ApplyCurrentFilter();
        }
    }

    // Step 8: Complete All button
    private void OnCompleteAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var task in _tasks)
        {
            task.IsCompleted = true;
        }
        ApplyCurrentFilter();
        TriggerAutoSave();
    }

    // Step 8: Clear Completed button
    private void OnClearCompletedClick(object? sender, RoutedEventArgs e)
    {
        var completedTasks = _tasks.Where(t => t.IsCompleted).ToList();
        foreach (var task in completedTasks)
        {
            _tasks.Remove(task);
        }
        ApplyCurrentFilter();
    }

    // Step 8: Inline editing (double-click to edit)
    private async void OnTaskDoubleClick(object? sender, TappedEventArgs e)
    {
        if (TaskList.SelectedItem is not TaskItem selectedTask)
            return;

        var dialog = new Window
        {
            Title = "Edit Task",
            Width = 400,
            Height = 300,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 10
            }
        };

        var titleBox = new TextBox
        {
            Text = selectedTask.Title,
            Watermark = "Task title"
        };

        var tagsBox = new TextBox
        {
            Text = selectedTask.Tags,
            Watermark = "Tags (comma-separated)"
        };

        var datePicker = new DatePicker
        {
            SelectedDate = selectedTask.DueDate.HasValue
                ? new DateTimeOffset(selectedTask.DueDate.Value)
                : null
        };

        var completedCheckBox = new CheckBox
        {
            Content = "Completed",
            IsChecked = selectedTask.IsCompleted
        };

        var saveBtn = new Button
        {
            Content = "Save",
            Width = 100,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var cancelBtn = new Button
        {
            Content = "Cancel",
            Width = 100,
            Margin = new Avalonia.Thickness(0, 0, 110, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        if (dialog.Content is StackPanel panel)
        {
            panel.Children.Add(new TextBlock { Text = "Title:" });
            panel.Children.Add(titleBox);
            panel.Children.Add(new TextBlock { Text = "Tags:" });
            panel.Children.Add(tagsBox);
            panel.Children.Add(new TextBlock { Text = "Due Date:" });
            panel.Children.Add(datePicker);
            panel.Children.Add(completedCheckBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };
            buttonPanel.Children.Add(cancelBtn);
            buttonPanel.Children.Add(saveBtn);
            panel.Children.Add(buttonPanel);
        }

        saveBtn.Click += (s, args) =>
        {
            selectedTask.Title = titleBox.Text ?? "";
            selectedTask.Tags = tagsBox.Text ?? "";
            selectedTask.DueDate = datePicker.SelectedDate?.DateTime;
            selectedTask.IsCompleted = completedCheckBox.IsChecked ?? false;
            ApplyCurrentFilter();
            TriggerAutoSave();
            dialog.Close();
        };

        cancelBtn.Click += (s, args) => dialog.Close();

        await dialog.ShowDialog(this);
    }

    // Step 9: Auto-save functionality
    private void OnTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        TriggerAutoSave();
    }

    private void TriggerAutoSave()
    {
        if (!_isAutoSaveEnabled)
            return;

        // Cancel previous auto-save timer
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();

        // Update status
        UpdateSaveStatus("Saving...", "Orange");

        // Auto-save after 1 second of inactivity
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1000, _autoSaveCts.Token);
                await AutoSaveAsync();
            }
            catch (TaskCanceledException)
            {
                // Auto-save was cancelled, ignore
            }
        });
    }

    private async Task AutoSaveAsync()
    {
        // Wait for the file lock to be available
        await _fileLock.WaitAsync();

        try
        {
            // Create data directory if it doesn't exist
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
            Directory.CreateDirectory(dataDir);

            // Serialize tasks to JSON
            string jsonPath = Path.Combine(dataDir, "tasks.json");
            string jsonString = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });

            // Use FileStream with FileShare.None to prevent concurrent access
            using (var fileStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var writer = new StreamWriter(fileStream))
            {
                await writer.WriteAsync(jsonString);
            }

            // Update status on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateSaveStatus("All changes saved ✓", "Green");
            });
        }
        catch (UnauthorizedAccessException)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateSaveStatus("Error: Permission denied", "Red");
            });
        }
        catch (IOException ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateSaveStatus($"Error: {ex.Message}", "Red");
            });
        }
        catch (Exception)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateSaveStatus("Error saving", "Red");
            });
        }
        finally
        {
            // Release the file lock
            _fileLock.Release();
        }
    }

    private void UpdateSaveStatus(string message, string color)
    {
        SaveStatusText.Text = message;
        SaveStatusText.Foreground = color switch
        {
            "Green" => Avalonia.Media.Brushes.Green,
            "Orange" => Avalonia.Media.Brushes.Orange,
            "Red" => Avalonia.Media.Brushes.Red,
            _ => Avalonia.Media.Brushes.Gray
        };
    }
}

