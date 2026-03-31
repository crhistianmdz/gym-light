import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { Card, CardContent, Typography, Box } from '@mui/material';

const MONTHS = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

const formatMonth = (year: number, month: number) => `${MONTHS[month - 1]} ${String(year).slice(2)}`;

export interface MonthlyBreakdown {
  year: number;
  month: number;
  membership: number;
  pos: number;
  total: number;
}

interface IncomeChartProps {
  data: MonthlyBreakdown[];
}

const IncomeChart: React.FC<IncomeChartProps> = ({ data }) => {
  if (data.length === 0) {
    return (
      <Box sx={{ minHeight: 200, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <Typography>No hay datos para el período seleccionado.</Typography>
      </Box>
    );
  }

  const chartData = data.map(({ year, month, membership, pos }) => ({
    name: formatMonth(year, month),
    membership,
    pos,
  }));

  return (
    <Card>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="name" />
            <YAxis />
            <Tooltip />
            <Legend />
            <Bar dataKey="membership" fill="#1976d2" name="Membresías" />
            <Bar dataKey="pos" fill="#ff9800" name="POS" />
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
};

export default IncomeChart;