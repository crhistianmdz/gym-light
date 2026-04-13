# Migración: AddProductsAndSales

### Cambios realizados
- Creación de las tablas:
  - Products
  - Sales
  - SaleLines
- Índices únicos creados:
  - `Products.Sku` (nullable)
  - `Sales.ClientGuid`
- Restricciones:
  - `Products.Stock >= 0`

### Comando para generar la migración
```
dotnet ef migrations add AddProductsAndSales --project "src/backend/Infrastructure"
```