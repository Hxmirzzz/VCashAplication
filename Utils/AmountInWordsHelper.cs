using System.Text.RegularExpressions;

namespace VCashApp.Utils
{
    public static class AmountInWordsHelper
    {
        public static string ToSpanishCurrency(decimal amount, string currencyCode = "COP", bool includeCents = false)
        {
            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            long entero = (long)Math.Floor(amount);
            int centavos = (int)Math.Round((amount - entero) * 100m);

            var numero = ToSpanishNumber(entero);

            if (currencyCode.Equals("COP", StringComparison.OrdinalIgnoreCase)
             || currencyCode.Equals("USD", StringComparison.OrdinalIgnoreCase)
             || currencyCode.Equals("EUR", StringComparison.OrdinalIgnoreCase))
            {
                numero = Regex.Replace(numero, @"\bveintiuno\b$", "veintiún", RegexOptions.IgnoreCase);
                numero = Regex.Replace(numero, @"\buno\b$", "un", RegexOptions.IgnoreCase);
                numero = Regex.Replace(numero, @"\by uno\b$", "y un", RegexOptions.IgnoreCase);
            }

            string moneda = currencyCode?.ToUpperInvariant() switch
            {
                "COP" => entero == 1 ? "PESO" : "PESOS",
                "USD" => entero == 1 ? "DÓLAR" : "DÓLARES",
                "EUR" => entero == 1 ? "EURO" : "EUROS",
                _ => entero == 1 ? "UNIDAD MONETARIA" : "UNIDADES MONETARIAS"
            };

            var endsWithExactMillions = entero >= 1_000_000 && (entero % 1_000_000 == 0);
            var separador = endsWithExactMillions ? " DE " : " ";

            if (includeCents)
                return $"{numero.ToUpperInvariant()}{separador}{moneda} CON {centavos:00}/100";

            return $"{numero.ToUpperInvariant()}{separador}{moneda}";
        }

        public static string ToSpanishNumber(long n)
        {
            if (n == 0) return "cero";
            if (n < 0) return "menos " + ToSpanishNumber(Math.Abs(n));

            var partes = new List<string>();
            long billones = n / 1_000_000_000; n %= 1_000_000_000;
            long millones = n / 1_000_000; n %= 1_000_000;
            long miles = n / 1_000; n %= 1_000;
            long cientos = n;

            if (billones > 0) partes.Add(billones == 1 ? "mil millones" : $"{ToSpanishNumber(billones)} mil millones");
            if (millones > 0) partes.Add(millones == 1 ? "un millón" : $"{ToSpanishNumber(millones)} millones");
            if (miles > 0) partes.Add(miles == 1 ? "mil" : $"{ToSpanishNumber(miles)} mil");
            if (cientos > 0) partes.Add(CientosATexto((int)cientos));

            return string.Join(" ", partes);
        }

        private static string CientosATexto(int n)
        {
            string[] u = { "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" };
            string[] e = { "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve" };
            string[] d = { "", "diez", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa" };
            string[] c = { "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos" };

            if (n == 100) return "cien";

            int C = n / 100; n %= 100;
            int D = n / 10; n %= 10;
            int U = n;

            var partes = new List<string>();
            if (C > 0) partes.Add(c[C]);

            if (D == 0)
            {
                if (U > 0) partes.Add(u[U]);
            }
            else if (D == 1)
            {
                partes.Add(e[U]);
            }
            else if (D == 2)
            {
                if (U == 0) partes.Add("veinte");
                else
                {
                    string veinti = U switch
                    {
                        2 => "veintidós",
                        3 => "veintitrés",
                        6 => "veintiséis",
                        _ => "veinti" + u[U]
                    };
                    partes.Add(veinti);
                }
            }
            else
            {
                if (U == 0) partes.Add(d[D]);
                else partes.Add($"{d[D]} y {u[U]}");
            }
            return string.Join(" ", partes).Trim();
        }
    }
}