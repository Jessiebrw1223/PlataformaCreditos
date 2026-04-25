using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        //await context.Database.MigrateAsync();

        const string analistaRole = "Analista";
        if (!await roleManager.RoleExistsAsync(analistaRole))
            await roleManager.CreateAsync(new IdentityRole(analistaRole));

        var analista = await EnsureUserAsync(userManager, "analista@demo.com", "Analista123*");
        if (!await userManager.IsInRoleAsync(analista, analistaRole))
            await userManager.AddToRoleAsync(analista, analistaRole);

        var clienteUser = await EnsureUserAsync(userManager, "cliente@demo.com", "Cliente123*");
        var clienteDosUser = await EnsureUserAsync(userManager, "cliente2@demo.com", "Cliente123*");

        await EnsureClienteAsync(context, clienteUser.Id, 2500, true);
        await EnsureClienteAsync(context, clienteDosUser.Id, 5000, true);

        if (!await context.SolicitudesCredito.AnyAsync())
        {
            var cliente1 = await context.Clientes.FirstAsync(c => c.UsuarioId == clienteUser.Id);
            var cliente2 = await context.Clientes.FirstAsync(c => c.UsuarioId == clienteDosUser.Id);

            context.SolicitudesCredito.AddRange(
                new SolicitudCredito
                {
                    ClienteId = cliente1.Id,
                    MontoSolicitado = 6000,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-1),
                    Estado = EstadoSolicitud.Pendiente
                },
                new SolicitudCredito
                {
                    ClienteId = cliente2.Id,
                    MontoSolicitado = 12000,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                    Estado = EstadoSolicitud.Aprobado
                });

            await context.SaveChangesAsync();
        }
    }

    private static async Task<IdentityUser> EnsureUserAsync(UserManager<IdentityUser> userManager, string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;

        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return user;
    }

    private static async Task EnsureClienteAsync(ApplicationDbContext context, string usuarioId, decimal ingresos, bool activo)
    {
        if (await context.Clientes.AnyAsync(c => c.UsuarioId == usuarioId)) return;

        context.Clientes.Add(new Cliente
        {
            UsuarioId = usuarioId,
            IngresosMensuales = ingresos,
            Activo = activo
        });

        await context.SaveChangesAsync();
    }
}
