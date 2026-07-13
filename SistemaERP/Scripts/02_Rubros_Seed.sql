-- 02_Rubros_Seed.sql
-- Semillas para Rubros y Plantillas Predeterminadas de Negocio

USE AdminDB;
GO

-- 1. Insertar Rubros Core
SET IDENTITY_INSERT Rubros ON;

IF NOT EXISTS (SELECT 1 FROM Rubros WHERE Id = 1)
    INSERT INTO Rubros (Id, Nombre, Descripcion, Icono, Activo)
    VALUES (1, 'Ferretería', 'Negocios de venta de materiales de construcción, herramientas, tornillería, plomería, electricidad y acabados.', 'fa-hammer', 1);

IF NOT EXISTS (SELECT 1 FROM Rubros WHERE Id = 2)
    INSERT INTO Rubros (Id, Nombre, Descripcion, Icono, Activo)
    VALUES (2, 'Farmacia', 'Farmacias, boticas, venta de medicamentos, insumos médicos, cosméticos y servicios de salud integrados.', 'fa-prescription-bottle-medical', 1);

IF NOT EXISTS (SELECT 1 FROM Rubros WHERE Id = 3)
    INSERT INTO Rubros (Id, Nombre, Descripcion, Icono, Activo)
    VALUES (3, 'Despensa', 'Supermercados, minimarkets, tiendas de abarrotes, despensas y negocios de venta de alimentos y artículos de primera necesidad.', 'fa-basket-shopping', 1);

IF NOT EXISTS (SELECT 1 FROM Rubros WHERE Id = 4)
    INSERT INTO Rubros (Id, Nombre, Descripcion, Icono, Activo)
    VALUES (4, 'Taller de Repuestos', 'Taller mecánico, venta de autopartes, repuestos automotrices, lubricantes y mantenimiento vehicular.', 'fa-wrench', 1);

SET IDENTITY_INSERT Rubros OFF;
GO

-- 2. Insertar Plantillas de Catálogos predeterminados por Rubro
-- NOTA: Estos registros representan catálogos (IdTipoCatalogo) y presentaciones que se generarán
-- automáticamente en la base de datos de cada cliente de acuerdo a su rubro.

-- Rubro 1: Ferretería
-- Catálogos del Producto: Líneas y Marcas
IF NOT EXISTS (SELECT 1 FROM PlantillasRubro WHERE RubroId = 1 AND TipoPlantilla = 'Catalogo')
BEGIN
    -- Líneas de Ferretería
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (1, 'Catalogo', 'Linea', 'Pernos Acerados', '1'),
    (1, 'Catalogo', 'Linea', 'Herramientas Eléctricas', '1'),
    (1, 'Catalogo', 'Linea', 'Plomería y Accesorios', '1'),
    (1, 'Catalogo', 'Linea', 'Electricidad e Iluminación', '1'),
    (1, 'Catalogo', 'Linea', 'Pinturas y Barnices', '1'),
    (1, 'Catalogo', 'Linea', 'Materiales de Construcción', '1'),
    (1, 'Catalogo', 'Linea', 'Cerrajería y Herrajes', '1'),
    (1, 'Catalogo', 'Linea', 'Fijaciones y Clavos', '1');

    -- SubLíneas de Ferretería
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (1, 'Catalogo', 'SubLinea', 'Cabeza de cuadro', '2'),
    (1, 'Catalogo', 'SubLinea', 'Cabezal Philips', '2'),
    (1, 'Catalogo', 'SubLinea', 'Cabezal Plano', '2'),
    (1, 'Catalogo', 'SubLinea', 'Destornilladores', '2'),
    (1, 'Catalogo', 'SubLinea', 'Alicates y Pinzas', '2'),
    (1, 'Catalogo', 'SubLinea', 'Tuberías PVC', '2'),
    (1, 'Catalogo', 'SubLinea', 'Cables y Alambres', '2'),
    (1, 'Catalogo', 'SubLinea', 'Pegamentos y Selladores', '2');

    -- Marcas de Ferretería
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (1, 'Catalogo', 'Marca', 'Infasco', '3'),
    (1, 'Catalogo', 'Marca', 'Stanley', '3'),
    (1, 'Catalogo', 'Marca', 'DeWalt', '3'),
    (1, 'Catalogo', 'Marca', 'Truper', '3'),
    (1, 'Catalogo', 'Marca', 'Bostik', '3'),
    (1, 'Catalogo', 'Marca', '3M', '3'),
    (1, 'Catalogo', 'Marca', 'Teka', '3'),
    (1, 'Catalogo', 'Marca', 'FV', '3');

    -- Presentaciones de Ferretería
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (1, 'Presentacion', 'Unidad', 'Unidad', NULL),
    (1, 'Presentacion', 'Par', 'Par', NULL),
    (1, 'Presentacion', 'Caja', 'Caja', NULL),
    (1, 'Presentacion', 'Bolsa', 'Bolsa', NULL),
    (1, 'Presentacion', 'Galon', 'Galón', NULL),
    (1, 'Presentacion', 'Litro', 'Litro', NULL),
    (1, 'Presentacion', 'Metro', 'Metro', NULL),
    (1, 'Presentacion', 'Rollo', 'Rollo', NULL),
    (1, 'Presentacion', 'Kilo', 'Kilogramo', NULL),
    (1, 'Presentacion', 'Libra', 'Libra', NULL);

    -- Campos personalizados automáticos para Ferretería (en la tabla CamposPersonalizados)
    -- Estos se cargarán para la empresa del rubro automáticamente
