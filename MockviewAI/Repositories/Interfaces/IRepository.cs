using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MockviewAI.Repositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        /*
         NOTE:
            READ-ONLY or NO-TRACKING: Retrieve the data solely for display purposes, then immediately free up the RAM.
            TRACKING: Retrieve data to perform a specific WRITE operation
            WRITE: This group modifies the state/date within the Database
         */

        /// a. READ-ONLY | Retrieve the entire list of records from the database
        Task<IEnumerable<T>> GetAllAsync();


        /// b. READ-ONLY | Retrieve a paginated list of data
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);


        /// c. READ-ONLY | Search for a list of records that satisfy a specific condition
        /// predicate: Conditional expression (e.g., x => x.IsActive == true)
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        

        /// d. READ-ONLY | Find and quickly retrieve a single record by its Primary Key (Id).
        Task<T?> GetByIdAsNoTrackingAsync(int id);

        /// e. TRACKING | This group is used when data needs to be retrieved for editing or deletion.
        Task<T?> GetByIdAsync(int id);


        /// Thêm params để cho phép truyền vào nhiều bảng cần Include (bảng con)
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        /// Hàm lấy danh sách (kèm Include) với tùy chọn Sắp xếp và Lấy giới hạn (Take)
        Task<IEnumerable<T>> GetListAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            params Expression<Func<T, object>>[] includes);


        /// f. WRITE OPERATIONS
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}
