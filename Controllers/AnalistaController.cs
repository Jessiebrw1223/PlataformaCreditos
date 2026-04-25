using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.ViewModels;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public AnalistaController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var solicitudes = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();

        return View(solicitudes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _context.SolicitudesCredito.Include(s => s.Cliente).FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "No se puede procesar una solicitud ya aprobada o rechazada.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.MontoSolicitado > solicitud.Cliente!.IngresosMensuales * 5)
        {
            TempData["Error"] = "No se puede aprobar: el monto excede 5 veces los ingresos mensuales.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        solicitud.MotivoRechazo = null;

        await _context.SaveChangesAsync();
        await InvalidarCacheClienteAsync(solicitud.Cliente!.UsuarioId);

        TempData["Success"] = "Solicitud aprobada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(RechazarSolicitudViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "El motivo de rechazo es obligatorio y debe ser claro.";
            return RedirectToAction(nameof(Index));
        }

        var solicitud = await _context.SolicitudesCredito.Include(s => s.Cliente).FirstOrDefaultAsync(s => s.Id == model.Id);
        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "No se puede procesar una solicitud ya aprobada o rechazada.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = model.MotivoRechazo.Trim();

        await _context.SaveChangesAsync();
        await InvalidarCacheClienteAsync(solicitud.Cliente!.UsuarioId);

        TempData["Success"] = "Solicitud rechazada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task InvalidarCacheClienteAsync(string userId)
    {
        await _cache.SetStringAsync(SolicitudesController.VersionKey(userId), Guid.NewGuid().ToString("N"));
    }
}
