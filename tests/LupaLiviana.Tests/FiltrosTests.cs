using Xunit;

namespace LupaLiviana.Tests;

/// <summary>Pruebas del ciclo de filtros de color.</summary>
public sealed class FiltrosTests
{
    [Fact]
    public void ElCicloRecorreTodosLosFiltrosYVuelveAlPrincipio()
    {
        FiltroColor filtro = FiltroColor.Ninguno;

        filtro = Filtros.Siguiente(filtro);
        Assert.Equal(FiltroColor.Invertir, filtro);

        filtro = Filtros.Siguiente(filtro);
        Assert.Equal(FiltroColor.EscalaDeGrises, filtro);

        filtro = Filtros.Siguiente(filtro);
        Assert.Equal(FiltroColor.AltoContraste, filtro);

        filtro = Filtros.Siguiente(filtro);
        Assert.Equal(FiltroColor.Ninguno, filtro);
    }

    [Fact]
    public void CadaFiltroTieneNombreEnEspanol()
    {
        (FiltroColor Filtro, string Nombre)[] casos =
        [
            (FiltroColor.Ninguno, "Ninguno"),
            (FiltroColor.Invertir, "Invertir"),
            (FiltroColor.EscalaDeGrises, "Escala de grises"),
            (FiltroColor.AltoContraste, "Alto contraste"),
        ];

        foreach ((FiltroColor filtro, string nombre) in casos)
        {
            Assert.Equal(nombre, Filtros.NombreVisible(filtro));
        }
    }
}