import React from 'react';
import { Box, Typography, Tabs, Tab } from '@mui/material';
import { SalePanel } from '@/components/SalePanel/SalePanel';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { SalesHistory } from '@/components/SalesHistory';
import { ProductsAdmin } from '@/components/ProductsAdmin';
import { useAuth } from '@/contexts/AuthContext';

export const SalesPage: React.FC = () => {
  const [tabValue, setTabValue] = React.useState(0);
  const { user } = useAuth();
  const isAdminOrOwner = ['Admin', 'Owner'].includes(user?.role ?? '');

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  return (
    <ProtectedRoute fallback={<Typography>No tenés acceso.</Typography>}>
      <Box sx={{ p: 3 }}>
        <Typography variant="h4" sx={{ mb: 2 }}>Punto de Venta</Typography>
        <Tabs value={tabValue} onChange={handleTabChange} sx={{ mb: 2 }}>
          <Tab label="Nueva Venta" />
          <Tab label="Historial" />
          {isAdminOrOwner && <Tab label="Productos" />}
        </Tabs>
        {tabValue === 0 && <SalePanel />}
        {tabValue === 1 && <SalesHistory />}
        {isAdminOrOwner && tabValue === 2 && <ProductsAdmin />}
      </Box>
    </ProtectedRoute>
  );
};
