using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using TodoListApp.Models;

namespace TodoListApp;

public partial class MainWindow : Window
{
    private ObservableCollection<TaskItem> _tasks = new();

    public MainWindow()
    {
        InitializeComponent();
        TaskList.ItemsSource = _tasks;

        AddButton.Click += OnAddClick;
        DeleteButton.Click += OnDeleteClick;
        SaveButton.Click += OnSaveClick;
        ImportButton.Click += OnImportClick;

        // Load tasks on startup (Step 4)
        LoadTasksAsync();
    }

    private async void LoadTasksAsync()
    {
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

            // Read and deserialize JSON
            string jsonString = await File.ReadAllTextAsync(jsonPath);

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
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TaskInput.Text))
        {
            _tasks.Add(new TaskItem { Title = TaskInput.Text });
            TaskInput.Text = string.Empty;
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (TaskList.SelectedItem is TaskItem selected)
        {
            _tasks.Remove(selected);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Create data directory if it doesn't exist
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
            Directory.CreateDirectory(dataDir);

            // Serialize tasks to JSON
            string jsonPath = Path.Combine(dataDir, "tasks.json");
            string jsonString = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(jsonPath, jsonString);

            // Show success message (optional)
            var messageBox = new Window
            {
                Title = "Success",
                Width = 300,
                Height = 150,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = "Tasks saved successfully!", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                        new Button { Content = "OK", Width = 80, Margin = new Avalonia.Thickness(0, 20, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                    }
                }
            };

            if (messageBox.Content is StackPanel panel && panel.Children[1] is Button okButton)
            {
                okButton.Click += (s, args) => messageBox.Close();
            }

            await messageBox.ShowDialog(this);
        }
        catch (Exception ex)
        {
            // Show error message
            var errorBox = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = $"Error saving tasks: {ex.Message}", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button { Content = "OK", Width = 80, Margin = new Avalonia.Thickness(0, 20, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                    }
                }
            };

            if (errorBox.Content is StackPanel panel && panel.Children[1] is Button okButton)
            {
                okButton.Click += (s, args) => errorBox.Close();
            }

            await errorBox.ShowDialog(this);
        }
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Get the path to the JSON file
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
            string jsonPath = Path.Combine(dataDir, "tasks.json");

            // Check if file exists
            if (!File.Exists(jsonPath))
            {
                var notFoundBox = new Window
                {
                    Title = "File Not Found",
                    Width = 300,
                    Height = 150,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = "No tasks.json file found!", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                            new Button { Content = "OK", Width = 80, Margin = new Avalonia.Thickness(0, 20, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                        }
                    }
                };

                if (notFoundBox.Content is StackPanel panel && panel.Children[1] is Button okButton)
                {
                    okButton.Click += (s, args) => notFoundBox.Close();
                }

                await notFoundBox.ShowDialog(this);
                return;
            }

            // Read and deserialize JSON
            string jsonString = await File.ReadAllTextAsync(jsonPath);
            var loadedTasks = JsonSerializer.Deserialize<ObservableCollection<TaskItem>>(jsonString);

            if (loadedTasks != null)
            {
                // Clear existing tasks and add loaded ones
                _tasks.Clear();
                foreach (var task in loadedTasks)
                {
                    _tasks.Add(task);
                }

                // Show success message
                var messageBox = new Window
                {
                    Title = "Success",
                    Width = 300,
                    Height = 150,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = $"Loaded {loadedTasks.Count} task(s) successfully!", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                            new Button { Content = "OK", Width = 80, Margin = new Avalonia.Thickness(0, 20, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                        }
                    }
                };

                if (messageBox.Content is StackPanel panel && panel.Children[1] is Button okButton)
                {
                    okButton.Click += (s, args) => messageBox.Close();
                }

                await messageBox.ShowDialog(this);
            }
        }
        catch (JsonException ex)
        {
            // Show error message for invalid JSON
            var errorBox = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = $"Invalid JSON format: {ex.Message}", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button { Content = "OK", Width = 80, Margin = new Avalonia.Thickness(0, 20, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                    }
                }
            };

            if (errorBox.Content is StackPanel panel && panel.Children[1] is Button okButton)
            {
                okButton.Click += (s, args) => errorBox.Close();
            }

            await errorBox.ShowDialog(this);
        }
        catch (Exception ex)
        {
            // Show error message for other errors
            var errorBox = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = $"Error loading tasks: {ex.Message}", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button { Content = "OK", Width = 80, Margin = new Avalonia.Thickness(0, 20, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                    }
                }
            };

            if (errorBox.Content is StackPanel panel && panel.Children[1] is Button okButton)
            {
                okButton.Click += (s, args) => errorBox.Close();
            }

            await errorBox.ShowDialog(this);
        }
    }
}
