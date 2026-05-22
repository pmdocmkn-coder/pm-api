using Microsoft.EntityFrameworkCore;
using Pm.Data;

namespace Pm.Helper
{
    public static class DocumentNumberHelper
    {
        public static async Task<string> NextRadioRepairJobNumberAsync(AppDbContext context)
        {
            var prefix = $"RRJ-{DateTime.UtcNow:yyyyMM}-";
            var last = await context.RadioRepairJobs
                .Where(j => j.JobNumber.StartsWith(prefix))
                .OrderByDescending(j => j.JobNumber)
                .Select(j => j.JobNumber)
                .FirstOrDefaultAsync();
            return prefix + NextSequence(last, prefix);
        }

        public static async Task<string> NextHandoverNumberAsync(AppDbContext context)
        {
            var prefix = $"STR-{DateTime.UtcNow:yyyyMM}-";
            var last = await context.RadioHandovers
                .Where(h => h.HandoverNumber.StartsWith(prefix))
                .OrderByDescending(h => h.HandoverNumber)
                .Select(h => h.HandoverNumber)
                .FirstOrDefaultAsync();
            return prefix + NextSequence(last, prefix);
        }

        public static async Task<string> NextBorrowNumberAsync(AppDbContext context)
        {
            var prefix = $"WBP-{DateTime.UtcNow:yyyyMM}-";
            var last = await context.WarehousePartBorrows
                .Where(b => b.BorrowNumber.StartsWith(prefix))
                .OrderByDescending(b => b.BorrowNumber)
                .Select(b => b.BorrowNumber)
                .FirstOrDefaultAsync();
            return prefix + NextSequence(last, prefix);
        }

        private static string NextSequence(string? last, string prefix)
        {
            if (string.IsNullOrEmpty(last)) return "001";
            var seqPart = last[prefix.Length..];
            if (int.TryParse(seqPart, out var n)) return (n + 1).ToString("D3");
            return "001";
        }
    }
}
