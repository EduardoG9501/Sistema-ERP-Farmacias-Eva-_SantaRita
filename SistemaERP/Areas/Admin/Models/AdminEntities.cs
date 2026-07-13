using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaERP.Areas.Admin.Models
{
    [Table("Rubros")]
    public class Rubro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(100)]
        public string? Icono { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<Empresa> Empresas { get; set; } = new List<Empresa>();
        public virtual ICollection<PlantillaRubro> Plantillas { get; set; } = new List<PlantillaRubro>();
        public virtual ICollection<CampoPersonalizado> CamposPersonalizados { get; set; } = new List<CampoPersonalizado>();
    }

    [Table("Empresas")]
    public class Empresa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string RUC { get; set; } = string.Empty;

        [Required]
        public int RubroId { get; set; }

        [Required]
        public string ConnectionStringEncriptado { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ServidorDB { get; set; }

        [StringLength(100)]
        public string? NombreDB { get; set; }

        [StringLength(500)]
        public string? Logo { get; set; }

        [StringLength(100)]
        public string? Subdominio { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(200)]
        public string? NombreContacto { get; set; }

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public DateTime? FechaVencimiento { get; set; }

        public bool Estado { get; set; } = true;

        [StringLength(20)]
        public string VersionSistema { get; set; } = "1.0.0";

        public DateTime? UltimoAccesoAdmin { get; set; }

        [ForeignKey("RubroId")]
        public virtual Rubro? Rubro { get; set; }

        public virtual ICollection<BackupRegistro> Backups { get; set; } = new List<BackupRegistro>();
        public virtual ICollection<AdminAuditLog> AuditLogs { get; set; } = new List<AdminAuditLog>();
        public virtual ICollection<CampoPersonalizado> CamposPersonalizados { get; set; } = new List<CampoPersonalizado>();
    }

    [Table("AdminUsuarios")]
    public class AdminUsuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<BackupRegistro> Backups { get; set; } = new List<BackupRegistro>();
        public virtual ICollection<AdminAuditLog> AuditLogs { get; set; } = new List<AdminAuditLog>();
        public virtual ICollection<CampoPersonalizado> CamposPersonalizados { get; set; } = new List<CampoPersonalizado>();
    }

    [Table("AdminAuditLog")]
    public class AdminAuditLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int AdminUsuarioId { get; set; }

        public int? EmpresaId { get; set; }

        [Required]
        [StringLength(200)]
        public string Accion { get; set; } = string.Empty;

        public string? Detalle { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IP { get; set; }

        [ForeignKey("AdminUsuarioId")]
        public virtual AdminUsuario? AdminUsuario { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }
    }

    [Table("Backups")]
    public class BackupRegistro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        public DateTime FechaBackup { get; set; } = DateTime.Now;

        [Required]
        [StringLength(500)]
        public string RutaArchivo { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TamanoMB { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "En proceso"; // 'Exitoso', 'Fallido', 'En proceso'

        [Required]
        public int AdminUsuarioId { get; set; }

        public string? Observaciones { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        [ForeignKey("AdminUsuarioId")]
        public virtual AdminUsuario? AdminUsuario { get; set; }
    }

    [Table("CamposPersonalizados")]
    public class CampoPersonalizado
    {
        [Key]
        public int Id { get; set; }

        public int? EmpresaId { get; set; }

        public int? RubroId { get; set; }

        [Required]
        [StringLength(100)]
        public string Entidad { get; set; } = string.Empty; // 'Producto', 'Cliente', 'Venta', 'Proveedor', etc.

        [Required]
        [StringLength(100)]
        public string NombreCampo { get; set; } = string.Empty; // 'PrincipioActivo'

        [Required]
        [StringLength(200)]
        public string Etiqueta { get; set; } = string.Empty; // 'Principio Activo'

        [Required]
        [StringLength(50)]
        public string TipoDato { get; set; } = "Texto"; // 'Texto', 'Numero', 'Fecha', 'Booleano', 'Lista'

        public string? OpcionesLista { get; set; } // JSON de opciones o valores separados por comas

        public bool Requerido { get; set; } = false;

        // Validaciones de Texto
        public int? MinLongitud { get; set; }
        public int? MaxLongitud { get; set; }
        [StringLength(500)]
        public string? ExpresionRegular { get; set; }
        public bool EsUnico { get; set; } = false;

        // Validaciones Numéricas
        [Column(TypeName = "decimal(18, 4)")]
        public decimal? ValorMinimo { get; set; }
        [Column(TypeName = "decimal(18, 4)")]
        public decimal? ValorMaximo { get; set; }
        public int? Decimales { get; set; }

        // Interfaz y Ayuda
        [StringLength(500)]
        public string? ValorDefault { get; set; }
        [StringLength(200)]
        public string? Placeholder { get; set; }
        [StringLength(500)]
        public string? Tooltip { get; set; }

        // Visibilidad
        public bool VisibleEnFormulario { get; set; } = true;
        public bool VisibleEnLista { get; set; } = false;
        public bool VisibleEnReporte { get; set; } = true;
        public bool VisibleEnEtiqueta { get; set; } = false;
        public bool VisibleEnFactura { get; set; } = false;

        public int OrdenVisualizacion { get; set; } = 0;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int? CreadoPorAdmin { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        [ForeignKey("RubroId")]
        public virtual Rubro? Rubro { get; set; }

        [ForeignKey("CreadoPorAdmin")]
        public virtual AdminUsuario? AdminUsuario { get; set; }
    }

    [Table("PlantillasRubro")]
    public class PlantillaRubro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RubroId { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoPlantilla { get; set; } = string.Empty; // 'Catalogo', 'Presentacion', 'Parametros'

        [Required]
        [StringLength(100)]
        public string Clave { get; set; } = string.Empty; // e.g. Linea, Marca, Unidad

        [Required]
        public string Valor { get; set; } = string.Empty; // e.g. Pernos Acerados

        public string? Extras { get; set; } // Opciones adicionales

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey("RubroId")]
        public virtual Rubro? Rubro { get; set; }
    }

    [Table("UsuarioEmpresa")]
    public class UsuarioEmpresa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public int EmpresaId { get; set; }

        public string? PasswordHash { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }
    }
}
