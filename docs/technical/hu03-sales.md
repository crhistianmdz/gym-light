# HU-03 — Venta de Producto / Consumo en Mostrador

## Resumen
HU-03 habilita la funcionalidad de registrar ventas de productos disponibles en el gimnasio (snacks, bebidas, suplementos, etc.) tanto en línea como fuera de línea. Incluye la gestión de stock, la sincronización de datos entre clientes y el servidor, y la capacidad de cancelar ventas realizadas.

## Modelo de datos

### Backend (SQL)
```plaintext
Product
- Id (Guid, PK)
- Sku (string, nullable)
- Name (string, not null)
- Description (string, nullable)
- Price (decimal, not null)
- Stock (int, not null)
- InitialStock (int, not null)
- CreatedAt (DateTime, not null)
- UpdatedAt (DateTime, not null)

Sale
- Id (Guid, PK)
- ClientGuid (Guid, not null)
- PerformedByUserId (Guid, not null)
- Timestamp (DateTime, not null)
- Status (enum: Active/Cancelled, default: Active)
- Total (decimal, not null)

SaleLine
- Id (Guid, PK)
- SaleId (Guid, FK, not null)
- ProductId (Guid, FK, not null)
- Quantity (int, not null)
- UnitPrice (decimal, not null)
- Subtotal (calculated: Quantity * UnitPrice)
```

### Frontend (IndexedDB / Dexie.js)
Stores:
- **products**
  - id (string)
  - sku (string, optional)
  - name (string)
  - description (string, optional)
  - price (number)
  - stock (number)
  - initialStock (number)
  - updatedAt (number)

- **sales**
  - id (string)
  - clientGuid (string)
  - lines (array of SaleLineLocal)
  - total (number)
  - status ('pending' | 'synced' | 'cancelled')
  - timestamp (number)
  - isOffline (boolean)

- **sync_queue**
  - guid (string)
  - type ('Sale' | 'SaleCancel')
  - payload (string)
  - timestamp (number)
  - isOffline (boolean)
  - retryCount (number)

## API Endpoints

### Productos
| Método | Ruta            | Descripción                   | Roles              |
|--------|-----------------|-------------------------------|--------------------|
| GET    | /api/products   | Obtener lista de productos    | Member, Admin, Owner |
| GET    | /api/products/{id} | Obtener producto por ID    | Member, Admin, Owner |

### Ventas
| Método | Ruta          | Descripción                    | Roles                |
|--------|---------------|--------------------------------|----------------------|
| POST   | /api/sales    | Crear una venta                | Receptionist, Admin, Owner |
| DELETE | /api/sales/{id} | Cancelar una venta             | Admin, Owner         |

## Flujo de venta online
1. El usuario selecciona productos a vender en el panel de ventas (SalePanel).
2. La aplicación envía una solicitud POST a `/api/sales` con los productos seleccionados.
3. El servidor valida la venta, ajusta el stock en la base de datos y registra la venta.
4. El cliente almacena la respuesta de la venta en IndexedDB.

## Flujo de venta offline
1. El usuario registra la venta mientras la aplicación está desconectada.
2. Los datos de la venta se almacenan en el store `sync_queue` con el estado `isOffline: true`.
3. Al reestablecerse la conexión, un proceso de sincronización envía las ventas pendientes al servidor.
4. Si la sincronización falla después de 3 intentos, la venta pasa a la bandeja de errores.

## Reglas de negocio
- **Stock bajo:** Generar alerta si el stock restante es menor o igual al 20% del stock inicial.
- **Bloqueo stock 0:** No permitir ventas de productos sin stock.
- **Idempotencia:** Utilizar `ClientGuid` (UUID v4) para evitar procesar ventas duplicadas.
- **Cancelación:** Restaurar el stock de productos en una venta cancelada.
- **Roles:** Solo usuarios con roles autorizados pueden registrar y cancelar ventas.

## Seed de prueba
Productos predefinidos:
- Agua mineral 500ml
- Barra de proteína
- Bebida isotónica 500ml
- Suplemento proteico 1kg
- Guantes de entrenamiento
- Cinta para muñecas
- Toalla deportiva
- Shake de chocolate 350ml
- Creatina monohidratada 300g
- Camiseta deportiva

## Archivos clave
| Archivo                                          | Responsabilidad                             |
|-------------------------------------------------|---------------------------------------------|
| `src/backend/Domain/Entities/Product.cs`       | Modelo de datos para los productos         |
| `src/backend/Domain/Entities/Sale.cs`          | Modelo de datos para las ventas            |
| `src/backend/Domain/Entities/SaleLine.cs`      | Modelo de datos para líneas de ventas      |
| `src/backend/Application/UseCases/CreateSaleUseCase.cs` | Lógica para registrar ventas   |
| `src/backend/Application/UseCases/CancelSaleUseCase.cs` | Lógica para cancelar ventas     |
| `src/frontend/src/db/gymflow.db.ts`            | Configuración de Dexie.js                  |
| `src/frontend/src/services/saleService.ts`     | Lógica de sincronización de ventas y productos |
| `src/frontend/src/components/SalePanel/SalePanel.tsx` | Componente React para la vista de ventas |