END
GO

-- Rubro 2: Farmacia
IF NOT EXISTS (SELECT 1 FROM PlantillasRubro WHERE RubroId = 2 AND TipoPlantilla = 'Catalogo')
BEGIN
    -- Procedencias
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (2, 'Catalogo', 'Procedencia', 'Nacional', '4'),
    (2, 'Catalogo', 'Procedencia', 'Importado', '4');

    -- Especificaciones
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (2, 'Catalogo', 'Especificacion', 'Medicamento General', '5'),
    (2, 'Catalogo', 'Especificacion', 'Venta Libre (OTC)', '5'),
    (2, 'Catalogo', 'Especificacion', 'Bajo Receta Médica', '5'),
    (2, 'Catalogo', 'Especificacion', 'Sustancia Controlada', '5'),
    (2, 'Catalogo', 'Especificacion', 'Cosméticos y Cuidado', '5'),
    (2, 'Catalogo', 'Especificacion', 'Suplemento Nutricional', '5');

    -- Composiciones / Principios Activos
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (2, 'Catalogo', 'Composicion', 'Ibuprofeno 400mg', '6'),
    (2, 'Catalogo', 'Composicion', 'Paracetamol 500mg', '6'),
    (2, 'Catalogo', 'Composicion', 'Amoxicilina 500mg', '6'),
    (2, 'Catalogo', 'Composicion', 'Losartán 50mg', '6'),
    (2, 'Catalogo', 'Composicion', 'Irbesartán 300mg', '6'),
    (2, 'Catalogo', 'Composicion', 'Clindamicina 300mg', '6'),
    (2, 'Catalogo', 'Composicion', 'Clotrimazol 1%', '6');

    -- Presentaciones de Farmacia
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (2, 'Presentacion', 'Tableta', 'Tableta', NULL),
    (2, 'Presentacion', 'Capsula', 'Cápsula', NULL),
    (2, 'Presentacion', 'Jarabe', 'Jarabe (ml)', NULL),
    (2, 'Presentacion', 'Ampolla', 'Ampolla', NULL),
    (2, 'Presentacion', 'Caja', 'Caja', NULL),
    (2, 'Presentacion', 'Frasco', 'Frasco', NULL),
    (2, 'Presentacion', 'Crema', 'Crema/Pomada (g)', NULL),
    (2, 'Presentacion', 'Unidad', 'Unidad', NULL);
END
GO

