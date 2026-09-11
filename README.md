# SmartPantry

Este repositorio corresponde al Trabajo Práctico Integrador de la asignatura Desarrollo de Software de la carrera de Ingeniería en Sistemas, UTN FRCU.

## Integrantes

- [Dacuez, Abril](https://github.com/AbrilDacuez)
- [Díaz, Emmanuel,](https://github.com/emmanueldiaz707)
- [Farabello, Luciana](https://github.com/farabelloLuciana)
- [Rodríguez, Rodrigo](https://github.com/NTVG-Rodri)

## Propósito

El trabajo práctico final consiste en desarrollar una aplicación web que ayude a las personas a consultar información pública de alimentos envasados, organizar una despensa personal y reconocer posibles incompatibilidades con sus preferencias alimentarias. La aplicación no brinda diagnóstico ni recomendación médica: presenta información proveniente de una fuente externa y la contrasta con preferencias configuradas por el usuario.

## Cómo ejecutar
### Requisitos previos

- Visual Studio 2022 o 2026 con Desarrollo de ASP.NET y web
- Node.js 24.15.0 o superior
- Yarn 1.22.x
- SQL Server Developer o Express
- SQL Server Management Studio (SSMS)
- ABP Studio
- Git

## Configuración Local

La cadena de conexión base `ConnectionStrings:Default` está configurada en los siguientes archivos `appsettings.json`:

1. `src/SmartPantry.DbMigrator/appsettings.json`
2. `src/SmartPantry.HttpApi.Host/appsettings.json`

### Cadena de conexión estándar (Instancia Local)

Por defecto, el proyecto utiliza la instancia local de SQL Server Express:

```text
Server=localhost\\SQLEXPRESS;Database=SmartPantry;Trusted_Connection=True;TrustServerCertificate=True;

### Puesta en marcha

Para compilar y ejecutar la aplicación de forma local, siga estos pasos:

1. Backend y Base de Datos (.NET)
Abrir una consola en la raíz de la solución y restaurar las dependencias del backend:

dotnet restore SmartPantry.slnx

Instalar las librerías web requeridas por ABP Framework:

abp install-libs

Ejecutar la aplicación de migración para crear la base de datos SmartPantry y cargar los datos iniciales (seeding):

dotnet run --project src/SmartPantry.DbMigrator/SmartPantry.DbMigrator.csproj

Iniciar el host de la API Web:

dotnet run --project src/SmartPantry.HttpApi.Host/SmartPantry.HttpApi.Host.csproj

URL de la API / Swagger UI: https://localhost:44300

2. Frontend (Angular)
Navegar al directorio del cliente web:

cd angular

Instalar los paquetes de Node:

yarn install

Iniciar el servidor de desarrollo de Angular:

yarn start

URL de la Aplicación Web: http://localhost:4200`

### Verificación 

Comandos ejecutados y verificados para asegurar la calidad antes del despliegue en la integración continua:

Backend (.NET)
Compilar la solución completa:
dotnet build SmartPantry.slnx --configuration Release

Ejecutar suite de pruebas unitarias e integración:
dotnet test SmartPantry.slnx --configuration Release --no-build

Frontend (Angular)
Compilar el cliente Angular:
yarn build

Ejecutar pruebas del cliente web:
yarn test --watch=false --passWithNoTests

### Estructura de la solución

Esta es una aplicación monolítica en capas que consta de las siguientes aplicaciones:

* `angular`: Aplicación frontend en Angular.
* `SmartPantry.DbMigrator`: Una aplicación de consola que aplica las migraciones y realiza la carga inicial de datos (seeding). Es útil tanto en entornos de desarrollo como de producción.
* `SmartPantry.HttpApi.Host`: Aplicación API en ASP.NET Core que se utiliza para exponer los servicios web/APIs a los clientes.

