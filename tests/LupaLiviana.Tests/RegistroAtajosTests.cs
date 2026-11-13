using System.Windows.Forms;
using Xunit;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana.Tests;

/// <summary>Pruebas del registro de atajos con RegisterHotKey simulado.</summary>
public sealed class RegistroAtajosTests
{
    private sealed class Entorno
    {
        public Dictionary<int, Atajo> Activos { get; } = [];
        public HashSet<(uint Modificadores, uint Tecla)> Ocupados { get; } = [];
        public List<int> Liberados { get; } = [];
        public RegistroAtajos Registro { get; }

        public Entorno()
        {
            Registro = new RegistroAtajos(Registrar, Liberar);
        }

        private bool Registrar(int identificador, uint modificadores, uint tecla)
        {
            var combinacion = (modificadores, tecla);
            if (Ocupados.Contains(combinacion) ||
                Activos.Values.Any(atajo => atajo.Modificadores == modificadores && (uint)atajo.Tecla == tecla))
            {
                return false;
            }

            Activos[identificador] = new Atajo(modificadores, (Keys)tecla);
            return true;
        }

        private void Liberar(int identificador)
        {
            Activos.Remove(identificador);
            Liberados.Add(identificador);
        }
    }

    private static Dictionary<int, Atajo> Deseados(params (int Id, Atajo Atajo)[] pares)
    {
        return pares.ToDictionary(par => par.Id, par => par.Atajo);
    }

    [Fact]
    public void RegistraLosAtajosIndicados()
    {
        var entorno = new Entorno();
        Atajo subir = new(MOD_CONTROL | MOD_ALT, Keys.Up);
        Atajo bajar = new(MOD_CONTROL | MOD_ALT, Keys.Down);

        IReadOnlyList<int> fallidos = entorno.Registro.Aplicar(Deseados((1, subir), (2, bajar)));

        Assert.Empty(fallidos);
        Assert.Equal(2, entorno.Activos.Count);
        Assert.Equal(subir, entorno.Activos[1]);
        Assert.Equal(bajar, entorno.Activos[2]);
        Assert.Equal(new[] { 1, 2 }, entorno.Registro.Registrados.Order());
    }

    [Fact]
    public void LosAtajosQueYaNoSeDeseanSeLiberan()
    {
        var entorno = new Entorno();
        entorno.Registro.Aplicar(Deseados(
            (1, new Atajo(MOD_CONTROL | MOD_ALT, Keys.Up)),
            (2, new Atajo(MOD_CONTROL | MOD_ALT, Keys.Down))));

        entorno.Registro.Aplicar(Deseados((1, new Atajo(MOD_CONTROL | MOD_ALT, Keys.Up))));

        Assert.Equal(new[] { 1 }, entorno.Registro.Registrados);
        Assert.DoesNotContain(2, entorno.Activos.Keys);
        Assert.Contains(2, entorno.Liberados);
    }

    [Fact]
    public void UnAtajoVacioNoSeRegistra()
    {
        var entorno = new Entorno();

        IReadOnlyList<int> fallidos = entorno.Registro.Aplicar(Deseados((1, Atajo.Ninguno)));

        Assert.Empty(fallidos);
        Assert.Empty(entorno.Activos);
    }

    [Fact]
    public void CambiarUnAtajoLiberaElAnteriorYRegistraElNuevo()
    {
        var entorno = new Entorno();
        entorno.Registro.Aplicar(Deseados((1, new Atajo(MOD_CONTROL | MOD_ALT, Keys.Up))));

        entorno.Registro.Aplicar(Deseados((1, new Atajo(MOD_CONTROL | MOD_ALT, Keys.Down))));

        Assert.Equal(new Atajo(MOD_CONTROL | MOD_ALT, Keys.Down), entorno.Activos[1]);
        Assert.Contains(1, entorno.Liberados);
    }

    [Fact]
    public void SiUnaCombinacionEstaOcupadaSeRestauraElConjuntoAnterior()
    {
        var entorno = new Entorno();
        Atajo subir = new(MOD_CONTROL | MOD_ALT, Keys.Up);
        entorno.Registro.Aplicar(Deseados((1, subir)));

        Atajo ocupado = new(MOD_CONTROL | MOD_SHIFT, Keys.F12);
        entorno.Ocupados.Add(((uint)MOD_CONTROL | MOD_SHIFT, (uint)Keys.F12));

        IReadOnlyList<int> fallidos = entorno.Registro.Aplicar(Deseados((1, subir), (2, ocupado)));

        Assert.Equal(new[] { 2 }, fallidos);
        Assert.Equal(subir, entorno.Activos[1]);
        Assert.DoesNotContain(2, entorno.Activos.Keys);
        Assert.Equal(new[] { 1 }, entorno.Registro.Registrados);
    }

    [Fact]
    public void LiberarTodoDejaElRegistroVacio()
    {
        var entorno = new Entorno();
        entorno.Registro.Aplicar(Deseados(
            (1, new Atajo(MOD_CONTROL | MOD_ALT, Keys.Up)),
            (2, new Atajo(MOD_ALT, Keys.F8))));

        entorno.Registro.LiberarTodo();

        Assert.Empty(entorno.Activos);
        Assert.Empty(entorno.Registro.Registrados);
    }
}