-- Rubro 3: Despensa
IF NOT EXISTS (SELECT 1 FROM PlantillasRubro WHERE RubroId = 3 AND TipoPlantilla = 'Catalogo')
BEGIN
    -- Grupos
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (3, 'Catalogo', 'Grupo', 'Lácteos y Derivados', '7'),
    (3, 'Catalogo', 'Grupo', 'Bebidas y Refrescos', '7'),
    (3, 'Catalogo', 'Grupo', 'Granos y Cereales', '7'),
    (3, 'Catalogo', 'Grupo', 'Enlatados y Conservas', '7'),
    (3, 'Catalogo', 'Grupo', 'Carnes y Embutidos', '7'),
    (3, 'Catalogo', 'Grupo', 'Artículos de Limpieza', '7'),
    (3, 'Catalogo', 'Grupo', 'Cuidado Personal', '7'),
    (3, 'Catalogo', 'Grupo', 'Snacks y Confitería', '7');

    -- Especificaciones de Despensa
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (3, 'Catalogo', 'Especificacion', 'Bajo en Grasa', '8'),
    (3, 'Catalogo', 'Especificacion', 'Sin Azúcar', '8'),
    (3, 'Catalogo', 'Especificacion', 'Sin Gluten', '8'),
    (3, 'Catalogo', 'Especificacion', 'Integral', '8'),
    (3, 'Catalogo', 'Especificacion', 'Orgánico', '8'),
    (3, 'Catalogo', 'Especificacion', 'Regular', '8');

    -- Presentaciones de Despensa
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (3, 'Presentacion', 'Unidad', 'Unidad', NULL),
    (3, 'Presentacion', 'Paquete', 'Paquete', NULL),
    (3, 'Presentacion', 'Docena', 'Docena', NULL),
    (3, 'Presentacion', 'Caja', 'Caja', NULL),
    (3, 'Presentacion', 'Funda', 'Funda', NULL),
    (3, 'Presentacion', 'Botella', 'Botella', NULL),
    (3, 'Presentacion', 'Galon', 'Galón', NULL),
    (3, 'Presentacion', 'Litro', 'Litro', NULL);
END
GO

-- Rubro 4: Taller de Repuestos
IF NOT EXISTS (SELECT 1 FROM PlantillasRubro WHERE RubroId = 4 AND TipoPlantilla = 'Catalogo')
BEGIN
    -- Familias de Repuestos
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (4, 'Catalogo', 'Familia', 'Rodamientos y Afines', '9'),
    (4, 'Catalogo', 'Familia', 'Sistema de Frenos', '9'),
    (4, 'Catalogo', 'Familia', 'Suspensión y Dirección', '9'),
    (4, 'Catalogo', 'Familia', 'Componentes del Motor', '9'),
    (4, 'Catalogo', 'Familia', 'Sistema Eléctrico', '9'),
    (4, 'Catalogo', 'Familia', 'Filtros y Lubricantes', '9'),
    (4, 'Catalogo', 'Familia', 'Transmisión y Embrague', '9');

    -- SubFamilias de Repuestos
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (4, 'Catalogo', 'SubFamilia', 'Rodamientos de rodillos cónicos', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Rodamientos de bolas', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Pastillas de Freno', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Discos de Freno', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Amortiguadores', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Filtros de Aceite', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Filtros de Aire', '10'),
    (4, 'Catalogo', 'SubFamilia', 'Bujías y Bobinas', '10');

    -- Marcas de Repuestos
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (4, 'Catalogo', 'Marca', 'NTN', '11'),
    (4, 'Catalogo', 'Marca', 'SKF', '11'),
    (4, 'Catalogo', 'Marca', 'Timken', '11'),
    (4, 'Catalogo', 'Marca', 'Brembo', '11'),
    (4, 'Catalogo', 'Marca', 'Bosch', '11'),
    (4, 'Catalogo', 'Marca', 'Denso', '11'),
    (4, 'Catalogo', 'Marca', 'Mobil', '11'),
    (4, 'Catalogo', 'Marca', 'Castrol', '11');

    -- Presentaciones de Repuestos
    INSERT INTO PlantillasRubro (RubroId, TipoPlantilla, Clave, Valor, Extras) VALUES
    (4, 'Presentacion', 'Unidad', 'Unidad', NULL),
    (4, 'Presentacion', 'Par', 'Par', NULL),
    (4, 'Presentacion', 'Set', 'Set', NULL),
    (4, 'Presentacion', 'Kit', 'Kit', NULL),
    (4, 'Presentacion', 'Litro', 'Litro', NULL),
    (4, 'Presentacion', 'Galon', 'Galón', NULL);
END
GO
