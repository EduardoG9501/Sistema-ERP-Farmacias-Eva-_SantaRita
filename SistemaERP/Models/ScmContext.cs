using Microsoft.EntityFrameworkCore;
using SistemaERP.Models.Entities;

namespace SistemaERP.Models
{
    public class ScmContext : DbContext
    {
        public ScmContext(DbContextOptions<ScmContext> options)
            : base(options)
        {
        }

        public DbSet<Banco> Bancos { get; set; } = null!;
        public DbSet<CatGasto> CatGastos { get; set; } = null!;
        public DbSet<Catalogo> Catalogos { get; set; } = null!;
        public DbSet<AperturaCaja> AperturasCaja { get; set; } = null!;
        public DbSet<CierreCaja> CierresCaja { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<UsuarioWeb> UsuariosWeb { get; set; } = null!;
        public DbSet<Permiso> Permisos { get; set; } = null!;
        public DbSet<UsuarioPermiso> UsuarioPermisos { get; set; } = null!;
        public DbSet<ModuloWeb> ModulosWeb { get; set; } = null!;
        public DbSet<PrivilegioWeb> PrivilegiosWeb { get; set; } = null!;
        public DbSet<UsuarioPrivilegioWeb> UsuarioPrivilegiosWeb { get; set; } = null!;
        public DbSet<Denominacion> Denominaciones { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Menu> Menus { get; set; } = null!;
        public DbSet<MenuRol> MenusRoles { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;
        public DbSet<Presentacion> Presentaciones { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<CargoCompra> Compras { get; set; } = null!;
        public DbSet<CargoCompra_Producto> ComprasDetalle { get; set; } = null!;
        public DbSet<CargoAjuste> AjustesIngreso { get; set; } = null!;
        public DbSet<CargoAjuste_Producto> AjustesIngresoDetalle { get; set; } = null!;
        public DbSet<DescargoAjuste> AjustesEgreso { get; set; } = null!;
        public DbSet<DescargoAjuste_Producto> AjustesEgresoDetalle { get; set; } = null!;
        public DbSet<VentaOperacion> Ventas { get; set; } = null!;
        public DbSet<Venta_Producto> VentasDetalle { get; set; } = null!;
        public DbSet<Stock_Producto> Kardex { get; set; } = null!;
        public DbSet<ConfiguracionSRI> ConfiguracionSRI { get; set; } = null!;
        public DbSet<Bodega> Bodegas { get; set; } = null!;
        public DbSet<StockBodega> StockBodegas { get; set; } = null!;
        public DbSet<DescuentoLineaProducto> DescuentosLinea { get; set; } = null!;
        public DbSet<DescuentoLineaProductoDet> DescuentosLineaDetalle { get; set; } = null!;
        public DbSet<DescuentoCombo> DescuentosCombo { get; set; } = null!;
        public DbSet<DescuentoComboProducto> DescuentosComboProductos { get; set; } = null!;
        public DbSet<VentaPrecioEspecial> VentasPreciosEspeciales { get; set; } = null!;
        public DbSet<DescuentoProveedor> DescuentosProveedor { get; set; } = null!;
        public DbSet<FacturasPorCobrar> FacturasPorCobrar { get; set; } = null!;
        public DbSet<FacturasPorCobrarAbono> FacturasPorCobrarAbonos { get; set; } = null!;
        public DbSet<CuentasPorPagar> CuentasPorPagar { get; set; } = null!;
        public DbSet<CuentasPorPagarAbono> CuentasPorPagarAbonos { get; set; } = null!;
        public DbSet<Traslado> Traslados { get; set; } = null!;
        public DbSet<Traslado_Producto> TrasladosDetalle { get; set; } = null!;
        public DbSet<ParametrosSistema> ParametrosSistema { get; set; } = null!;
        public DbSet<Moneda> Monedas { get; set; } = null!;
        public DbSet<CuentaContable> CuentasContables { get; set; } = null!;
        public DbSet<Gasto> Gastos { get; set; } = null!;
        public DbSet<ConfiguracionEstadoResultados> ConfiguracionEstadoResultados { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuración de clave compuesta para el detalle de venta
            modelBuilder.Entity<Venta_Producto>().HasKey(v => new { v.Id_Venta, v.Id_Producto });

            // Configuraciones adicionales si son necesarias
            modelBuilder.Entity<Banco>().Property(b => b.Nombre).HasColumnName("Banco");

            // Evitar múltiples rutas en cascada SQL Server
            modelBuilder.Entity<AperturaCaja>()
                .HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CierreCaja>()
                .HasOne(c => c.UsuarioCierra)
                .WithMany()
                .HasForeignKey(c => c.UsuarioCierraId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CierreCaja>()
                .HasOne(c => c.Apertura)
                .WithMany()
                .HasForeignKey(c => c.AperturaCajaId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Ignorar la advertencia de cambios pendientes en el modelo (exclusivo de EF Core 9/10)
            // para evitar errores críticos al migrar dinámicamente las bases de datos de inquilinos.
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}
