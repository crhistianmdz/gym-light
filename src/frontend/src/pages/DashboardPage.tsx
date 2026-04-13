import React, { useState, useEffect } from 'react';
import { Box, Typography, Card, CardContent, Alert, Button, Select, MenuItem, TextField } from '@mui/material';
import { useAuth } from '@/contexts/AuthContext';
import { Navigate } from 'react-router-dom';
import { dashboardService } from '@/services/dashboardService';
import type { IncomeReport, ChurnReport } from '@/services/dashboardService';
import IncomeChart from '@/components/Dashboard/IncomeChart';
import ChurnStats from '@/components/Dashboard/ChurnStats';

const DashboardPage: React.FC = () => {
  const { user } = useAuth();
  if (!user || (user.role !== 'Owner' && user.role !== 'Admin')) {
    return <Navigate to="/" replace />;
  }

  const currentYear = new Date().getFullYear();
  const [fromDate, setFromDate] = useState(`${currentYear}-01-01`);
  const [toDate, setToDate] = useState(new Date().toISOString().substring(0, 10));
  const [year, setYear] = useState(currentYear);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [incomeReport, setIncomeReport] = useState<IncomeReport | null>(null);
  const [churnReport, setChurnReport] = useState<ChurnReport | null>(null);

  const fetchIncomeReport = async () => {
    setLoading(true);
    setError(null);
    try {
      const report = await dashboardService.getIncomeReport(fromDate, toDate);
      setIncomeReport(report);
    } catch (err) {
      setError('Error al cargar el informe de ingresos.');
    } finally {
      setLoading(false);
    }
  };

  const fetchChurnReport = async () => {
    setLoading(true);
    setError(null);
    try {
      const report = await dashboardService.getChurnReport(year);
      setChurnReport(report);
    } catch (err) {
      setError('Error al cargar el informe de churn.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchIncomeReport();
    fetchChurnReport();
  }, []);

  const isOffline = incomeReport?.isOffline || churnReport?.isOffline;

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Dashboard de Métricas
      </Typography>

      {isOffline && <Alert severity="warning">Mostrando datos locales — sin conexión</Alert>}

      {error && <Alert severity="error">{error}</Alert>}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6">Ingresos</Typography>

          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 2 }}>
            <TextField
              type="date"
              label="Desde"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <TextField
              type="date"
              label="Hasta"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <Button variant="contained" onClick={fetchIncomeReport} disabled={loading}>
              Consultar
            </Button>
          </Box>

          {incomeReport && <IncomeChart data={incomeReport.byMonth} />}
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Typography variant="h6">Churn</Typography>

          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 2 }}>
            <Select
              value={year}
              onChange={(e) => setYear(e.target.value as number)}
              displayEmpty
            >
              {[...Array(5)].map((_, idx) => (
                <MenuItem key={idx} value={currentYear - idx}>
                  {currentYear - idx}
                </MenuItem>
              ))}
            </Select>
            <Button variant="contained" onClick={fetchChurnReport} disabled={loading}>
              Consultar
            </Button>
          </Box>

          {churnReport && <ChurnStats data={churnReport} />}
        </CardContent>
      </Card>
    </Box>
  );
};

export default DashboardPage;