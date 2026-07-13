using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Banco = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bancos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bodegas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodegas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CargoAjuste",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Maquina = table.Column<int>(type: "int", nullable: true),
                    Almacen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Movimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Laboratorio = table.Column<long>(type: "bigint", nullable: true),
                    Laboratorio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha_Documento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaHora_Registro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Id_Resp = table.Column<int>(type: "int", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Bodega = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoAjuste", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CargoAjuste_Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Factura = table.Column<long>(type: "bigint", nullable: false),
                    Id_Producto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Presentacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: true),
                    PVF = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PVP = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Contiene = table.Column<int>(type: "int", nullable: true),
                    Disponible = table.Column<long>(type: "bigint", nullable: true),
                    FechaVence = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsCaja = table.Column<bool>(type: "bit", nullable: true),
                    Id_Bodega = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoAjuste_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CargoCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Factura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Proveedor = table.Column<int>(type: "int", nullable: true),
                    Fecha_Documento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Dias_Pago = table.Column<int>(type: "int", nullable: true),
                    Fecha_Pago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Desc_Porc = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Bodega = table.Column<int>(type: "int", nullable: true),
                    FechaHora_Reg = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoCompra", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CargoCompra_Productos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Factura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Presentacion = table.Column<long>(type: "bigint", nullable: true),
                    Id_Proveedor = table.Column<long>(type: "bigint", nullable: true),
                    Contiene = table.Column<int>(type: "int", nullable: true),
                    PVF = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: true),
                    Bonificacion = table.Column<int>(type: "int", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPagar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Id_Usuario = table.Column<int>(type: "int", nullable: true),
                    PUR = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Utilidad = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Vencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsCaja = table.Column<bool>(type: "bit", nullable: true),
                    Id_Bodega = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoCompra_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cat_Gastos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cat_Gastos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Catalogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdTipoCatalogo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    Fecha_Registro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalogo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cedula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RUC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombres_Apellidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoDocumento = table.Column<int>(type: "int", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionSRI",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Entorno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PathFileSaveXml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PathFileCertificado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordCertificado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RUC_Emisor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RazonSocial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreComercial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Establecimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuntoEmision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DireccionMatriz = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DireccionEstablecimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObligadoContabilidad = table.Column<bool>(type: "bit", nullable: false),
                    AgenteRetencion = table.Column<bool>(type: "bit", nullable: false),
                    NumeroResolucion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContribuyenteEspecialNro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoComprobanteRetencion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodDocSustento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MontoMinimoConsumidorFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UrlSriLocal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivarEnvioDesdeVentas = table.Column<bool>(type: "bit", nullable: false),
                    ImpresoraTickets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Regimen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoNumerico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoResolucion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivarServicioSRI = table.Column<bool>(type: "bit", nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClaveMaestra = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpHost = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: true),
                    SmtpUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpSsl = table.Column<bool>(type: "bit", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionSRI", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Denominaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denominaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DescargoAjuste",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Maquina = table.Column<int>(type: "int", nullable: true),
                    Almacen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Movimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Laboratorio = table.Column<long>(type: "bigint", nullable: true),
                    Laboratorio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha_Documento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaHora_Registro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Id_Resp = table.Column<int>(type: "int", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Bodega = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescargoAjuste", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DescargoAjuste_Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Factura = table.Column<long>(type: "bigint", nullable: false),
                    Id_Producto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: true),
                    PVP = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Disponible = table.Column<long>(type: "bigint", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaVence = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EsCaja = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescargoAjuste_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Descuento_Combo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Precio_Combo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cant_Min = table.Column<long>(type: "bigint", nullable: false),
                    Cant_Max = table.Column<long>(type: "bigint", nullable: false),
                    Vence_Desde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vence_Hasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuento_Combo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Descuento_Combo_Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Descuento_Combo = table.Column<int>(type: "int", nullable: false),
                    Id_Producto = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuento_Combo_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Descuento_Linea_Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Porcentaje = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Vence_Desde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vence_Hasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuento_Linea_Producto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Descuento_Linea_Producto_Det",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Descuento_Linea = table.Column<int>(type: "int", nullable: false),
                    Id_Producto = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuento_Linea_Producto_Det", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Descuento_Proveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Proveedor = table.Column<long>(type: "bigint", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Porcentaje = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Vence_Desde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vence_Hasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    CreateUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuento_Proveedor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacturasPorCobrar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Num_Venta = table.Column<long>(type: "bigint", nullable: false),
                    Factura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Cliente = table.Column<long>(type: "bigint", nullable: false),
                    Fecha_Emision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiasPago = table.Column<int>(type: "int", nullable: false),
                    Fecha_Vencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto_Fact = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Monto_NC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Monto_RET = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Monto_TotalPagar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Monto_Abonado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Monto_Saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retencion_Renta = table.Column<bool>(type: "bit", nullable: false),
                    Retencion_IVA = table.Column<bool>(type: "bit", nullable: false),
                    ORIGEN_FACTURA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Monto_RET_IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasPorCobrar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacturasPorCobrar_Abonos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Usuario = table.Column<int>(type: "int", nullable: false),
                    Id_Venta = table.Column<long>(type: "bigint", nullable: false),
                    Fecha_Cobro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto_Efectivo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaHora_Reg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo_Pago = table.Column<int>(type: "int", nullable: false),
                    Num_Doc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Banco = table.Column<int>(type: "int", nullable: true),
                    Monto_Multipago = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Imagen_Doc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Adquirente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Interes_MultiPago = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasPorCobrar_Abonos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Menu",
                columns: table => new
                {
                    idMenu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    icono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu", x => x.idMenu);
                });

            migrationBuilder.CreateTable(
                name: "MenuRol",
                columns: table => new
                {
                    idMenuRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idMenu = table.Column<int>(type: "int", nullable: true),
                    idRol = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuRol", x => x.idMenuRol);
                });

            migrationBuilder.CreateTable(
                name: "ModulosWeb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ControllerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModulosWeb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Monedas",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Simbolo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pais = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monedas", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NombreEmpresa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AliasEmpresa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RUC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SitioWeb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumItemsFactura = table.Column<int>(type: "int", nullable: false),
                    AltoImpresion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MostrarLogoEnDocumentos = table.Column<bool>(type: "bit", nullable: false),
                    ImpresoraPredeterminada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TicketEncabezado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TicketPie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TicketNotaAviso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TicketMostrarLogo = table.Column<bool>(type: "bit", nullable: false),
                    TicketMostrarFecha = table.Column<bool>(type: "bit", nullable: false),
                    TicketMostrarCajero = table.Column<bool>(type: "bit", nullable: false),
                    TicketAncho = table.Column<int>(type: "int", nullable: false),
                    ReporteEncabezado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportePie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReporteMostrarLogo = table.Column<bool>(type: "bit", nullable: false),
                    DiasAlertaVencimiento = table.Column<int>(type: "int", nullable: false),
                    MostrarAlertaStock = table.Column<bool>(type: "bit", nullable: false),
                    StockMinimoCargar = table.Column<int>(type: "int", nullable: false),
                    NumVentanasSimultaneas = table.Column<int>(type: "int", nullable: false),
                    FechaVigenciaDesde = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVigenciaHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MonedaOficial = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Num_Ventana = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ventanas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha_Sistema = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Presentaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Presentacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presentaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoBarra = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Presentacion = table.Column<long>(type: "bigint", nullable: true),
                    Id_Proveedor = table.Column<long>(type: "bigint", nullable: true),
                    PVF = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PVP = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Contiene = table.Column<int>(type: "int", nullable: true),
                    Vencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Descuento_Desde = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Descuento_Hasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Disponible = table.Column<long>(type: "bigint", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Foto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PUR = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Utilidad = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Id_Especificacion = table.Column<long>(type: "bigint", nullable: true),
                    Id_Composicion = table.Column<int>(type: "int", nullable: true),
                    Pvp_Iva = table.Column<bool>(type: "bit", nullable: false),
                    RegistroSanitario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsFraccionable = table.Column<bool>(type: "bit", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Proveedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Matriz = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RUC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gerente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Autorizacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Datos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoIdentifiacion = table.Column<int>(type: "int", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreComercial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipocontribuyente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgenteRetencion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rol",
                columns: table => new
                {
                    idRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rol", x => x.idRol);
                });

            migrationBuilder.CreateTable(
                name: "Stock_Bodegas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Bodega = table.Column<int>(type: "int", nullable: false),
                    Id_Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stock = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock_Bodegas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stock_Productos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Factura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdVenta = table.Column<int>(type: "int", nullable: true),
                    Id_Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: true),
                    PVF = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TipoMovimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: true),
                    Id_Bodega = table.Column<int>(type: "int", nullable: true),
                    Id_Usuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Traslados",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Fecha_Envio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id_Bodega_Origen = table.Column<int>(type: "int", nullable: false),
                    Id_Bodega_Destino = table.Column<int>(type: "int", nullable: false),
                    Id_Usuario_Envio = table.Column<int>(type: "int", nullable: false),
                    Fecha_Recepcion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Id_Usuario_Recepcion = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traslados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Traslados_Productos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Traslado = table.Column<long>(type: "bigint", nullable: false),
                    Id_Producto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    EsCaja = table.Column<bool>(type: "bit", nullable: false),
                    Contiene = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traslados_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario_Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Usuario = table.Column<int>(type: "int", nullable: false),
                    Id_Permiso = table.Column<int>(type: "int", nullable: false),
                    Fecha_Sistema = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Heredar_Permiso = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contraseña = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo_Usuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosWeb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosWeb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Venta_Precio_Especial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Venta = table.Column<int>(type: "int", nullable: true),
                    Id_Det_Venta = table.Column<long>(type: "bigint", nullable: true),
                    Id_Producto = table.Column<long>(type: "bigint", nullable: false),
                    Precio_Especial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Usuario_Solicita = table.Column<int>(type: "int", nullable: true),
                    Usuario_Aprueba = table.Column<int>(type: "int", nullable: true),
                    Usuario_Rechaza = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAprueba = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRechaza = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta_Precio_Especial", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Maquina = table.Column<int>(type: "int", nullable: true),
                    Id_Cliente = table.Column<long>(type: "bigint", nullable: true),
                    Id_Usuario = table.Column<int>(type: "int", nullable: true),
                    FechaHora_Registro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Procesado = table.Column<int>(type: "int", nullable: true),
                    NameClienteTem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CF_Cedula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CF_Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo_Venta = table.Column<int>(type: "int", nullable: true),
                    NumDoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroComprobante = table.Column<int>(type: "int", nullable: true),
                    SRI_Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SRI_ClaveAcceso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SRI_Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ventas_Productos",
                columns: table => new
                {
                    Id_Venta = table.Column<long>(type: "bigint", nullable: false),
                    Id_Producto = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Producto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Presentacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Laboratorio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Vendio = table.Column<long>(type: "bigint", nullable: true),
                    PVP = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IVA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Total_Pagar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Ganancia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Contiene = table.Column<long>(type: "bigint", nullable: true),
                    Disponible = table.Column<long>(type: "bigint", nullable: true),
                    Parcial = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Vencimiento_Texto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtotal_DP = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Subtotal_CP = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Id_Combo = table.Column<int>(type: "int", nullable: true),
                    Id_Precio_Especial = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas_Productos", x => new { x.Id_Venta, x.Id_Producto });
                });

            migrationBuilder.CreateTable(
                name: "PrivilegiosWeb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Modulo = table.Column<int>(type: "int", nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegiosWeb", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivilegiosWeb_ModulosWeb_Id_Modulo",
                        column: x => x.Id_Modulo,
                        principalTable: "ModulosWeb",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AperturasCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AperturasCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AperturasCaja_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuario_PrivilegiosWeb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Usuario = table.Column<int>(type: "int", nullable: false),
                    Id_Privilegio = table.Column<int>(type: "int", nullable: false),
                    Fecha_Sistema = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Heredar_Permiso = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario_PrivilegiosWeb", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_PrivilegiosWeb_PrivilegiosWeb_Id_Privilegio",
                        column: x => x.Id_Privilegio,
                        principalTable: "PrivilegiosWeb",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CierresCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AperturaCajaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioCierraId = table.Column<int>(type: "int", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Diferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DetalleDenominaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetodosPago = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CierresCaja_AperturasCaja_AperturaCajaId",
                        column: x => x.AperturaCajaId,
                        principalTable: "AperturasCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CierresCaja_Usuarios_UsuarioCierraId",
                        column: x => x.UsuarioCierraId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AperturasCaja_UsuarioId",
                table: "AperturasCaja",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CierresCaja_AperturaCajaId",
                table: "CierresCaja",
                column: "AperturaCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_CierresCaja_UsuarioCierraId",
                table: "CierresCaja",
                column: "UsuarioCierraId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegiosWeb_Id_Modulo",
                table: "PrivilegiosWeb",
                column: "Id_Modulo");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PrivilegiosWeb_Id_Privilegio",
                table: "Usuario_PrivilegiosWeb",
                column: "Id_Privilegio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bancos");

            migrationBuilder.DropTable(
                name: "Bodegas");

            migrationBuilder.DropTable(
                name: "CargoAjuste");

            migrationBuilder.DropTable(
                name: "CargoAjuste_Productos");

            migrationBuilder.DropTable(
                name: "CargoCompra");

            migrationBuilder.DropTable(
                name: "CargoCompra_Productos");

            migrationBuilder.DropTable(
                name: "Cat_Gastos");

            migrationBuilder.DropTable(
                name: "Catalogo");

            migrationBuilder.DropTable(
                name: "CierresCaja");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "ConfiguracionSRI");

            migrationBuilder.DropTable(
                name: "Denominaciones");

            migrationBuilder.DropTable(
                name: "DescargoAjuste");

            migrationBuilder.DropTable(
                name: "DescargoAjuste_Productos");

            migrationBuilder.DropTable(
                name: "Descuento_Combo");

            migrationBuilder.DropTable(
                name: "Descuento_Combo_Productos");

            migrationBuilder.DropTable(
                name: "Descuento_Linea_Producto");

            migrationBuilder.DropTable(
                name: "Descuento_Linea_Producto_Det");

            migrationBuilder.DropTable(
                name: "Descuento_Proveedor");

            migrationBuilder.DropTable(
                name: "FacturasPorCobrar");

            migrationBuilder.DropTable(
                name: "FacturasPorCobrar_Abonos");

            migrationBuilder.DropTable(
                name: "Menu");

            migrationBuilder.DropTable(
                name: "MenuRol");

            migrationBuilder.DropTable(
                name: "Monedas");

            migrationBuilder.DropTable(
                name: "ParametrosSistema");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Presentaciones");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Rol");

            migrationBuilder.DropTable(
                name: "Stock_Bodegas");

            migrationBuilder.DropTable(
                name: "Stock_Productos");

            migrationBuilder.DropTable(
                name: "Traslados");

            migrationBuilder.DropTable(
                name: "Traslados_Productos");

            migrationBuilder.DropTable(
                name: "Usuario_Permisos");

            migrationBuilder.DropTable(
                name: "Usuario_PrivilegiosWeb");

            migrationBuilder.DropTable(
                name: "UsuariosWeb");

            migrationBuilder.DropTable(
                name: "Venta_Precio_Especial");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Ventas_Productos");

            migrationBuilder.DropTable(
                name: "AperturasCaja");

            migrationBuilder.DropTable(
                name: "PrivilegiosWeb");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "ModulosWeb");
        }
    }
}
