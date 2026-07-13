using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using WsSRI.Repository;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Services
{
    public interface ISriService
    {
        Task<string> ProcesarFactura(long ventaId);
    }

    public class SriService : ISriService
    {
        private readonly ScmContext _context;
        private readonly IConfiguration _config;
        private readonly AdminContext _adminContext;
        private readonly ICamposPersonalizadosService _camposService;

        public SriService(
            ScmContext context,
            IConfiguration config,
            AdminContext adminContext,
            ICamposPersonalizadosService camposService)
        {
            _context = context;
            _config = config;
            _adminContext = adminContext;
            _camposService = camposService;
        }

        public async Task<string> ProcesarFactura(long ventaId)
        {
            try
            {
                var settings = await _context.ConfiguracionSRI.AsNoTracking().FirstOrDefaultAsync();
                if (settings == null) return "Error: No se han configurado los parámetros del SRI.";

                var venta = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == ventaId);
                if (venta == null) return "Error: Venta no encontrada.";

                var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == (long?)venta.Id_Cliente);
                var detalles = await _context.VentasDetalle.AsNoTracking().Where(d => d.Id_Venta == ventaId).ToListAsync();

                string claveAcceso = "";
                try {
                    claveAcceso = GenerarClaveAcceso(venta, settings);
                } catch (Exception ex) {
                    return $"Error generando Clave de Acceso: {ex.Message}. Verifique que el RUC y códigos de establecimiento sean solo números.";
                }

                // --- 1. PREPARACIÓN DE DATOS PARA EL XML ---
                var infoTributaria = new infoTributaria
                {
                    razonSocial = settings.RazonSocial,
                    nombreComercial = settings.NombreComercial,
                    estab = OnlyDigits(settings.Establecimiento) ?? "001",
                    ptoEmi = OnlyDigits(settings.PuntoEmision) ?? "001",
                    dirMatriz = settings.DireccionMatriz
                };

                // Si el cajero ingresó datos manuales del comprador (CF > monto mínimo),
                // usarlos en lugar del cliente genérico de la BD.
                bool tieneExtraData = !string.IsNullOrWhiteSpace(venta.NameClienteTem);

                string nombreComprador    = tieneExtraData ? venta.NameClienteTem!.ToUpper()
                                          : (cliente?.Nombres_Apellidos ?? "CONSUMIDOR FINAL").ToUpper();
                string cedulaComprador    = tieneExtraData ? (venta.CF_Cedula ?? "9999999999999")
                                          : (OnlyDigits(cliente?.Cedula ?? cliente?.RUC) ?? "9999999999999");
                string direccionComprador = tieneExtraData ? (venta.CF_Direccion ?? "S/N")
                                          : (cliente?.Direccion ?? "S/N");

                // Tipo de identificación según cedula/RUC
                string tipoId;
                if (tieneExtraData) {
                    tipoId = (venta.CF_Cedula?.Length == 13) ? "04" :   // RUC
                             (venta.CF_Cedula?.Length == 10) ? "05" :   // Cédula
                             "07";                                        // Pasaporte/otro
                } else {
                    tipoId = IdentificarTipoIdentificacion(cliente);
                }

                var infoFact = new infoFactura
                {
                    dirEstablecimiento = settings.DireccionEstablecimiento ?? settings.DireccionMatriz,
                    obligadoContabilidad = settings.ObligadoContabilidad ? "SI" : "NO",
                    tipoIdentificacionComprador = tipoId,
                    identificacionComprador = cedulaComprador.PadLeft(13, '0')[..Math.Min(13, cedulaComprador.Length > 13 ? 13 : cedulaComprador.PadLeft(13,'0').Length)],
                    razonSocialComprador = nombreComprador,
                    direccionComprador = direccionComprador,
                    guiaRemision = null,
                    totalSinImpuestos = detalles.Sum(d => d.Subtotal ?? 0),
                    totalDescuento = detalles.Sum(d => d.Descuento ?? 0),
                    importeTotal = detalles.Sum(d => d.Total_Pagar ?? 0),
                    moneda = "DOLAR"
                };

                var items = new List<ItemPorProducto>();
                var allDetalleImpuestos = new List<DetalleImpuestosItem>();
                
                foreach (var det in detalles)
                {
                    var itemImpuestos = new List<DetalleImpuestosItem>
                    {
                        new DetalleImpuestosItem { codigo = "2", codigoPorcentaje = "2", tarifa = 12.00m, baseImponible = det.Subtotal ?? 0, valor = det.IVA ?? 0 }
                    };
                    
                    items.Add(new ItemPorProducto
                    {
                        codigoPrincipal = det.Id_Producto,
                        descripcion = det.Producto,
                        cantidad = (decimal)(det.Cantidad ?? 0),
                        precioUnitario = det.PVP ?? 0,
                        descuento = det.Descuento ?? 0,
                        precioTotalSinImpuesto = det.Subtotal ?? 0,
                        DetItemImpuestos = itemImpuestos
                    });
                    
                    allDetalleImpuestos.AddRange(itemImpuestos);
                }

                var totalImpuestos = new List<totalImpuesto> {
                    new totalImpuesto { 
                        codigo = "2", 
                        codigoPorcentaje = "2", 
                        baseImponible = infoFact.totalSinImpuestos, 
                        valor = detalles.Sum(d => d.IVA ?? 0) 
                    }
                };

                var pagos = new List<Pagos> {
                    new Pagos { formaPago = "01", total = infoFact.importeTotal, plazo = "0", unidadTiempo = "dias" }
                };

                var infoKey = new InfoClaveAcceso {
                    FechaEmision = (venta.FechaHora_Registro ?? DateTime.Now).ToString("dd/MM/yyyy"),
                    TipoComprobante = "01",
                    NumRuc = (OnlyDigits(settings.RUC_Emisor) ?? "9999999999999").PadLeft(13, '0'),
                    TipoAmbiente = settings.Entorno == "PRODUCCION" ? "2" : "1",
                    Serie = (OnlyDigits(settings.Establecimiento) ?? "001") + (OnlyDigits(settings.PuntoEmision) ?? "001"),
                    NumeroComprobante = claveAcceso.Substring(30, 9),
                    CodNumerico = (!string.IsNullOrEmpty(settings.CodigoNumerico) ? settings.CodigoNumerico : "12345678").PadLeft(8,'0').Substring(0,8),
                    TipoEmision = "1"
                };

                // --- 2. GENERACIÓN DEL XML USANDO EL MOTOR DE LA LIBRERÍA ---
                string pathXml = settings.PathFileSaveXml ?? "C:\\SRI\\XML";
                string fullPathXml = Path.Combine(pathXml, claveAcceso + ".xml");

                try {
                    if (!Directory.Exists(pathXml)) Directory.CreateDirectory(pathXml);
                    
                    var engineXml = new CXml();
                    var xmlDoc = engineXml.GetXmlDocumentFactura(
                        claveAcceso, infoKey, infoTributaria, infoFact, 
                        totalImpuestos, pagos, items, allDetalleImpuestos
                    );
                    xmlDoc.Save(fullPathXml);
                    
                    // --- LIMPIEZA DE NODOS OPCIONALES VACÍOS ---
                    // Algunas librerías dejan etiquetas vacías que el SRI rechaza.
                    var cleanDoc = new XmlDocument();
                    cleanDoc.Load(fullPathXml);
                    
                    // Lista de etiquetas opcionales que el SRI no acepta si están vacías
                    string[] tagsOpcionales = { "guiaRemision", "propina", "ordenCompra", "codigoAuxiliar" };
                    bool cambios = false;
                    
                    foreach (var tag in tagsOpcionales) {
                        var nodos = cleanDoc.GetElementsByTagName(tag);
                        for (int i = nodos.Count - 1; i >= 0; i--) {
                            if (string.IsNullOrWhiteSpace(nodos[i].InnerText)) {
                                nodos[i].ParentNode?.RemoveChild(nodos[i]);
                                cambios = true;
                            }
                        }
                    }

                    // --- REPARACIÓN DE IMPUESTOS POR ITEM (DINÁMICO) ---
                    var detallesNodos = cleanDoc.GetElementsByTagName("detalle");
                    foreach (XmlNode detNode in detallesNodos) {
                        if (detNode.SelectNodes("impuestos").Count == 0) {
                            string codProd = detNode.SelectSingleNode("codigoPrincipal")?.InnerText;
                            var detOriginal = detalles.FirstOrDefault(d => d.Id_Producto == codProd);
                            
                            decimal baseImp = decimal.Parse(detNode.SelectSingleNode("precioTotalSinImpuesto")?.InnerText ?? "0",
                                System.Globalization.CultureInfo.InvariantCulture);
                            decimal valorIva = detOriginal?.IVA ?? 0;
                            
                            // Determinar código de porcentaje según normativa SRI Ecuador:
                            // 0 = 0%, 2 = 12%, 3 = 14%, 4 = 15%
                            string codPorcentaje;
                            string tarifa;
                            if (valorIva == 0) {
                                codPorcentaje = "0"; tarifa = "0.00";
                            } else {
                                decimal pct = baseImp > 0 ? Math.Round((valorIva / baseImp) * 100, 0) : 15;
                                if (pct <= 12)      { codPorcentaje = "2"; tarifa = "12.00"; }
                                else if (pct <= 14) { codPorcentaje = "3"; tarifa = "14.00"; }
                                else                { codPorcentaje = "4"; tarifa = "15.00"; }
                            }

                            var impuestosNode = cleanDoc.CreateElement("impuestos");
                            var impuestoNode = cleanDoc.CreateElement("impuesto");

                            var c = cleanDoc.CreateElement("codigo"); c.InnerText = "2";
                            var cp = cleanDoc.CreateElement("codigoPorcentaje"); cp.InnerText = codPorcentaje;
                            var t = cleanDoc.CreateElement("tarifa"); t.InnerText = tarifa;
                            var bi = cleanDoc.CreateElement("baseImponible"); bi.InnerText = baseImp.ToString("F2");
                            var vNode = cleanDoc.CreateElement("valor"); vNode.InnerText = valorIva.ToString("F2");

                            impuestoNode.AppendChild(c);
                            impuestoNode.AppendChild(cp);
                            impuestoNode.AppendChild(t);
                            impuestoNode.AppendChild(bi);
                            impuestoNode.AppendChild(vNode);
                            impuestosNode.AppendChild(impuestoNode);
                            detNode.AppendChild(impuestosNode);
                            cambios = true;
                        }
                    }

                    // --- INTEGRACIÓN DE CAMPOS DINÁMICOS DE FACTURA EN INFOADICIONAL ---
                    try {
                        var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
                        var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
                        if (empresa != null)
                        {
                            var campos = await _camposService.GetCamposActivosAsync("Venta", empresa.RubroId, empresa.Id);
                            var camposFactura = campos.Where(c => c.VisibleEnFactura).ToList();
                            if (camposFactura.Any())
                            {
                                var dictValores = _camposService.ParseDatosAdicionales(venta.DatosAdicionales);
                                
                                // Buscar o crear el nodo <infoAdicional>
                                var rootNode = cleanDoc.DocumentElement;
                                if (rootNode != null)
                                {
                                    var infoAdicNode = rootNode.SelectSingleNode("infoAdicional");
                                    if (infoAdicNode == null)
                                    {
                                        infoAdicNode = cleanDoc.CreateElement("infoAdicional");
                                        rootNode.AppendChild(infoAdicNode);
                                    }

                                    foreach (var campo in camposFactura)
                                    {
                                        if (dictValores.TryGetValue(campo.NombreCampo, out var val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                                        {
                                            // Validar si ya existe el campoAdicional con el mismo nombre para no duplicarlo
                                            var selector = $"campoAdicional[@nombre='{campo.NombreCampo}']";
                                            var existingNode = infoAdicNode.SelectSingleNode(selector);
                                            if (existingNode == null)
                                            {
                                                var campoAdicNode = cleanDoc.CreateElement("campoAdicional");
                                                var attr = cleanDoc.CreateAttribute("nombre");
                                                attr.Value = campo.NombreCampo;
                                                campoAdicNode.Attributes.Append(attr);
                                                campoAdicNode.InnerText = val.ToString();
                                                infoAdicNode.AppendChild(campoAdicNode);
                                                cambios = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } catch {
                        // Omitir si hay algún fallo al cargar los campos para no romper el flujo principal de facturación
                    }
                    
                    if (cambios) cleanDoc.Save(fullPathXml);
                    // -------------------------------------------
                } catch (Exception exXml) {
                    return $"Error generando XML inicial: {exXml.Message}";
                }

                // --- 3. FIRMA NATIVA (XAdES-BES) ---
                string pass = settings.PasswordCertificado?.Trim() ?? "";
                string pathFirmado = "";
                try {
                    pathFirmado = SriNativeSigner.SignXml(fullPathXml, settings.PathFileCertificado, pass);
                } catch (Exception exSign) {
                    return $"Error en Firma Nativa: {exSign.Message}";
                }

                // --- 4. ENVÍO AL SRI (SOAP NATIVO) ---
                var soapClient = new SriSoapClient();
                // Endpoint real (sin ?wsdl)
                string urlRecepcion = settings.Entorno == "PRODUCCION" ? 
                    "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline" :
                    "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";

                try {
                    // Serializar el XML firmado como string UTF-8 sin BOM
                    var signedDoc = new XmlDocument();
                    signedDoc.PreserveWhitespace = true;
                    signedDoc.Load(pathFirmado);
                    
                    var sb = new System.Text.StringBuilder();
                    using (var sw = new System.IO.StringWriter(sb))
                    using (var xw = XmlWriter.Create(sw, new XmlWriterSettings { 
                        Encoding = new System.Text.UTF8Encoding(false), // Sin BOM
                        Indent = false,
                        OmitXmlDeclaration = false
                    }))
                    {
                        signedDoc.WriteTo(xw);
                    }
                    string xmlString = sb.ToString();
                    string base64Xml = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlString));
                    
                    string xmlRespuesta = await soapClient.EnviarComprobante(base64Xml, urlRecepcion);
                    
                    // LOG DIAGNÓSTICO: guardar respuesta cruda del SRI
                    try {
                        string logPath = Path.Combine(pathXml, "log_sri_respuesta.txt");
                        await File.WriteAllTextAsync(logPath, $"[{DateTime.Now}]\n{xmlRespuesta}\n\n", System.Text.Encoding.UTF8);
                    } catch { /* ignorar si falla el log */ }
                    
                    var v = await _context.Ventas.FindAsync(ventaId);
                    var resParsed = ParseRespuestaSRI(xmlRespuesta);
                    
                    if (resParsed.Estado == "RECIBIDA") {
                        if (v != null) {
                            v.SRI_Estado = "AUTORIZADO";
                            v.SRI_ClaveAcceso = claveAcceso;
                            await _context.SaveChangesAsync();
                        }
                        await EnviarCorreo(venta, cliente, settings, pathFirmado);
                        return "Factura RECIBIDA por el SRI con éxito.";
                    } else {
                        string errorLimpio = $"{resParsed.Mensaje} {resParsed.InfoAdicional}".Trim();
                        if (v != null) {
                            v.SRI_Estado = "DEVUELTA";
                            v.SRI_Error = errorLimpio;
                            await _context.SaveChangesAsync();
                        }
                        return $"SRI {resParsed.Estado}: {errorLimpio}";
                    }
                } catch (Exception exSoap) {
                    return $"Error en comunicación con el SRI: {exSoap.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Error inesperado: {ex.Message}";
            }
        }

        private (string Estado, string Mensaje, string InfoAdicional) ParseRespuestaSRI(string xml)
        {
            try {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                
                // Buscamos ignorando namespaces para mayor robustez
                string estado = doc.SelectSingleNode("//*[local-name()='estado']")?.InnerText ?? "DESCONOCIDO";
                string mensaje = doc.SelectSingleNode("//*[local-name()='mensaje']/*[local-name()='mensaje']")?.InnerText 
                                ?? doc.SelectSingleNode("//*[local-name()='mensaje']")?.InnerText ?? "";
                string info = doc.SelectSingleNode("//*[local-name()='informacionAdicional']")?.InnerText ?? "";
                
                return (estado, mensaje, info);
            } catch {
                return ("ERROR_PARSE", "No se pudo leer la respuesta del SRI", xml);
            }
        }

        private async Task<string> EnviarCorreo(VentaOperacion venta, Cliente? cliente, ConfiguracionSRI settings, string xmlPath)
        {
            if (string.IsNullOrEmpty(settings.SmtpHost)) return "Configuración SMTP pendiente.";
            if (cliente == null || string.IsNullOrEmpty(cliente.Correo)) return "El cliente no tiene correo.";

            try {
                var message = new MailMessage();
                message.From = new MailAddress(settings.SenderEmail ?? settings.SmtpUser!);
                message.To.Add(new MailAddress(cliente.Correo));
                message.Subject = $"Comprobante Electrónico - {settings.RazonSocial}";
                message.Body = $"Estimado cliente, adjuntamos su factura electrónica. Gracias.";
                if (File.Exists(xmlPath)) message.Attachments.Add(new Attachment(xmlPath));

                using (var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort ?? 587))
                {
                    client.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword);
                    client.EnableSsl = settings.SmtpSsl;
                    await client.SendMailAsync(message);
                }
                return "Correo enviado.";
            } catch (Exception ex) { return $"Error correo: {ex.Message}"; }
        }

        private string IdentificarTipoIdentificacion(Cliente? cliente)
        {
            if (cliente == null) return "08";
            if (!string.IsNullOrEmpty(cliente.RUC)) return "04";
            if (!string.IsNullOrEmpty(cliente.Cedula)) return "05";
            return "08";
        }

        private string GenerarClaveAcceso(VentaOperacion venta, ConfiguracionSRI settings)
        {
            string fecha = (venta.FechaHora_Registro ?? DateTime.Now).ToString("ddMMyyyy");
            
            // Normalizar RUC a 13 dígitos (pone el cero si la BD lo quitó)
            string ruc = OnlyDigits(settings.RUC_Emisor) ?? "9999999999999";
            ruc = ruc.PadLeft(13, '0');
            
            string amb = settings.Entorno == "PRODUCCION" ? "2" : "1";
            string serie = (OnlyDigits(settings.Establecimiento) ?? "001") + (OnlyDigits(settings.PuntoEmision) ?? "001");
            string secuencia = (venta.Id % 1000000000).ToString().PadLeft(9, '0');
            
            string codigoNum = (!string.IsNullOrEmpty(settings.CodigoNumerico) ? settings.CodigoNumerico : "12345678").PadLeft(8,'0').Substring(0, 8);
            
            // Clave base 48 dígitos
            string clave = fecha + "01" + ruc + amb + serie.PadLeft(6, '0') + secuencia + codigoNum + "1";
            
            if (clave.Length != 48) {
                // Ajustar si el RUC o sede no tienen el largo esperado
                ruc = ruc.PadLeft(13, '0').Substring(0, 13);
                serie = serie.PadLeft(6, '0').Substring(0, 6);
                clave = fecha + "01" + ruc + amb + serie + secuencia + "12345678" + "1";
            }

            return clave + CalcularDigitoModulo11(clave);
        }

        private int CalcularDigitoModulo11(string cadena)
        {
            int suma = 0, factor = 2;
            for (int i = cadena.Length - 1; i >= 0; i--) {
                if (!char.IsDigit(cadena[i])) {
                    throw new FormatException($"Carácter no válido '{cadena[i]}' encontrado en la posición {i} de la clave.");
                }
                suma += (int.Parse(cadena[i].ToString()) * factor);
                factor = factor == 7 ? 2 : factor + 1;
            }
            int r = 11 - (suma % 11);
            return r == 11 ? 0 : (r == 10 ? 1 : r);
        }

        private string OnlyDigits(string? input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            return Regex.Replace(input, @"\D", "");
        }
    }
}
