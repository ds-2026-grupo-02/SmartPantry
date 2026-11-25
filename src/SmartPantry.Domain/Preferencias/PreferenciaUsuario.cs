using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Values;

namespace SmartPantry.Preferencias;

public class PreferenciaUsuario : AggregateRoot<Guid>
{
    public Guid UsuarioId { get; private set; }
    public bool MostrarAlertas { get; private set; }

    private readonly List<AlergenoVigilar> _alergenos = new();
    public IReadOnlyCollection<AlergenoVigilar> Alergenos => _alergenos.AsReadOnly();

    private readonly List<IngredienteVigilar> _ingredientes = new();
    public IReadOnlyCollection<IngredienteVigilar> Ingredientes => _ingredientes.AsReadOnly();

    private PreferenciaUsuario() { }

    public PreferenciaUsuario(Guid id, Guid usuarioId, bool mostrarAlertas = true)
        : base(id)
    {
        UsuarioId = usuarioId;
        MostrarAlertas = mostrarAlertas;
    }

    public void Actualizar(IEnumerable<string> alergenos, IEnumerable<string> ingredientes, bool mostrarAlertas)
    {
        // 1. Construir y validar las dos listas nuevas antes de modificar cualquier estado
        var nuevosAlergenos = ConstruirYValidarLista(alergenos, s => new AlergenoVigilar(s));
        var nuevosIngredientes = ConstruirYValidarLista(ingredientes, s => new IngredienteVigilar(s));

        // 2. Si ambas listas son válidas, reemplazar el estado del agregado
        _alergenos.Clear();
        _alergenos.AddRange(nuevosAlergenos);

        _ingredientes.Clear();
        _ingredientes.AddRange(nuevosIngredientes);

        MostrarAlertas = mostrarAlertas;
    }

    private static List<T> ConstruirYValidarLista<T>(IEnumerable<string>? items, Func<string, T> factory)
        where T : ValueObject
    {
        if (items == null)
        {
            return new List<T>();
        }

        var lista = new List<T>();

        foreach (var item in items)
        {
            var vo = factory(item);

            if (lista.Any(x => x.ValueEquals(vo)))
            {
                var nombreRecortado = item?.Trim() ?? string.Empty;
                throw new BusinessException(SmartPantryDomainErrorCodes.PreferenciaDuplicado)
                    .WithData("Nombre", nombreRecortado);
            }

            lista.Add(vo);

            if (lista.Count > PreferenciaUsuarioConsts.MaxItemsPorLista)
            {
                throw new BusinessException(SmartPantryDomainErrorCodes.PreferenciaDemasiadosItems)
                    .WithData("Maximo", PreferenciaUsuarioConsts.MaxItemsPorLista);
            }
        }

        return lista;
    }
}
