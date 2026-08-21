## Issue asociado

Closes #1 

## Qué se implementó

- Se incorporó y adaptó la plantilla provista por la cátedra.
- Se agregó gitignore.

## Cómo se probó

- [x] Verificación manual o documental
- [x] Job `verify` de GitHub Actions en verde
- Pruebas automatizadas (No aplica)

## Evidencia

Pasos de ejecución: 

1. Crear rama `feature/configuracion-inicial` .
2. Extraer archivos de la plantilla provista por la cátedra y copiarlos en la raíz del repositorio.
3. Eliminar archivos `desktop.ini`.
4. Reemplazar marcadores  `<organizacion>` y `<repositorio>` en `CODEOWNERS` y `CONTRIBUTING.md` por datos reales.
5. Reemplazar marcadores `<Nombre del proyecto>` y `<Nombre y usuario GitHub>` en `README.md` por datos reales.
6. Agregar archivo `.gitignore` utilizando [VisualStudio.gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore).

## Decisiones o dudas para la revisión

- Las ramas `stg`y `prod` no coinciden con `main` (error mío). ¿Qué nos conviene hacer?
