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

### Configuración local

indicar los dos appsettings.json que contienen ConnectionStrings:Default y mostrar
la cadena local utilizada para LocalDB o SQL Server Express. Si se usa una conexión con credenciales,
documentar sólo el mecanismo User Secrets o ConnectionStrings__Default, sin publicar su valor.

### Puesta en marcha

Restaurar, ejecutar abp install-libs, instalar paquetes Angular, iniciar DbMigrator,
HttpApi.Host y Angular, e informar las URLs locales reales del grupo.

### Verificación 

Comandos de build y test de .NET y Angular que el grupo ejecutó correctamente.

### Estructura de la solución

Esta es una aplicación monolítica en capas que consta de las siguientes aplicaciones:

* `angular`: Aplicación frontend en Angular.
* `SmartPantry.DbMigrator`: Una aplicación de consola que aplica las migraciones y realiza la carga inicial de datos (seeding). Es útil tanto en entornos de desarrollo como de producción.
* `SmartPantry.HttpApi.Host`: Aplicación API en ASP.NET Core que se utiliza para exponer los servicios web/APIs a los clientes.

