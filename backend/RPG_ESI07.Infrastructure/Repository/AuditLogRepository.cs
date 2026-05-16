using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Repository;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context) => _context = context;

    public async Task<List<AuditLog>> GetAllAsync() =>
        await _context.AuditLogs.OrderBy(x => x.Id).ToListAsync();

    public async Task<AuditLog?> GetByIdAsync(int id) =>
        await _context.AuditLogs.FindAsync(id);

    public async Task AddAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.AuditLogs.FindAsync(id);
        if (entity != null)
        {
            _context.AuditLogs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}