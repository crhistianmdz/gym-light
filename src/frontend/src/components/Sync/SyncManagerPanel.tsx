import { useState, useEffect } from 'react';
import { Card, CardContent, Typography, Chip, Button, Table, TableBody, TableCell, TableHead, TableRow } from '@mui/material';
import { db, ErrorQueueItem } from '@/db/gymflow.db';
import { syncService } from '@/services/syncService';

export function SyncManagerPanel() {
  const [pendingCount, setPendingCount] = useState(0);
  const [errorCount, setErrorCount] = useState(0);
  const [errorItems, setErrorItems] = useState<ErrorQueueItem[]>([]);
  const [isSyncing, setIsSyncing] = useState(false);
  const [lastSyncAt, setLastSyncAt] = useState<Date | null>(null);

  useEffect(() => {
    const fetchCounts = async () => {
      setPendingCount(await db.sync_queue.count());
      setErrorCount(await db.error_queue.count());
      setErrorItems(await db.error_queue.toArray());
    };

    fetchCounts();

    const handleCompletion = () => {
      fetchCounts();
      setLastSyncAt(new Date());
    };

    const handleError = () => {
      fetchCounts();
    };

    window.addEventListener('sync:completed', handleCompletion);
    window.addEventListener('sync:item-failed', handleError);

    return () => {
      window.removeEventListener('sync:completed', handleCompletion);
      window.removeEventListener('sync:item-failed', handleError);
    };
  }, []);

  const handleSyncNow = async () => {
    setIsSyncing(true);
    try {
      await syncService.processQueue();
    } finally {
      setIsSyncing(false);
    }
  };

  const retryError = async (guid: string) => {
    await syncService.retryFromErrorQueue(guid);
    setErrorItems(await db.error_queue.toArray());
  };

  const discardError = async (guid: string) => {
    await syncService.discardFromErrorQueue(guid);
    setErrorItems(await db.error_queue.toArray());
  };

  return (
    <div style={{ padding: 16 }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h6">Sincronización</Typography>
        <Chip 
          label={
            errorCount > 0 
              ? `${errorCount} errores` 
              : pendingCount > 0 
              ? `${pendingCount} pendientes` 
              : 'Sincronizado'
          }
          color={errorCount > 0 ? 'error' : pendingCount > 0 ? 'warning' : 'success'}
        />
      </header>

      <div style={{ display: 'flex', gap: 16, marginTop: 24 }}>
        <Card>
          <CardContent>
            <Typography variant="h6">Pendientes</Typography>
            <Typography>{pendingCount}</Typography>
          </CardContent>
        </Card>
        <Card>
          <CardContent>
            <Typography variant="h6">Errores</Typography>
            <Typography color={errorCount > 0 ? 'error' : 'inherit'}>{errorCount}</Typography>
          </CardContent>
        </Card>
        <Card>
          <CardContent>
            <Typography variant="h6">Última sync</Typography>
            <Typography>{lastSyncAt ? lastSyncAt.toLocaleString() : 'Nunca'}</Typography>
          </CardContent>
        </Card>
      </div>

      <Button 
        variant="contained" 
        onClick={handleSyncNow} 
        disabled={isSyncing} 
        style={{ marginTop: 16 }}
      >
        {isSyncing ? 'Sincronizando...' : 'Sincronizar ahora'}
      </Button>

      {errorCount > 0 && (
        <Table style={{ marginTop: 24 }}>
          <TableHead>
            <TableRow>
              <TableCell>Tipo</TableCell>
              <TableCell>Timestamp</TableCell>
              <TableCell>Reintentos</TableCell>
              <TableCell>Último error</TableCell>
              <TableCell>Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {errorItems.map((item) => (
              <TableRow key={item.guid}>
                <TableCell>{item.type}</TableCell>
                <TableCell>{new Date(item.timestamp).toLocaleString()}</TableCell>
                <TableCell>{item.retryCount}</TableCell>
                <TableCell>{item.lastError}</TableCell>
                <TableCell>
                  <Button onClick={() => retryError(item.guid)}>Reintentar</Button>
                  <Button onClick={() => discardError(item.guid)}>Descartar</Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}