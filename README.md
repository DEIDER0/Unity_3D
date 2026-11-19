# Dungeon Depths (nombre provisional)

Dungeon crawler 3D en Unity 6000.3.21f1 (URP), con arquitectura MVC. Proyecto de la asignatura Tecnologías Inmersivas.

* Plan y decisiones: [`docs/PLAN.md`](docs/PLAN.md)
* Scripts de Blender para regenerar los modelos: `BlenderScripts/`

## Pruebas del Modelo
* **En Unity:** Window → General → Test Runner → EditMode → Run All.
* **Sin Unity** (requiere .NET 8 SDK): compila los mismos archivos del Modelo y corre sus pruebas.

```bash
dotnet test Tools/ModelTests
```

## Metadatos
Después de crear scripts o carpetas nuevas fuera de Unity, corre `python3 Tools/generate_meta.py`. Así todo el equipo comparte los mismos GUID.
