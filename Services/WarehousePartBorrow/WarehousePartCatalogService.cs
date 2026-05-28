using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.WarehousePartBorrow;
using Pm.Models;

namespace Pm.Services.WarehousePartBorrow
{
    public class WarehousePartCatalogService : IWarehousePartCatalogService
    {
        private readonly AppDbContext _context;

        public WarehousePartCatalogService(AppDbContext context) => _context = context;

        public async Task<Pm.DTOs.Common.PagedResultDto<WarehousePartCatalogDto>> GetAllAsync(int page, int pageSize, string? search)
        {
            var query = _context.WarehousePartCatalogs.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(x =>
                    x.PartCode.ToLower().Contains(s) ||
                    x.PartName.ToLower().Contains(s) ||
                    (x.Description != null && x.Description.ToLower().Contains(s)) ||
                    (x.Category != null && x.Category.ToLower().Contains(s)));
            }

            var totalCount = await query.CountAsync();
            var items = await query.OrderBy(x => x.PartName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WarehousePartCatalogDto
                {
                    Id = x.Id,
                    PartCode = x.PartCode,
                    PartName = x.PartName,
                    Category = x.Category,
                    Unit = x.Unit,
                    Description = x.Description
                })
                .ToListAsync();

            return new Pm.DTOs.Common.PagedResultDto<WarehousePartCatalogDto>(items, page, pageSize, totalCount);
        }

        public async Task<List<WarehousePartCatalogDto>> SearchAsync(string? query, int limit = 10)
        {
            var q = (query ?? "").Trim();
            var items = _context.WarehousePartCatalogs.AsNoTracking().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.ToLower();
                items = items.Where(x =>
                    x.PartCode.ToLower().Contains(s) ||
                    x.PartName.ToLower().Contains(s) ||
                    (x.Description != null && x.Description.ToLower().Contains(s)) ||
                    (x.Category != null && x.Category.ToLower().Contains(s)));
            }

