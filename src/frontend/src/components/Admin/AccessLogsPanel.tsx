import React, { useEffect, useState } from 'react';
import {
  Button,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Typography,
  Alert,
} from '@mui/material';
import { getAccessLogs, exportAccessLogs } from '@/services/adminService';
import type { AccessLogFilter, AccessLogDto, PagedResult } from '@/types/accessLog';

const initialFilter: AccessLogFilter = {
  fromDate: '',
  toDate: '',
  performedByUserId: '',
  memberId: '',
  result: '',
  page: 1,
  pageSize: 10,
};

export function AccessLogsPanel() {
  const [filter, setFilter] = useState(initialFilter);
  const [logs, setLogs] = useState<PagedResult<AccessLogDto> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchLogs = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getAccessLogs(filter);
      setLogs(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error fetching logs');
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async (format: 'csv' | 'pdf') => {
    if (format === 'pdf') {
      setError('Export to PDF is coming soon!');
      return;
    }

    try {
      const blob = await exportAccessLogs(filter, format);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `access_logs.${format}`;
      link.click();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error exporting logs');
    }
  };

  useEffect(() => {
    fetchLogs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter]);

  return (
    <div style={{ padding: 24 }}>
      <Typography variant="h6">Access Logs</Typography>

      {error && (
        <Alert severity="error" style={{ marginBottom: 16 }}>
          {error}
        </Alert>
      )}

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 16, marginBottom: 16 }}>
        <TextField
          label="From Date"
          type="date"
          value={filter.fromDate}
          onChange={(e) => setFilter({ ...filter, fromDate: e.target.value })}
          InputLabelProps={{ shrink: true }}
        />

        <TextField
          label="To Date"
          type="date"
          value={filter.toDate}
          onChange={(e) => setFilter({ ...filter, toDate: e.target.value })}
          InputLabelProps={{ shrink: true }}
        />

        <TextField
          label="Performed By (User ID)"
          value={filter.performedByUserId}
          onChange={(e) => setFilter({ ...filter, performedByUserId: e.target.value })}
        />

        <TextField
          label="Member (ID)"
          value={filter.memberId}
          onChange={(e) => setFilter({ ...filter, memberId: e.target.value })}
        />

        <FormControl>
          <InputLabel id="result-label">Result</InputLabel>
          <Select
            labelId="result-label"
            value={filter.result}
            onChange={(e) => setFilter({ ...filter, result: e.target.value as 'Allowed' | 'Denied' | '' })}
          >
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Allowed">Allowed</MenuItem>
            <MenuItem value="Denied">Denied</MenuItem>
          </Select>
        </FormControl>
      </div>

      <div style={{ display: 'flex', gap: 16, marginBottom: 16 }}>
        <Button
          onClick={fetchLogs}
          variant="contained"
          disabled={loading}
        >
          Search
        </Button>
        <Button
          onClick={() => setFilter(initialFilter)}
          variant="outlined"
          disabled={loading}
        >
          Clear
        </Button>
        <Button
          onClick={() => handleExport('csv')}
          variant="contained"
          disabled={loading}
        >
          Export CSV
        </Button>
        <Button
          onClick={() => handleExport('pdf')}
          variant="contained"
          disabled={loading}
        >
          Export PDF
        </Button>
      </div>

      {loading ? (
        <CircularProgress />
      ) : (
        logs && logs.items.length > 0 ? (
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Date/Time</TableCell>
                <TableCell>Member Name</TableCell>
                <TableCell>Performed By</TableCell>
                <TableCell>Result</TableCell>
                <TableCell>Reason</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.items.map((log) => (
                <TableRow key={log.id}>
                  <TableCell>{new Date(log.createdAt).toLocaleString()}</TableCell>
                  <TableCell>{log.memberName}</TableCell>
                  <TableCell>{log.performedByUserName}</TableCell>
                  <TableCell>
                    <span style={{ color: log.result === 'Allowed' ? 'green' : 'red' }}>
                      {log.result}
                    </span>
                  </TableCell>
                  <TableCell>{log.denialReason || 'N/A'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <Typography>No records found.</Typography>
        )
      )}
    </div>
  );
}