import React from 'react';
import { Box, Grid, Card, CardContent, Typography, Alert } from '@mui/material';

export interface ChurnReport {
  year: number;
  totalMembers: number;
  activeMembers: number;
  notRenewed: number;
  churnRate: number;
  isOffline?: boolean;
}

interface ChurnStatsProps {
  data: ChurnReport;
}

const ChurnStats: React.FC<ChurnStatsProps> = ({ data }) => {
  if (data.totalMembers === 0) {
    return <Alert severity="info">Sin datos suficientes para calcular el churn.</Alert>;
  }

  const { totalMembers, activeMembers, notRenewed, churnRate } = data;
  const churnColor =
    churnRate > 20
      ? 'error.main'
      : churnRate > 10
      ? 'warning.main'
      : 'success.main';

  return (
    <Box>
      <Grid container spacing={2}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6">Total Socios</Typography>
              <Typography variant="h4" color="text.primary">
                {totalMembers}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6">Activos</Typography>
              <Typography variant="h4" color="success.main">
                {activeMembers}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6">No Renovaron</Typography>
              <Typography variant="h4" color="warning.main">
                {notRenewed}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6">Churn Rate</Typography>
              <Typography variant="h4" color={churnColor}>
                {churnRate.toFixed(1)}%
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ChurnStats;