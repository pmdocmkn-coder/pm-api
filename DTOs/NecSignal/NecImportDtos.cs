using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Pm.DTOs
{
    public class NecSignalImportRequestDto
    {
        public IFormFile ExcelFile { get; set; } = null!;
    }

    public class NecSignalImportResultDto
    {
        public int TotalRowsProcessed { get; set; }
        public int SuccessfulInserts { get; set; }
        public int FailedRows { get; set; }
        public List<string> Errors { get; set; } = new();
        public string Message { get; set; } = null!;
    }
}