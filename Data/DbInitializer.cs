using Microsoft.AspNetCore.Identity;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        // 1. Crear Roles
        string[] roles = { "Supervisor", "Operador" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Crear Usuarios de prueba
        var supervisorEmail = "supervisor@bicicletas.com";
        var supervisorUser = await userManager.FindByEmailAsync(supervisorEmail);
        if (supervisorUser == null)
        {
            supervisorUser = new IdentityUser
            {
                UserName = supervisorEmail,
                Email = supervisorEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(supervisorUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
            }
        }

        var operadorEmail = "operador@bicicletas.com";
        var operadorUser = await userManager.FindByEmailAsync(operadorEmail);
        if (operadorUser == null)
        {
            operadorUser = new IdentityUser
            {
                UserName = operadorEmail,
                Email = operadorEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(operadorUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(operadorUser, "Operador");
            }
        }

        // 3. Crear Incidencias de prueba
        if (!context.Incidencias.Any())
        {
            var incidencias = new List<Incidencia>
            {
                new Incidencia
                {
                    Estacion = "Estación San Isidro",
                    Descripcion = "Freno trasero bloqueado en módulo 04",
                    Prioridad = "Alta",
                    Estado = "Abierta",
                    FechaRegistro = DateTime.UtcNow.AddHours(-5)
                },
                new Incidencia
                {
                    Estacion = "Estación Miraflores",
                    Descripcion = "Cadena rota en bicicleta B-201",
                    Prioridad = "Media",
                    Estado = "Abierta",
                    FechaRegistro = DateTime.UtcNow.AddHours(-4)
                },
                new Incidencia
                {
                    Estacion = "Estación Central",
                    Descripcion = "Pantalla de cobro rota e inoperativa",
                    Prioridad = "Alta",
                    Estado = "Abierta",
                    FechaRegistro = DateTime.UtcNow.AddHours(-3)
                },
                new Incidencia
                {
                    Estacion = "Estación Barranco",
                    Descripcion = "Asiento suelto y desgastado en módulo 02",
                    Prioridad = "Baja",
                    Estado = "Abierta",
                    FechaRegistro = DateTime.UtcNow.AddHours(-2)
                },
                new Incidencia
                {
                    Estacion = "Estación Surco",
                    Descripcion = "Neumático delantero desinflado en B-105",
                    Prioridad = "Media",
                    Estado = "Abierta",
                    FechaRegistro = DateTime.UtcNow.AddHours(-1)
                },
                new Incidencia
                {
                    Estacion = "Estación San Borja",
                    Descripcion = "Sensor de anclaje no reconoce bicicleta",
                    Prioridad = "Alta",
                    Estado = "Abierta",
                    FechaRegistro = DateTime.UtcNow.AddMinutes(-30)
                },
                new Incidencia
                {
                    Estacion = "Estación La Molina",
                    Descripcion = "Pedal desprendido en bicicleta B-330",
                    Prioridad = "Baja",
                    Estado = "Cerrada",
                    FechaRegistro = DateTime.UtcNow.AddDays(-1),
                    FechaCierre = DateTime.UtcNow.AddHours(-10),
                    CerradoPor = "supervisor@bicicletas.com"
                },
                new Incidencia
                {
                    Estacion = "Estación Magdalena",
                    Descripcion = "Manillar desviado por impacto",
                    Prioridad = "Media",
                    Estado = "Cerrada",
                    FechaRegistro = DateTime.UtcNow.AddDays(-2),
                    FechaCierre = DateTime.UtcNow.AddDays(-1),
                    CerradoPor = "supervisor@bicicletas.com"
                }
            };

            await context.Incidencias.AddRangeAsync(incidencias);
            await context.SaveChangesAsync();
        }
    }
}
