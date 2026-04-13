import React, { useState } from 'react';
import { Snackbar, Button, Typography, Alert, Table, TableBody, TableCell, TableHead, TableRow } from '@mui/material';
import { saleService } from '@/services/saleService';
import type { ProductResponse } from '@/services/saleService';
import { SyncStatusBadge } from '@/components/CheckInPanel/SyncStatusBadge';
import { ProductRow } from './ProductRow';

export function SalePanel() {
  const [products, setProducts] = useState<ProductResponse[]>([]);
  const [selectedLines, setSelectedLines] = useState<{ product: ProductResponse; quantity: number }[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadProducts = async () => {
    setLoading(true);
    try {
      const fetchedProducts = await saleService.getProducts();
      setProducts(fetchedProducts);
    } catch (err) {
      setError('Error fetching products.');
    } finally {
      setLoading(false);
    }
  };

  const handleAddLine = (product: ProductResponse, quantity: number) => {
    setSelectedLines([...selectedLines, { product, quantity }]);
  };

  const handleConfirmSale = async () => {
    if (!selectedLines.length) return;
    try {
      const clientGuid = crypto.randomUUID();
      const lines = selectedLines.map(line => ({
        productId: line.product.id,
        quantity: line.quantity
      }));
      const request = { clientGuid, lines, performedByUserId: 'currentUser' };
      await saleService.createSale(request);
      setSuccessMessage('Sale successfully created!');
      setSelectedLines([]);
    } catch (err) {
      setError('Failed to create sale.');
    }
  };

  return (
    <div>
      <Typography variant="h6">Make a Sale</Typography>
      {error && <Alert severity="error">{error}</Alert>}
      {successMessage && <Snackbar open autoHideDuration={6000} message={successMessage} />}

      <Button onClick={loadProducts} variant="contained" disabled={loading}>
        Load Products
      </Button>

      <SyncStatusBadge />

      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Product Name</TableCell>
            <TableCell>Stock</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {products.map(product => (
            <React.Fragment key={product.id}>
              <ProductRow product={product} onAddLine={handleAddLine} />
              <TableRow>
                <TableCell>{product.name}</TableCell>
                <TableCell>{product.stock}</TableCell>
              </TableRow>
            </React.Fragment>
          ))}
        </TableBody>
      </Table>
      <Button onClick={handleConfirmSale}>Confirm Sale</Button>
    </div>
  );
}