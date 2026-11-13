namespace LupaLiviana;

/// <summary>
/// Mantiene el conjunto de atajos globales registrados y aplica cambios en
/// caliente. Si una combinación nueva no se puede registrar (porque otro
/// programa la usa), restaura las combinaciones anteriores.
///
/// Las llamadas reales a RegisterHotKey/UnregisterHotKey se reciben como
/// delegados, lo que permite probar la lógica sin Windows.
/// </summary>
internal sealed class RegistroAtajos
{
    private readonly Func<int, uint, uint, bool> _registrar;
    private readonly Action<int> _liberar;
    private readonly Dictionary<int, Atajo> _deseados = [];
    private readonly HashSet<int> _registrados = [];

    public RegistroAtajos(Func<int, uint, uint, bool> registrar, Action<int> liberar)
    {
        _registrar = registrar;
        _liberar = liberar;
    }

    /// <summary>Identificadores registrados en este momento.</summary>
    public IReadOnlyCollection<int> Registrados => _registrados;

    /// <summary>Atajos deseados en este momento (incluye solo los registrados).</summary>
    public IReadOnlyDictionary<int, Atajo> Deseados => _deseados;

    /// <summary>
    /// Deja registrados exactamente los atajos indicados. Devuelve los
    /// identificadores que no se pudieron registrar; en ese caso deja todo
    /// como estaba antes de la llamada.
    /// </summary>
    public IReadOnlyList<int> Aplicar(IReadOnlyDictionary<int, Atajo> deseados)
    {
        var anteriores = new Dictionary<int, Atajo>(_deseados);
        var fallidos = new List<int>();

        foreach (int identificador in _registrados.ToArray())
        {
            bool sigueIgual = deseados.TryGetValue(identificador, out Atajo nuevo) && nuevo == _deseados[identificador];
            if (!sigueIgual)
            {
                _liberar(identificador);
                _registrados.Remove(identificador);
                _deseados.Remove(identificador);
            }
        }

        foreach (KeyValuePair<int, Atajo> par in deseados)
        {
            if (!par.Value.EsValido || _registrados.Contains(par.Key))
            {
                continue;
            }

            if (_registrar(par.Key, par.Value.Modificadores, (uint)par.Value.Tecla))
            {
                _registrados.Add(par.Key);
                _deseados[par.Key] = par.Value;
            }
            else
            {
                fallidos.Add(par.Key);
            }
        }

        if (fallidos.Count == 0)
        {
            return fallidos;
        }

        // No se pudo aplicar todo: se vuelve al conjunto anterior.
        foreach (int identificador in _registrados.ToArray())
        {
            _liberar(identificador);
        }

        _registrados.Clear();
        _deseados.Clear();

        foreach (KeyValuePair<int, Atajo> par in anteriores)
        {
            if (par.Value.EsValido && _registrar(par.Key, par.Value.Modificadores, (uint)par.Value.Tecla))
            {
                _registrados.Add(par.Key);
                _deseados[par.Key] = par.Value;
            }
        }

        return fallidos;
    }

    /// <summary>Libera todos los atajos registrados.</summary>
    public void LiberarTodo()
    {
        foreach (int identificador in _registrados.ToArray())
        {
            _liberar(identificador);
        }

        _registrados.Clear();
        _deseados.Clear();
    }
}