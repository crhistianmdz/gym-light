import React from 'react';
import { Box, Typography, Chip, Button } from '@mui/material';
import { QuantityEditor } from './QuantityEditor';
import type { ProductResponse } from '@/services/saleService';

export interface ProductRowProps {
  product: ProductResponse;
  onAddLine: (product: ProductResponse, quantity: number) => void;
}

export const ProductRow: React.FC<ProductRowProps> = ({ product, onAddLine }) => {
  const [quantity, setQuantity] = React.useState(1);

  const isStockLow = product.stock > 0 && product.stock <= product.initialStock * 0.2;

  const handleAddClick = () => {
    if (quantity > 0 && quantity <= product.stock) {
      onAddLine(product, quantity);
    }
  };

  return (
    <Box
      display="flex"
      alignItems="center"
      justifyContent="space-between"
      padding={1}
      borderBottom={1}
      borderColor="divider"
    >
      <Box>
        <Typography variant="body1">{product.name}</Typography>
        <Typography variant="body2" color="text.secondary">
          {product.price.toLocaleString('es-AR', {
            style: 'currency',
            currency: 'ARS',
          })}
        </Typography>

        {product.stock === 0 ? (
          <Chip label="Sin stock" color="error" />
        ) : isStockLow ? (
          <Chip label="Stock bajo" color="warning" />
        ) : null}
      </Box>

      <Box display="flex" alignItems="center">
        <QuantityEditor
          value={quantity}
          max={product.stock}
          onChange={setQuantity}
          disabled={product.stock === 0}
        />

        <Button
          variant="contained"
          color="primary"
          onClick={handleAddClick}
          disabled={product.stock === 0 || quantity > product.stock}
        >
          Agregar
        </Button>
      </Box>
    </Box>
  );
};