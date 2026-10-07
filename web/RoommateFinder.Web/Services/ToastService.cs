namespace RoommateFinder.Web.Services;

public record Toast(Guid Id, string Kind, string Message);

/// <summary>Thông báo nổi góc màn hình (Bootstrap toast). Component Toasts.razor hiển thị danh sách.</summary>
public class ToastService
{
    private readonly List<Toast> _items = new();
    public IReadOnlyList<Toast> Items => _items;
    public event Action? Changed;

    public void Success(string message) => Add("success", message);
    public void Error(string message) => Add("danger", message);
    public void Warning(string message) => Add("warning", message);
    public void Info(string message) => Add("primary", message);

    /// <summary>Hiển thị lỗi từ API (message tiếng Việt) hoặc thông điệp chung.</summary>
    public void Error(Exception ex) => Error(ex is ApiException api ? api.Message : "Đã xảy ra lỗi. Vui lòng thử lại.");

    public void Remove(Guid id)
    {
        _items.RemoveAll(t => t.Id == id);
        Changed?.Invoke();
    }

    private void Add(string kind, string message)
    {
        var toast = new Toast(Guid.NewGuid(), kind, message);
        _items.Add(toast);
        if (_items.Count > 4) _items.RemoveAt(0);
        Changed?.Invoke();
        _ = AutoRemoveAsync(toast.Id, kind == "danger" ? 7000 : 4000);
    }

    private async Task AutoRemoveAsync(Guid id, int ms)
    {
        await Task.Delay(ms);
        Remove(id);
    }
}
