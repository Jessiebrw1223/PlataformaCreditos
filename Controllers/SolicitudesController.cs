using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.ViewModels;

namespace PlataformaCreditos.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public SolicitudesController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IActionResult> MisSolicitudes(EstadoSolicitud? estado, decimal? montoMin, decimal? montoMax, DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (montoMin < 0 || montoMax < 0)
            ModelState.AddModelError(string.Empty, "No se aceptan montos negativos.");

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio.Value.Date > fechaFin.Value.Date)
            ModelState.AddModelError(string.Empty, "El rango de fechas es inválido: la fecha inicio no puede ser mayor a la fecha fin.");

        var model = new SolicitudFiltroViewModel
        {
            Estado = estado,
            MontoMin = montoMin,
            MontoMax = montoMax,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };

        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var version = await GetCacheVersionAsync(userId);
        var cacheKey = BuildCacheKey(userId, version, estado, montoMin, montoMax, fechaInicio, fechaFin);
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cached))
        {
            model.Solicitudes = JsonSerializer.Deserialize<List<SolicitudCredito>>(cached) ?? new();
            ViewBag.CacheStatus = "Listado recuperado desde Redis/cache.";
            return View(model);
        }

        var query = _context.SolicitudesCredito
            .AsNoTracking()
            .Include(s => s.Cliente)
            .Where(s => s.Cliente!.UsuarioId == userId);

        if (estado.HasValue) query = query.Where(s => s.Estado == estado.Value);
        if (montoMin.HasValue) query = query.Where(s => s.MontoSolicitado >= montoMin.Value);
        if (montoMax.HasValue) query = query.Where(s => s.MontoSolicitado <= montoMax.Value);
        if (fechaInicio.HasValue) query = query.Where(s => s.FechaSolicitud.Date >= fechaInicio.Value.Date);
        if (fechaFin.HasValue) query = query.Where(s => s.FechaSolicitud.Date <= fechaFin.Value.Date);

        model.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(model.Solicitudes, new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        }), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        });

        ViewBag.CacheStatus = "Listado generado desde base de datos y guardado 60s en cache.";
        return View(model);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var solicitud = await _context.SolicitudesCredito
            .AsNoTracking()
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id && s.Cliente!.UsuarioId == userId);

        if (solicitud == null)
            return NotFound();

        HttpContext.Session.SetInt32("UltimaSolicitudId", solicitud.Id);
        HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString("N2"));

        return View(solicitud);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new CrearSolicitudViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearSolicitudViewModel model)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Challenge();

        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (cliente == null)
        {
            ModelState.AddModelError(string.Empty, "No existe un cliente asociado al usuario autenticado.");
            return View(model);
        }

        if (!cliente.Activo)
        {
            ModelState.AddModelError(string.Empty, "El cliente no está activo. No puede registrar solicitudes.");
            return View(model);
        }

        var existePendiente = await _context.SolicitudesCredito.AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);
        if (existePendiente)
        {
            ModelState.AddModelError(string.Empty, "Ya existe una solicitud pendiente para este cliente.");
            return View(model);
        }

        if (model.MontoSolicitado > cliente.IngresosMensuales * 10)
        {
            ModelState.AddModelError(string.Empty, "El monto solicitado no puede superar 10 veces los ingresos mensuales.");
            return View(model);
        }

        var solicitud = new SolicitudCredito
        {
            ClienteId = cliente.Id,
            MontoSolicitado = model.MontoSolicitado,
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoSolicitud.Pendiente
        };

        _context.SolicitudesCredito.Add(solicitud);
        await _context.SaveChangesAsync();

        await InvalidarCacheUsuarioAsync(userId);

        TempData["Success"] = "Solicitud registrada correctamente en estado Pendiente.";
        return RedirectToAction(nameof(MisSolicitudes));
    }

    public static string VersionKey(string userId) => $"solicitudes:{userId}:version";

    public static string BuildCacheKey(string userId, string version, EstadoSolicitud? estado, decimal? montoMin, decimal? montoMax, DateTime? fechaInicio, DateTime? fechaFin)
    {
        return $"solicitudes:{userId}:{version}:{estado}:{montoMin}:{montoMax}:{fechaInicio:yyyyMMdd}:{fechaFin:yyyyMMdd}";
    }

    private async Task<string> GetCacheVersionAsync(string userId)
    {
        var version = await _cache.GetStringAsync(VersionKey(userId));
        if (!string.IsNullOrWhiteSpace(version)) return version;

        version = Guid.NewGuid().ToString("N");
        await _cache.SetStringAsync(VersionKey(userId), version);
        return version;
    }

    private async Task InvalidarCacheUsuarioAsync(string userId)
    {
        await _cache.SetStringAsync(VersionKey(userId), Guid.NewGuid().ToString("N"));
    }
}
