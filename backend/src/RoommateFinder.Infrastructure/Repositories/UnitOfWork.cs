using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

public class EfUnitOfWork : IUnitOfWork
{
    private static readonly Regex IndexNameRegex = new(@"(?:index|constraint) '([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AppDbContext _db;

    public EfUnitOfWork(AppDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && IsDuplicateKey(sql))
        {
            // Quyết định (d): để DB bắt trùng, đổi thành lỗi có tên chỉ mục để Service trả 409 thân thiện.
            var match = IndexNameRegex.Match(sql.Message);
            throw new DuplicateKeyException(match.Success ? match.Groups[1].Value : null, ex);
        }
        catch (DbUpdateConcurrencyException)
        {
            // rowversion của Posts: dữ liệu đã bị người khác đổi kể từ lúc đọc (ví dụ chủ tin sửa đúng lúc kiểm duyệt viên duyệt).
            throw BusinessException.Conflict("Dữ liệu vừa được thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.");
        }
    }

    /// <summary>2601: unique index / filtered unique index; 2627: UNIQUE constraint / PRIMARY KEY.</summary>
    public static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_db.Database.CurrentTransaction != null) return NoopTransaction.Instance; // đã nằm trong giao dịch bên ngoài
        return new EfTransaction(await _db.Database.BeginTransactionAsync(ct));
    }

    private sealed class EfTransaction : IAppTransaction
    {
        private readonly IDbContextTransaction _tx;
        private bool _committed;

        public EfTransaction(IDbContextTransaction tx) => _tx = tx;

        public async Task CommitAsync(CancellationToken ct = default)
        {
            await _tx.CommitAsync(ct);
            _committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                try { await _tx.RollbackAsync(); } catch { /* kết nối có thể đã đóng */ }
            }
            await _tx.DisposeAsync();
        }
    }

    private sealed class NoopTransaction : IAppTransaction
    {
        public static readonly NoopTransaction Instance = new();
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
