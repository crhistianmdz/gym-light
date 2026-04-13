import React, { useEffect, useState } from 'react';
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Button,
  Snackbar,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  TextField,

  Alert,
} from '@mui/material';
import { Edit, Delete, Add } from '@mui/icons-material';
import { productService } from '@/services/productService';
import { useAuth } from '@/contexts/AuthContext';

interface Product {
  id: string;
  name: string;
  sku?: string;
  price: number;
  stock: number;
  initialStock: number;
}

type FormState = {
  name: string;
  sku?: string;
  description?: string;
  price: number;
  initialStock: number;
};

export const ProductsAdmin: React.FC = () => {
  const { user } = useAuth();
  const role = user?.role;

  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null);
  const [form, setForm] = useState<FormState>({
    name: '',
    price: 0,
    initialStock: 0,
  });

  useEffect(() => {
    const fetchProducts = async () => {
      setLoading(true);
      setError(null);

      try {
        const products = await productService.getProducts();
        setProducts(products);
      } catch (err: unknown) {
        setError('Error al cargar productos.');
      } finally {
        setLoading(false);
      }
    };

    fetchProducts();
  }, []);

  const handleSave = async () => {
    try {
      if (editingProduct) {
        await productService.updateProduct(editingProduct.id, form);
        setSnackbar({ message: 'Producto actualizado con éxito.', severity: 'success' });
      } else {
        await productService.createProduct(form);
        setSnackbar({ message: 'Producto creado con éxito.', severity: 'success' });
      }
      setDialogOpen(false);
      setEditingProduct(null);
      setForm({ name: '', price: 0, initialStock: 0 });
    } catch (err) {
      setSnackbar({ message: 'Error al guardar el producto.', severity: 'error' });
    }
  };

  const handleDelete = async () => {
    if (!selectedProductId) return;

    try {
      await productService.deleteProduct(selectedProductId);
      setSnackbar({ message: 'Producto eliminado con éxito.', severity: 'success' });
    } catch (err) {
      setSnackbar({ message: 'Error al eliminar el producto.', severity: 'error' });
    } finally {
      setDeleteDialogOpen(false);
      setSelectedProductId(null);
    }
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" marginBottom={2}>
        <Button
          variant="contained"
          color="primary"
          startIcon={<Add />}
          onClick={() => setDialogOpen(true)}
          disabled={!['Admin', 'Owner'].includes(role || '')}
        >
          Agregar Producto
        </Button>
      </Box>

      {loading && <p>Cargando...</p>}
      {error && <p>{error}</p>}

      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Nombre</TableCell>
              <TableCell>SKU</TableCell>
              <TableCell>Precio</TableCell>
              <TableCell>Stock</TableCell>
              <TableCell>Stock Inicial</TableCell>
              <TableCell>% Stock</TableCell>
              <TableCell>Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {products.map((product) => (
              <TableRow key={product.id}>
                <TableCell>{product.name}</TableCell>
                <TableCell>{product.sku || '-'}</TableCell>
                <TableCell>${product.price.toFixed(2)}</TableCell>
                <TableCell>{product.stock}</TableCell>
                <TableCell>{product.initialStock}</TableCell>
                <TableCell>
                  {Math.round((product.stock / product.initialStock) * 100)}%
                </TableCell>
                <TableCell>
                  {['Admin', 'Owner'].includes(role || '') && (
                    <>
                      <IconButton
                        color="primary"
                        onClick={() => {
                          setEditingProduct(product);
                          setForm({
                            name: product.name,
                            sku: product.sku,
                            price: product.price,
                            initialStock: product.initialStock,
                          });
                          setDialogOpen(true);
                        }}
                      >
                        <Edit />
                      </IconButton>
                      <IconButton
                        color="error"
                        onClick={() => {
                          setDeleteDialogOpen(true);
                          setSelectedProductId(product.id);
                        }}
                      >
                        <Delete />
                      </IconButton>
                    </>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>{editingProduct ? 'Editar Producto' : 'Agregar Producto'}</DialogTitle>
        <DialogContent>
          <TextField
            label="Nombre"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            fullWidth
            required
          />
          <TextField
            label="SKU"
            value={form.sku}
            onChange={(e) => setForm({ ...form, sku: e.target.value })}
            fullWidth
          />
          <TextField
            label="Descripción"
            value={form.description || ''}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            fullWidth
          />
          <TextField
            label="Precio"
            type="number"
            value={form.price}
            onChange={(e) => setForm({ ...form, price: parseFloat(e.target.value) })}
            fullWidth
            required
          />
          <TextField
            label="Stock Inicial"
            type="number"
            value={form.initialStock}
            onChange={(e) => setForm({ ...form, initialStock: parseInt(e.target.value, 10) })}
            fullWidth
            required
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)} color="primary">
            Cancelar
          </Button>
          <Button onClick={handleSave} color="primary">
            Guardar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Confirmar eliminación</DialogTitle>
        <DialogContent>
          <DialogContentText>
            ¿Estás seguro de que quieres eliminar este producto?
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)} color="primary">
            Cancelar
          </Button>
          <Button onClick={handleDelete} color="error">
            Eliminar
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={snackbar !== null}
        autoHideDuration={6000}
        onClose={() => setSnackbar(null)}
        message={snackbar?.message}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert severity={snackbar?.severity}>{snackbar?.message}</Alert>
      </Snackbar>
    </Box>
  );
};