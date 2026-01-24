using Pm.DTOs;

namespace Pm.Services
{
    public interface IDocumentTypeService
    {
        Task<DocumentTypeResponseDto> CreateAsync(DocumentTypeCreateDto dto, int userId);
        Task<DocumentTypeResponseDto> UpdateAsync(int id, DocumentTypeUpdateDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task<DocumentTypeResponseDto?> GetByIdAsync(int id);
        Task<List<DocumentTypeListDto>> GetAllAsync(bool activeOnly = true);
    }
}
