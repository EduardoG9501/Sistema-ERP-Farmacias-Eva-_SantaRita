using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaERP.Models.Entities
{
    [Table("Bancos")]
    public class Banco
    {
        [Key]
        public int Id { get; set; }
        
        [Column("Banco")]
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }

        public string? Correo { get; set; }
        public string? Direccion { get; set; }
    }

    [Table("Cat_Gastos")]
    public class CatGasto
    {
        [Key]
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public string? CreateUser { get; set; }
        public bool Estado { get; set; }
        public DateTime? CreateDate { get; set; }
    }


    [Table("Catalogo")]
    public class Catalogo
    {
        [Key]
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public int IdTipoCatalogo { get; set; }
        public bool Estado { get; set; }
        public DateTime Fecha_Registro { get; set; }
        public string? Codigo { get; set; }
    }

    

    [Table("Denominaciones")]
    public class Denominacion
    {
        [Key]
        public int Id { get; set; }

        public string Nombre { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }

        public string Tipo { get; set; } // "Moneda" o "Billete"

        public bool Activo { get; set; } = true;
    }

    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        public string? UsuarioNombre { get; set; } // No existe en la DB actual

        [Column("Usuario")]
        public string? Login { get; set; }

        [Column("Contraseña")] // Corregido el nombre de la columna
        public string? Password { get; set; }
        public string? Tipo_Usuario { get; set; }
        public string? Estado { get; set; }
        public string? FotoPath { get; set; }
    }

    [Table("Permisos")]
    public class Permiso
    {
        [Key]
        public int Id { get; set; }
        public string Num_Ventana { get; set; } = null!;
        public string Ventanas { get; set; } = null!;
        public DateTime? Fecha_Sistema { get; set; }
        public bool Estado { get; set; }
    }

    [Table("Usuario_Permisos")]
    public class UsuarioPermiso
    {
        [Key]
        public int Id { get; set; }
        public int Id_Usuario { get; set; }
        public int Id_Permiso { get; set; }
        public DateTime? Fecha_Sistema { get; set; }
        public bool Heredar_Permiso { get; set; }
    }

    [Table("ModulosWeb")]
    public class ModuloWeb
    {
        [Key]
        public int Id { get; set; }
        public string ControllerName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public bool Estado { get; set; }
    }

    [Table("PrivilegiosWeb")]
    public class PrivilegioWeb
    {
        [Key]
        public int Id { get; set; }
        public int Id_Modulo { get; set; }
        public string ActionName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public bool Estado { get; set; }

        [ForeignKey("Id_Modulo")]
        public ModuloWeb? Modulo { get; set; }
    }

    [Table("Usuario_PrivilegiosWeb")]
    public class UsuarioPrivilegioWeb
    {
        [Key]
        public int Id { get; set; }
        public int Id_Usuario { get; set; }
        public int Id_Privilegio { get; set; }
        public DateTime Fecha_Sistema { get; set; }
        public bool Heredar_Permiso { get; set; }

        [ForeignKey("Id_Privilegio")]
        public PrivilegioWeb? Privilegio { get; set; }
    }





    [Table("Rol")]
    public class Rol
    {
        [Key]
        public int idRol { get; set; }
        public string? nombre { get; set; }
        public DateTime? fechaRegistro { get; set; }
    }



    [Table("Menu")]
    public class Menu
    {
        [Key]
        public int idMenu { get; set; }
        public string? nombre { get; set; }
        public string? icono { get; set; }
        public string? url { get; set; }
    }

    [Table("MenuRol")]
    public class MenuRol
    {
        [Key]
        public int idMenuRol { get; set; }
        public int? idMenu { get; set; }
        public int? idRol { get; set; }
    }

    [Table("Clientes")]
    public class Cliente
    {
        [Key]
        public long Id { get; set; }
        public string? Cedula { get; set; }
        public string? RUC { get; set; }
        public string? Nombres_Apellidos { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Correo { get; set; }
        public string? Ciudad { get; set; }
        public int? TipoDocumento { get; set; }
        public string? DatosAdicionales { get; set; }
    }

    [Table("Proveedores")]
    public class Proveedor
    {
        [Key]
        public long Id { get; set; }
        
        [Column("Proveedor")]
        public string? Nombre { get; set; }
        
        public string? Matriz { get; set; }

        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? RUC { get; set; }
        public string? Gerente { get; set; }
        public string? Autorizacion { get; set; }
        public string? Datos { get; set; }
        public int? TipoIdentifiacion { get; set; }
        public string? Correo { get; set; }
        public string? NombreComercial { get; set; }
        public string? Tipocontribuyente { get; set; }
        public string? AgenteRetencion { get; set; }
        public string? DatosAdicionales { get; set; }
    }

    [Table("Presentaciones")]
    public class Presentacion
    {
        [Key]
        public long Id { get; set; }
        [Column("Presentacion")]
        public string? Nombre { get; set; }
    }

    [Table("Productos")]
    public class Producto
    {
        [Key]
        public long Id { get; set; }
        public string? CodigoBarra { get; set; }
        [Column("Producto")]
        public string? Nombre { get; set; }
        public long? Id_Presentacion { get; set; }
        public long? Id_Proveedor { get; set; }
        public decimal? PVF { get; set; }
        public decimal? PVP { get; set; }
        public int? Contiene { get; set; }
        public DateTime? Vencimiento { get; set; }
        public string? Estado { get; set; }
        public decimal? Descuento { get; set; }
        public DateTime? Descuento_Desde { get; set; }
        public DateTime? Descuento_Hasta { get; set; }
        public long? Disponible { get; set; }
        public string? Lote { get; set; }
        public string? Foto { get; set; }
        public decimal? IVA { get; set; }
        public decimal? PUR { get; set; }
        public decimal? Utilidad { get; set; }
        public long? Id_Especificacion { get; set; }
        public int? Id_Composicion { get; set; }
        public bool Pvp_Iva { get; set; }
        public string? RegistroSanitario { get; set; }
        public bool? EsFraccionable { get; set; }
        public string? DatosAdicionales { get; set; }
    }

    [Table("CargoCompra")]
    public class CargoCompra
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Factura { get; set; }

        public int? Id_Proveedor { get; set; }
        public DateTime? Fecha_Documento { get; set; }

        public int? Dias_Pago { get; set; }
        public DateTime? Fecha_Pago { get; set; }
        public decimal? IVA { get; set; }
        public decimal? Desc_Porc { get; set; }
        public string? Observacion { get; set; }
        public string? Estado { get; set; }
        public int? Id_Bodega { get; set; }
        public DateTime? FechaHora_Reg { get; set; }
        public string? DatosAdicionales { get; set; }
    }

    [Table("CargoCompra_Productos")]
    public class CargoCompra_Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? Id_Factura { get; set; }
        public string? Id_Producto { get; set; }
        public string? Producto { get; set; }
        public long? Id_Presentacion { get; set; }
        public long? Id_Proveedor { get; set; }
        public int? Contiene { get; set; }
        public decimal? PVF { get; set; }
        public int? Cantidad { get; set; }
        public int? Bonificacion { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? IVA { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? TotalPagar { get; set; }
        public int? Id_Usuario { get; set; }
        public decimal? PUR { get; set; }
        public decimal? Utilidad { get; set; }
        public DateTime? Vencimiento { get; set; }
        public string? Lote { get; set; }
        public bool? EsCaja { get; set; }
        public int? Id_Bodega { get; set; }
    }

    [Table("CargoAjuste")]
    public class CargoAjuste
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int? Maquina { get; set; }

        public string? Almacen { get; set; }
        public string? Movimiento { get; set; }
        public long? Id_Laboratorio { get; set; }
        public string? Laboratorio { get; set; }
        public DateTime? Fecha_Documento { get; set; }
        public string? Observacion { get; set; }
        public DateTime? FechaHora_Registro { get; set; }
        public int? Id_Resp { get; set; }
        public decimal? Subtotal { get; set; }
        public string? Estado { get; set; }
        public int? Id_Bodega { get; set; }
    }


    [Table("CargoAjuste_Productos")]
    public class CargoAjuste_Producto
    {
        [Key]
        public int Id { get; set; }
        public long Id_Factura { get; set; }
        public string Id_Producto { get; set; } = null!;
        public string? Producto { get; set; }
        public string? Presentacion { get; set; }
        public int? Cantidad { get; set; }
        public decimal? PVF { get; set; }
        public decimal? PVP { get; set; }
        public decimal? Subtotal { get; set; }
        public int? Contiene { get; set; }
        public long? Disponible { get; set; }
        public DateTime? FechaVence { get; set; }
        public string? Lote { get; set; }
        public bool? EsCaja { get; set; }
        public int? Id_Bodega { get; set; }
    }

    [Table("DescargoAjuste")]
    public class DescargoAjuste
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int? Maquina { get; set; }

        public string? Almacen { get; set; }
        public string? Movimiento { get; set; }
        public long? Id_Laboratorio { get; set; }
        public string? Laboratorio { get; set; }
        public DateTime? Fecha_Documento { get; set; }
        public string? Observacion { get; set; }
        public DateTime? FechaHora_Registro { get; set; }
        public int? Id_Resp { get; set; }
        public decimal? Subtotal { get; set; }
        public string? Estado { get; set; }
        public int? Id_Bodega { get; set; }
    }


    [Table("DescargoAjuste_Productos")]
    public class DescargoAjuste_Producto
    {
        [Key]
        public int Id { get; set; }
        public long Id_Factura { get; set; }
        public string Id_Producto { get; set; } = null!;
        public string? Producto { get; set; }
        public int? Cantidad { get; set; }
        public decimal? PVP { get; set; }
        public decimal? Subtotal { get; set; }
        public long? Disponible { get; set; }
        public string? Lote { get; set; }
        public DateTime? FechaVence { get; set; }
        public bool? EsCaja { get; set; }
    }

    [Table("Ventas")]
    public class VentaOperacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; } // PK en la DB no es Identity
        public int? Maquina { get; set; }

        public long? Id_Cliente { get; set; }
        public int? Id_Usuario { get; set; }
        public DateTime? FechaHora_Registro { get; set; }
        public int? Procesado { get; set; }
        public string? NameClienteTem { get; set; }  // Nombre comprador (Consumidor Final identificado)
        public string? CF_Cedula { get; set; }         // Cédula/RUC ingresada manualmente
        public string? CF_Direccion { get; set; }      // Dirección ingresada manualmente
        public int? Tipo_Venta { get; set; }
        public string? NumDoc { get; set; }
        public int? NumeroComprobante { get; set; }

        // Campos para Facturación Electrónica
        public string? SRI_Estado { get; set; }
        public string? SRI_ClaveAcceso { get; set; }
        public string? SRI_Error { get; set; }
        public string? DatosAdicionales { get; set; }
    }


    [Table("Ventas_Productos")]
    public class Venta_Producto
    {
        public long Id_Venta { get; set; }
        public string Id_Producto { get; set; } = null!;
        public string? Producto { get; set; }
        public string? Presentacion { get; set; }
        public string? Laboratorio { get; set; }
        [Column("Vendio")]
        public long? Cantidad { get; set; } // Es bigint en la DB
        public decimal? PVP { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? IVA { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? Total_Pagar { get; set; }
        public decimal? Ganancia { get; set; }
        public long? Contiene { get; set; }
        public long? Disponible { get; set; }
        public decimal? Parcial { get; set; }
        public string? Lote { get; set; }
        public string? Vencimiento_Texto { get; set; }
        public decimal? Subtotal_DP { get; set; }
        public decimal? Subtotal_CP { get; set; }
        public int? Id_Combo { get; set; }
        public int? Id_Precio_Especial { get; set; }
    }


    [Table("Stock_Productos")]
    public class Stock_Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? Id_Factura { get; set; }

        public int? IdVenta { get; set; } // Es 'int' en Stock_Productos según el esquema
        public string? Id_Producto { get; set; }
        public string? Producto { get; set; }
        public int? Cantidad { get; set; }
        public decimal? PVF { get; set; }
        public decimal? Subtotal { get; set; }
        public string? TipoMovimiento { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? Stock { get; set; }
        public int? Id_Bodega { get; set; }
        public int? Id_Usuario { get; set; }
    }

    [Table("ConfiguracionSRI")]
    public class ConfiguracionSRI
    {
        [Key]
        public int Id { get; set; }
        public string? Entorno { get; set; } // "PRUEBAS" o "PRODUCCION"
        public string? PathFileSaveXml { get; set; }
        public string? PathFileCertificado { get; set; }
        public string? PasswordCertificado { get; set; }
        public string? RUC_Emisor { get; set; }
        public string? RazonSocial { get; set; }
        public string? NombreComercial { get; set; }
        public string? Establecimiento { get; set; } 
        public string? PuntoEmision { get; set; }   
        public string? DireccionMatriz { get; set; }
        public string? DireccionEstablecimiento { get; set; }
        public bool ObligadoContabilidad { get; set; }
        public bool AgenteRetencion { get; set; }        // Es agente de retención
        public string? NumeroResolucion { get; set; }     // Número de resolución del agente
        public string? ContribuyenteEspecialNro { get; set; } // Nro. contribuyente especial
        public string? TipoComprobanteRetencion { get; set; } // 07
        public string? CodDocSustento { get; set; }       // 42
        public decimal? MontoMinimoConsumidorFinal { get; set; } // Monto mínimo para emitir factura
        public string? UrlSriLocal { get; set; }          // URL local del servicio SRI
        public bool ActivarEnvioDesdeVentas { get; set; } // Enviar al SRI automáticamente al cerrar venta
        public string? ImpresoraTickets { get; set; }     // Nombre de la impresora de tickets
        public string? Regimen { get; set; }           // RIMPE, GENERAL, etc.
        public string? CodigoNumerico { get; set; }    // 8 dígitos para clave de acceso
        public string? CodigoResolucion { get; set; }  
        public bool ActivarServicioSRI { get; set; }   // Habilitar emisión electrónica
        public DateTime? UltimaActualizacion { get; set; }
        public string? ClaveMaestra { get; set; }      // Clave para autorizar cambio de precio por debajo del costo

        // Configuración para envío de correos
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public bool SmtpSsl { get; set; }
        public string? SenderEmail { get; set; }
    }
    [Table("Bodegas")]
    public class Bodega
    {
        [Key]
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public bool EsPrincipal { get; set; }
        public bool Estado { get; set; }
    }

    [Table("Stock_Bodegas")]
    public class StockBodega
    {
        [Key]
        public int Id { get; set; }
        public int Id_Bodega { get; set; }
        public string? Id_Producto { get; set; }
        public long Stock { get; set; }
    }

    [Table("Descuento_Linea_Producto")]
    public class DescuentoLineaProducto
    {
        [Key]
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public decimal Porcentaje { get; set; }
        public DateTime Vence_Desde { get; set; }
        public DateTime Vence_Hasta { get; set; }
        public string? CreateUser { get; set; }
        public bool Estado { get; set; }
        public DateTime? CreateDate { get; set; }
    }

    [Table("Descuento_Linea_Producto_Det")]
    public class DescuentoLineaProductoDet
    {
        [Key]
        public int Id { get; set; }
        public int Id_Descuento_Linea { get; set; }
        public long Id_Producto { get; set; }
    }

    [Table("Descuento_Combo")]
    public class DescuentoCombo
    {
        [Key]
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public decimal Precio_Combo { get; set; }
        public long Cant_Min { get; set; }
        public long Cant_Max { get; set; }
        public DateTime Vence_Desde { get; set; }
        public DateTime Vence_Hasta { get; set; }
        public string? CreateUser { get; set; }
        public bool Estado { get; set; }
        public DateTime? CreateDate { get; set; }
    }

    [Table("Descuento_Combo_Productos")]
    public class DescuentoComboProducto
    {
        [Key]
        public int Id { get; set; }
        public int Id_Descuento_Combo { get; set; }
        public long Id_Producto { get; set; }
    }

    [Table("Venta_Precio_Especial")]
    public class VentaPrecioEspecial
    {
        [Key]
        public int Id { get; set; }
        public int? Id_Venta { get; set; }
        public long? Id_Det_Venta { get; set; }
        public long Id_Producto { get; set; }
        public decimal Precio_Especial { get; set; }
        public decimal Cantidad { get; set; }
        public string? Estado { get; set; }
        public int? Usuario_Solicita { get; set; }
        public int? Usuario_Aprueba { get; set; }
        public int? Usuario_Rechaza { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? FechaAprueba { get; set; }
        public DateTime? FechaRechaza { get; set; }
    }

    [Table("Descuento_Proveedor")]
    public class DescuentoProveedor
    {
        [Key]
        public int Id { get; set; }
        public long Id_Proveedor { get; set; }
        public string? Descripcion { get; set; }
        public decimal Porcentaje { get; set; }
        public DateTime Vence_Desde { get; set; }
        public DateTime Vence_Hasta { get; set; }
        public bool Estado { get; set; }
        public string? CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }
    }

    [Table("FacturasPorCobrar")]
    public class FacturasPorCobrar
    {
        [Key]
        public long Id { get; set; }
        public long Num_Venta { get; set; }
        public string? Factura { get; set; }
        public long Id_Cliente { get; set; }
        public DateTime Fecha_Emision { get; set; }
        public int DiasPago { get; set; }
        public DateTime Fecha_Vencimiento { get; set; }
        public decimal Monto_Fact { get; set; }
        public decimal Monto_NC { get; set; }
        public decimal Monto_RET { get; set; }
        public decimal Monto_TotalPagar { get; set; }
        public decimal Monto_Abonado { get; set; }
        public decimal Monto_Saldo { get; set; }
        public string? Estado { get; set; }
        public bool Retencion_Renta { get; set; }
        public bool Retencion_IVA { get; set; }
        public string? ORIGEN_FACTURA { get; set; }
        public decimal Monto_RET_IVA { get; set; }
    }

    [Table("FacturasPorCobrar_Abonos")]
    public class FacturasPorCobrarAbono
    {
        [Key]
        public long Id { get; set; }
        public int Id_Usuario { get; set; }
        public long Id_Venta { get; set; }
        public DateTime Fecha_Cobro { get; set; }
        public decimal Monto_Efectivo { get; set; }
        public DateTime FechaHora_Reg { get; set; }
        public int Tipo_Pago { get; set; }
        public string? Num_Doc { get; set; }
        public int? Id_Banco { get; set; }
        public decimal Monto_Multipago { get; set; }
        public string? Imagen_Doc { get; set; }
        public string? Adquirente { get; set; }
        public decimal Interes_MultiPago { get; set; }
    }

    [Table("CuentasPorPagar")]
    public class CuentasPorPagar
    {
        [Key]
        public long Id { get; set; }
        public long Id_Compra { get; set; }
        public string? Factura { get; set; }
        public long Id_Proveedor { get; set; }
        public DateTime Fecha_Emision { get; set; }
        public int DiasPago { get; set; }
        public DateTime Fecha_Vencimiento { get; set; }
        public decimal Monto_Fact { get; set; }
        public decimal Monto_NC { get; set; }
        public decimal Monto_RET { get; set; }
        public decimal Monto_TotalPagar { get; set; }
        public decimal Monto_Abonado { get; set; }
        public decimal Monto_Saldo { get; set; }
        public string? Estado { get; set; } // "POR PAGAR", "PAGADA"
        public decimal Monto_RET_IVA { get; set; }
    }

    [Table("CuentasPorPagar_Abonos")]
    public class CuentasPorPagarAbono
    {
        [Key]
        public long Id { get; set; }
        public int Id_Usuario { get; set; }
        public long Id_Compra { get; set; }
        public DateTime Fecha_Pago { get; set; }
        public decimal Monto_Efectivo { get; set; }
        public DateTime FechaHora_Reg { get; set; }
        public int Tipo_Pago { get; set; } // 1: Efectivo, 2: Deposito/Transferencia, 3: Cheque
        public string? Num_Doc { get; set; }
        public int? Id_Banco { get; set; }
        public decimal Monto_Multipago { get; set; }
        public string? Imagen_Doc { get; set; }
        public string? Adquirente { get; set; }
    }

    [Table("Traslados")]
    public class Traslado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public DateTime Fecha_Envio { get; set; }
        public int Id_Bodega_Origen { get; set; }
        public int Id_Bodega_Destino { get; set; }
        public int Id_Usuario_Envio { get; set; }
        public DateTime? Fecha_Recepcion { get; set; }
        public int? Id_Usuario_Recepcion { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string? Observacion { get; set; }
        public string? DatosAdicionales { get; set; }
    }

    [Table("Traslados_Productos")]
    public class Traslado_Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long Id_Traslado { get; set; }
        public string Id_Producto { get; set; } = null!;
        public string Producto { get; set; } = null!;
        public int Cantidad { get; set; }
        public bool EsCaja { get; set; }
        public int Contiene { get; set; }
    }
    [Table("ParametrosSistema")]
    public class ParametrosSistema
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } = 1;

        // Empresa
        public string? NombreEmpresa { get; set; }
        public string? AliasEmpresa { get; set; }
        public string? Telefono { get; set; }
        public string? Telefono2 { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? RUC { get; set; }
        public string? SitioWeb { get; set; }
        public string? LogoPath { get; set; }

        // Formato de impresión
        public int NumItemsFactura { get; set; } = 14;
        public string? AltoImpresion { get; set; } = "Automatico";
        public bool MostrarLogoEnDocumentos { get; set; } = true;
        public string? ImpresoraPredeterminada { get; set; }

        // Ticket de venta
        public string? TicketEncabezado { get; set; }
        public string? TicketPie { get; set; }
        public string? TicketNotaAviso { get; set; }
        public bool TicketMostrarLogo { get; set; } = true;
        public bool TicketMostrarFecha { get; set; } = true;
        public bool TicketMostrarCajero { get; set; } = true;
        public int TicketAncho { get; set; } = 80;

        // Reportes
        public string? ReporteEncabezado { get; set; }
        public string? ReportePie { get; set; }
        public bool ReporteMostrarLogo { get; set; } = true;

        // Inventario
        public int DiasAlertaVencimiento { get; set; } = 30;
        public bool MostrarAlertaStock { get; set; } = true;
        public string? ClaveAprobacionVencido { get; set; }

        // Ventas
        public int StockMinimoCargar { get; set; } = 1;
        public int NumVentanasSimultaneas { get; set; } = 3;

        // Vigencia del sistema
        public DateTime? FechaVigenciaDesde { get; set; }
        public DateTime? FechaVigenciaHasta { get; set; }

        // Auditoría
        public DateTime? UltimaActualizacion { get; set; }

        // Moneda
        public string? MonedaOficial { get; set; }

        // Catálogo Público Web
        public bool CatalogoPublicoActivo { get; set; } = false;
        public string? CatalogoTitulo { get; set; }
        public bool CatalogoMostrarPrecios { get; set; } = true;
        public string? CatalogoWhatsApp { get; set; }
        public string? PosLayout { get; set; } = "imagenes";

        // IVA Parametrizado
        public string? IvaCodigoSri { get; set; } = "2";
        public string? IvaCodPorcentajeSri { get; set; } = "4";
        public decimal IvaTarifa { get; set; } = 15.00m;

        // Integración Contable
        public int? CuentaContableVentasId { get; set; }
        public int? CuentaContableCostosId { get; set; }
    }

    [Table("Monedas")]
    public class Moneda
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Codigo { get; set; } = "";
        public string? Nombre { get; set; }
        public string? Simbolo { get; set; }
        public string? Pais { get; set; }
        public bool Activo { get; set; } = true;
    }

    [Table("CuentasContables")]
    public class CuentaContable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = ""; // "Ingreso", "Costo", "Gasto", "Activo", "Pasivo", "Patrimonio"
        public int? IdPadre { get; set; }
        public bool Activo { get; set; } = true;
    }

    [Table("Gastos")]
    public class Gasto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }
        public int CuentaContableId { get; set; }
        [ForeignKey("CuentaContableId")]
        public virtual CuentaContable? CuentaContable { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string? UsuarioRegistro { get; set; }
    }

    [Table("ConfiguracionEstadoResultados")]
    public class ConfiguracionEstadoResultados
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Orden { get; set; }
        public string Descripcion { get; set; } = "";
        public string TipoFila { get; set; } = ""; // "Cuenta", "Formula"
        public int? CuentaContableId { get; set; }
        [ForeignKey("CuentaContableId")]
        public virtual CuentaContable? CuentaContable { get; set; }
        public string? Formula { get; set; } // e.g. "{3} - {8}"
        public bool EsNegrita { get; set; } = false;
    }
}




