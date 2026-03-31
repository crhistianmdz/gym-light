import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  CircularProgress,
  TablePagination,
  Chip,
  Tooltip,
  IconButton,
  Snackbar,
  Alert,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Button,
} from '@mui/material';
import { CloudOff, Delete } from '@mui/icons-material';
import { saleService } from '@/services/saleService';
import { useAuth } from '@/contexts/AuthContext';

type Sale = {
  id: string;
  timestamp: string;
  status: 'pending' | 'synced' | 'cancelled';
  total: number;
  lines: { quantity: number }[];
  isOffline?: boolean;
};

export const SalesHistory: React.FC = () => {
  const { user } = useAuth();
  const role = user?.role;

  const [sales, setSales] = useState<Sale[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [selectedSaleId, setSelectedSaleId] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null);

  useEffect(() => {
    const fetchSales = async () => {
      setLoading(true);
      setError(null);

      try {
        const result = await saleService.getSales(page + 1, pageSize);
        setSales(result.data);
        setTotalCount(result.total);
      } catch (err: unknown) {
        setError('Error al cargar historial de ventas.');
      } finally {
        setLoading(false);
      }
    };

    fetchSales();
  }, [page, pageSize]);

  const handleCancel = async () => {
    if (!selectedSaleId) return;

    try {
      await saleService.cancelSale(selectedSaleId);
      setSnackbar({ message: 'La venta fue cancelada exitosamente.', severity: 'success' });
      setPage(0); // Refrescar ventas desde la primera página
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error desconocido al cancelar.';
      setSnackbar({ message, severity: 'error' });
    } finally {
      setCancelDialogOpen(false);
      setSelectedSaleId(null);
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100%">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">{error}</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h5">Historial de Ventas</Typography>
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Fecha</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell>Total</TableCell>
              <TableCell>Nº de Líneas</TableCell>
              <TableCell>Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sales.map((sale) => (
              <TableRow key={sale.id}>
                <TableCell>{new Date(sale.timestamp).toLocaleString()}</TableCell>
                <TableCell>
                  <Chip
                    label={sale.status}
                    color={
                      sale.status === 'synced'
                        ? 'success'
                        : sale.status === 'pending'
                        ? 'warning'
                        : 'error'
                    }
                  />
                  {sale.isOffline && (
                    <Tooltip title="Pendiente de sincronización">
                      <CloudOff color="action" style={{ marginLeft: 8 }} />
                    </Tooltip>
                  )}
                </TableCell>
                <TableCell>${sale.total.toFixed(2)}</TableCell>
                <TableCell>{sale.lines.reduce((sum, line) => sum + line.quantity, 0)}</TableCell>
                <TableCell>
                  {['Admin', 'Owner'].includes(role || '') && sale.status !== 'cancelled' && (
                    <IconButton
                      color="error"
                      onClick={() => {
                        setCancelDialogOpen(true);
                        setSelectedSaleId(sale.id);
                      }}
                    >
                      <Delete />
                    </IconButton>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={totalCount}
          page={page}
          onPageChange={(_event, newPage) => setPage(newPage)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(event) => setPageSize(parseInt(event.target.value, 10))}
        />
      </TableContainer>

      <Dialog open={cancelDialogOpen} onClose={() => setCancelDialogOpen(false)}>
        <DialogTitle>Confirmar cancelación</DialogTitle>
        <DialogContent>
          <DialogContentText>
            ¿Estás seguro de que querés cancelar esta venta? Esta acción no puede deshacerse.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelDialogOpen(false)} color="primary">
            Cancelar
          </Button>
          <Button onClick={handleCancel} color="error" autoFocus>
            Confirmar
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