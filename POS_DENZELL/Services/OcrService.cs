using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Tesseract;

namespace POS_DENZELL.Services
{
    public class ResultadoOcrYapeDTO
    {
        public bool Exito { get; set; }
        public decimal? Monto { get; set; }
        public string? NumeroOperacion { get; set; }
        public string TextoCrudo { get; set; } = string.Empty;
        public string? MensajeError { get; set; }
    }

    public interface IOcrService
    {
        Task<ResultadoOcrYapeDTO> ExtraerDatosYapeAsync(IFormFile archivoImagen);
    }

    public class OcrService : IOcrService
    {
        private readonly IWebHostEnvironment _env;

        public OcrService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<ResultadoOcrYapeDTO> ExtraerDatosYapeAsync(IFormFile archivoImagen)
        {
            if (archivoImagen == null || archivoImagen.Length == 0)
                return new ResultadoOcrYapeDTO { Exito = false, MensajeError = "No se recibió ninguna imagen." };

            try
            {
                string rutaTessdata = Path.Combine(_env.ContentRootPath, "tessdata");
                if (!Directory.Exists(rutaTessdata))
                {
                    return new ResultadoOcrYapeDTO
                    {
                        Exito = false,
                        MensajeError = "Falta la carpeta 'tessdata' con el archivo spa.traineddata en la raíz del proyecto."
                    };
                }

                // 1. Preprocesar la foto de la pantalla del celular (Escala de grises + Contraste)
                byte[] bytesProcesados;
                using (var inputStream = archivoImagen.OpenReadStream())
                using (var image = await Image.LoadAsync(inputStream))
                {
                    // Si la foto de la cámara es gigante (ej. 4000px), la escalamos a un ancho óptimo para OCR rápido
                    if (image.Width > 1400)
                    {
                        int nuevoAlto = (int)((double)image.Height / image.Width * 1400);
                        image.Mutate(x => x.Resize(1400, nuevoAlto));
                    }

                    // Mejoramos contraste para eliminar el reflejo de la pantalla del celular
                    image.Mutate(x => x.Grayscale().Contrast(1.35f));

                    using var outputStream = new MemoryStream();
                    await image.SaveAsPngAsync(outputStream);
                    bytesProcesados = outputStream.ToArray();
                }

                // 2. Ejecutar Tesseract OCR en el servidor (Laptop)
                string textoExtraido;
                using (var engine = new TesseractEngine(rutaTessdata, "spa", EngineMode.Default))
                using (var pix = Pix.LoadFromMemory(bytesProcesados))
                using (var page = engine.Process(pix))
                {
                    textoExtraido = page.GetText() ?? string.Empty;
                }

                // 3. Extraer Monto (ej. "S/ 10", "S/10", "S/ 63.50")
                decimal? montoDetectado = ExtraerMontoYape(textoExtraido);

                // 4. Extraer N° de Operación (ej. "24905831" de 7 u 8 dígitos)
                string? operacionDetectada = ExtraerNumeroOperacionYape(textoExtraido);

                return new ResultadoOcrYapeDTO
                {
                    Exito = montoDetectado.HasValue || !string.IsNullOrEmpty(operacionDetectada),
                    Monto = montoDetectado,
                    NumeroOperacion = operacionDetectada,
                    TextoCrudo = textoExtraido
                };
            }
            catch (Exception ex)
            {
                return new ResultadoOcrYapeDTO
                {
                    Exito = false,
                    MensajeError = "No se pudo leer la imagen con OCR: " + ex.Message
                };
            }
        }

        private decimal? ExtraerMontoYape(string texto)
        {
            // Caso 1: Busca "S/" o "S /" seguido del número (ej. "S/ 10" o "S/ 25.50")
            var matchSoles = Regex.Match(texto, @"S\s*[/I1l|]\s*(\d{1,5}(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase);
            if (matchSoles.Success)
            {
                string valorStr = matchSoles.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(valorStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto))
                    return monto;
            }

            // Caso 2: Si el OCR no leyó el símbolo "S/", busca el número justo debajo de "¡Yapeaste!"
            var matchYapeaste = Regex.Match(texto, @"Yapeaste\s*!?\s*(?:S\s*[/I1l|])?\s*(\d{1,5}(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase);
            if (matchYapeaste.Success)
            {
                string valorStr = matchYapeaste.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(valorStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto))
                    return monto;
            }

            return null;
        }

        private string? ExtraerNumeroOperacionYape(string texto)
        {
            // Caso 1: Junto a "Nro. de operación" o "operación" (como en la pantalla de Yape: 24905831)
            var matchOp = Regex.Match(texto, @"(?:operaci[oó]n|operacion)\D{0,25}(\d{7,8})\b", RegexOptions.IgnoreCase);
            if (matchOp.Success)
            {
                return matchOp.Groups[1].Value;
            }

            // Caso 2: Si leyó las columnas por separado, buscamos cualquier número exacto de 7 u 8 dígitos
            // (Descartando años como 2026 o teléfonos de 9 dígitos)
            var matchesNumeros = Regex.Matches(texto, @"\b(\d{7,8})\b");
            if (matchesNumeros.Count > 0)
            {
                return matchesNumeros[matchesNumeros.Count - 1].Groups[1].Value;
            }

            return null;
        }
    }
}