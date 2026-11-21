using System;
using System.Collections.Generic;

namespace POS_DENZELL.Helpers
{
    public static class NumeroALetras
    {
        private static readonly string[] Bajos =
        {
            "CERO","UNO","DOS","TRES","CUATRO","CINCO","SEIS","SIETE","OCHO","NUEVE",
            "DIEZ","ONCE","DOCE","TRECE","CATORCE","QUINCE","DIECISEIS","DIECISIETE",
            "DIECIOCHO","DIECINUEVE","VEINTE","VEINTIUNO","VEINTIDOS","VEINTITRES",
            "VEINTICUATRO","VEINTICINCO","VEINTISEIS","VEINTISIETE","VEINTIOCHO","VEINTINUEVE"
        };

        private static readonly string[] Decenas =
            { "", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };

        private static readonly string[] Centenas =
        {
            "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
            "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

        public static string ConvertirSoles(decimal monto)
        {
            // Redondeo comercial primero, así 9.995 pasa a 10.00 y no a 9 CON 100/100
            monto = Math.Round(Math.Abs(monto), 2, MidpointRounding.AwayFromZero);

            long entera = (long)Math.Truncate(monto);
            int centimos = (int)((monto - entera) * 100);

            return $"SON: {Texto(entera)} CON {centimos:00}/100 SOLES";
        }

        private static string Texto(long n)
        {
            if (n == 0) return "CERO";

            var partes = new List<string>();
            int millones = (int)(n / 1_000_000);
            int miles = (int)((n / 1000) % 1000);
            int resto = (int)(n % 1000);

            if (millones > 0)
                partes.Add(millones == 1 ? "UN MILLON" : Apocopar(Menor1000(millones)) + " MILLONES");
            if (miles > 0)
                partes.Add(miles == 1 ? "MIL" : Apocopar(Menor1000(miles)) + " MIL");
            if (resto > 0)
                partes.Add(Menor1000(resto));

            return string.Join(" ", partes);
        }

        private static string Menor1000(int n)
        {
            if (n == 100) return "CIEN";

            int r = n % 100;
            string resto = r < 30
                ? (r == 0 ? "" : Bajos[r])
                : Decenas[r / 10] + (r % 10 > 0 ? " Y " + Bajos[r % 10] : "");

            return (Centenas[n / 100] + " " + resto).Trim();
        }

        // "VEINTIUNO MIL" -> "VEINTIUN MIL", "UNO" -> "UN"
        private static string Apocopar(string s) => s.EndsWith("UNO") ? s[..^1] : s;
    }
}