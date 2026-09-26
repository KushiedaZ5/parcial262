# Plataforma de Incidencias - BiciShared

Plataforma web interna desarrollada con **ASP.NET Core MVC (.NET 10)** para la gestión y control de incidencias en estaciones de bicicletas compartidas, integrando **Identity**, **Entity Framework Core (SQLite)**, **Algolia Search**, **Redis Cache** y **WebSockets en tiempo real con PieHost**.

## Arquitectura y Servicios
- **Framework Web:** ASP.NET Core MVC (.NET 10)
- **Base de Datos & ORM:** SQLite + Entity Framework Core 10
- **Seguridad e Identidad:** ASP.NET Core Identity (Roles: `Supervisor`, `Operador`)
- **Búsqueda Full-Text:** Algolia Search API
- **Caché en Memoria Distribuida:** Redis (StackExchange.Redis)
- **Tiempo Real:** WebSockets con PieHost
- **Contenedorización & Cloud:** Docker + Render.com Web Service

## Cuentas de Acceso (Seed Data)
- **Supervisor:** `supervisor@bicicletas.com` / `Password123!`
- **Operador:** `operador@bicicletas.com` / `Password123!`
