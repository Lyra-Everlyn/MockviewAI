using System.Linq.Expressions;

namespace MockviewAI.Repositories.Interfaces
{
    /// <summary>
    /// [DATA] 
    /// Thao tác dữ liệu chung cho mọi entity (EF Core).
    /// Đọc để hiển thị: không theo dõi thay đổi (no-tracking), nhẹ RAM.
    /// Đọc để sửa / xoá: có theo dõi (tracking), sau đó gọi UpdateAsync / DeleteAsync.
    /// Ghi: mỗi hàm tự lưu ngay (SaveChanges).
    /// </summary>
    /// <typeparam name="T">Entity có khoá chính kiểu int.</typeparam>
    public interface IRepository<T> where T : class
    {
        #region Đọc để hiển thị (no-tracking)
        /// <summary>Toàn bộ bản ghi. Chỉ dùng cho bảng nhỏ.</summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>Một trang dữ liệu, đánh số trang từ 1.</summary>
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);

        /// <summary>Các bản ghi thoả điều kiện, vd: <c>x => x.IsActive</c>.</summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Một bản ghi theo khoá chính; null nếu không có.</summary>
        Task<T?> GetByIdAsNoTrackingAsync(int id);

        /// <summary>Danh sách thoả điều kiện, kèm bảng con, sắp xếp và lấy N bản ghi đầu.</summary>
        /// <param name="predicate">Điều kiện lọc.</param>
        /// <param name="orderBy">Cách sắp xếp; null = giữ thứ tự của database.</param>
        /// <param name="take">Số bản ghi tối đa; null = lấy hết.</param>
        /// <param name="includes">Bảng con cần nạp kèm, vd: <c>s => s.Answers</c>.</param>
        Task<IEnumerable<T>> GetListAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            params Expression<Func<T, object>>[] includes);
        #endregion

        #region Đọc để sửa / xoá (tracking)
        /// <summary>Một bản ghi theo khoá chính, được theo dõi để sửa rồi gọi UpdateAsync.</summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>Bản ghi đầu tiên thoả điều kiện, kèm bảng con; được theo dõi để sửa cả bảng con.</summary>
        /// <param name="includes">Bảng con cần nạp kèm, vd: <c>s => s.Answers</c>.</param>
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        #endregion

        #region Ghi
        Task AddAsync(T entity);

        /// <summary>Lưu thay đổi. Bản ghi chưa được theo dõi sẽ được gắn vào và đánh dấu đã sửa toàn bộ.</summary>
        Task UpdateAsync(T entity);

        /// <summary>Xoá theo khoá chính; không có bản ghi thì bỏ qua.</summary>
        Task DeleteAsync(int id);
        #endregion
    }
}
