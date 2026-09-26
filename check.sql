USE [TRPO_ElectronicsStore];
GO
SELECT 'products' AS [Table], COUNT(*) AS [Actual], 150 AS [Expected] FROM dbo.products
UNION ALL SELECT 'categories',COUNT(*),5 FROM dbo.categories
UNION ALL SELECT 'brands',COUNT(*),5 FROM dbo.brands
UNION ALL SELECT 'tags',COUNT(*),5 FROM dbo.tags
UNION ALL SELECT 'product_tags',COUNT(*),253 FROM dbo.product_tags;
SELECT COUNT(*) AS [LowStock], 2 AS [Expected] FROM dbo.products WHERE stock<10;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
