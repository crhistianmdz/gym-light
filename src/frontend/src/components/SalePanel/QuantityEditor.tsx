import React from 'react';
import { Box, IconButton, TextField } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import RemoveIcon from '@mui/icons-material/Remove';

export interface QuantityEditorProps {
  value: number;
  min?: number;
  max?: number;
  onChange: (value: number) => void;
  disabled?: boolean;
}

export const QuantityEditor: React.FC<QuantityEditorProps> = ({
  value,
  min = 1,
  max = Infinity,
  onChange,
  disabled = false,
}) => {
  const handleDecrease = () => {
    if (value > min) {
      onChange(value - 1);
    }
  };

  const handleIncrease = () => {
    if (value < max) {
      onChange(value + 1);
    }
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = parseInt(event.target.value, 10);
    if (!isNaN(newValue) && newValue >= min && newValue <= max) {
      onChange(newValue);
    }
  };

  return (
    <Box display="flex" alignItems="center">
      <IconButton onClick={handleDecrease} disabled={value <= min || disabled}>
        <RemoveIcon />
      </IconButton>

      <TextField
        type="number"
        value={value}
        onChange={handleInputChange}
        disabled={disabled}
        inputProps={{
          min,
          max,
          style: { textAlign: 'center' },
        }}
        style={{ width: 50 }}
      />

      <IconButton onClick={handleIncrease} disabled={value >= max || disabled}>
        <AddIcon />
      </IconButton>
    </Box>
  );
};