            return await items
                .OrderBy(x => x.PartName)
                .Take(limit)
                .Select(x => new WarehousePartCatalogDto
                {
                    Id = x.Id,
                    PartCode = x.PartCode,
                    PartName = x.PartName,
                    Category = x.Category,
                    Unit = x.Unit,
                    Description = x.Description,
                })
                .ToListAsync();
        }

        public async Task<WarehousePartImportResultDto> ImportAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File import wajib dipilih.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var rows = new List<(string OwnerId, string PartCode, string PartName, string? Category, string? Unit, string? Description)>();

            if (ext == ".xlsx" || ext == ".xls")
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                    throw new InvalidOperationException("File Excel tidak mengandung worksheet.");

                var headers = ws.FirstRowUsed()?.CellsUsed().Select(c => c.GetString().Trim().ToLowerInvariant()).ToList() ?? [];
                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var values = Enumerable.Range(1, headers.Count).Select(i => row.Cell(i).GetString().Trim()).ToList();
                    var partCode = FindValue(headers, values, new[] { "partcode", "kode", "kode part", "item code", "materialid", "material id" });
                    var partName = FindValue(headers, values, new[] { "partname", "nama part", "nama barang", "deskripsi", "item name", "nama", "materialname", "material name" });
                    var ownerId = FindValue(headers, values, new[] { "ownerid", "owner", "owner id" });
                    if (string.IsNullOrWhiteSpace(partCode) && string.IsNullOrWhiteSpace(partName)) continue;

                    rows.Add((
                        ownerId ?? "",
                        string.IsNullOrWhiteSpace(partCode) ? "-" : partCode,
                        string.IsNullOrWhiteSpace(partName) ? partCode : partName,
                        FindValue(headers, values, new[] { "category", "kategori", "group" }),
                        FindValue(headers, values, new[] { "unit", "satuan", "uom" }),
                        FindValue(headers, values, new[] { "description", "keterangan", "remark" })
                    ));
                }
            }
            else if (ext == ".csv")
            {
                using var reader = new StreamReader(file.OpenReadStream());
                string? line;
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                    throw new InvalidOperationException("File CSV kosong.");

                var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var values = line.Split(',').Select(v => v.Trim()).ToArray();
                    var partCode = FindValue(headers, values, new[] { "partcode", "kode", "kode part", "item code", "materialid", "material id" });
                    var partName = FindValue(headers, values, new[] { "partname", "nama part", "nama barang", "deskripsi", "item name", "nama", "materialname", "material name" });
                    var ownerId = FindValue(headers, values, new[] { "ownerid", "owner", "owner id" });
                    if (string.IsNullOrWhiteSpace(partCode) && string.IsNullOrWhiteSpace(partName)) continue;

                    rows.Add((
                        ownerId ?? "",
                        string.IsNullOrWhiteSpace(partCode) ? "-" : partCode,
                        string.IsNullOrWhiteSpace(partName) ? partCode : partName,
                        FindValue(headers, values, new[] { "category", "kategori", "group" }),
                        FindValue(headers, values, new[] { "unit", "satuan", "uom" }),
                        FindValue(headers, values, new[] { "description", "keterangan", "remark" })
                    ));
                }
            }
            else
            {
                throw new InvalidOperationException("Format file harus .xlsx, .xls, atau .csv.");
            }

            if (rows.Count == 0)
                throw new InvalidOperationException("Tidak ada data part yang bisa diimpor.");

            var imported = 0;
            var updated = 0;
            foreach (var row in rows)
            {
                var existing = await _context.WarehousePartCatalogs.FirstOrDefaultAsync(x => x.PartCode.ToLower() == row.PartCode.ToLower());
                if (existing == null)
                {
                    _context.WarehousePartCatalogs.Add(new WarehousePartCatalog
                    {
                        OwnerId = string.IsNullOrWhiteSpace(row.OwnerId) ? null : row.OwnerId,
                        PartCode = row.PartCode,
                        PartName = row.PartName,
                        Category = row.Category,
                        Unit = row.Unit,
                        Description = row.Description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    imported++;
                }
                else
                {
                    existing.OwnerId = string.IsNullOrWhiteSpace(row.OwnerId) ? existing.OwnerId : row.OwnerId;
                    existing.PartName = string.IsNullOrWhiteSpace(row.PartName) ? existing.PartName : row.PartName;
                    existing.Category = string.IsNullOrWhiteSpace(row.Category) ? existing.Category : row.Category;
                    existing.Unit = string.IsNullOrWhiteSpace(row.Unit) ? existing.Unit : row.Unit;
                    existing.Description = string.IsNullOrWhiteSpace(row.Description) ? existing.Description : row.Description;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
            }

            await _context.SaveChangesAsync();
            return new WarehousePartImportResultDto
            {
                ImportedCount = imported,
                UpdatedCount = updated,
                Message = $"Import selesai: {imported} baru, {updated} diperbarui."
            };
        }

        private static string? FindValue(IReadOnlyList<string> headers, IReadOnlyList<string> values, string[] candidates)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                var key = headers[i].Replace(" ", string.Empty).Replace("_", string.Empty);
                if (candidates.Any(c => key.Equals(c.Replace(" ", string.Empty).Replace("_", string.Empty), StringComparison.OrdinalIgnoreCase)))
                {
                    if (i < values.Count) return values[i];
                }
            }

            return null;
        }

        public async Task<WarehousePartCatalogDto> CreateAsync(CreateUpdateWarehousePartCatalogDto dto, int userId)
        {
            var part = new Pm.Models.WarehousePartCatalog
            {
                PartCode = dto.PartCode,
                PartName = dto.PartName,
                OwnerId = dto.OwnerId,
                Category = dto.Category,
                Unit = dto.Unit,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.WarehousePartCatalogs.Add(part);
            await _context.SaveChangesAsync();

            return new WarehousePartCatalogDto
            {
                Id = part.Id,
                PartCode = part.PartCode,
                PartName = part.PartName,
                OwnerId = part.OwnerId,
                Category = part.Category,
                Unit = part.Unit,
                Description = part.Description
            };
        }

        public async Task<WarehousePartCatalogDto> UpdateAsync(int id, CreateUpdateWarehousePartCatalogDto dto, int userId)
        {
            var part = await _context.WarehousePartCatalogs.FindAsync(id);
            if (part == null || !part.IsActive)
                throw new KeyNotFoundException("Part tidak ditemukan.");

            part.PartCode = dto.PartCode;
            part.PartName = dto.PartName;
            part.OwnerId = dto.OwnerId;
            part.Category = dto.Category;
            part.Unit = dto.Unit;
            part.Description = dto.Description;
            part.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WarehousePartCatalogDto
            {
                Id = part.Id,
                PartCode = part.PartCode,
                PartName = part.PartName,
                OwnerId = part.OwnerId,
                Category = part.Category,
                Unit = part.Unit,
                Description = part.Description
            };
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.WarehousePartCatalogs.FindAsync(id);
            if (item == null || !item.IsActive)
                throw new KeyNotFoundException("Part tidak ditemukan.");

            item.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllAsync()
        {
            var items = await _context.WarehousePartCatalogs.Where(x => x.IsActive).ToListAsync();
            foreach (var item in items)
            {
                item.IsActive = false;
            }
            await _context.SaveChangesAsync();
        }
    }
}
