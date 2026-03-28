import { useState } from 'react';
import { Snackbar, Button, TextField, Typography, Alert, Table, TableBody, TableCell, TableHead, TableRow } from '@mui/material';
import { db } from '@/db/gymflow.db';
import { saleService } from '@/services/saleService';
import { SyncStatusBadge } from '@/components/CheckInPanel/SyncStatusBadge';

export function SalePanel() {
  const [products, setProducts] = useState([]);
  const [selectedLines, setSelectedLines] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [syncPending, setSyncPending] = useState(false);

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

  const handleAddLine = (product, quantity) => {
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
            <TableRow key={product.id}>
              <TableCell>{product.name}</TableCell>
              <TableCell>{product.stock}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Button onClick={handleConfirmSale}>Confirm Sale</Button>
    </div>
  );
}