using Microsoft.AspNetCore.Http;
using Pm.DTOs.WarehousePartBorrow;

namespace Pm.Services.WarehousePartBorrow
{
    public interface IWarehousePartCatalogService
    {
        Task<List<WarehousePartCatalogDto>> SearchAsync(string? query, int limit = 10);
        Task<Pm.DTOs.Common.PagedResultDto<WarehousePartCatalogDto>> GetAllAsync(int page, int pageSize, string? search);
        Task<WarehousePartImportResultDto> ImportAsync(IFormFile file, int userId);
        Task<WarehousePartCatalogDto> CreateAsync(CreateUpdateWarehousePartCatalogDto dto, int userId);
        Task<WarehousePartCatalogDto> UpdateAsync(int id, CreateUpdateWarehousePartCatalogDto dto, int userId);
        Task DeleteAsync(int id);
        Task DeleteAllAsync();
    }
}
