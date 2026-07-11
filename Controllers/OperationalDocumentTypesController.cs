using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;

namespace Pm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OperationalDocumentTypesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // GET: api/OperationalDocumentTypes
        [HttpGet]
        public async Task<IActionResult> GetOperationalDocumentTypes([FromQuery] OperationalDocumentTypeQueryDto query)
        {
            var queryable = _context.OperationalDocumentTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                queryable = queryable.Where(x => x.Name.Contains(query.Search) || (x.Description != null && x.Description.Contains(query.Search)));
            }

            if (query.IsActive.HasValue)
            {
                queryable = queryable.Where(x => x.IsActive == query.IsActive.Value);
            }

            var totalCount = await queryable.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            var items = await queryable
                .OrderBy(x => x.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new OperationalDocumentTypeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            var result = new PagedResultDto<OperationalDocumentTypeDto>(items, query.Page, query.PageSize, totalCount);

            // We must return the raw object so the frontend parses it as { data, meta }
            // The Pm.Helper.ApiResponse.Success wraps it in { data: PagedResultDto, message: null }, which changes the JSON structure.
            // Wait, letterNumberApi.ts expects `response.data` for PagedResult.
            // Let's just return Ok(result) for PagedResult.
            return Ok(result);
        }

        // GET: api/OperationalDocumentTypes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOperationalDocumentType(int id)
        {
            var docType = await _context.OperationalDocumentTypes.FindAsync(id);

            if (docType == null)
            {
                return ApiResponse.NotFound("Document type not found.");
            }

            var result = new OperationalDocumentTypeDto
            {
                Id = docType.Id,
                Name = docType.Name,
                Description = docType.Description,
                IsActive = docType.IsActive,
                CreatedAt = docType.CreatedAt,
                UpdatedAt = docType.UpdatedAt
            };

            return ApiResponse.Success(result);
        }

        // POST: api/OperationalDocumentTypes
        [HttpPost]
        public async Task<IActionResult> CreateOperationalDocumentType(CreateOperationalDocumentTypeDto dto)
        {
            if (await _context.OperationalDocumentTypes.AnyAsync(x => x.Name == dto.Name))
            {
                return ApiResponse.BadRequest("OperationalDocumentType", "Document type name already exists.");
            }

            var docType = new OperationalDocumentType
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            _context.OperationalDocumentTypes.Add(docType);
            await _context.SaveChangesAsync();

            var result = new OperationalDocumentTypeDto
            {
                Id = docType.Id,
                Name = docType.Name,
                Description = docType.Description,
                IsActive = docType.IsActive,
                CreatedAt = docType.CreatedAt
            };

            return ApiResponse.Created(result, "Document type created successfully.");
        }

        // PUT: api/OperationalDocumentTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOperationalDocumentType(int id, UpdateOperationalDocumentTypeDto dto)
        {
            var docType = await _context.OperationalDocumentTypes.FindAsync(id);
            if (docType == null)
            {
                return ApiResponse.NotFound("Document type not found.");
            }

            if (docType.Name != dto.Name && await _context.OperationalDocumentTypes.AnyAsync(x => x.Name == dto.Name))
            {
                return ApiResponse.BadRequest("OperationalDocumentType", "Document type name already exists.");
            }

            docType.Name = dto.Name;
            docType.Description = dto.Description;
            docType.IsActive = dto.IsActive;
            docType.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new OperationalDocumentTypeDto
            {
                Id = docType.Id,
                Name = docType.Name,
                Description = docType.Description,
                IsActive = docType.IsActive,
                CreatedAt = docType.CreatedAt,
                UpdatedAt = docType.UpdatedAt
            };

            return ApiResponse.Success(result, "Document type updated successfully.");
        }

        // DELETE: api/OperationalDocumentTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOperationalDocumentType(int id)
        {
            var docType = await _context.OperationalDocumentTypes.FindAsync(id);
            if (docType == null)
            {
                return ApiResponse.NotFound("Document type not found.");
            }

            // Check if it's used in OperationalDocuments
            bool isUsed = await _context.OperationalDocuments.AnyAsync(x => x.Type == docType.Name);
            if (isUsed)
            {
                return ApiResponse.BadRequest("OperationalDocumentType", "Cannot delete document type because it is being used by existing operational documents.");
            }

            _context.OperationalDocumentTypes.Remove(docType);
            await _context.SaveChangesAsync();

            return ApiResponse.Success(null, "Document type deleted successfully.");
        }
    }
}
