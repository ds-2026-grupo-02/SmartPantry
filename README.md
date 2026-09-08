# SmartPantry

Este repositorio corresponde al Trabajo Práctico Integrador de la asignatura Desarrollo de Software de la carrera de Ingeniería en Sistemas, UTN FRCU.

## Integrantes

- [Dacuez, Abril](https://github.com/AbrilDacuez)
- [Díaz, Emmanuel,](https://github.com/emmanueldiaz707)
- [Farabello, Luciana](https://github.com/farabelloLuciana)
- [Rodríguez, Rodrigo](https://github.com/NTVG-Rodri)

## Propósito

El trabajo práctico final consiste en desarrollar una aplicación web que ayude a las personas a consultar información pública de alimentos envasados, organizar una despensa personal y reconocer posibles incompatibilidades con sus preferencias alimentarias. La aplicación no brinda diagnóstico ni recomendación médica: presenta información proveniente de una fuente externa y la contrasta con preferencias configuradas por el usuario.

### Requisitos previos

* [.NET10.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Node v18 or 20](https://nodejs.org/en)

### Estructura de la solución

Esta es una aplicación monolítica en capas que consta de las siguientes aplicaciones:

* `angular`: Aplicación frontend en Angular.
* `SmartPantry.DbMigrator`: Una aplicación de consola que aplica las migraciones y realiza la carga inicial de datos (seeding). Es útil tanto en entornos de desarrollo como de producción.
* `SmartPantry.HttpApi.Host`: Aplicación API en ASP.NET Core que se utiliza para exponer los servicios web/APIs a los clientes.

