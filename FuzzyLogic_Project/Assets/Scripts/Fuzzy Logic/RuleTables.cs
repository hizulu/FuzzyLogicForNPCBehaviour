using UnityEngine;

public enum TagDistancia { MuyCerca = 0, Cerca = 1, Media = 2, Lejos = 3, MuyLejos = 4 }
public enum TagMiedo { Relajado = 0, Cauto = 1, Alerta = 2, Asustado = 3, Panico = 4 }
public enum TagCuriosidad { Nula = 0, Baja = 1, Media = 2, Alta = 3, Extrema = 4 }
public enum Accion { HuidaRapida = 0, RetiradaLenta = 1, Idle = 2, AproxLenta = 3, AproxRapida = 4 }

public static class RuleTables
{
    private static int[,,] baseReglas;
    private static bool estaInicializado = false;

    public static int ObtenerAccion(int curiosidad, int distancia, int miedo)
    {
        if (!estaInicializado)
        {
            InicializarReglas();
        }
        return baseReglas[curiosidad, distancia, miedo];
    }

    private static void InicializarReglas()
    {
        baseReglas = new int[5, 5, 5];

        //Por defecto se inicia en Idle
        for (int c = 0; c < 5; c++)
            for (int d = 0; d < 5; d++)
                for (int m = 0; m < 5; m++)
                    baseReglas[c, d, m] = (int)Accion.Idle;

        //TODO: hay que comprobar que las reglas funcionan bien

        //Formato: baseReglas[Curiosidad, Distancia, Miedo]
        #region CURIOSIDAD NULA
        // Muy Cerca
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyCerca, (int)TagMiedo.Relajado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyCerca, (int)TagMiedo.Cauto] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyCerca, (int)TagMiedo.Alerta] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyCerca, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyCerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Cerca
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Cerca, (int)TagMiedo.Relajado] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Cerca, (int)TagMiedo.Cauto] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Cerca, (int)TagMiedo.Alerta] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Cerca, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Cerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Media Distancia
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Media, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Media, (int)TagMiedo.Cauto] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Media, (int)TagMiedo.Alerta] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Media, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Media, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Lejos
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Lejos, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Lejos, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Lejos, (int)TagMiedo.Alerta] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Lejos, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.Lejos, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Muy Lejos
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyLejos, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyLejos, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyLejos, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyLejos, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Nula, (int)TagDistancia.MuyLejos, (int)TagMiedo.Panico] = (int)Accion.Idle;
        #endregion

        #region CURIOSIDAD BAJA
        // Muy Cerca
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyCerca, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyCerca, (int)TagMiedo.Cauto] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyCerca, (int)TagMiedo.Alerta] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyCerca, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyCerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Cerca
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Cerca, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Cerca, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Cerca, (int)TagMiedo.Alerta] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Cerca, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Cerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Media Distancia
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Media, (int)TagMiedo.Relajado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Media, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Media, (int)TagMiedo.Alerta] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Media, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Media, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Lejos
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Lejos, (int)TagMiedo.Relajado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Lejos, (int)TagMiedo.Cauto] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Lejos, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Lejos, (int)TagMiedo.Asustado] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.Lejos, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Muy Lejos
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyLejos, (int)TagMiedo.Relajado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyLejos, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyLejos, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyLejos, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Baja, (int)TagDistancia.MuyLejos, (int)TagMiedo.Panico] = (int)Accion.Idle;
        #endregion

        #region CURIOSIDAD MEDIA
        // Muy Cerca
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyCerca, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyCerca, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyCerca, (int)TagMiedo.Alerta] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyCerca, (int)TagMiedo.Asustado] = (int)Accion.HuidaRapida;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyCerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Cerca
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Cerca, (int)TagMiedo.Relajado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Cerca, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Cerca, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Cerca, (int)TagMiedo.Asustado] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Cerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Media Distancia
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Media, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Media, (int)TagMiedo.Cauto] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Media, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Media, (int)TagMiedo.Asustado] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Media, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Lejos
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Lejos, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Lejos, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Lejos, (int)TagMiedo.Alerta] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Lejos, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.Lejos, (int)TagMiedo.Panico] = (int)Accion.RetiradaLenta;

        // Muy Lejos
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyLejos, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyLejos, (int)TagMiedo.Cauto] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyLejos, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyLejos, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Media, (int)TagDistancia.MuyLejos, (int)TagMiedo.Panico] = (int)Accion.Idle;
        #endregion

        #region CURIOSIDAD ALTA
        // Muy Cerca
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyCerca, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyCerca, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyCerca, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyCerca, (int)TagMiedo.Asustado] = (int)Accion.RetiradaLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyCerca, (int)TagMiedo.Panico] = (int)Accion.HuidaRapida;

        // Cerca
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Cerca, (int)TagMiedo.Relajado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Cerca, (int)TagMiedo.Cauto] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Cerca, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Cerca, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Cerca, (int)TagMiedo.Panico] = (int)Accion.RetiradaLenta;

        // Media Distancia
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Media, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Media, (int)TagMiedo.Cauto] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Media, (int)TagMiedo.Alerta] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Media, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Media, (int)TagMiedo.Panico] = (int)Accion.RetiradaLenta;

        // Lejos
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Lejos, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Lejos, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Lejos, (int)TagMiedo.Alerta] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Lejos, (int)TagMiedo.Asustado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.Lejos, (int)TagMiedo.Panico] = (int)Accion.Idle;

        // Muy Lejos
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyLejos, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyLejos, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyLejos, (int)TagMiedo.Alerta] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyLejos, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Alta, (int)TagDistancia.MuyLejos, (int)TagMiedo.Panico] = (int)Accion.Idle;
        #endregion

        #region CURIOSIDAD MUY ALTA (EXTREMA)
        // Muy Cerca
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyCerca, (int)TagMiedo.Relajado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyCerca, (int)TagMiedo.Cauto] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyCerca, (int)TagMiedo.Alerta] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyCerca, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyCerca, (int)TagMiedo.Panico] = (int)Accion.RetiradaLenta;

        // Cerca
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Cerca, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Cerca, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Cerca, (int)TagMiedo.Alerta] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Cerca, (int)TagMiedo.Asustado] = (int)Accion.Idle;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Cerca, (int)TagMiedo.Panico] = (int)Accion.RetiradaLenta;

        // Media Distancia
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Media, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Media, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Media, (int)TagMiedo.Alerta] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Media, (int)TagMiedo.Asustado] = (int)Accion.AproxLenta;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Media, (int)TagMiedo.Panico] = (int)Accion.Idle;

        // Lejos
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Lejos, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Lejos, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Lejos, (int)TagMiedo.Alerta] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Lejos, (int)TagMiedo.Asustado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.Lejos, (int)TagMiedo.Panico] = (int)Accion.AproxLenta;

        // Muy Lejos
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyLejos, (int)TagMiedo.Relajado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyLejos, (int)TagMiedo.Cauto] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyLejos, (int)TagMiedo.Alerta] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyLejos, (int)TagMiedo.Asustado] = (int)Accion.AproxRapida;
        baseReglas[(int)TagCuriosidad.Extrema, (int)TagDistancia.MuyLejos, (int)TagMiedo.Panico] = (int)Accion.AproxRapida;
        #endregion

        estaInicializado = true;
        Debug.Log("Base de reglas difusas inicializada correctamente.");
    }
}