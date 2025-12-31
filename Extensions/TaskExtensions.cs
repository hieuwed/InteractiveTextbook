namespace InteractiveTextbook.Extensions;

public static class TaskExtensions
{
    /// <summary>
    /// Fire-and-forget để async tasks chạy ngầm mà không cần await
    /// </summary>
    public static async void FireAndForget(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            // Log exception nếu cần
            System.Diagnostics.Debug.WriteLine($"FireAndForget exception: {ex.Message}");
        }
    }
}
